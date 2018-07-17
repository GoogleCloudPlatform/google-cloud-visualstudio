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

using EnvDTE;
using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Deployment
{
    [TestClass]
    public class WindowsVmDeploymentTests : ExtensionTestBase
    {
        private const string DefaultUser = "DefaultUser";
        private const string DefaultPassword = "DefaultPassword";
        private const string DefaultWebSite = "Default Web Site";
        private const string DefaultUniqueName = "DefaultUniqueName";
        private const string DefaultConfigurationName = "Default Configuration Name";

        private static readonly WindowsInstanceCredentials s_defaultCredentials =
            new WindowsInstanceCredentials(DefaultUser, DefaultPassword);

        private static readonly Instance s_defaultInstance = new Instance
        {
            NetworkInterfaces = new[]
            {
                new NetworkInterface {AccessConfigs = new[] {new AccessConfig {NatIP = "DefaultIp"}}}
            }
        };

        private WindowsVmDeployment _objectUnderTest;
        private Mock<IProcessService> _processServiceMock;
        private Mock<IParsedDteProject> _dteProjectMock;
        private Mock<Configuration> _activeConfigMock;
        private Mock<SolutionBuild> _solutionBuildMock;

        private Mock<IToolsPathProvider> _toolsPathProviderMock;
        private Lazy<IToolsPathProvider> _oldToolsPathLazy;

        private Task<bool> _runCommandTask;
        private string _path;
        private string _parameters;
        private EventHandler<OutputHandlerEventArgs> _handler;
        private Mock<IGcpOutputWindow> _gcpOutputWindowMock;
        private Mock<IStatusbarService> _statusbarServiceMock;
        private Mock<IShellUtils> _shellUtilsMock;

        protected override void BeforeEach()
        {

            _toolsPathProviderMock = new Mock<IToolsPathProvider>();
            _oldToolsPathLazy = VsVersionUtils.s_toolsPathProvider;
            VsVersionUtils.s_toolsPathProvider = _toolsPathProviderMock.ToLazy();

            _solutionBuildMock = new Mock<SolutionBuild>();
            _activeConfigMock = new Mock<Configuration>();
            _activeConfigMock.Setup(c => c.ConfigurationName).Returns(DefaultConfigurationName);

            _dteProjectMock = new Mock<IParsedDteProject> { DefaultValue = DefaultValue.Mock };
            _dteProjectMock.Setup(p => p.Project.DTE.Solution.SolutionBuild).Returns(_solutionBuildMock.Object);
            _dteProjectMock.Setup(p => p.Project.ConfigurationManager.ActiveConfiguration)
                .Returns(_activeConfigMock.Object);
            _dteProjectMock.Setup(p => p.Project.UniqueName).Returns(DefaultUniqueName);

            _runCommandTask = Task.FromResult(true);
            _processServiceMock = new Mock<IProcessService>();
            _processServiceMock
                .Setup(
                    ps => ps.RunCommandAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<EventHandler<OutputHandlerEventArgs>>(),
                        It.IsAny<string>(),
                        It.IsAny<IDictionary<string, string>>()))
                .Callback<string, string, EventHandler<OutputHandlerEventArgs>, string, IDictionary<string, string>>(
                    (path, parameters, handler, workingDir, env) =>
                    {
                        _path = path;
                        _parameters = parameters;
                        _handler = handler;
                    })
                .Returns(() => _runCommandTask);
            _gcpOutputWindowMock = new Mock<IGcpOutputWindow>();
            _statusbarServiceMock = new Mock<IStatusbarService> { DefaultValue = DefaultValue.Mock };
            _shellUtilsMock = new Mock<IShellUtils> { DefaultValue = DefaultValue.Mock };
            _objectUnderTest = new WindowsVmDeployment(
                _processServiceMock.ToLazy(),
                _shellUtilsMock.ToLazy(),
                _statusbarServiceMock.ToLazy(),
                _gcpOutputWindowMock.ToLazy());
        }

        protected override void AfterEach() => VsVersionUtils.s_toolsPathProvider = _oldToolsPathLazy;

        [TestMethod]
        public async Task TestPublishProjectAsync_RunsProjectBuildForGivenConfiguration()
        {
            const string uniqueProjectName = "Unique Project Name";
            const string expectedConfiguration = "ExpectedConfiguration";
            _dteProjectMock.Setup(p => p.Project.UniqueName).Returns(uniqueProjectName);

            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                expectedConfiguration);

            _solutionBuildMock.Verify(sb => sb.BuildProject(expectedConfiguration, uniqueProjectName, true));
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_GetsMSBuildPathFromToolsPathProvider()
        {
            const string expectedMSBuildPath = "Expected MSBuild Path";
            _toolsPathProviderMock.Setup(tpp => tpp.GetMsbuildPath()).Returns(expectedMSBuildPath);


            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            Assert.AreEqual(expectedMSBuildPath, _path);
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_ParametersStartWithProjectPath()
        {
            const string expectedProjectPath = "Expected MSBuild Path";
            _dteProjectMock.Setup(p => p.FullPath).Returns(expectedProjectPath);

            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            StringAssert.StartsWith(_parameters, '"' + expectedProjectPath + '"');
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_ParametersIncludeWebPublishTargetForAspNetApp()
        {
            _dteProjectMock.Setup(p => p.ProjectType).Returns(KnownProjectTypes.WebApplication);

            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            StringAssert.Contains(_parameters, "/t:WebPublish");
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_ParametersIncludePublishTargetForAspNetCoreApp()
        {
            _dteProjectMock.Setup(p => p.ProjectType).Returns(KnownProjectTypes.NetCoreWebApplication);

            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            StringAssert.Contains(_parameters, "/t:Publish");
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_ParametersIncludeWebPublishMethodProperty()
        {
            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            StringAssert.Contains(_parameters, "/p:WebPublishMethod=\"MSDeploy\"");
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_ParametersIncludeMSDeployPublishMethodProperty()
        {
            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            StringAssert.Contains(_parameters, "/p:MSDeployPublishMethod=\"WMSVC\"");
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_ParametersIncludeConfigurationProperty()
        {
            const string expectedConfiguration = "ExpectedConfiguration";
            const string expectedConfigurationArgument = "/p:Configuration=\"ExpectedConfiguration\"";

            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                expectedConfiguration);

            StringAssert.Contains(_parameters, expectedConfigurationArgument);
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_ParametersIncludeMSDeployServiceURLProperty()
        {
            const string expectedPublicIp = "Expected Public Ip";
            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                new Instance
                {
                    NetworkInterfaces = new[]
                    {
                        new NetworkInterface {AccessConfigs = new[] {new AccessConfig {NatIP = expectedPublicIp}}}
                    }
                },
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            StringAssert.Contains(_parameters, $"/p:MSDeployServiceURL=\"{expectedPublicIp}\"");
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_ParametersIncludeDeployIisAppPathProperty()
        {
            const string expectedTargetWebSite = "Expected Target Web Site";
            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                expectedTargetWebSite,
                DefaultConfigurationName);

            StringAssert.Contains(_parameters, $"/p:DeployIisAppPath=\"{expectedTargetWebSite}\"");
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_ParametersIncludeUserNameProperty()
        {
            const string expectedUserName = "Expected User Name";
            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                new WindowsInstanceCredentials(expectedUserName, DefaultPassword),
                DefaultWebSite,
                DefaultConfigurationName);

            StringAssert.Contains(_parameters, $"/p:UserName=\"{expectedUserName}\"");
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_ParametersIncludePasswordProperty()
        {
            const string expectedPassword = "Expected Password";
            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                new WindowsInstanceCredentials(DefaultUser, expectedPassword),
                DefaultWebSite,
                DefaultConfigurationName);

            StringAssert.Contains(_parameters, $"/p:Password=\"{expectedPassword}\"");
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_ParametersIncludeAllowUntrustedCertificateProperty()
        {
            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            StringAssert.Contains(_parameters, "/p:AllowUntrustedCertificate=\"True\"");
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_OutputHandlerWritesToGcpOutputWindow()
        {
            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            Assert.AreEqual(_handler.Target, _gcpOutputWindowMock.Object);
        }

        [TestMethod]
        public void TestPublishProjectAsync_AwaitsRunCommand()
        {
            var taskSource = new TaskCompletionSource<bool>();
            _runCommandTask = taskSource.Task;

            Task<bool> t = _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            Assert.IsFalse(t.IsCompleted);
        }

        [TestMethod]
        public void TestPublishProjectAsync_FreezesStatusbarText()
        {
            const string expectedName = "Expected Name";
            var taskSource = new TaskCompletionSource<bool>();
            _runCommandTask = taskSource.Task;

            _ = _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                new Instance { Name = expectedName },
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            _statusbarServiceMock.Verify(
                sb => sb.FreezeText(string.Format(Resources.GcePublishProgressMessage, expectedName)));
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_UnFreezesStatusbarText()
        {
            const string expectedName = "Expected Name";

            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                new Instance { Name = expectedName },
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            _statusbarServiceMock.Verify(
                sb => sb.FreezeText(string.Format(Resources.GcePublishProgressMessage, expectedName)).Dispose());
        }

        [TestMethod]
        public void TestPublishProjectAsync_ShowsDeployAnimation()
        {
            var taskSource = new TaskCompletionSource<bool>();
            _runCommandTask = taskSource.Task;

            _ = _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            _statusbarServiceMock.Verify(sb => sb.ShowDeployAnimation());
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_EndsDeployAnimation()
        {
            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            _statusbarServiceMock.Verify(sb => sb.ShowDeployAnimation().Dispose());
        }

        [TestMethod]
        public void TestPublishProjectAsync_SetsUIBusy()
        {
            var taskSource = new TaskCompletionSource<bool>();
            _runCommandTask = taskSource.Task;

            _ = _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            _shellUtilsMock.Verify(s => s.SetShellUIBusy());
        }

        [TestMethod]
        public async Task TestPublishProjectAsync_UnsetsUIBusy()
        {
            await _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);

            _shellUtilsMock.Verify(s => s.SetShellUIBusy().Dispose());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestPublishProjectAsync_ReturnsResultOfRunCommand(bool result)
        {
            var taskSource = new TaskCompletionSource<bool>();
            _runCommandTask = taskSource.Task;

            Task<bool> t = _objectUnderTest.PublishProjectAsync(
                _dteProjectMock.Object,
                s_defaultInstance,
                s_defaultCredentials,
                DefaultWebSite,
                DefaultConfigurationName);
            await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
            taskSource.SetResult(result);

            Assert.AreEqual(result, await t);
        }
    }
}
