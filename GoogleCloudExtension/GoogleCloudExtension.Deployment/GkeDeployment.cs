using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    public static class GkeDeployment
    {
        public class DeploymentOptions
        {
            public string Cluster { get; set; }

            public string Zone { get; set; }

            public string DeploymentName { get; set; }

            public string DeploymentVersion { get; set; }

            public bool ExposeService { get; set; }

            public GCloudContext Context { get; set; }

            public Action WaitingForServiceIpCallback { get; set; }
        }

        public static async Task<GkeDeploymentResult> PublishProjectAsync(
            string projectPath,
            DeploymentOptions options,
            IProgress<double> progress,
            Action<string> outputAction)
        {
            if (!File.Exists(projectPath))
            {
                Debug.WriteLine($"Cannot find {projectPath}, not a valid project.");
                return null;
            }

            var stageDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(stageDirectory);
            progress.Report(0.1);

            using (var cleanup = new Disposable(() => Cleanup(stageDirectory)))
            {
                var appRootPath = Path.Combine(stageDirectory, "app");
                var buildFilePath = Path.Combine(stageDirectory, "cloudbuild.yaml");
                var kubeconfigPath = Path.Combine(stageDirectory, "kube.config");
                var projectName = CommonUtils.GetProjectName(projectPath);
                var kubectlContext = new KubectlContext
                {
                    Config = kubeconfigPath
                };

                if (!await ProgressHelper.UpdateProgress(
                        NetCoreAppUtils.CreateAppBundleAsync(projectPath, appRootPath, outputAction),
                        progress,
                        from: 0.1, to: 0.3))
                {
                    Debug.WriteLine("Failed to create app bundle.");
                    return null;
                }

                NetCoreAppUtils.CopyOrCreateDockerfile(projectPath, appRootPath);
                var image = CloudBuilderUtils.CreateBuildFile(
                    project: options.Context.ProjectId,
                    imageName: options.DeploymentName,
                    imageVersion: options.DeploymentVersion,
                    buildFilePath: buildFilePath);

                if (!await ProgressHelper.UpdateProgress(
                    GCloudWrapper.BuildContainerAsync(buildFilePath, appRootPath, outputAction, options.Context),
                    progress,
                    from: 0.4, to: 0.7))
                {
                    Debug.WriteLine("Failed to build container.");
                    return null;
                }

                if (!await GCloudWrapper.CreateCredentialsForClusterAsync(
                    cluster: options.Cluster,
                    zone: options.Zone,
                    path: kubeconfigPath,
                    context: options.Context))
                {
                    Debug.WriteLine("Failed to create kubectl config file.");
                    return null;
                }
                progress.Report(0.7);

                string ipAddress = null;
                bool deploymentUpdated = false;
                bool serviceExposed = false;

                // Create or update the deployment.
                var deployments = await KubectlWrapper.GetDeploymentsAsync(kubectlContext);
                var deployment = deployments?.FirstOrDefault(x => x.Metadata.Name == options.DeploymentName);
                if (deployment == null)
                {
                    Debug.WriteLine($"Creating new deployment {options.DeploymentName}");
                    if (!await KubectlWrapper.CreateDeploymentAsync(
                            name: options.DeploymentName,
                            image: image,
                            outputAction: outputAction,
                            context: kubectlContext))
                    {
                        Debug.WriteLine($"Failed to create deployment {options.DeploymentName}");
                        return null;
                    }
                    progress.Report(0.8);
                }
                else
                {
                    Debug.WriteLine($"Updating existing deployment {options.DeploymentName}");
                    if (!await KubectlWrapper.UpdateDeploymentImageAsync(
                            options.DeploymentName,
                            image,
                            outputAction,
                            kubectlContext))
                    {
                        Debug.WriteLine($"Failed to update deployemnt {options.DeploymentName}");
                        return null;
                    }
                    deploymentUpdated = true;
                }

                // Expose the service if requested and it is not already exposed.
                if (options.ExposeService)
                {
                    var services = await KubectlWrapper.GetServicesAsync(kubectlContext);
                    var service = services?.FirstOrDefault(x => x.Metadata.Name == options.DeploymentName);
                    if (service == null)
                    {
                        if (!await KubectlWrapper.ExposeServiceAsync(options.DeploymentName, outputAction, kubectlContext))
                        {
                            Debug.WriteLine($"Failed to expose service {options.DeploymentName}");
                            return null;
                        }
                    }

                    ipAddress = await WaitForServiceAddressAsync(
                        options.DeploymentName,
                        options.WaitingForServiceIpCallback,
                        kubectlContext);

                    serviceExposed = true;
                }


                return new GkeDeploymentResult(
                    serviceIpAddress: ipAddress,
                    wasExposed: serviceExposed,
                    deploymentUpdated: deploymentUpdated);
            }
        }

        private static async Task<string> WaitForServiceAddressAsync(string name, Action waitingCallback, KubectlContext kubectlContext)
        {
            DateTime start = DateTime.Now;
            TimeSpan actualTime = DateTime.Now - start;
            while (actualTime.TotalMinutes < 5)
            {
                waitingCallback();
                var service = await KubectlWrapper.GetServiceAsync(name, kubectlContext);
                if (service?.Status?.LoadBalancer.Ingress != null)
                {
                    var ingress = service?.Status?.LoadBalancer.Ingress.FirstOrDefault();
                    if (ingress != null)
                    {
                        string ipAddress = null;
                        if (ingress.TryGetValue("ip", out ipAddress))
                        {
                            Debug.WriteLine($"Found service IP address: {ipAddress}");
                            return ipAddress;
                        }
                    }
                }
                Debug.WriteLine("Waiting for service to be public.");
                await Task.Delay(2000);
                actualTime = DateTime.Now - start;
            }

            Debug.WriteLine("Timeout while waiting for the ip address.");
            return null;
        }

        private static void Cleanup(string stageDirectory)
        {

        }
    }
}
