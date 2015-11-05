using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    public static class AppEngineClient
    {
        // This prefix allows us to detect the builtin services so we can avoid showing
        // them as user's versions.
        private const string BuiltinServiceVersionPrefix = "ah-";

        // The Dockefile to use for the Mono runtime.
        private const string DockerfileTemplate =
            "FROM gcr.io/tryinggce/aspnet_runtime:{0}-{1}\n" +
            "ADD ./ /app\n" +
            "RUN chmod +x /app/gae_start\n";

        // The app.yaml to use for all apps.
        private const string AppYamlContent =
            "vm: true\n" +
            "threadsafe: true\n" +
            "api_version: 1\n";

        private const string AppYamlFilename = "app.yaml";
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
        /// Returns the list of AppEngine apps for this class' notion of current
        /// user and project.
        /// </summary>
        /// <returns>The list of AppEngine apps.</returns>
        public static async Task<IList<AppEngineApp>> GetAppEngineAppListAsync()
        {
            var result = await GCloudWrapper.Instance.GetJsonOutputAsync<IList<AppEngineApp>>("preview app modules list")
                ?? Enumerable.Empty<AppEngineApp>();
            return result.Where(x => !x.Version.StartsWith(BuiltinServiceVersionPrefix)).ToList();
        }

        /// <summary>
        /// Sets the given version as the default version for the given module.
        /// </summary>
        /// <param name="module">The module to change.</param>
        /// <param name="version">The version to be made default.</param>
        /// <returns>The task.</returns>
        public static async Task SetDefaultAppVersionAsync(string module, string version)
        {
            await GCloudWrapper.Instance.GetCommandOutputAsync($"preview app modules set-default {module} --version={version} --quiet");
        }

        /// <summary>
        /// Deletes the given app version from the module.
        /// </summary>
        /// <param name="module">The module that owns the version to remove.</param>
        /// <param name="version">The version to remove.</param>
        /// <returns>Taks that will be fullfilled when done.</returns>
        public static async Task DeleteAppVersion(string module, string version)
        {
            await GCloudWrapper.Instance.GetCommandOutputAsync($"preview app modules delete {module} --version={version} --quiet");
        }

        /// <summary>
        /// Deployes an Asp.NET application to AppEngine using a managed vm.
        /// </summary>
        /// <param name="startupProjectPath">The path to the startup project.</param>
        /// <param name="projectPaths">The paths to all of the projects in the solution.</param>
        /// <param name="versionName">The name, if any, of the version to deploy, it empty or null then a default name will be chosen.</param>
        /// <param name="runtime">The target runtime, Mono, CoreClr, etc...</param>
        /// <param name="promoteVersion">Is this version to receive all traffic.</param>
        /// <param name="callback">The delegate that will be called with the output from the process.</param>
        /// <returns>The task.</returns>
        public static async Task DeployApplication(
            string startupProjectPath,
            IList<string> projectPaths,
            string versionName,
            AspNetRuntime runtime,
            bool promoteVersion,
            Action<string> callback,
            AccountAndProjectId accountAndProject)
        {
            var appTempPath = GetAppStagingDirectory();
            try
            {
                await RestoreProjects(projectPaths, runtime, callback);
                await PrepareAppBundle(startupProjectPath, appTempPath, runtime, callback);
                PrepareEntryPoint(startupProjectPath, appTempPath, callback);
                CopyAppEngineFiles(startupProjectPath, appTempPath, runtime, callback);
                await DeployToAppEngine(appTempPath, versionName, promoteVersion, callback, accountAndProject);
            }
            catch (Exception ex)
            {
                callback($"Failed to deploy: {ex.Message}");
                throw;
            }
            finally
            {
                callback($"Performing cleanup.");
                Directory.Delete(appTempPath, true);
            }
        }

        private static void CopyAppEngineFiles(string projectPath, string appTempPath, AspNetRuntime runtime, Action<string> callback)
        {
            Debug.Assert(runtime == AspNetRuntime.Mono || runtime == AspNetRuntime.CoreClr);

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
                var runtimeName = DnxEnvironment.GetImageNameFromRuntime(runtime);
                var dockerFileContent = String.Format(DockerfileTemplate, DnxEnvironment.DnxVersion, runtimeName);
                callback($"Writing file [{dockerfileDest}] for runtime {runtimeName}.");
                File.WriteAllText(dockerfileDest, dockerFileContent);
            }

            var appYamlSrc = new FileInfo(Path.Combine(projectPath, AppYamlFilename));
            var appYamlDest = Path.Combine(appTempPath, AppYamlFilename);
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
            var entryPointPath = Path.Combine(appTempPath, "gae_start");
            if (File.Exists(entryPointPath))
            {
                callback($"Entry point already found at {entryPointPath}, skipping creation.");
                return;
            }

            callback($"Creating entry point at {entryPointPath}");
            var projectName = Path.GetFileNameWithoutExtension(startupProjectPath);
            var entryPointContent = String.Format(EntryPointTemplate, projectName);

            // Because the file is going to be executed inside of the Docker container it needs to have
            // Unix line termination, not Windows. Therefore we save the string as is without going through
            // the text conversion layer, the \n will remain as is and not converted to \r\n.
            File.WriteAllBytes(entryPointPath, Encoding.UTF8.GetBytes(entryPointContent));
        }

        private static async Task RestoreProjects(
           IList<string> projectPaths,
           AspNetRuntime runtime,
           Action<string> callback)
        {
            callback("Restoring projects.");
            await Task.WhenAll(projectPaths.Select(x => RestoreProject(x, runtime, callback)));
            callback("Done restoring projects.");
        }

        private static Dictionary<string, string> GetDnxEnvironmentForRuntime(AspNetRuntime runtime)
        {
            var result = new Dictionary<string, string>();
            var webTools = DnxEnvironment.GetWebToolsPath();
            var dnxPath = DnxEnvironment.GetDNXPathForRuntime(runtime);
            var newPath = Environment.ExpandEnvironmentVariables($"{webTools};{dnxPath};%PATH%");
            result["PATH"] = newPath;
            return result;
        }

        private static async Task RestoreProject(
            string projectPath,
            AspNetRuntime runtime,
            Action<string> callback)
        {
            var environment = GetDnxEnvironmentForRuntime(runtime);
            var command = $"/c dnu restore \"{projectPath}\"";
            callback($"Restoring project: {projectPath}");
            // This has hard dependency on dnu bing a batch file.
            var result = await ProcessUtils.RunCommandAsync("cmd.exe", command, (s, e) => callback(e.Line), environment);
            if (!result)
            {
                throw new GCloudException($"Failed to restore project: {projectPath}");
            }
        }

        private static async Task PrepareAppBundle(
            string projectPath,
            string appTempPath,
            AspNetRuntime runtime,
            Action<string> callback)
        {
            // Customize the environment by adding the path to the node_modules directory, which can be necessary for
            // the publish process.
            var environment = GetDnxEnvironmentForRuntime(runtime);
            var nodeModulesPath = Path.Combine(projectPath, "node_modules", ".bin");
            var newPath = $"{nodeModulesPath};{environment["PATH"]}";
            environment["PATH"] = newPath;

            // This is a dependency on the fact that DNU is a batch file, but it has to be launched this way.
            callback($"Preparing app bundle in {appTempPath}.");
            string command = $"/c dnu publish \"{projectPath}\" --out \"{appTempPath}\" --framework {DnxEnvironment.GetDnxFrameworkNameFromRuntime(runtime)} --configuration release";
            callback($"Executing command: {command}");
            var result = await ProcessUtils.RunCommandAsync("cmd.exe", command, (s, e) => callback(e.Line), environment);
            if (!result)
            {
                throw new GCloudException($"Failed to prepare bundle for project: {projectPath}");
            }
        }

        private static Task DeployToAppEngine(
            string appTempPath,
            string versionName,
            bool makeDefaultVersion,
            Action<string> callback,
            AccountAndProjectId accountAndProject)
        {
            var makeDefault = makeDefaultVersion ? "--promote" : "--no-promote";
            var name = String.IsNullOrEmpty(versionName) ? "" : $"--version={versionName}";
            var appYaml = Path.Combine(appTempPath, AppYamlFilename);
            string command = $"preview app deploy \"{appYaml}\" {makeDefault} {name} --docker-build=remote --verbosity=info --quiet";
            callback($"Executing command: {command}");
            // This has a hardcoded dependency on the fact that gcloud is a batch file.
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
