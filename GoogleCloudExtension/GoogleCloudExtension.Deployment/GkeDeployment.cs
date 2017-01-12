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

        public static async Task<bool> PublishProjectAsync(
            string projectPath,
            DeploymentOptions options,
            IProgress<double> progress,
            Action<string> outputAction)
        {
            if (!File.Exists(projectPath))
            {
                Debug.WriteLine($"Cannot find {projectPath}, not a valid project.");
                return false;
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
                        NetCoreAppUtils.CreateAppBundleAsync(projectPath, stageDirectory, outputAction),
                        progress,
                        from: 0.1, to: 0.3))
                {
                    Debug.WriteLine("Failed to create app bundle.");
                    return false;
                }

                NetCoreAppUtils.CopyOrCreateDockerfile(projectPath, stageDirectory);
                var image = CloudBuilderUtils.CreateBuildFile(
                    project: projectName,
                    imageName: options.DeploymentName,
                    imageVersion: options.DeploymentVersion,
                    buildFilePath: buildFilePath);
                progress.Report(0.4);

                if (!await GCloudWrapper.CreateCredentialsForClusterAsync(
                    cluster: options.Cluster,
                    zone: options.Zone,
                    path: kubeconfigPath,
                    context: options.Context))
                {
                    Debug.WriteLine("Failed to create kubectl config file.");
                    return false;
                }
                progress.Report(0.6);

                if (!await KubectlWrapper.CreateDeploymentAsync(
                        name: options.DeploymentName,
                        image: image,
                        outputAction: outputAction,
                        context: kubectlContext))
                {
                    Debug.WriteLine($"Failed to create deployment {options.DeploymentName}");
                    return false;
                }
                progress.Report(0.8);

                if (options.ExposeService)
                {
                    if (!await KubectlWrapper.ExposeServiceAsync(options.DeploymentName, outputAction, kubectlContext))
                    {
                        Debug.WriteLine($"Failed to expose service {options.DeploymentName}");
                        return false;
                    }
                }

                // All done.
                return true;
            }

            return false;
        }

        private static void Cleanup(string stageDirectory)
        {
            throw new NotImplementedException();
        }
    }
}
