// Copyright 2018 Google Inc. All Rights Reserved.
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PowerShellUtils.Tests
{
    [TestClass]
    [DeploymentItem(InstallerPsFilePath, "Resources")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public class RemotePowerShellUtilsTests
    {
        private const string InstallerPsFilePath = "Resources/InstallRemoteTool.ps1";
        private const string InstallerPsResourcePath = RemoteToolInstaller.InstallerPsFilePath;
        private const string DefaultUser = "DefaultUser";
        private const string DefaultPassword = "DefaultPassword";

        [TestMethod]
        public void TestGetEmbeddedFile_ThrowsForUnknownFile()
        {
            const string expectedResourceName = "Unknown/File.txt";
            FileNotFoundException e = Assert.ThrowsException<FileNotFoundException>(
                () => RemotePowerShellUtils.GetEmbeddedFile(expectedResourceName));
            Assert.AreEqual(expectedResourceName, e.Message);
        }

        [TestMethod]
        public void TestGetEmbeddedFile_GetFile()
        {
            string result = RemotePowerShellUtils.GetEmbeddedFile(InstallerPsResourcePath);

            Assert.AreEqual(File.ReadAllText(InstallerPsFilePath), result);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" \r\n\t\f")]
        public void TestCreatePSCredential_ThrowsForInvalidPassword(string invalidPassword)
        {
            ArgumentException e = Assert.ThrowsException<ArgumentException>(
                () => RemotePowerShellUtils.CreatePSCredential(DefaultUser, invalidPassword));

            Assert.AreEqual("input", e.Message);
        }

        [TestMethod]
        public void TestCreatePSCredential_CreatesCredentialWithGivenUser()
        {
            const string expectedUser = "ExpectedUser";
            PSCredential result = RemotePowerShellUtils.CreatePSCredential(expectedUser, DefaultPassword);
            Assert.AreEqual(expectedUser, result.UserName);
        }

        [TestMethod]
        public void TestCreatePSCredential_CreatesCredentialWithGivenPassword()
        {
            const string expectedPassword = "ExpectedPassword";
            PSCredential result = RemotePowerShellUtils.CreatePSCredential(DefaultUser, expectedPassword);
            Assert.AreEqual(expectedPassword.Length, result.Password.Length);
        }

        [TestMethod]
        public async Task TestInvokeAsync_ThrowsOperationCanceledWhenTokenAlreadyCanceled()
        {
            using (var powerShell = PowerShell.Create())
            {
                await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                    () => powerShell.InvokeAsync(new CancellationToken(true)));
            }
        }

        [TestMethod]
        public async Task TestInvokeAsync_DoesNotStartPowerShellWhenTokenAlreadyCanceled()
        {
            using (var powerShell = PowerShell.Create())
            {
                // PowerShell can not start without a command to execute.
                // Adding this script helps avoid false positives.
                powerShell.AddScript("Start-Sleep -Seconds 30");

                await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                    () => powerShell.InvokeAsync(new CancellationToken(true)));

                Assert.AreEqual(PSInvocationState.NotStarted, powerShell.InvocationStateInfo.State);
            }
        }

        [TestMethod]
        [TestCategory("SkipOnAppVeyor")]
        [Timeout(5000)]
        public async Task TestInvokeAsync_ThrowsTaskCanceledExceptionWhenTokenCanceled()
        {
            using (var powerShell = PowerShell.Create())
            {
                powerShell.AddScript("Start-Sleep -Seconds 30");

                await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                    () => powerShell.InvokeAsync(new CancellationTokenSource(16).Token));
            }
        }

        [TestMethod]
        [TestCategory("SkipOnAppVeyor")]
        [Timeout(5000)]
        public async Task TestInvokeAsync_StopsPowerShellWhenTokenCanceled()
        {
            using (var powerShell = PowerShell.Create())
            {
                powerShell.AddScript("Start-Sleep -Seconds 30");

                await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                    () => powerShell.InvokeAsync(new CancellationTokenSource(16).Token));

                Assert.AreEqual(PSInvocationState.Stopped, powerShell.InvocationStateInfo.State);
            }
        }

        [TestMethod]
        public async Task TestInvokeAsync_ExecutesPowerShell()
        {
            const string expectedValue = "expectedValue";
            using (var powerShell = PowerShell.Create())
            {
                powerShell.AddVariable("varName", expectedValue);
                powerShell.AddScript("Write-Output $varName");

                PSDataCollection<PSObject> results = await powerShell.InvokeAsync();

                Assert.AreEqual(expectedValue, results.Single().BaseObject);
            }
        }

        [TestMethod]
        public async Task TestInvokeAsync_ThrowsThrownPowerShellException()
        {
            const string expectedValue = "Test Exception";
            using (var powerShell = PowerShell.Create())
            {
                powerShell.AddVariable("varName", expectedValue);
                powerShell.AddScript("throw $varName");

                RuntimeException e =
                    await Assert.ThrowsExceptionAsync<RuntimeException>(() => powerShell.InvokeAsync());

                Assert.AreEqual(expectedValue, e.ErrorRecord.Exception.Message);
            }
        }

        [TestMethod]
        public async Task TestInvokeAsync_ThrowsPowerShellErrorOnErrorActionStop()
        {
            const string expectedValue = "Test Exception";
            using (var powerShell = PowerShell.Create())
            {
                powerShell.AddVariable("varName", expectedValue);
                powerShell.AddScript("$ErrorActionPreference = \"Stop\"");
                powerShell.AddScript("Write-Error $varName");

                ActionPreferenceStopException e =
                    await Assert.ThrowsExceptionAsync<ActionPreferenceStopException>(() => powerShell.InvokeAsync());

                Assert.AreEqual(expectedValue, e.ErrorRecord.Exception.Message);
            }
        }
    }
}
