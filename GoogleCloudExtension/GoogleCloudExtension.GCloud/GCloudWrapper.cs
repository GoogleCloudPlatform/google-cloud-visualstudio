// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// Supported ASP.NET runtimes.
    /// </summary>
    public enum AspNetRuntime
    {
        None,
        Mono,
        CoreCLR
    }

    /// <summary>
    /// This class wraps the gcloud command and offers up some of its services in a
    /// as async methods. It also manages the its own notion of "current user" and 
    /// "current project".
    /// This class is a singleton.
    /// </summary>
    public sealed class GCloudWrapper
    {
        private AccountAndProjectId _currentAccountAndProject;

        private GCloudWrapper()
        { }

        /// <summary>
        /// Lazily creates the singleton instace for the class.
        /// </summary>
        private static GCloudWrapper s_Instance = new GCloudWrapper();
        public static GCloudWrapper Instance
        {
            get { return s_Instance; }
        }

        /// <summary>
        /// This event is raised whenever the current account or project has changed;
        /// there's no notification of what the new values are so the caller has to call
        /// to find out what the new values are.
        /// </summary>
        public event EventHandler AccountOrProjectChanged;

        private void RaiseAccountOrProjectChanged()
        {
            AccountOrProjectChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Calculates what the current project and account is, it might return what "gcloud"
        /// things is the current account and project or what this instance override is. This is
        /// hidden from the caller.
        /// </summary>
        /// <returns>The current AccountAndProjectId.</returns>
        public async Task<AccountAndProjectId> GetCurrentAccountAndProjectAsync()
        {
            if (_currentAccountAndProject == null)
            {
                // Fetching the current account and project, for gcloud, does not need to use the current
                // account.
                var settings = await GetJsonOutputAsync<Settings>("config list", accountAndProject: null);
                _currentAccountAndProject = new AccountAndProjectId(
                    account: settings.CoreSettings.Account,
                    projectId: settings.CoreSettings.Project);
            }
            return _currentAccountAndProject;
        }

        /// <summary>
        /// Updates the local concet of "current account and project" in the instance *only* it does not
        /// affect what gcloud thinks is the current account and project.
        /// </summary>
        /// <param name="accountAndProject">The new accountAndProject to use</param>
        public void UpdateUserAndProject(AccountAndProjectId accountAndProject)
        {
            _currentAccountAndProject = accountAndProject;
            RaiseAccountOrProjectChanged();
        }

        public void UpdateProject(string projectId)
        {
            var newAccountAndProject = new AccountAndProjectId(_currentAccountAndProject.Account, projectId);
            UpdateUserAndProject(newAccountAndProject);
        }

        private static Dictionary<string, string> s_GCloudEnvironment;

        private static Dictionary<string, string> GetGCloudEnvironment()
        {
            if (s_GCloudEnvironment == null)
            {
                s_GCloudEnvironment = new Dictionary<string, string>();
                var gcloudPath = GetGCloudPath();
                var webTools = GetWebToolsPath();
                var newPath = Environment.ExpandEnvironmentVariables($"{webTools};{gcloudPath};%PATH%");
                s_GCloudEnvironment["PATH"] = newPath;
            }
            return s_GCloudEnvironment;
        }

        // TODO(ivann): Possibly use MSI APIs to find the location where gcloud was installed instead
        // of hardcoding it to program files.
        // If the user installs it by hand then we need to have a registry key, or environment variable, where
        // the user can override this location.
        private static string GetGCloudPath()
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            // This is where the binaries are supposed to be stored.
            string result = Path.Combine(programFiles, @"Google\Cloud SDK\google-cloud-sdk\bin");
            if (Directory.Exists(result))
            {
                return result;
            }

            // There's a bug in gcloud setup and it might be stored in this directory.
            return Path.Combine(programFiles, @"Google\Cloud SDK\GOOGLE~1\bin");
        }

        private static string GetWebToolsPath()
        {
            var vsInstallPath = GetVSInstallPath();
            return Path.Combine(vsInstallPath, WebToolsRelativePath);
        }

        private static readonly List<string> s_VSKeysToCheck = new List<string>
        {
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\14.0",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VWDExpress\14.0",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VWDExpress\14.0",
        };

        private const string WebToolsRelativePath = @"Extensions\Microsoft\Web Tools\External";

        private const string InstallDirValue = "InstallDir";

        private static string s_VSInstallPath;

        private static string GetVSInstallPath()
        {
            if (s_VSInstallPath == null)
            {
                foreach (var key in s_VSKeysToCheck)
                {
                    var value = (string)Registry.GetValue(key, InstallDirValue, null);
                    if (value != null)
                    {
                        s_VSInstallPath = value;
                        break;
                    }
                }
            }
            return s_VSInstallPath;
        }

        private string FormatAccountAndProjectParameters(AccountAndProjectId accountAndProject)
        {
            if (accountAndProject == null)
            {
                return "";
            }
            var projectParam = accountAndProject?.ProjectId == null ? "" : $"--project={accountAndProject.ProjectId}";
            var accountParam = accountAndProject?.Account == null ? "" : $"--account={accountAndProject.Account}";
            return $"{projectParam} {accountParam}";
        }


        private string FormatCommand(string command, AccountAndProjectId accountAndProject, bool useJson)
        {
            var accountAndProjectParams = FormatAccountAndProjectParameters(accountAndProject);
            var jsonFormatParam = useJson ? "--format=json" : "";
            return $"/c gcloud {jsonFormatParam} {accountAndProjectParams} {command}";
        }

        private async Task<string> GetActualCommandAsync(string command, bool useCurrentAccount, bool useJson = false)
        {
            var accountAndProject = useCurrentAccount ? await GetCurrentAccountAndProjectAsync() : null;
            return FormatCommand(command, accountAndProject, useJson);
        }

        private async Task RunCommandAsync(string command, Action<string> callback, AccountAndProjectId accountAndProject)
        {
            var actualCommand = FormatCommand(command, accountAndProject, useJson: false);
            Debug.WriteLine($"Executing gcloud command: {actualCommand}");
            var result = await ProcessUtils.RunCommandAsync("cmd.exe", actualCommand, (s, e) => callback(e.Line), GetGCloudEnvironment());
            if (!result)
            {
                throw new GCloudException($"Failed to execute: {actualCommand}");
            }
        }

        private async Task RunCommandAsync(string command, Action<string> callback)
        {
            await RunCommandAsync(command, callback, await GetCurrentAccountAndProjectAsync());
        }

        private async Task<string> GetCommandOutputAsync(string command, AccountAndProjectId accountAndProject)
        {
            var actualCommand = FormatCommand(command, accountAndProject, useJson: false);
            Debug.WriteLine($"Executing gcloud command: {actualCommand}");
            var output = await ProcessUtils.GetCommandOutputAsync("cmd.exe", actualCommand, GetGCloudEnvironment());
            if (!output.Succeeded)
            {
                throw new GCloudException($"Failed with message: {output.Error}");
            }
            return output.Output;
        }

        private async Task<string> GetCommandOutputAsync(string command)
        {
            return await GetCommandOutputAsync(command, await GetCurrentAccountAndProjectAsync());
        }

        private async Task<IList<T>> GetJsonArrayOutputAsync<T>(string command, AccountAndProjectId accountAndProject)
        {
            var actualCommand = FormatCommand(command, accountAndProject, useJson: true);
            try
            {
                Debug.Write($"Executing gcloud command: {actualCommand}");
                return await ProcessUtils.GetJsonArrayOutputAsync<T>("cmd.exe", actualCommand, GetGCloudEnvironment());
            }
            catch (JsonOutputException ex)
            {
                throw new GCloudException($"Failed to execute command {actualCommand}\nInner message:\n{ex.Message}", ex);
            }
        }

        private async Task<IList<T>> GetJsonArrayOutputAsync<T>(string command)
        {
            return await GetJsonArrayOutputAsync<T>(command, await GetCurrentAccountAndProjectAsync());
        }

        private async Task<T> GetJsonOutputAsync<T>(string command, AccountAndProjectId accountAndProject)
        {
            var actualCommand = FormatCommand(command, accountAndProject, useJson: true);
            try
            {
                Debug.Write($"Executing gcloud command: {actualCommand}");
                return await ProcessUtils.GetJsonOutputAsync<T>("cmd.exe", actualCommand, GetGCloudEnvironment());
            }
            catch (JsonOutputException ex)
            {
                throw new GCloudException($"Failed to execute command {actualCommand}\nInner message:\n{ex.Message}", ex);
            }
        }

        private async Task<T> GetJsonOutputAsync<T>(string command)
        {
            return await GetJsonOutputAsync<T>(command, await GetCurrentAccountAndProjectAsync());
        }

        /// <summary>
        /// Returns the access token for this class' notion of current user.
        /// </summary>
        /// <returns>The string representation of the access token.</returns>
        public Task<string> GetAccessTokenAsync()
        {
            return GetCommandOutputAsync("auth print-access-token");
        }

        /// <summary>
        /// Returns the list of compute instances for this class' notion of current
        /// user and project.
        /// </summary>
        /// <returns>The list of compute instances.</returns>
        public Task<IList<ComputeInstance>> GetComputeInstanceListAsync()
        {
            return GetJsonArrayOutputAsync<ComputeInstance>("compute instances list");
        }

        // This prefix allows us to detect the builtin services so we can avoid showing
        // them as user's versions.
        private const string BuiltinServiceVersionPrefix = "ah-";

        /// <summary>
        /// Returns the list of AppEngine apps for this class' notion of current
        /// user and project.
        /// </summary>
        /// <returns>The list of AppEngine apps.</returns>
        public async Task<IList<AppEngineApp>> GetAppEngineAppListAsync()
        {
            var result = await GetJsonArrayOutputAsync<AppEngineApp>("preview app modules list") ?? new List<AppEngineApp>();
            return result.Where(x => !x.Version.StartsWith(BuiltinServiceVersionPrefix)).ToList();
        }

        /// <summary>
        /// Returns the list of accounts registered with gcloud.
        /// </summary>
        /// <returns>List of accounts.</returns>
        public async Task<IList<string>> GetAccountListAsync()
        {
            // Getting the list of accounts needs to not filter down by the current account
            // being used or nothing will be shown, so we don't need to use the current account.
            var settings = await GetJsonOutputAsync<AccountSettings>("auth list", accountAndProject: null);
            return settings.Accounts;
        }

        /// <summary>
        /// Returns the list of projects accessible by the current account.
        /// </summary>
        /// <returns>List of projects.</returns>
        public Task<IList<GcpProject>> GetProjectsAsync()
        {
            return GetJsonArrayOutputAsync<GcpProject>("alpha projects list");
        }

        public async Task<IList<GcpProject>> GetProjectsAsync(AccountAndProjectId accountAndProject)
        {
            return await GetJsonArrayOutputAsync<GcpProject>("alpha projects list", accountAndProject);
        }

        /// <summary>
        /// Deletes the given app version from the module.
        /// </summary>
        /// <param name="module">The module that owns the version to remove.</param>
        /// <param name="version">The version to remove.</param>
        /// <returns>Taks that will be fullfilled when done.</returns>
        public async Task DeleteAppVersion(string module, string version)
        {
            await GetCommandOutputAsync($"preview app modules delete {module} --version={version} --quiet");
        }

        /// <summary>
        /// Sets the given version as the defualt version for the given module.
        /// </summary>
        /// <param name="module">The module to change.</param>
        /// <param name="version">The version to be made default.</param>
        /// <returns>The task.</returns>
        public async Task SetDefaultAppVersionAsync(string module, string version)
        {
            await GetCommandOutputAsync($"preview app modules set-default {module} --version={version} --quiet");
        }

        /// <summary>
        /// Starts the GCE instance given it's name and zone.
        /// </summary>
        /// <param name="name">The name of the GCE instance.</param>
        /// <param name="zone">The zone where the GCE instance resides.</param>
        /// <returns>The task.</returns>
        public async Task StartComputeInstanceAsync(string name, string zone)
        {
            await GetCommandOutputAsync($"compute instances start {name} --zone={zone}");
        }

        /// <summary>
        /// Stops the GCE instance given it's name and zone.
        /// </summary>
        /// <param name="name">The name of the GCE instance.</param>
        /// <param name="zone">The zone where the GCE instance resides.</param>
        /// <returns></returns>
        public async Task StopComputeInstanceAsync(string name, string zone)
        {
            await GetCommandOutputAsync($"compute instances stop {name} --zone={zone}");
        }

        /// <summary>
        /// Deployes an Asp.NET application to AppEngine using a managed vm.
        /// </summary>
        /// <param name="startupProjectPath">The path to the startup project.</param>
        /// <param name="projectPaths">The paths to all of the projects int he solution.</param>
        /// <param name="versionName">The name, if any, of the version to deploy.</param>
        /// <param name="runtime">The target runtime, Mono, CoreCLR, etc...</param>
        /// <param name="makeDefaultVersion">Is this version to be made the default version.</param>
        /// <param name="callback">The output handler that will be called with the output from the process.</param>
        /// <returns>The task.</returns>
        public async Task DeployApplication(
            string startupProjectPath,
            IList<string> projectPaths,
            string versionName,
            AspNetRuntime runtime,
            bool makeDefaultVersion,
            Action<string> callback,
            AccountAndProjectId accountAndProject)
        {
            var appTempPath = GetAppStagingDirectory();
            try
            {
                await RestoreProjects(projectPaths, runtime, callback);
                await PrepareAppBundle(startupProjectPath, appTempPath, runtime, callback);
                PrepareEntryPoint(startupProjectPath, appTempPath, callback);
                CopyGaeFiles(startupProjectPath, appTempPath, runtime, callback);
                await DeployToGae(appTempPath, versionName, makeDefaultVersion, callback, accountAndProject);
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

        public bool ValidateDNXInstallationForRuntime(AspNetRuntime runtime)
        {
            bool result = false;
            Debug.WriteLine("Validating DNX installation.");
            var dnxDirectory = GetDNXPathForRuntime(runtime);
            var dnuPath = Path.Combine(dnxDirectory, "dnu.cmd");

            result = File.Exists(dnuPath);
            Debug.WriteLineIf(!result, $"DNX runtime {runtime} not installed, cannot find {dnuPath}");
            return result;
        }

        private bool? _validGCloudInstallation;
        public bool ValidateGCloudInstallation()
        {
            if (_validGCloudInstallation != null)
            {
                return _validGCloudInstallation.Value;
            }

            Debug.WriteLine("Validating GCloud installation.");
            var gcloudDirectory = GetGCloudPath();
            var gcloudPath = Path.Combine(gcloudDirectory, "gcloud.cmd");

            var result = File.Exists(gcloudPath);
            Debug.WriteLineIf(!result, $"GCloud cannot be found, can't find {gcloudPath}");
            _validGCloudInstallation = new bool?(result);
            return result;
        }

        public bool ValidateEnvironment()
        {
            var validDNXInstallation = ValidateDNXInstallationForRuntime(AspNetRuntime.CoreCLR) || ValidateDNXInstallationForRuntime(AspNetRuntime.Mono);
            var validGCloudInstallation = ValidateGCloudInstallation();
            return validDNXInstallation && validGCloudInstallation;
        }

        private const string DnxVersion = "1.0.0-beta8";
        private const string MonoRuntimeName = "mono";
        private const string CoreCLRRuntimeName = "coreclr";

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

        private static void CopyGaeFiles(string projectPath, string appTempPath, AspNetRuntime runtime, Action<string> callback)
        {
            Debug.Assert(runtime == AspNetRuntime.Mono || runtime == AspNetRuntime.CoreCLR);

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
                var runtimeName = runtime == AspNetRuntime.Mono ? MonoRuntimeName : CoreCLRRuntimeName;
                var dockerFileContent = String.Format(DockerfileTemplate, DnxVersion, runtimeName);
                callback($"Writting file [{dockerfileDest}] for runtime {runtimeName}.");
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
                callback($"Writting file [{appYamlDest}].");
                File.WriteAllText(appYamlDest, AppYamlContent);
            }
        }

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

        private void PrepareEntryPoint(string startupProjectPath, string appTempPath, Action<string> callback)
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
            List<Task> restoreTasks = new List<Task>();
            foreach (string path in projectPaths)
            {
                restoreTasks.Add(RestoreProject(path, runtime, callback));
            }
            await Task.WhenAll(restoreTasks);
            callback("Done restoring projects.");
        }

        // The path where the binaries for the particular runtime live.
        //   {0} the runtime name.
        private const string DnxRuntimesBinPathFormat = @".dnx\runtimes\{0}\bin";

        // Names for the runtime to use depending on the runtime.
        //   {0} is the bitness of the os, x86 or x64.
        //   {1} is the version of the runtime.
        private const string DnxClrRuntimeNameFormat = "dnx-clr-win-{0}.{1}";
        private const string DnxCoreClrRuntimeNameFormat = "dnx-coreclr-win-{0}.{1}";

        private static string GetDNXPathForRuntime(AspNetRuntime runtime)
        {
            var userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string bitness = Environment.Is64BitProcess ? "x64" : "x86";

            string runtimeNameFormat = null;
            switch (runtime)
            {
                case AspNetRuntime.Mono:
                    runtimeNameFormat = DnxClrRuntimeNameFormat;
                    break;
                case AspNetRuntime.CoreCLR:
                    runtimeNameFormat = DnxCoreClrRuntimeNameFormat;
                    break;
                default:
                    Debug.Assert(false, "Should not get here.");
                    break;
            }

            var runtimeName = String.Format(runtimeNameFormat, bitness, DnxVersion);
            var runtimeRelativePath = String.Format(DnxRuntimesBinPathFormat, runtimeName);
            Debug.WriteLine($"Using runtime path: {runtimeRelativePath}");

            return Path.Combine(userDirectory, runtimeRelativePath);
        }

        private static async Task RestoreProject(
            string projectPath,
            AspNetRuntime runtime,
            Action<string> callback)
        {
            // Customize the environment by adding the path to the node_modules directory, which can be necessary for
            // the publish process.
            var restoreEnvironment = new Dictionary<string, string>(GetGCloudEnvironment());
            var dnxPath = GetDNXPathForRuntime(runtime);
            var newPath = $"{dnxPath};{restoreEnvironment["PATH"]}";
            restoreEnvironment["PATH"] = newPath;

            var command = $"/c dnu restore \"{projectPath}\"";
            callback($"Restoring project: {projectPath}");
            // This has hard dependency on dnu bing a batch file.
            var result = await ProcessUtils.RunCommandAsync("cmd.exe", command, (s, e) => callback(e.Line), restoreEnvironment);
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
            var publishEnvironment = new Dictionary<string, string>(GetGCloudEnvironment());
            var dnxPath = GetDNXPathForRuntime(runtime);
            var nodeModulesPath = Path.Combine(projectPath, @"node_modules\.bin");
            var newPath = $"{dnxPath};{nodeModulesPath};{publishEnvironment["PATH"]}";
            publishEnvironment["PATH"] = newPath;

            // This is a dependency on the fact that DNU is a batch file, but it has to be launched this way.
            callback($"Preparing app bundle in {appTempPath}.");
            string command = $"/c dnu publish \"{projectPath}\" --out \"{appTempPath}\" --framework {GetDNXFrameworkNameFromRuntime(runtime)} --configuration release";
            callback($"Executing command: {command}");
            var result = await ProcessUtils.RunCommandAsync("cmd.exe", command, (s, e) => callback(e.Line), publishEnvironment);
            if (!result)
            {
                throw new GCloudException($"Failed to prepare bundle for project: {projectPath}");
            }
        }

        private static string GetDNXFrameworkNameFromRuntime(AspNetRuntime runtime)
        {
            switch (runtime)
            {
                case AspNetRuntime.Mono:
                    return "dnx451";
                case AspNetRuntime.CoreCLR:
                    return "dnxcore50";
                default:
                    return "none";
            }
        }

        private Task DeployToGae(
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
            return RunCommandAsync(command, callback, accountAndProject);
        }

        private static string GetAppStagingDirectory()
        {
            var result = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(result);
            return result;
        }
    }
}
