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

using GoogleCloudExtension;
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
        private const string ExpectedPublicIp = "expected-public-ip";
        private const string ExpectedClusterIp = "expected-cluster-ip";
        private const string DefaultPulblicIp = "default-pulblic-ip";
        private GkeDeploymentService _objectUnderTest;
        private Mock<IGcpOutputWindow> _gcpOutputWindowMock;
        private Mock<IStatusbarService> _statusbarServiceMock;
        private Mock<IShellUtils> _shellUtilsMock;
        private Mock<IBrowserService> _browserMock;
        private Mock<IToolsPathProvider> _toolsPathProviderMock;
        private Mock<INetCoreAppUtils> _netCoreAppUtilsMock;
        private Mock<IKubectlContext> _kubectlContextMock;
        private IParsedProject _mockedParsedProject;
        private static readonly GkeDeployment s_defaultExistingDeployment = new GkeDeployment { Spec = new GkeSpec { Replicas = DefaultReplicas } };
        private Mock<IDisposable> _disposableMock;

        protected override void BeforeEach()
        {
            _disposableMock = new Mock<IDisposable>();
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
                        It.IsAny<Func<string, OutputStream, Task>>(),
                        It.IsAny<string>()))
                .ReturnsResult(true);
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
                .Setup(g => g.BuildContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, Task>>()))
                .ReturnsResult(true);
            _kubectlContextMock
                .Setup(
                    g => g.CreateDeploymentAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<Action<string>>()))
                .ReturnsResult(true);
            _kubectlContextMock
                .Setup(
                    g => g.UpdateDeploymentImageAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<string>>()))
                .ReturnsResult(true);
            _kubectlContextMock.Setup(
                    k => k.ScaleDeploymentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Action<string>>()))
                .ReturnsResult(true);
            _kubectlContextMock.Setup(k => k.DeleteServiceAsync(It.IsAny<string>(), It.IsAny<Action<string>>()))
                .ReturnsResult(true);
            _kubectlContextMock
                .Setup(k => k.ExposeServiceAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Action<string>>()))
                .ReturnsResult(true);
            _kubectlContextMock.Setup(k => k.GetPublicServiceIpAsync(It.IsAny<string>()))
                .ReturnsResult(DefaultPulblicIp);
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

            _gcpOutputWindowMock.Verify(o => o.ClearAsync());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_ActivatesGcpOutput()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _gcpOutputWindowMock.Verify(o => o.ActivateAsync());
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

            _statusbarServiceMock.Verify(s => s.FreezeAsync());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_UnFreezesStatusbar()
        {
            _statusbarServiceMock.Setup(s => s.FreezeAsync()).ReturnsResult(_disposableMock.Object);

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _disposableMock.Verify(d => d.Dispose());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_StartsStatusbarAnimation()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _statusbarServiceMock.Verify(s => s.ShowDeployAnimationAsync());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_StopsStatusbarAnimation()
        {
            _statusbarServiceMock.Setup(s => s.ShowDeployAnimationAsync()).ReturnsResult(_disposableMock.Object);

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _disposableMock.Verify(d => d.Dispose());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_ShowsStatusbarProgress()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _statusbarServiceMock.Verify(s => s.ShowProgressBarAsync(It.IsAny<string>()));
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_RemovesStatusbarProgress()
        {
            var disposableProgress = new Mock<IDisposableProgress>();
            _statusbarServiceMock.Setup(s => s.ShowProgressBarAsync(It.IsAny<string>()))
                .ReturnsResult(disposableProgress.Object);

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            disposableProgress.Verify(dp => dp.Dispose());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_SetsShellUiBusy()
        {
            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _shellUtilsMock.Verify(s => s.SetShellUIBusyAsync());
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_CancelsShellUiBusy()
        {
            _shellUtilsMock.Setup(s => s.SetShellUIBusyAsync()).ReturnsResult(_disposableMock.Object);

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _disposableMock.Verify(d => d.Dispose());
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
                    _gcpOutputWindowMock.Object.OutputLineAsync,
                    ExpectedConfiguration));
        }

        [TestMethod]
        public async Task TestBuildImageAsync_FailingAppBundleBuildFailsDeployment()
        {
            _netCoreAppUtilsMock
                .Setup(
                    n => n.CreateAppBundleAsync(
                        It.IsAny<IParsedProject>(),
                        It.IsAny<string>(),
                        It.IsAny<Func<string, OutputStream, Task>>(),
                        It.IsAny<string>()))
                .ReturnsResult(false);

            await _objectUnderTest.DeployProjectToGkeAsync(
                _mockedParsedProject,
                GetOptionsBuilder().SetConfiguration(ExpectedConfiguration).Build());

            _statusbarServiceMock.Verify(s => s.SetTextAsync(Resources.PublishFailureStatusMessage));
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
                    _gcpOutputWindowMock.Object.OutputLineAsync));
        }

        [TestMethod]
        public async Task TestBuildImageAsync_BuildContainerFailureFailsDeployment()
        {
            _kubectlContextMock
                .Setup(g => g.BuildContainerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, Task>>()))
                .ReturnsResult(false);

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _statusbarServiceMock.Verify(s => s.SetTextAsync(Resources.PublishFailureStatusMessage));
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
        public async Task TestCreateOrUpdateDeploymentAsync_CreateDeploymentFailureFailsDeployment()
        {
            GkeDeploymentService.Options options = GetOptionsBuilder().SetExistingDeployment(null).Build();
            _kubectlContextMock.Setup(
                    g => g.CreateDeploymentAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<Action<string>>()))
                .ReturnsResult(false);

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _statusbarServiceMock.Verify(s => s.SetTextAsync(Resources.PublishFailureStatusMessage));
        }

        [TestMethod]
        public async Task TestCreateOrUpdateDeploymentAsync_UpdatesDeploymentImageForExisting()
        {
            _kubectlContextMock.Setup(k => k.ProjectId).Returns(ExpectedProjectId);
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetDeploymentName(ExpectedDeploymentName)
                .SetDeploymentVersion(ExpectedDeploymentVersion)
                .SetExistingDeployment(s_defaultExistingDeployment)
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
        public async Task TestCreateOrUpdateDeploymentAsync_UpdateDeploymentImageFailureFailsDeployment()
        {
            _kubectlContextMock.Setup(
                    g => g.UpdateDeploymentImageAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Action<string>>()))
                .ReturnsResult(false);
            GkeDeploymentService.Options options =
                GetOptionsBuilder().SetExistingDeployment(s_defaultExistingDeployment).Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _statusbarServiceMock.Verify(s => s.SetTextAsync(Resources.PublishFailureStatusMessage));
        }

        [TestMethod]
        public async Task TestCreateOrUpdateDeploymentAsync_ScalesExistingDeployment()
        {
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetDeploymentName(ExpectedDeploymentName)
                .SetReplicas(ExpectedReplicas)
                .SetExistingDeployment(s_defaultExistingDeployment)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _kubectlContextMock.Verify(
                g => g.ScaleDeploymentAsync(
                    ExpectedDeploymentName,
                    ExpectedReplicas,
                    _gcpOutputWindowMock.Object.OutputLine));
        }

        [TestMethod]
        public async Task TestCreateOrUpdateDeploymentAsync_ScaleDeploymentFailureFailsDeployment()
        {
            _kubectlContextMock.Setup(
                    g => g.ScaleDeploymentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Action<string>>()))
                .ReturnsResult(false);
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetReplicas(ExpectedReplicas)
                .SetExistingDeployment(s_defaultExistingDeployment)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _statusbarServiceMock.Verify(s => s.SetTextAsync(Resources.PublishFailureStatusMessage));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestExposeNewServiceAsync_ExposesNewServiceWhenMissingAndRequested(bool exposePublicService)
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
        public async Task TestExposeNewServiceAsync_ExposeServiceFailureFailsDeployment()
        {
            _kubectlContextMock
                .Setup(k => k.ExposeServiceAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Action<string>>()))
                .ReturnsResult(false);
            _kubectlContextMock.Setup(k => k.GetServiceAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<GkeService>(null));

            await _objectUnderTest.DeployProjectToGkeAsync(
                _mockedParsedProject,
                GetOptionsBuilder().SetExposeService(true).Build());

            _statusbarServiceMock.Verify(s => s.SetTextAsync(Resources.PublishFailureStatusMessage));
        }

        [TestMethod]
        public async Task TestUpdateExistingServiceAsync_DoesNotUpdateWhenAlreadyCorrectType()
        {
            _kubectlContextMock.Setup(k => k.GetServiceAsync(It.IsAny<string>()))
                .ReturnsResult(
                    new GkeService
                    {
                        Spec = new GkeServiceSpec { Type = GkeServiceSpec.ClusterIpType },
                        Metadata = new GkeMetadata { Name = ExpectedDeploymentName }
                    });
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(true)
                .SetExposePublicService(false)
                .SetDeploymentName(ExpectedDeploymentName)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _kubectlContextMock.Verify(
                k => k.DeleteServiceAsync(It.IsAny<string>(), It.IsAny<Action<string>>()), Times.Never);
            _kubectlContextMock.Verify(
                k => k.ExposeServiceAsync(
                    It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Action<string>>()), Times.Never);
        }

        [TestMethod]
        public async Task TestUpdateExistingServiceAsync_RecreatesServiceWhenWrongType()
        {
            _kubectlContextMock.Setup(k => k.GetServiceAsync(It.IsAny<string>()))
                .ReturnsResult(
                    new GkeService
                    {
                        Spec = new GkeServiceSpec { Type = GkeServiceSpec.LoadBalancerType },
                        Metadata = new GkeMetadata { Name = ExpectedDeploymentName }
                    });
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(true)
                .SetExposePublicService(false)
                .SetDeploymentName(ExpectedDeploymentName)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _kubectlContextMock.Verify(
                k => k.DeleteServiceAsync(ExpectedDeploymentName, _gcpOutputWindowMock.Object.OutputLine));
            _kubectlContextMock.Verify(
                k => k.ExposeServiceAsync(
                    ExpectedDeploymentName,
                    false,
                    _gcpOutputWindowMock.Object.OutputLine));
        }

        [TestMethod]
        public async Task TestUpdateExistingServiceAsync_FailureToDeleteServiceFailsDeployment()
        {
            _kubectlContextMock.Setup(
                    k => k.DeleteServiceAsync(It.IsAny<string>(), It.IsAny<Action<string>>()))
                .ReturnsResult(false);
            _kubectlContextMock.Setup(k => k.GetServiceAsync(It.IsAny<string>()))
                .ReturnsResult(
                    new GkeService
                    {
                        Spec = new GkeServiceSpec { Type = GkeServiceSpec.LoadBalancerType },
                        Metadata = new GkeMetadata { Name = DefaultDeploymentName }
                    });

            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(true)
                .SetExposePublicService(false)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _statusbarServiceMock.Verify(s => s.SetTextAsync(Resources.PublishFailureStatusMessage));
        }

        [TestMethod]
        public async Task TestDeleteExistingServiceAsync_DeletesExistingServiceWhenNotRequested()
        {
            _kubectlContextMock.Setup(k => k.GetServiceAsync(It.IsAny<string>()))
                .ReturnsResult(
                    new GkeService
                    {
                        Spec = new GkeServiceSpec { Type = GkeServiceSpec.LoadBalancerType },
                        Metadata = new GkeMetadata { Name = ExpectedDeploymentName }
                    });
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(false)
                .SetDeploymentName(ExpectedDeploymentName)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _kubectlContextMock.Verify(
                k => k.DeleteServiceAsync(ExpectedDeploymentName, _gcpOutputWindowMock.Object.OutputLine));
        }

        [TestMethod]
        public async Task TestDeleteExistingServiceAsync_DeleteServiceFailureFailsDeployment()
        {
            _kubectlContextMock.Setup(
                    k => k.DeleteServiceAsync(It.IsAny<string>(), It.IsAny<Action<string>>()))
                .ReturnsResult(false);
            _kubectlContextMock.Setup(k => k.GetServiceAsync(It.IsAny<string>()))
                .ReturnsResult(
                    new GkeService
                    {
                        Spec = new GkeServiceSpec { Type = GkeServiceSpec.LoadBalancerType },
                        Metadata = new GkeMetadata { Name = ExpectedDeploymentName }
                    });
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(false)
                .SetDeploymentName(ExpectedDeploymentName)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _statusbarServiceMock.Verify(s => s.SetTextAsync(Resources.PublishFailureStatusMessage));

        }

        [TestMethod]
        public async Task TestOutputResultData_OutputsPublicIpMessageWhenExposedServiceIsPublic()
        {
            _kubectlContextMock.Setup(k => k.GetPublicServiceIpAsync(It.IsAny<string>()))
                .ReturnsResult(ExpectedPublicIp);
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(true)
                .SetExposePublicService(true)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _gcpOutputWindowMock.Verify(o => o.OutputLine(It.Is<string>(s => s.Contains(ExpectedPublicIp))));
        }

        [TestMethod]
        public async Task TestOutputResultData_OutputsClusterIpMessageWhenExposedServiceIsPrivate()
        {
            _kubectlContextMock.Setup(k => k.GetServiceClusterIpAsync(It.IsAny<string>()))
                .ReturnsResult(ExpectedClusterIp);
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(true)
                .SetExposePublicService(false)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _gcpOutputWindowMock.Verify(o => o.OutputLine(It.Is<string>(s => s.Contains(ExpectedClusterIp))));
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_OpensBrowserWhenRequested()
        {
            _kubectlContextMock.Setup(k => k.GetPublicServiceIpAsync(It.IsAny<string>()))
                .ReturnsResult(ExpectedPublicIp);
            GkeDeploymentService.Options options = GetOptionsBuilder()
                .SetExposeService(true)
                .SetExposePublicService(true)
                .SetOpenWebsite(true)
                .Build();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, options);

            _browserMock.Verify(b => b.OpenBrowser(It.Is<string>(s => s.Contains(ExpectedPublicIp))));
        }

        [TestMethod]
        public async Task TestDeployProjectToGkeAsync_FailsBuildOnException()
        {
            _gcpOutputWindowMock.Setup(o => o.ClearAsync()).Throws<Exception>();

            await _objectUnderTest.DeployProjectToGkeAsync(_mockedParsedProject, GetOptionsBuilder().Build());

            _statusbarServiceMock.Verify(s => s.SetTextAsync(Resources.PublishFailureStatusMessage));
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
