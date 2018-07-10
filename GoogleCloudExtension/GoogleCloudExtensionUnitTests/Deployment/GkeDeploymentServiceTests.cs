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

using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Deployment
{
    [TestClass]
    public class GkeDeploymentServiceTests : ExtensionTestBase
    {
        private const string DefaultDeploymentName = "default-deployment-name";
        private const string DefaultDeploymentVersion = "default-deployment-version";
        private const string DefaultConfiguration = "default-configuration";
        private const int DefaultReplicas = 4;
        private const string ExpectedConfiguration = "selected-configuration";
        private const string ExpectedDeploymentName = "expected-deployment-name";
        private const string ExpectedDeploymentVersion = "expected-deployment-version";
        private const string ExpectedProjectId = "expected-project-id";
        private const int ExpectedReplicas = 20;
        private const string ExpectedPublicIP = "expected-public-ip";
        private const string ExpectedClusterIP = "expected-cluster-ip";
        private GkeDeploymentService _objectUnderTest;
        private Mock<IGcpOutputWindow> _gcpOutputWindowMock;
        private Mock<IStatusbarService> _statusbarServiceMock;
        private Mock<IShellUtils> _shellUtilsMock;
        private Mock<IBrowserService> _browserMock;
        private Mock<IToolsPathProvider> _toolsPathProviderMock;
        private Mock<INetCoreAppUtils> _netCoreAppUtilsMock;
        private Mock<IKubectlContext> _kubectlContextMock;
        private IParsedProject _mockedParsedProject;

        protected override void BeforeEach()
        {
            _toolsPathProviderMock = new Mock<IToolsPathProvider>();
            _gcpOutputWindowMock = new Mock<IGcpOutputWindow>();
            _statusbarServiceMock = new Mock<IStatusbarService> { DefaultValueProvider = DefaultValueProvider.Mock };
            _shellUtilsMock = new Mock<IShellUtils> { DefaultValueProvider = DefaultValueProvider.Mock };
            _browserMock = new Mock<IBrowserService>();
            _netCoreAppUtilsMock = new Mock<INetCoreAppUtils>();
            _netCoreAppUtilsMock
                .Setup(
                    n => n.CreateAppBundleAsync(
                        It.IsAny<IParsedProject>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<string>>(),
                        It.IsAny<string>()))
                .Returns(Task.FromResult(true));
            _netCoreAppUtilsMock.Setup(n => n.CopyOrCreateDockerfile(It.IsAny<IParsedProject>(), It.IsAny<string>()));

            VsVersionUtils.s_toolsPathProviderOverride = _toolsPathProviderMock.Object;

            _objectUnderTest = new GkeDeploymentService(
                _gcpOutputWindowMock.ToLazy(),
                _statusbarServiceMock.ToLazy(),
                _shellUtilsMock.ToLazy(),
                _browserMock.ToLazy(),
                _netCoreAppUtilsMock.ToLazy());

            _kubectlContextMock = new Mock<IKubectlContext>();
            _kubectlContextMock
                .Setup(g => g.BuildContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string>>()))
                .Returns(Task.FromResult(true));
            _kubectlContextMock
                .Setup(
                    g => g.CreateDeploymentAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<Action<string>>()))
                .Returns(Task.FromResult(true));
            _kubectlContextMock
                .Setup(
                    g => g.UpdateDeploymentImageAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<string>>()))
                .Returns(Task.FromResult(true));
            _kubectlContextMock.Setup(
                    k => k.ScaleDeploymentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Action<string>>()))
                .Returns(Task.FromResult(true));
            _kubectlContextMock.Setup(k => k.DeleteServiceAsync(It.IsAny<string>(), It.IsAny<Action<string>>()))
                .Returns(Task.FromResult(true));
            _kubectlContextMock
                .Setup(k => k.ExposeServiceAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Action<string>>()))
                .Returns(Task.FromResult(true));
            _mockedParsedProject = Mock.Of<IParsedProject>();
        }

        protected override void AfterEach()
        {
            VsVersionUtils.s_toolsPathProviderOverride = null;
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_ClearsGcpOutput()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _gcpOutputWindowMock.Verify(o => o.Clear());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_ActivatesGcpOutput()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _gcpOutputWindowMock.Verify(o => o.Activate());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_WritesDeploymentMessage()
        {
            const string expectedProjectName = "ExpectedProjectName";
            await _objectUnderTest.DeployProjectToGkeAsync(
                Mock.Of<IParsedProject>(p => p.Name == expectedProjectName),
                GetOptionsBuilder().Build());

            _gcpOutputWindowMock.Verify(o => o.OutputLine(It.Is<string>(s => s.Contains(expectedProjectName))));
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_FreezesStatusbar()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _statusbarServiceMock.Verify(s => s.Freeze());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_UnFreezesStatusbar()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _statusbarServiceMock.Verify(s => s.Freeze().Dispose());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_StartsStatusbarAnimation()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _statusbarServiceMock.Verify(s => s.ShowDeployAnimation());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_StopsStatusbarAnimation()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _statusbarServiceMock.Verify(s => s.ShowDeployAnimation().Dispose());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_ShowsStatusbarProgress()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _statusbarServiceMock.Verify(s => s.ShowProgressBar(It.IsAny<string>()));
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_RemovesStatusbarProgress()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _statusbarServiceMock.Verify(s => s.ShowProgressBar(It.IsAny<string>()).Dispose());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_SetsShellUiBusy()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _shellUtilsMock.Verify(s => s.SetShellUIBusy());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_CancelsShellUiBusy()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _shellUtilsMock.Verify(s => s.SetShellUIBusy().Dispose());
        }

        [TestMethod]
        public async Task TestBuildImageAsync_SavesFiles()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _shellUtilsMock.Verify(s => s.SaveAllFiles());
        }

        [TestMethod]
        public async Task TestBuildImageAsync_BuildsAppBundle()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(
                _mockedParsedProject,
                GetOptionsBuilder().SetConfiguration(ExpectedConfiguration).Build());

            _netCoreAppUtilsMock.Verify(
                n => n.CreateAppBundleAsync(
                    _mockedParsedProject,
                    It.IsAny<string>(),
                    _gcpOutputWindowMock.Object.OutputLine,
                    ExpectedConfiguration));
        }

        [TestMethod]
        public async Task TestBuildImageAsync_CreatesDockerFile()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _netCoreAppUtilsMock.Verify(n => n.CopyOrCreateDockerfile(_mockedParsedProject, It.IsAny<string>()));
        }

        [TestMethod]
        public async Task TestBuildImageAsync_BuildsContainer()
        {
            _kubectlContextMock.Setup(k => k.ProjectId).Returns(ExpectedProjectId);
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetDeploymentName(ExpectedDeploymentName)
                .SetDeploymentVersion(ExpectedDeploymentVersion)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _kubectlContextMock.Verify(
                g => g.BuildContainerAsync(
                    It.Is<string>(
                        s => s.Contains(ExpectedProjectId) &&
                            s.Contains(ExpectedDeploymentName) &&
                            s.Contains(ExpectedDeploymentVersion)),
                    It.IsAny<string>(),
                    _gcpOutputWindowMock.Object.OutputLine));
        }

        [TestMethod]
        public async Task TestCreateOrUpdateDeploymentAsync_CreatesDeploymentForNotExisting()
        {
            _kubectlContextMock.Setup(k => k.ProjectId).Returns(ExpectedProjectId);
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetDeploymentName(ExpectedDeploymentName)
                .SetDeploymentVersion(ExpectedDeploymentVersion)
                .SetExistingDeployment(null)
                .SetReplicas(ExpectedReplicas)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _kubectlContextMock.Verify(
                g => g.CreateDeploymentAsync(
                    ExpectedDeploymentName,
                    It.Is<string>(
                        s => s.Contains(ExpectedProjectId) &&
                            s.Contains(ExpectedDeploymentName) &&
                            s.Contains(ExpectedDeploymentVersion)),
                    ExpectedReplicas,
                    _gcpOutputWindowMock.Object.OutputLine));
        }

        [TestMethod]
        public async Task TestCreateOrUpdateDeploymentAsync_UpdatesDeploymentImageForExisting()
        {
            _kubectlContextMock.Setup(k => k.ProjectId).Returns(ExpectedProjectId);
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetDeploymentName(ExpectedDeploymentName)
                .SetDeploymentVersion(ExpectedDeploymentVersion)
                .SetExistingDeployment(new GkeDeployment { Spec = new GkeSpec { Replicas = DefaultReplicas } })
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _kubectlContextMock.Verify(
                g => g.UpdateDeploymentImageAsync(
                    ExpectedDeploymentName,
                    It.Is<string>(
                        s => s.Contains(ExpectedProjectId) &&
                            s.Contains(ExpectedDeploymentName) &&
                            s.Contains(ExpectedDeploymentVersion)),
                    _gcpOutputWindowMock.Object.OutputLine));
        }

        [TestMethod]
        public async Task TestCreateOrUpdateDeploymentAsync_ScalesExistingDeployment()
        {
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetDeploymentName(ExpectedDeploymentName)
                .SetReplicas(ExpectedReplicas)
                .SetExistingDeployment(new GkeDeployment { Spec = new GkeSpec { Replicas = DefaultReplicas } })
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _kubectlContextMock.Verify(
                g => g.ScaleDeploymentAsync(
                    ExpectedDeploymentName,
                    ExpectedReplicas,
                    _gcpOutputWindowMock.Object.OutputLine));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestUpdateOrExposeServiceAsync_ExposesNewServiceWhenMissingAndRequested(bool exposePublicService)
        {
            _kubectlContextMock.Setup(k => k.GetServiceAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<GkeService>(null));
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(true)
                .SetExposePublicService(exposePublicService)
                .SetDeploymentName(ExpectedDeploymentName)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _kubectlContextMock.Verify(
                k => k.ExposeServiceAsync(
                    ExpectedDeploymentName,
                    exposePublicService,
                    _gcpOutputWindowMock.Object.OutputLine));
        }

        [TestMethod]
        public async Task TestUpdateOrExposeServiceAsync_RecreatesServiceWhenWrongType()
        {
            _kubectlContextMock.Setup(k => k.GetServiceAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(new GkeService { Spec = new GkeServiceSpec { Type = GkeServiceSpec.ClusterIpType } }));
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(true)
                .SetExposePublicService(true)
                .SetDeploymentName(ExpectedDeploymentName)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _kubectlContextMock.Verify(
                k => k.DeleteServiceAsync(ExpectedDeploymentName, _gcpOutputWindowMock.Object.OutputLine));
            _kubectlContextMock.Verify(
                k => k.ExposeServiceAsync(
                    ExpectedDeploymentName,
                    true,
                    _gcpOutputWindowMock.Object.OutputLine));
        }

        [TestMethod]
        public async Task TestOutputResultData_OutputsPublicIpMessageWhenExposedServiceIsPublic()
        {
            _kubectlContextMock.Setup(k => k.GetPublicServiceIpAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(ExpectedPublicIP));
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(true)
                .SetExposePublicService(true)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _gcpOutputWindowMock.Verify(o => o.OutputLine(It.Is<string>(s => s.Contains(ExpectedPublicIP))));
        }

        [TestMethod]
        public async Task TestOutputResultData_OutputsClusterIPMessageWhenExposedServiceIsPrivate()
        {
            _kubectlContextMock.Setup(k => k.GetServiceClusterIpAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(ExpectedClusterIP));
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(true)
                .SetExposePublicService(false)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _gcpOutputWindowMock.Verify(o => o.OutputLine(It.Is<string>(s => s.Contains(ExpectedClusterIP))));
        }

        [TestMethod]
        public async Task TestOutputResultData_OpensBrowserWhenRequested()
        {
            _kubectlContextMock.Setup(k => k.GetPublicServiceIpAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(ExpectedPublicIP));
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(true)
                .SetExposePublicService(true)
                .SetOpenWebsite(true)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _browserMock.Verify(b => b.OpenBrowser(It.Is<string>(s => s.Contains(ExpectedPublicIP))));
        }

        private class GkeDeploymentOptionsBuilder
        {
            private readonly IKubectlContext _context;
            private string _deploymentName = DefaultDeploymentName;
            private string _deploymentVersion = DefaultDeploymentVersion;
            private GkeDeployment _existingDeployment = null;
            private bool _exposeService = false;
            private bool _exposePublicService = false;
            private bool _openWebsite = false;
            private string _configuration = DefaultConfiguration;
            private int _replicas = DefaultReplicas;

            public GkeDeploymentOptionsBuilder(IKubectlContext context)
            {
                _context = context;
            }

            public GkeDeploymentService.Options Build() => new GkeDeploymentService.Options(
                _context,
                _deploymentName,
                _deploymentVersion,
                _existingDeployment,
                _exposeService,
                _exposePublicService,
                _openWebsite,
                _configuration,
                _replicas);

            public GkeDeploymentOptionsBuilder SetConfiguration(string configuration)
            {
                _configuration = configuration;
                return this;
            }

            public GkeDeploymentOptionsBuilder SetDeploymentName(string deploymentName)
            {
                _deploymentName = deploymentName;
                return this;
            }

            public GkeDeploymentOptionsBuilder SetDeploymentVersion(string deploymentVersion)
            {
                _deploymentVersion = deploymentVersion;
                return this;
            }

            public GkeDeploymentOptionsBuilder SetExistingDeployment(GkeDeployment existingDeployment)
            {
                _existingDeployment = existingDeployment;
                return this;
            }

            public GkeDeploymentOptionsBuilder SetReplicas(int replicas)
            {
                _replicas = replicas;
                return this;
            }

            public GkeDeploymentOptionsBuilder SetExposeService(bool exposeService)
            {
                _exposeService = exposeService;
                return this;
            }

            public GkeDeploymentOptionsBuilder SetExposePublicService(bool exposePublicService)
            {
                _exposePublicService = exposePublicService;
                return this;
            }

            public GkeDeploymentOptionsBuilder SetOpenWebsite(bool openWebsite)
            {
                _openWebsite = openWebsite;
                return this;
            }
        }

        private GkeDeploymentOptionsBuilder GetOptionsBuilder() =>
            new GkeDeploymentOptionsBuilder(_kubectlContextMock.Object);
    }
}
