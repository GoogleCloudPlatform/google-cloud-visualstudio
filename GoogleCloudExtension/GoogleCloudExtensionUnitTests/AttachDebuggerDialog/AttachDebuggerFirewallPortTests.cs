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
using GoogleCloudExtension.AttachDebuggerDialog;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.FirewallManagement;
using GoogleCloudExtensionUnitTests.FirewallManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.AttachDebuggerDialog
{
    [TestClass]
    public class AttachDebuggerFirewallPortTests
    {
        private Mock<IGceDataSource> _dataSourceMock;
        private Instance _defaultInstance;
        private const string DefaultDescription = "Default Description";
        private const string DefaultInstanceName = "default-instance-name";
        private const string ExpectedInstanceName = "expected-instance-name";
        private PortInfo _defaultPortInfo;
        private PortInfo _expectedPortInfo;
        private Instance _expectedInstance;

        [TestInitialize]
        public void BeforeEach()
        {
            _dataSourceMock = new Mock<IGceDataSource>();
            _defaultPortInfo = new PortInfo("default-port", 0);
            _defaultInstance = new Instance { Name = DefaultInstanceName };
            _expectedPortInfo = new PortInfo("expected-port-name", 15);
            _expectedInstance = new Instance { Name = ExpectedInstanceName };
        }

        [TestMethod]
        public void TestConstructor_ThrowsForNullPortInfo()
        {
            var e = Assert.ThrowsException<ArgumentNullException>(
                () => _ = new AttachDebuggerFirewallPort(
                    null,
                    DefaultDescription,
                    _defaultInstance,
                    _dataSourceMock.ToLazy()));
            Assert.AreEqual("portInfo", e.ParamName);
        }

        [TestMethod]
        public void TestConstructor_ThrowsForNullDescription()
        {
            var e = Assert.ThrowsException<ArgumentException>(
                () => _ = new AttachDebuggerFirewallPort(
                    _defaultPortInfo,
                    null,
                    _defaultInstance,
                    _dataSourceMock.ToLazy()));
            Assert.AreEqual("description", e.Message);
        }

        [TestMethod]
        public void TestConstructor_ThrowsForNullInstance()
        {
            var e = Assert.ThrowsException<ArgumentNullException>(
                () => _ = new AttachDebuggerFirewallPort(
                    _defaultPortInfo,
                    DefaultDescription,
                    null,
                    _dataSourceMock.ToLazy()));
            Assert.AreEqual("gceInstance", e.ParamName);
        }

        [TestMethod]
        public void TestConstructor_ThrowsForNullDataSource()
        {
            var e = Assert.ThrowsException<ArgumentNullException>(
                () => _ = new AttachDebuggerFirewallPort(_defaultPortInfo, DefaultDescription, _defaultInstance, null));
            Assert.AreEqual("lazyDataSource", e.ParamName);
        }

        [TestMethod]
        public void TestConstructor_SetsPortInfo()
        {
            var objectUnderTest = new AttachDebuggerFirewallPort(
                _expectedPortInfo,
                DefaultDescription,
                _defaultInstance,
                _dataSourceMock.ToLazy());
            Assert.AreEqual(_expectedPortInfo, objectUnderTest.PortInfo);
        }

        [TestMethod]
        public void TestConstructor_SetsDescription()
        {
            const string expectedDescription = "Expected Description";
            var objectUnderTest = new AttachDebuggerFirewallPort(
                _defaultPortInfo,
                expectedDescription,
                _defaultInstance,
                _dataSourceMock.ToLazy());
            Assert.AreEqual(expectedDescription, objectUnderTest.Description);
        }

        [TestMethod]
        public async Task TestEnablePort_UpdatesInstancePortsFor()
        {
            _dataSourceMock.Setup(ds => ds.RefreshInstance(It.IsAny<Instance>()))
                .Returns(Task.FromResult(_expectedInstance));
            _dataSourceMock.Setup(
                    ds => ds.UpdateInstancePorts(
                        _expectedInstance,
                        It.Is<IList<FirewallPort>>(
                            toEnable => toEnable.Single().Port == _expectedPortInfo.Port &&
                                toEnable.Single().Name == _expectedPortInfo.GetTag(ExpectedInstanceName)),
                        It.Is<IList<FirewallPort>>(l => l.Count == 0)))
                .Returns(new GceOperation(OperationType.ModifyingFirewall, "", "", "") { OperationTask = Task.CompletedTask })
                .Verifiable();

            var objectUnderTest = new AttachDebuggerFirewallPort(
                _expectedPortInfo,
                DefaultDescription,
                _defaultInstance,
                _dataSourceMock.ToLazy());
            await objectUnderTest.EnablePortAsync();

            _dataSourceMock.Verify();
        }

        [TestMethod]
        public async Task TestEnablePort_TracksRefreshInstanceTask()
        {
            var tcs = new TaskCompletionSource<Instance>();
            _dataSourceMock.Setup(ds => ds.RefreshInstance(It.IsAny<Instance>()))
                .Returns(tcs.Task);
            _dataSourceMock
                .Setup(
                    ds => ds.UpdateInstancePorts(
                        It.IsAny<Instance>(),
                        It.IsAny<IList<FirewallPort>>(),
                        It.IsAny<IList<FirewallPort>>()))
                .Returns(
                    new GceOperation(OperationType.ModifyingFirewall, "", "", "") { OperationTask = Task.CompletedTask });

            var objectUnderTest = new AttachDebuggerFirewallPort(
                _defaultPortInfo,
                DefaultDescription,
                _defaultInstance,
                _dataSourceMock.ToLazy());
            Task task = objectUnderTest.EnablePortAsync();

            Assert.IsFalse(task.IsCompleted);

            tcs.SetResult(_defaultInstance);

            await task;
        }

        [TestMethod]
        public async Task TestEnablePort_TracksUpdatePortsTask()
        {
            var tcs = new TaskCompletionSource<object>();
            _dataSourceMock.Setup(ds => ds.RefreshInstance(It.IsAny<Instance>()))
                .Returns(Task.FromResult(_defaultInstance));
            _dataSourceMock
                .Setup(
                    ds => ds.UpdateInstancePorts(
                        It.IsAny<Instance>(),
                        It.IsAny<IList<FirewallPort>>(),
                        It.IsAny<IList<FirewallPort>>()))
                .Returns(
                    new GceOperation(OperationType.ModifyingFirewall, "", "", "") { OperationTask = tcs.Task });

            var objectUnderTest = new AttachDebuggerFirewallPort(
                _defaultPortInfo,
                DefaultDescription,
                _defaultInstance,
                _dataSourceMock.ToLazy());
            Task task = objectUnderTest.EnablePortAsync();

            Assert.IsFalse(task.IsCompleted);

            tcs.SetResult(null);

            await task;
        }

        [TestMethod]
        public async Task TestIsPortEnabled_ShortCircutFalse()
        {
            _expectedInstance.Tags = new Tags { Items = new List<string>() };
            var objectUnderTests = new AttachDebuggerFirewallPort(
                _expectedPortInfo,
                DefaultDescription,
                _expectedInstance,
                _dataSourceMock.ToLazy());

            bool result = await objectUnderTests.IsPortEnabledAsync();

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestIsPortEnabled_False()
        {
            _dataSourceMock.Setup(ds => ds.GetFirewallListAsync())
                .Returns(Task.FromResult<IList<Firewall>>(new List<Firewall>()));
            _expectedInstance.Tags =
                new Tags { Items = new List<string> { _expectedPortInfo.GetTag(ExpectedInstanceName) } };
            var objectUnderTests = new AttachDebuggerFirewallPort(
                _expectedPortInfo,
                DefaultDescription,
                _expectedInstance,
                _dataSourceMock.ToLazy());

            bool result = await objectUnderTests.IsPortEnabledAsync();
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestIsPortEnabled_True()
        {
            string targetTag = _expectedPortInfo.GetTag(ExpectedInstanceName);
            _dataSourceMock.Setup(ds => ds.GetFirewallListAsync())
                .Returns(
                    Task.FromResult<IList<Firewall>>(
                        new List<Firewall>
                        {
                            new Firewall
                            {
                                TargetTags = new List<string> {targetTag},
                                Allowed = new List<Firewall.AllowedData>
                                {
                                    new Firewall.AllowedData
                                    {
                                        IPProtocol = "tcp",
                                        Ports = new List<string> {_expectedPortInfo.Port.ToString()}
                                    }
                                }
                            }
                        }));
            var objectUnderTests = new AttachDebuggerFirewallPort(
                _expectedPortInfo,
                DefaultDescription,
                PortTestHelpers.GetInstanceWithEnabledPort(_expectedPortInfo, ExpectedInstanceName),
                _dataSourceMock.ToLazy());

            bool result = await objectUnderTests.IsPortEnabledAsync();
            Assert.IsTrue(result);
        }
    }
}
