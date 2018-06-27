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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.FirewallManagement;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.FirewallManagement
{
    [TestClass]
    public class PortManagerViewModelTests : ExtensionTestBase
    {
        private Action _mockedCloseAction;

        protected override void BeforeEach()
        {
            _mockedCloseAction = Mock.Of<Action>();
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);
        }

        [TestMethod]
        public void TestConstructor_InitializesPorts()
        {
            var instance = new Instance();
            var objectUnderTest = new PortManagerViewModel(_mockedCloseAction, instance);

            CollectionAssert.AreEqual(
                PortManagerViewModel.s_supportedPorts.ToList(),
                objectUnderTest.Ports.Select(p => p.PortInfo).ToList());
            Assert.IsTrue(objectUnderTest.Ports.All(p => p.Instance == instance));
        }

        [TestMethod]
        public void TestConstructor_InitializesOkCommand()
        {
            var objectUnderTest = new PortManagerViewModel(_mockedCloseAction, new Instance());

            Assert.IsTrue(objectUnderTest.OkCommand.CanExecute(null));
        }

        [TestMethod]
        public void TestConstructor_InitalizesNavigateToCloudConsoleCommand()
        {
            var objectUnderTest = new PortManagerViewModel(_mockedCloseAction, new Instance());

            Assert.IsTrue(objectUnderTest.NavigateToCloudConsoleCommand.CanExecute(null));
        }

        [TestMethod]
        public void TestConstructor_NoPortsEnabled()
        {
            var objectUnderTest = new PortManagerViewModel(_mockedCloseAction, new Instance());

            Assert.IsFalse(objectUnderTest.Ports.Any(p => p.IsEnabled));
        }

        [TestMethod]
        public void TestConstructor_PortEnabledByTag()
        {
            PortInfo enabledPort = PortManagerViewModel.s_supportedPorts[0];
            Instance instance = PortTestHelpers.GetInstanceWithEnabledPort(enabledPort);
            var objectUnderTest = new PortManagerViewModel(_mockedCloseAction, instance);

            Assert.AreEqual(enabledPort, objectUnderTest.Ports.Single(p => p.IsEnabled).PortInfo);
        }

        [TestMethod]
        public void TestOkCommand_ClosesWindow()
        {
            var objectUnderTest = new PortManagerViewModel(_mockedCloseAction, new Instance());

            objectUnderTest.OkCommand.Execute(null);

            Mock.Get(_mockedCloseAction).Verify(c => c());
        }

        [TestMethod]
        public void TestOkCommand_InitalizesResult()
        {
            var objectUnderTest = new PortManagerViewModel(_mockedCloseAction, new Instance());

            objectUnderTest.OkCommand.Execute(null);

            Assert.IsNotNull(objectUnderTest.Result);
        }

        [TestMethod]
        public void TestOkCommand_ResultIsEmpty()
        {
            var objectUnderTest = new PortManagerViewModel(_mockedCloseAction, new Instance());

            objectUnderTest.OkCommand.Execute(null);

            CollectionAssert.That.IsEmpty(objectUnderTest.Result.PortsToDisable);
            CollectionAssert.That.IsEmpty(objectUnderTest.Result.PortsToEnable);
        }

        [TestMethod]
        public void TestOkCommand_ResultHasPortToEnable()
        {
            var objectUnderTest = new PortManagerViewModel(_mockedCloseAction, new Instance());

            PortModel portToEnable = objectUnderTest.Ports[0];
            portToEnable.IsEnabled = true;
            objectUnderTest.OkCommand.Execute(null);

            FirewallPort firewallPortToEnable = objectUnderTest.Result.PortsToEnable.Single();
            Assert.AreEqual(portToEnable.PortInfo.Port, firewallPortToEnable.Port);
            Assert.AreEqual(portToEnable.GetPortInfoTag(), firewallPortToEnable.Name);
        }

        [TestMethod]
        public void TestOkCommand_ResultHasPortToDisable()
        {
            PortInfo enabledPort = PortManagerViewModel.s_supportedPorts[0];
            const string instanceName = "instance-name";
            Instance instance = PortTestHelpers.GetInstanceWithEnabledPort(enabledPort, instanceName);
            var objectUnderTest = new PortManagerViewModel(_mockedCloseAction, instance);

            objectUnderTest.Ports.Single(p => p.IsEnabled).IsEnabled = false;
            objectUnderTest.OkCommand.Execute(null);

            FirewallPort portToDisable = objectUnderTest.Result.PortsToDisable.Single();
            Assert.AreEqual(enabledPort.Port, portToDisable.Port);
            Assert.AreEqual(enabledPort.GetTag(instanceName), portToDisable.Name);
        }

        [TestMethod]
        public void TestNavigateToCloudConsoleCommand_OpensBrowserFromProject()
        {
            const string expectedProjectID = "expected-project-id";
            var credentialsStoreMock = new Mock<ICredentialsStore>();
            var browerServiceMock = new Mock<IBrowserService>();
            credentialsStoreMock.Setup(cs => cs.CurrentProjectId).Returns(expectedProjectID);
            PackageMock.Setup(p => p.GetMefServiceLazy<ICredentialsStore>()).Returns(credentialsStoreMock.ToLazy());
            PackageMock.Setup(p => p.GetMefServiceLazy<IBrowserService>()).Returns(browerServiceMock.ToLazy());

            var objectUnderTest = new PortManagerViewModel(_mockedCloseAction, new Instance());

            objectUnderTest.NavigateToCloudConsoleCommand.Execute(null);

            browerServiceMock.Verify(
                b => b.OpenBrowser(string.Format(PortManagerViewModel.ConsoleFirewallsUrlFormat, expectedProjectID)));
        }
    }
}
