﻿// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PowerShellUtils
{
    /// <summary>
    /// Installs Visual Studio Remote Debugger Tool.
    /// The remote tools currently is part of Visual Studio installation.
    /// This class opens remote PowerShell session to copy the tools to target machine.
    /// For more detail, please also refer to file Resources.InstallRemoteTool.ps1.
    /// </summary>
    public class RemoteToolInstaller
    {
        private const string InstallerPsFilePath = "GoogleCloudExtension.PowerShellUtils.Resources.InstallRemoteTool.ps1";
        private const string CredentialVariablename = "credential";
        private const string RemoteToolSourcePath = "debuggerSourcePath";

        private readonly string _debuggerToolLocalPath;
        private readonly RemoteTarget _remoteTarget;
        private readonly PSCredential _credential;
        private readonly string _computerName;

        /// <summary>
        /// Initializes a new instance of class <seealso cref="RemoteToolInstaller"/>
        /// </summary>
        /// <param name="computerName">
        /// The remote computer name. It can be a public ip address too.
        /// </param>
        /// <param name="username">Credential user name.</param>
        /// <param name="password">Credential password.</param>
        /// <param name="debuggerToolLocalPath">
        /// The path to Visual Studio remote debugging tools.
        /// It is located under Visual Studio installation path.
        /// </param>
        public RemoteToolInstaller(
            string computerName,
            string username,
            string password,
            string debuggerToolLocalPath)
        {
            _debuggerToolLocalPath = debuggerToolLocalPath;
            var securePassword = PsUtils.ConvertToSecureString(password);
            _remoteTarget = new RemoteTarget(computerName, username, securePassword);
            _credential = PsUtils.CreatePSCredential(username, securePassword);
            _computerName = computerName;
        }

        /// <summary>
        /// Execute remote PowerShell command to copy/setup debugger remote tools.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Install(CancellationToken cancelToken)
        {
            return await _remoteTarget.ExecuteAsync(AddInstallCommands, cancelToken);
        }

        private void AddInstallCommands(PowerShell powerShell)
        {
            var setupScript = PsUtils.GetScript(InstallerPsFilePath);

            powerShell.AddVarialbe("credential", _credential);
            powerShell.AddVarialbe("debuggerSourcePath", _debuggerToolLocalPath);
            powerShell.AddScript(@"$sessionOptions = New-PSSessionOption –SkipCACheck –SkipCNCheck –SkipRevocationCheck");
            powerShell.AddScript($@"$session = New-PSSession {_computerName} -UseSSL -Credential $credential -SessionOption $sessionOptions");
            powerShell.AddScript(setupScript);
            powerShell.AddScript(@"Remove-PSSession -Session $session");
        }
    }
}
