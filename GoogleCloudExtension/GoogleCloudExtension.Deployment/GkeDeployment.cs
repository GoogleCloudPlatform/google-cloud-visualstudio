using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

                string ipAddress = null;
                if (options.ExposeService)
                {
                    if (!await KubectlWrapper.ExposeServiceAsync(options.DeploymentName, outputAction, kubectlContext))
                    {
                        Debug.WriteLine($"Failed to expose service {options.DeploymentName}");
                        return null;
                    }

                    ipAddress = await WaitForServiceAddressAsync(options.DeploymentName, kubectlContext);
                }

                return new GkeDeploymentResult(ipAddress, options.ExposeService);
            }
        }

        private static async Task<string> WaitForServiceAddressAsync(string name, KubectlContext kubectlContext)
        {
            DateTime start = DateTime.Now;
            TimeSpan actualTime = DateTime.Now - start;
            while (actualTime.TotalMinutes < 5)
            {
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
