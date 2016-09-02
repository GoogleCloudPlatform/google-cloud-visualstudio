using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    public static class NetCoreDeployment
    {
        private const string AppYamlName = "app.yaml";

        private const string AppYamlDefaultContent =
            "runtime: custom\n" +
            "vm: true\n";

        private const string DockerfileName = "Dockerfile";

        private const string DockerfileDefaultContent =
            "FROM microsoft/dotnet:1.0.0-core\n" +
            "COPY . /app\n" +
            "WORKDIR /app\n" +
            "EXPOSE 8080\n" +
            "ENV ASPNETCORE_URLS=http://*:8080\n" +
            "ENTRYPOINT [\"dotnet\", \"{0}.dll\"]\n";

        private static readonly Lazy<string> s_dotnetPath = new Lazy<string>(GetDotnetPath);

        public class DeploymentOptions
        {
            public string Version { get; set; }

            public bool Promote { get; set; }
        }

        public static async Task<bool> PublishProjectAsync(
            string projectPath,
            DeploymentOptions options,
            Action<string> outputAction)
        {
            if (!File.Exists(projectPath))
            {
                Debug.WriteLine($"Cannot find {projectPath}, not a valid project.");
                return false;
            }

            var stageDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(stageDirectory);

            if (!await CreateAppBundleAsync(projectPath, stageDirectory, outputAction))
            {
                return false;
            }

            CopyOrCreateDockerfile(projectPath, stageDirectory);

            CopyOrCreateAppYaml(projectPath, stageDirectory);

            var result = await DeployAppBundleAsync(stageDirectory, options, outputAction);

            // TODO: Cleanup the staging directory.

            return result;
        }

        private static Task<bool> CreateAppBundleAsync(string projectPath, string stageDirectory, Action<string> outputAction)
        {
            var arguments = $"publish \"{projectPath}\" " +
                $"-o \"{stageDirectory}\" " +
                "-c Release";

            outputAction($"dotnet {arguments}");
            return ProcessUtils.RunCommandAsync(s_dotnetPath.Value, arguments, (o, e) => outputAction(e.Line));
        }

        private static void CopyOrCreateAppYaml(string projectPath, string stageDirectory)
        {
            var sourceDir = Path.GetDirectoryName(projectPath);
            var sourceAppYaml = Path.Combine(sourceDir, AppYamlName);
            var targetAppYaml = Path.Combine(stageDirectory, AppYamlName);

            if (File.Exists(sourceAppYaml))
            {
                File.Copy(sourceAppYaml, targetAppYaml, overwrite: true);
            }
            else
            {
                File.WriteAllText(targetAppYaml, AppYamlDefaultContent);
            }
        }

        private static void CopyOrCreateDockerfile(string projectPath, string stageDirectory)
        {
            var sourceDir = Path.GetDirectoryName(projectPath);
            var sourceDockerfile = Path.Combine(sourceDir, DockerfileName);
            var targetDockerfile = Path.Combine(stageDirectory, DockerfileName);

            if (File.Exists(sourceDockerfile))
            {
                File.Copy(sourceDockerfile, targetDockerfile, overwrite: true);
            }
            else
            {
                var content = String.Format(DockerfileDefaultContent, GetProjectName(projectPath));
                File.WriteAllText(targetDockerfile, content);
            }
        }

        private static Task<bool> DeployAppBundleAsync(string stageDirectory, DeploymentOptions options, Action<string> outputAction)
        {
            var version = String.IsNullOrEmpty(options.Version) ? "" : $"--version={options.Version}";
            var promote = options.Promote ? "--promote" : "--no-promote";
            var appYamlPath = Path.Combine(stageDirectory, AppYamlName);
            var command = $"app deploy \"{appYamlPath}\" {version} {promote} --verbosity=info --quiet";
            var arguments = $"/c gcloud.cmd {command}";

            outputAction($"gcloud {command}");
            return ProcessUtils.RunCommandAsync("cmd.exe", arguments, (o, e) => outputAction(e.Line));
        }

        private static string GetDotnetPath()
        {
            var programFilesPath = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            return Path.Combine(programFilesPath, @"dotnet\dotnet.exe");
        }

        private static string GetProjectName(string projectPath)
        {
            var directory = Path.GetDirectoryName(projectPath);
            return Path.GetFileName(directory);
        }
    }
}
