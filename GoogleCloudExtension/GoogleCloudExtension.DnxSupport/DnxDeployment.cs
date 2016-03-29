using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DnxSupport
{
    public static class DnxDeployment
    {
        // The Dockefile to use for the specified runtime.
        // {0}, the version of the runtime.
        private const string DockerfileTemplate =
            "FROM b.gcr.io/aspnet-docker/dotnet:{0}\n" +
            "ADD ./ /app\n" +
            "RUN chmod +x /app/app_engine_start\n";

        // The app.yaml to use for all apps. The skip_files entry makes it so no local files are sent
        // to the server, no files need to be sent because we're deploying a Docker image but gcloud
        // will send the entire app to the server, which can take a long while.
        private const string AppYamlContent =
            "runtime: custom\n" +
            "vm: true\n" +
            "api_version: 1\n" +
            "skip_files: \n" +
            "- ^.*$";

        private const string StartupScriptFileName = "app_engine_start";
        private const string AppYamlFileName = "app.yaml";
        private const string DockerfileFilename = "Dockerfile";

        // The template to generate an entry point that will launch Kestrel in the port 8080 on
        // the given project name (the {0} parameter). This project is the startup project.
        // This entry point works with the standard base images (for Mono and CoreCLR) that will setup
        // the environment correctly.
        private const string EntryPointTemplate =
            "#! /usr/bin/env bash\n" +
            "source /root/.dnx/dnvm/dnvm.sh\n" +
            "dnvm use default\n" +
            "cd /app/approot/src/{0}\n" +
            "dnx Microsoft.Dnx.ApplicationHost " +  // This is a single line, hence no \n at the end.
                "Microsoft.AspNet.Hosting " +
                "--server Microsoft.AspNet.Server.Kestrel " +
                "--server.urls http://0.0.0.0:8080\n";

        /// <summary>
        /// Deployes an Asp.NET application to AppEngine using a managed vm.
        /// </summary>
        /// <param name="startupProjectPath">The path to the startup project.</param>
        /// <param name="projectPaths">The paths to all of the projects in the solution.</param>
        /// <param name="versionName">The name, if any, of the version to deploy, it empty or null then a default name will be chosen.</param>
        /// <param name="promoteVersion">Is this version to receive all traffic.</param>
        /// <param name="preserveOutput">Whether to preserve the result of the publish directory.</param>
        /// <param name="callback">The delegate that will be called with the output from the tools used during the deployment.</param>
        /// <param name="accountAndProject">The credentials to use.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task DeployApplicationAsync(
            string startupProjectPath,
            IList<string> projectPaths,
            string versionName,
            bool promoteVersion,
            bool preserveOutput,
            Action<string> callback,
            Credentials accountAndProject)
        {
            var appTempPath = GetAppStagingDirectory();
            try
            {
                await RestoreProjectsAsync(projectPaths, callback);
                await PrepareAppBundleAsync(startupProjectPath, appTempPath, callback);
                PrepareEntryPoint(startupProjectPath, appTempPath, callback);
                CopyAppEngineFiles(startupProjectPath, appTempPath, callback);
                await DeployToAppEngineAsync(appTempPath, versionName, promoteVersion, callback, accountAndProject);
            }
            catch (Exception ex)
            {
                callback($"Failed to deploy: {ex.Message}");
                throw;
            }
            finally
            {
                if (preserveOutput)
                {
                    callback($"Preserving the published app at {appTempPath}");
                }
                else
                {
                    callback("Performing cleanup.");
                    Directory.Delete(appTempPath, true);
                }
            }
        }



        private static void CopyAppEngineFiles(string projectPath, string appTempPath, Action<string> callback)
        {
            var dockerfileSrc = new FileInfo(Path.Combine(projectPath, DockerfileFilename));
            var dockerfileDest = Path.Combine(appTempPath, DockerfileFilename);
            if (dockerfileSrc.Exists)
            {
                // Copy the source file.
                callback($"Copying file [{dockerfileSrc}] to [{dockerfileDest}].");
                dockerfileSrc.CopyTo(dockerfileDest);
            }
            else
            {
                // Copy the template file.
                var dockerFileContent = String.Format(DockerfileTemplate, DnxEnvironment.DnxVersion);
                callback($"Writing file [{dockerfileDest}].");
                File.WriteAllText(dockerfileDest, dockerFileContent);
            }

            var appYamlSrc = new FileInfo(Path.Combine(projectPath, AppYamlFileName));
            var appYamlDest = Path.Combine(appTempPath, AppYamlFileName);
            if (appYamlSrc.Exists)
            {
                // Copy the source file.
                callback($"Copying file [{appYamlSrc}] to [{appYamlDest}].");
                appYamlSrc.CopyTo(appYamlDest);
            }
            else
            {
                // Copy the template file.
                callback($"Writing file [{appYamlDest}].");
                File.WriteAllText(appYamlDest, AppYamlContent);
            }
        }

        private static void PrepareEntryPoint(string startupProjectPath, string appTempPath, Action<string> callback)
        {
            var entryPointPath = Path.Combine(appTempPath, StartupScriptFileName);
            if (File.Exists(entryPointPath))
            {
                callback($"Entry point already found at {entryPointPath}, skipping creation.");
                return;
            }

            callback($"Creating entry point at {entryPointPath}");
            var projectName = Path.GetFileNameWithoutExtension(startupProjectPath);
            var entryPointContent = String.Format(EntryPointTemplate, projectName);

            File.WriteAllText(entryPointPath, entryPointContent);
        }

        private static async Task RestoreProjectsAsync(
           IList<string> projectPaths,
           Action<string> callback)
        {
            callback("Restoring projects.");
            await Task.WhenAll(projectPaths.Select(x => RestoreProject(x, callback)));
            callback("Done restoring projects.");
        }

        private static Dictionary<string, string> GetDnxEnvironment()
        {
            var result = new Dictionary<string, string>();
            var webTools = DnxEnvironment.GetWebToolsPath();
            var dnxPath = DnxEnvironment.GetDnxPath();
            var newPath = Environment.ExpandEnvironmentVariables($"{webTools};{dnxPath};%PATH%");
            result["PATH"] = newPath;
            return result;
        }

        private static async Task RestoreProject(
            string projectPath,
            Action<string> callback)
        {
            var environment = GetDnxEnvironment();
            var command = $"/c dnu restore \"{projectPath}\"";
            callback($"Restoring project: {projectPath}");
            // This has hard dependency on dnu being a batch file.
            var result = await ProcessUtils.RunCommandAsync("cmd.exe", command, (s, e) => callback(e.Line), environment);
            if (!result)
            {
                throw new GCloudException($"Failed to restore project: {projectPath}");
            }
        }

        private static async Task PrepareAppBundleAsync(
            string projectPath,
            string appTempPath,
            Action<string> callback)
        {
            // Customize the environment by adding the path to the node_modules directory, which can be necessary for
            // the publish process.
            var environment = GetDnxEnvironment();
            var nodeModulesPath = Path.Combine(projectPath, "node_modules", ".bin");
            var newPath = $"{nodeModulesPath};{environment["PATH"]}";
            environment["PATH"] = newPath;

            // This is a dependency on the fact that DNU is a batch file, but it has to be launched this way.
            callback($"Preparing app bundle in {appTempPath}.");
            var frameworkName = DnxRuntimeInfo.DnxCore50FrameworkName;
            string command = $"/c dnu publish \"{projectPath}\" --out \"{appTempPath}\" --framework {frameworkName} --configuration release";
            callback($"Executing command: {command}");
            var result = await ProcessUtils.RunCommandAsync("cmd.exe", command, (s, e) => callback(e.Line), environment);
            if (!result)
            {
                throw new GCloudException($"Failed to prepare bundle for project: {projectPath}");
            }
        }

        private static Task DeployToAppEngineAsync(
            string appTempPath,
            string versionName,
            bool makeDefaultVersion,
            Action<string> callback,
            Credentials accountAndProject)
        {
            var makeDefault = makeDefaultVersion ? "--promote" : "--no-promote";
            var name = String.IsNullOrEmpty(versionName) ? "" : $"--version={versionName}";
            var appYaml = Path.Combine(appTempPath, AppYamlFileName);
            string command = $"preview app deploy \"{appYaml}\" {makeDefault} {name} --docker-build=remote --verbosity=info --quiet";
            callback($"Executing command: {command}");
            return GCloudWrapper.Instance.RunCommandAsync(command, callback, accountAndProject);
        }

        private static string GetAppStagingDirectory()
        {
            var result = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(result);
            return result;
        }
    }
}
