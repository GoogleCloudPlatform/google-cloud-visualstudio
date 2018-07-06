// Copyright 2017 Google Inc. All Rights Reserved.
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

using Google.Apis.Container.v1.Data;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps.Gke;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Services.VsProject;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using GoogleCloudExtensionUnitTests.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.PublishDialog.Steps.Gke
{
    [TestClass]
    public class GkeStepViewModelTests : ExtensionTestBase
    {
        private const string VisualStudioProjectName = "VisualStudioProjectName";
        private const string ClusterCId = "id for c";
        private const string DeploymentName = "deployment-name";

        private static readonly Cluster s_aCluster = new Cluster { Name = "Acluster", SelfLink = "id for a" };
        private static readonly Cluster s_bCluster = new Cluster { Name = "Bcluster", SelfLink = "id for b" };
        private static readonly Cluster s_cCluster = new Cluster { Name = "Ccluster", SelfLink = ClusterCId };
        private static readonly List<Cluster> s_outOfOrderClusters = new List<Cluster>
        {
            s_bCluster, s_aCluster, s_cCluster
        };
        private static readonly List<Cluster> s_inOrderClusters = new List<Cluster>
        {
            s_aCluster, s_bCluster, s_cCluster
        };

        private GkeStepViewModel _objectUnderTest;
        private TaskCompletionSource<IList<Cluster>> _getClusterListTaskSource;
        private List<string> _changedProperties;
        private int _canPublishChangedCount;
        private Mock<IVsProjectPropertyService> _propertyServiceMock;
        private FakeParsedProject _parsedProject;
        private Mock<IBrowserService> _browserServiceMock;
        private Mock<IKubectlContext> _kubectlContextMock;
        private TaskCompletionSource<IList<GkeDeployment>> _getDeploymentsSource;
        private GkeDeployment _deployment;
        private TaskCompletionSource<GCloudValidationResult> _validateGcloudSource;
        private Mock<IGkeDeploymentService> _gkeDeploymentServiceMock;

        [ClassInitialize]
        public static void BeforeAll(TestContext context) =>
            GcpPublishStepsUtils.NowOverride = DateTime.Parse("2088-12-23 01:01:01");

        [ClassCleanup]
        public static void AfterAll() => GcpPublishStepsUtils.NowOverride = null;

        protected override void BeforeEach()
        {
            _validateGcloudSource = new TaskCompletionSource<GCloudValidationResult>();
            GCloudWrapperUtils.ValidateGCloudAsyncOverride =
                Mock.Of<Func<GCloudComponent, Task<GCloudValidationResult>>>(
                    f => f(It.IsAny<GCloudComponent>()) == _validateGcloudSource.Task);

            _deployment = new GkeDeployment { Metadata = new GkeMetadata { Name = DeploymentName } };

            _propertyServiceMock = new Mock<IVsProjectPropertyService>();
            _browserServiceMock = new Mock<IBrowserService>();

            var mockedApiManager = Mock.Of<IApiManager>(
                x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>()) == Task.FromResult(true) &&
                    x.EnableServicesAsync(It.IsAny<IEnumerable<string>>()) == Task.FromResult(true));

            _getClusterListTaskSource = new TaskCompletionSource<IList<Cluster>>();
            var mockedDataSourceFactory = Mock.Of<IDataSourceFactory>(
                dsf => dsf.CreateGkeDataSource().GetClusterListAsync() == _getClusterListTaskSource.Task);

            _kubectlContextMock = new Mock<IKubectlContext>();
            _getDeploymentsSource = new TaskCompletionSource<IList<GkeDeployment>>();
            _kubectlContextMock.Setup(kube => kube.GetDeploymentsAsync()).Returns(_getDeploymentsSource.Task);
            _gkeDeploymentServiceMock = new Mock<IGkeDeploymentService>();
            _gkeDeploymentServiceMock
                .Setup(
                    s => s.DepoloyProjectToGkeAsync(
                        It.IsAny<IParsedProject>(),
                        It.IsAny<GkeDeploymentService.Options>()))
                .Returns(Task.CompletedTask);

            PackageMock.Setup(p => p.GetMefService<IVsProjectPropertyService>()).Returns(_propertyServiceMock.Object);
            PackageMock.Setup(p => p.GetMefService<IBrowserService>()).Returns(_browserServiceMock.Object);
            PackageMock.Setup(p => p.GetMefService<IApiManager>()).Returns(mockedApiManager);
            PackageMock.Setup(p => p.GetMefServiceLazy<IDataSourceFactory>())
                .Returns(new Lazy<IDataSourceFactory>(() => mockedDataSourceFactory));
            PackageMock.Setup(p => p.GetMefService<IKubectlContextProvider>().GetForClusterAsync(It.IsAny<Cluster>()))
                .Returns(_kubectlContextMock.ToTask);
            PackageMock.Setup(p => p.GetMefServiceLazy<IGkeDeploymentService>())
                .Returns(_gkeDeploymentServiceMock.ToLazy);

            _parsedProject = new FakeParsedProject { Name = VisualStudioProjectName };
            _parsedProject.ProjectMock.Setup(p => p.ConfigurationManager.ConfigurationRowNames).Returns(new string[0]);

            _objectUnderTest = new GkeStepViewModel(Mock.Of<IPublishDialog>(pd => pd.Project == _parsedProject));

            _changedProperties = new List<string>();
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
            _objectUnderTest.PublishCommand.CanExecuteChanged += (sender, args) => _canPublishChangedCount++;
        }

        protected override void AfterEach()
        {
            _objectUnderTest.OnFlowFinished();
        }

        [TestMethod]
        public void TestInitialState()
        {
            CollectionAssert.That.IsEmpty(_objectUnderTest.Clusters);
            Assert.IsNull(_objectUnderTest.SelectedCluster);
            Assert.IsNull(_objectUnderTest.DeploymentName);
            Assert.AreEqual(GcpPublishStepsUtils.GetDefaultVersion(), _objectUnderTest.DeploymentVersion);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.ExposeService);
            Assert.IsFalse(_objectUnderTest.ExposePublicService);
            Assert.IsFalse(_objectUnderTest.OpenWebsite);
            Assert.IsFalse(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestSetSelectedCluster()
        {
            _objectUnderTest.SelectedCluster = s_aCluster;

            Assert.AreEqual(s_aCluster, _objectUnderTest.SelectedCluster);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.SelectedCluster));
        }

        [TestMethod]
        public async Task TestSetSelectedCluster_EnablesPublish()
        {
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);
            _validateGcloudSource.SetResult(GCloudValidationResult.Valid);
            _objectUnderTest.OnVisible();
            _objectUnderTest.SelectedCluster = null;
            await _objectUnderTest._verifyGCloudTask.SafeTask;
            _canPublishChangedCount = 0;

            _objectUnderTest.SelectedCluster = s_aCluster;

            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.AreEqual(1, _canPublishChangedCount);
        }

        [TestMethod]
        public async Task TestSetSelectedCluster_ToNullDisablesPublish()
        {
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);
            _validateGcloudSource.SetResult(GCloudValidationResult.Valid);
            _objectUnderTest.OnVisible();
            _objectUnderTest.SelectedCluster = s_aCluster;
            await _objectUnderTest._verifyGCloudTask.SafeTask;
            _canPublishChangedCount = 0;

            _objectUnderTest.SelectedCluster = null;

            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.AreEqual(1, _canPublishChangedCount);
        }

        [TestMethod]
        public async Task TestSetSelectedCluster_ToPlaceholderDisablesPublish()
        {
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);
            _validateGcloudSource.SetResult(GCloudValidationResult.Valid);
            _objectUnderTest.OnVisible();
            _objectUnderTest.SelectedCluster = s_aCluster;
            await _objectUnderTest._verifyGCloudTask.SafeTask;
            _canPublishChangedCount = 0;

            _objectUnderTest.SelectedCluster = GkeStepViewModel.s_placeholderCluster;

            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.AreEqual(1, _canPublishChangedCount);
        }

        [TestMethod]
        public void TestSetSelectedCluster_UpdatesExistingDeployments()
        {
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);
            _objectUnderTest.OnVisible();
            AsyncProperty<IList<GkeDeployment>> initalDeployments = _objectUnderTest.ExistingDeployments;
            _objectUnderTest.SelectedCluster = s_aCluster;

            Assert.AreNotEqual(initalDeployments, _objectUnderTest.ExistingDeployments);
        }

        [TestMethod]
        public void TestSetDeploymentName()
        {
            const string testDeploymentName = "test-deployment-name";

            _objectUnderTest.DeploymentName = testDeploymentName;

            Assert.AreEqual(testDeploymentName, _objectUnderTest.DeploymentName);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.DeploymentName));
        }

        [TestMethod]
        public void TestSetDeploymentName_ValidationInvalid()
        {
            _objectUnderTest.DeploymentName = "(AnInvalidName)";

            Assert.IsTrue(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestSetDeploymentName_ValidationValid()
        {
            _objectUnderTest.DeploymentName = "a-valid-name";

            Assert.IsFalse(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestSetDeploymentName_UpdatesSelectedDeploymentToFoundDeployment()
        {
            _objectUnderTest.ExistingDeployments = new AsyncProperty<IList<GkeDeployment>>(new[] { _deployment });

            _objectUnderTest.DeploymentName = _deployment.Metadata.Name;

            Assert.AreEqual(_deployment, _objectUnderTest.SelectedDeployment);
        }

        [TestMethod]
        public void TestSetDeploymentName_ClearsSelectedDeploymentWhenMissing()
        {
            _objectUnderTest.ExistingDeployments = new AsyncProperty<IList<GkeDeployment>>(new[] { _deployment });

            _objectUnderTest.DeploymentName = "some-other-name";

            Assert.IsNull(_objectUnderTest.SelectedDeployment);
        }

        [TestMethod]
        public void TestSetDeploymentVersion()
        {
            const string testDeploymentVersion = "test-deployment-name";

            _objectUnderTest.DeploymentVersion = testDeploymentVersion;

            Assert.AreEqual(testDeploymentVersion, _objectUnderTest.DeploymentVersion);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.DeploymentVersion));
        }

        [TestMethod]
        public void TestSetDeploymentVersion_ValidationInvalid()
        {
            _objectUnderTest.DeploymentVersion = "(AnInvalidVersion)";

            Assert.IsTrue(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestSetDeploymentVersion_ValidationValid()
        {
            _objectUnderTest.DeploymentVersion = "a-valid-version";

            Assert.IsFalse(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestSetReplicas()
        {
            const string replicas = "some string";

            _objectUnderTest.Replicas = replicas;

            Assert.AreEqual(replicas, _objectUnderTest.Replicas);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.Replicas));
        }

        [TestMethod]
        public void TestSetReplicas_ValidationInvalid()
        {
            _objectUnderTest.Replicas = "(Not A Number)";

            Assert.IsTrue(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestSetReplicas_ValidationValid()
        {
            _objectUnderTest.Replicas = " 5 ";

            Assert.IsFalse(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestSetExposeService()
        {
            _objectUnderTest.ExposeService = true;

            Assert.IsTrue(_objectUnderTest.ExposeService);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.ExposeService));
        }

        [TestMethod]
        public void TestExposePublicService_HiddenByExposeService()
        {
            _objectUnderTest.ExposeService = false;
            _objectUnderTest.ExposePublicService = true;

            Assert.IsFalse(_objectUnderTest.ExposePublicService);
        }

        [TestMethod]
        public void TestExposePublicService_FalseIndependentOfExposeService()
        {
            _objectUnderTest.ExposeService = true;
            _objectUnderTest.ExposePublicService = false;

            Assert.IsFalse(_objectUnderTest.ExposePublicService);
        }

        [TestMethod]
        public void TestExposePublicService_True()
        {
            _objectUnderTest.ExposeService = true;
            _objectUnderTest.ExposePublicService = true;

            Assert.IsTrue(_objectUnderTest.ExposePublicService);
        }

        [TestMethod]
        public void TestOpenWebsite_HiddenByExposeService()
        {
            _objectUnderTest.ExposeService = false;
            _objectUnderTest.ExposePublicService = true;
            _objectUnderTest.OpenWebsite = true;

            Assert.IsFalse(_objectUnderTest.OpenWebsite);
        }

        [TestMethod]
        public void TestOpenWebsite_HiddenByExposePublicService()
        {
            _objectUnderTest.ExposeService = true;
            _objectUnderTest.ExposePublicService = false;
            _objectUnderTest.OpenWebsite = true;

            Assert.IsFalse(_objectUnderTest.OpenWebsite);
        }

        [TestMethod]
        public void TestOpenWebsite_FalseIndependently()
        {
            _objectUnderTest.ExposeService = true;
            _objectUnderTest.ExposePublicService = true;
            _objectUnderTest.OpenWebsite = false;

            Assert.IsFalse(_objectUnderTest.OpenWebsite);
        }

        [TestMethod]
        public void TestOpenWebsite_True()
        {
            _objectUnderTest.ExposeService = true;
            _objectUnderTest.ExposePublicService = true;
            _objectUnderTest.OpenWebsite = true;

            Assert.IsTrue(_objectUnderTest.OpenWebsite);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.OpenWebsite));
        }

        [TestMethod]
        public void TestCreateClusterCommand()
        {
            const string projectId = "test-project-id";
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(projectId);
            _objectUnderTest.CreateClusterCommand.Execute(null);

            _browserServiceMock.Verify(
                s => s.OpenBrowser(string.Format(GkeStepViewModel.GkeAddClusterUrlFormat, projectId)),
                Times.Once);
        }

        [TestMethod]
        public async Task TestRefreshClustersListCommand_ReceivingNullSetsPlaceholder()
        {
            _getClusterListTaskSource.SetResult(null);
            _objectUnderTest.RefreshClustersListCommand.Execute(null);
            await _objectUnderTest.LoadProjectTask.SafeTask;

            Assert.AreEqual(GkeStepViewModel.s_placeholderList, _objectUnderTest.Clusters);
            Assert.AreEqual(GkeStepViewModel.s_placeholderCluster, _objectUnderTest.SelectedCluster);
        }

        [TestMethod]
        public async Task TestRefreshClustersListCommand_ReceivingEmptySetsPlaceholder()
        {
            _getClusterListTaskSource.SetResult(new List<Cluster>());
            _objectUnderTest.RefreshClustersListCommand.Execute(null);
            await _objectUnderTest.LoadProjectTask.SafeTask;

            Assert.AreEqual(GkeStepViewModel.s_placeholderList, _objectUnderTest.Clusters);
            Assert.AreEqual(GkeStepViewModel.s_placeholderCluster, _objectUnderTest.SelectedCluster);
        }

        [TestMethod]
        public async Task TestRefreshClustersListCommand_SetsClustersInOrder()
        {
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);
            _objectUnderTest.RefreshClustersListCommand.Execute(null);
            await _objectUnderTest.LoadProjectTask.SafeTask;

            CollectionAssert.AreEqual(s_inOrderClusters, _objectUnderTest.Clusters.ToList());
            Assert.AreEqual(s_aCluster, _objectUnderTest.SelectedCluster);
        }

        [TestMethod]
        public void TestInitializeDialogAsync_SetsValidDeploymentName()
        {
            _parsedProject.Name = "VisualStudioProjectName";
            _getClusterListTaskSource.SetResult(null);

            _objectUnderTest.OnVisible();

            Assert.AreEqual("visual-studio-project-name", _objectUnderTest.DeploymentName);
        }

        [TestMethod]
        public void TestValidateProjectAsync_MissingProjectDisablesCommands()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(() => null);
            _objectUnderTest.RefreshClustersListCommand.CanExecuteCommand = true;
            _objectUnderTest.CreateClusterCommand.CanExecuteCommand = true;

            _objectUnderTest.OnVisible();

            Assert.IsFalse(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestValidateProjectAsync_EnablesCommands()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns("valid-project-id");
            _objectUnderTest.RefreshClustersListCommand.CanExecuteCommand = false;
            _objectUnderTest.CreateClusterCommand.CanExecuteCommand = false;
            _getClusterListTaskSource.SetResult(null);

            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestLoadValidProjectDataAsync_ReceivingNullSetsPlaceholder()
        {
            _getClusterListTaskSource.SetResult(null);
            _objectUnderTest.OnVisible();

            Assert.AreEqual(GkeStepViewModel.s_placeholderList, _objectUnderTest.Clusters);
            Assert.AreEqual(GkeStepViewModel.s_placeholderCluster, _objectUnderTest.SelectedCluster);
        }

        [TestMethod]
        public void TestLoadValidProjectDataAsync_ReceivingEmptySetsPlaceholder()
        {
            _getClusterListTaskSource.SetResult(new List<Cluster>());
            _objectUnderTest.OnVisible();

            Assert.AreEqual(GkeStepViewModel.s_placeholderList, _objectUnderTest.Clusters);
            Assert.AreEqual(GkeStepViewModel.s_placeholderCluster, _objectUnderTest.SelectedCluster);
        }

        [TestMethod]
        public void TestLoadValidProjectDataAsync_SetsClustersInOrder()
        {
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);
            _objectUnderTest.OnVisible();

            CollectionAssert.AreEqual(s_inOrderClusters, _objectUnderTest.Clusters.ToList());
            Assert.AreEqual(s_aCluster, _objectUnderTest.SelectedCluster);
        }

        [TestMethod]
        public void TestOnFlowFinished_ResetsValues()
        {
            _objectUnderTest.DeploymentVersion = "test-deployment-version";
            _objectUnderTest.DeploymentName = "test-deployment-name";
            _objectUnderTest.Replicas = "200";
            _objectUnderTest.RefreshClustersListCommand.CanExecuteCommand = true;
            _objectUnderTest.CreateClusterCommand.CanExecuteCommand = true;

            _objectUnderTest.OnFlowFinished();

            Assert.AreEqual(GcpPublishStepsUtils.GetDefaultVersion(), _objectUnderTest.DeploymentVersion);
            Assert.IsNull(_objectUnderTest.DeploymentName);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
        }

        [TestMethod]
        public async Task TestLoadProjectProperties_LoadsClusterIdProperty()
        {
            _propertyServiceMock.Setup(
                    s => s.GetUserProperty(_parsedProject.Project, GkeStepViewModel.ClusterIdProjectPropertyName))
                .Returns(ClusterCId);
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);

            _objectUnderTest.OnVisible();
            await _objectUnderTest.LoadProjectTask.SafeTask;

            Assert.AreEqual(s_cCluster, _objectUnderTest.SelectedCluster);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsDeploymentName()
        {
            const string expectedDeploymentName = "DeploymentName";
            _propertyServiceMock.Setup(
                    s => s.GetUserProperty(_parsedProject.Project, GkeStepViewModel.DeploymentProjectPropertyName))
                .Returns(expectedDeploymentName);

            _objectUnderTest.OnVisible();

            Assert.AreEqual(expectedDeploymentName, _objectUnderTest.DeploymentName);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsVersion()
        {
            const string expectedVersion = "ExpectedVersion";
            _propertyServiceMock.Setup(
                    s => s.GetUserProperty(_parsedProject.Project, GkeStepViewModel.VersionProjectPropertyName))
                .Returns(expectedVersion);

            _objectUnderTest.OnVisible();

            Assert.AreEqual(expectedVersion, _objectUnderTest.DeploymentVersion);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsReplicas()
        {
            const string expectedReplicas = "47";
            _propertyServiceMock.Setup(
                    s => s.GetUserProperty(_parsedProject.Project, GkeStepViewModel.ReplicasProjectPropertyName))
                .Returns(expectedReplicas);

            _objectUnderTest.OnVisible();

            Assert.AreEqual(expectedReplicas, _objectUnderTest.Replicas);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsExposeService()
        {
            _propertyServiceMock.Setup(
                    s => s.GetUserProperty(_parsedProject.Project, GkeStepViewModel.ExposeServiceProjectPropertyName))
                .Returns(bool.TrueString);

            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.ExposeService);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsExposePublicService()
        {
            _propertyServiceMock.Setup(
                    s => s.GetUserProperty(_parsedProject.Project, GkeStepViewModel.ExposePublicServiceProjectPropertyName))
                .Returns(bool.TrueString);

            _objectUnderTest.OnVisible();
            _objectUnderTest.ExposeService = true;

            Assert.IsTrue(_objectUnderTest.ExposePublicService);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsOpenWebsite()
        {
            _propertyServiceMock.Setup(
                    s => s.GetUserProperty(_parsedProject.Project, GkeStepViewModel.OpenWebsiteProjectPropertyName))
                .Returns(bool.TrueString);

            _objectUnderTest.OnVisible();
            _objectUnderTest.ExposeService = true;
            _objectUnderTest.ExposePublicService = true;

            Assert.IsTrue(_objectUnderTest.OpenWebsite);
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesClusterIdProperty()
        {
            _objectUnderTest.SelectedCluster = s_bCluster;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(s => s.SaveUserProperty(_parsedProject.Project, GkeStepViewModel.ClusterIdProjectPropertyName,
                s_bCluster.SelfLink));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesDeploymentName()
        {
            const string expectedDeploymentName = "deployment-name";
            _objectUnderTest.DeploymentName = expectedDeploymentName;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, GkeStepViewModel.DeploymentProjectPropertyName,
                    expectedDeploymentName));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SkipsSaveOfInvalidDeploymentName()
        {
            _objectUnderTest.DeploymentName = "!Invalid Deployment Name!";

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    It.IsAny<EnvDTE.Project>(), GkeStepViewModel.DeploymentProjectPropertyName,
                    It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesVersion()
        {
            const string deploymentVersion = "123456";
            _objectUnderTest.DeploymentVersion = deploymentVersion;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, GkeStepViewModel.VersionProjectPropertyName,
                    deploymentVersion));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SkipsSaveOfInvalidVersion()
        {
            _objectUnderTest.DeploymentVersion = "!Invalid Deployment Version!";

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    It.IsAny<EnvDTE.Project>(), GkeStepViewModel.VersionProjectPropertyName,
                    It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesReplicas()
        {
            const string replicas = "47";
            _objectUnderTest.Replicas = replicas;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, GkeStepViewModel.ReplicasProjectPropertyName,
                    replicas));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SkipsSaveOfInvalidReplicas()
        {
            const string replicas = " not valid replicas int ";
            _objectUnderTest.Replicas = replicas;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    It.IsAny<EnvDTE.Project>(), GkeStepViewModel.ReplicasProjectPropertyName,
                    It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesExposeService()
        {
            _objectUnderTest.ExposeService = true;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, GkeStepViewModel.ExposeServiceProjectPropertyName,
                    bool.TrueString));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesExposePublicService()
        {
            _objectUnderTest.ExposePublicService = true;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, GkeStepViewModel.ExposePublicServiceProjectPropertyName,
                    bool.TrueString));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesOpenWebsite()
        {
            _objectUnderTest.OpenWebsite = true;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, GkeStepViewModel.OpenWebsiteProjectPropertyName,
                    bool.TrueString));
        }

        [TestMethod]
        public void TestSetExistingDeployments_SetsProperty()
        {
            var expectedValue = new AsyncProperty<IList<GkeDeployment>>(null);

            _objectUnderTest.ExistingDeployments = expectedValue;

            Assert.AreEqual(expectedValue, _objectUnderTest.ExistingDeployments);
        }

        [TestMethod]
        public void TestSetExistingDeployments_RaisesPropertyChanged()
        {
            _objectUnderTest.ExistingDeployments = new AsyncProperty<IList<GkeDeployment>>(null);

            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.ExistingDeployments));
        }

        [TestMethod]
        public void TestSetExistingDeployments_UpdatesSelectedDeploymentSyncronously()
        {
            _objectUnderTest.DeploymentName = _deployment.Metadata.Name;
            _objectUnderTest.ExistingDeployments = new AsyncProperty<IList<GkeDeployment>>(new[] { _deployment });

            Assert.AreEqual(_deployment, _objectUnderTest.SelectedDeployment);
        }

        [TestMethod]
        public void TestSetExistingDeployments_ClearsSelectedDeploymentWhenNotComplete()
        {

            _objectUnderTest.DeploymentName = DeploymentName;
            _objectUnderTest.ExistingDeployments = new AsyncProperty<IList<GkeDeployment>>(_getDeploymentsSource.Task);

            Assert.IsNull(_objectUnderTest.SelectedDeployment);
        }

        [TestMethod]
        public async Task TestSetExistingDeployments_UpdatesSelectedDeploymentAsyncronously()
        {
            _objectUnderTest.DeploymentName = _deployment.Metadata.Name;
            _objectUnderTest.ExistingDeployments = new AsyncProperty<IList<GkeDeployment>>(_getDeploymentsSource.Task);
            _getDeploymentsSource.SetResult(new[] { new GkeDeployment(), _deployment });
            await _objectUnderTest.ExistingDeployments.SafeTask;

            Assert.AreEqual(_deployment, _objectUnderTest.SelectedDeployment);
        }

        [TestMethod]
        public void TestSetSelectedDeployment_SetsProperty()
        {
            _objectUnderTest.SelectedDeployment = _deployment;

            Assert.AreEqual(_deployment, _objectUnderTest.SelectedDeployment);
        }

        [TestMethod]
        public void TestSetSelectedDeployment_RaisesPropertyChanged()
        {
            _objectUnderTest.SelectedDeployment = _deployment;

            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.SelectedDeployment));
        }

        [TestMethod]
        public async Task TestInvalidGcloud_DisablesPublish()
        {
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);
            _objectUnderTest.OnVisible();
            _objectUnderTest.SelectedCluster = s_aCluster;
            _canPublishChangedCount = 0;

            _validateGcloudSource.SetResult(GCloudValidationResult.GetObsoleteVersion(new Version(10, 0, 0)));
            await _objectUnderTest._verifyGCloudTask.SafeTask;

            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsTrue(_canPublishChangedCount > 0);
        }

        [TestMethod]
        public async Task TestValidGcloud_EnablesPublish()
        {
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);
            _objectUnderTest.OnVisible();
            _objectUnderTest.SelectedCluster = s_aCluster;
            _canPublishChangedCount = 0;

            _validateGcloudSource.SetResult(GCloudValidationResult.Valid);
            await _objectUnderTest._verifyGCloudTask.SafeTask;

            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.AreEqual(1, _canPublishChangedCount);
        }

        [TestMethod]
        public async Task TestPublishCommand_CallsGkeDeploymentService()
        {
            // Initalize KubectlContext task.
            _objectUnderTest.SelectedCluster = s_aCluster;

            _objectUnderTest.PublishCommand.Execute(null);
            await _objectUnderTest.PublishCommand.LatestExecution.SafeTask;

            _gkeDeploymentServiceMock
                .Verify(
                    s => s.DepoloyProjectToGkeAsync(
                        It.IsAny<IParsedProject>(),
                        It.IsAny<GkeDeploymentService.Options>()));
        }

        [TestMethod]
        public async Task TestPublishCommand_CallsGkeDeploymentServiceWithDeploymentName()
        {
            // Initalize KubectlContext task.
            _objectUnderTest.SelectedCluster = s_aCluster;

            _objectUnderTest.DeploymentName = DeploymentName;
            _objectUnderTest.PublishCommand.Execute(null);
            await _objectUnderTest.PublishCommand.LatestExecution.SafeTask;

            _gkeDeploymentServiceMock
                .Verify(
                    s => s.DepoloyProjectToGkeAsync(
                        It.IsAny<IParsedProject>(),
                        It.Is<GkeDeploymentService.Options>(o => o.DeploymentName == DeploymentName)));
        }

        [TestMethod]
        public async Task TestPublishCommand_CallsGkeDeploymentServiceWithDeploymentVersion()
        {
            // Initalize KubectlContext task.
            _objectUnderTest.SelectedCluster = s_aCluster;

            _objectUnderTest.DeploymentVersion = DeploymentName;
            _objectUnderTest.PublishCommand.Execute(null);
            await _objectUnderTest.PublishCommand.LatestExecution.SafeTask;

            _gkeDeploymentServiceMock
                .Verify(
                    s => s.DepoloyProjectToGkeAsync(
                        It.IsAny<IParsedProject>(),
                        It.Is<GkeDeploymentService.Options>(o => o.DeploymentVersion == DeploymentName)));
        }

        [TestMethod]
        public async Task TestPublishCommand_CallsGkeDeploymentServiceWithSelectedDeployment()
        {
            // Initalize KubectlContext task.
            _objectUnderTest.SelectedCluster = s_aCluster;
            var expectedDeployment = new GkeDeployment();

            _objectUnderTest.SelectedDeployment = expectedDeployment;
            _objectUnderTest.PublishCommand.Execute(null);
            await _objectUnderTest.PublishCommand.LatestExecution.SafeTask;

            _gkeDeploymentServiceMock
                .Verify(
                    s => s.DepoloyProjectToGkeAsync(
                        It.IsAny<IParsedProject>(),
                        It.Is<GkeDeploymentService.Options>(o => o.ExistingDeployment == expectedDeployment)));
        }

        [TestMethod]
        public async Task TestPublishCommand_CallsGkeDeploymentServiceExposingService()
        {
            // Initalize KubectlContext task.
            _objectUnderTest.SelectedCluster = s_aCluster;

            _objectUnderTest.ExposeService = true;
            _objectUnderTest.PublishCommand.Execute(null);
            await _objectUnderTest.PublishCommand.LatestExecution.SafeTask;

            _gkeDeploymentServiceMock
                .Verify(
                    s => s.DepoloyProjectToGkeAsync(
                        It.IsAny<IParsedProject>(),
                        It.Is<GkeDeploymentService.Options>(o => o.ExposeService)));
        }

        [TestMethod]
        public async Task TestPublishCommand_CallsGkeDeploymentServiceExposingPublicService()
        {
            // Initalize KubectlContext task.
            _objectUnderTest.SelectedCluster = s_aCluster;

            _objectUnderTest.ExposeService = true;
            _objectUnderTest.ExposePublicService = true;
            _objectUnderTest.PublishCommand.Execute(null);
            await _objectUnderTest.PublishCommand.LatestExecution.SafeTask;

            _gkeDeploymentServiceMock
                .Verify(
                    s => s.DepoloyProjectToGkeAsync(
                        It.IsAny<IParsedProject>(),
                        It.Is<GkeDeploymentService.Options>(o => o.ExposeService)));
        }

        [TestMethod]
        public async Task TestPublishCommand_CallsGkeDeploymentServiceOpeningWebsite()
        {
            // Initalize KubectlContext task.
            _objectUnderTest.SelectedCluster = s_aCluster;

            _objectUnderTest.ExposeService = true;
            _objectUnderTest.ExposePublicService = true;
            _objectUnderTest.OpenWebsite = true;
            _objectUnderTest.PublishCommand.Execute(null);
            await _objectUnderTest.PublishCommand.LatestExecution.SafeTask;

            _gkeDeploymentServiceMock
                .Verify(
                    s => s.DepoloyProjectToGkeAsync(
                        It.IsAny<IParsedProject>(),
                        It.Is<GkeDeploymentService.Options>(o => o.OpenWebsite)));
        }

        [TestMethod]
        public async Task TestPublishCommand_CallsGkeDeploymentServiceWithConfiguration()
        {
            // Initalize KubectlContext task.
            _objectUnderTest.SelectedCluster = s_aCluster;
            const string expectedConfiguration = "expected-configuration";

            _objectUnderTest.SelectedConfiguration = expectedConfiguration;
            _objectUnderTest.PublishCommand.Execute(null);
            await _objectUnderTest.PublishCommand.LatestExecution.SafeTask;

            _gkeDeploymentServiceMock
                .Verify(
                    s => s.DepoloyProjectToGkeAsync(
                        It.IsAny<IParsedProject>(),
                        It.Is<GkeDeploymentService.Options>(o => o.Configuration == expectedConfiguration)));
        }

        [TestMethod]
        public async Task TestPublishCommand_CallsGkeDeploymentServiceWithReplicas()
        {
            // Initalize KubectlContext task.
            _objectUnderTest.SelectedCluster = s_aCluster;
            const int expectedReplicas = 4;

            _objectUnderTest.Replicas = expectedReplicas.ToString();
            _objectUnderTest.PublishCommand.Execute(null);
            await _objectUnderTest.PublishCommand.LatestExecution.SafeTask;

            _gkeDeploymentServiceMock
                .Verify(
                    s => s.DepoloyProjectToGkeAsync(
                        It.IsAny<IParsedProject>(),
                        It.Is<GkeDeploymentService.Options>(o => o.Replicas == expectedReplicas)));
        }

        [TestMethod]
        public async Task TestPublishCommand_IncrementsDeploymentVersion()
        {
            // Initalize KubectlContext task.
            _objectUnderTest.SelectedCluster = s_aCluster;

            _objectUnderTest.DeploymentVersion = DeploymentName;
            _objectUnderTest.PublishCommand.Execute(null);
            await _objectUnderTest.PublishCommand.LatestExecution.SafeTask;

            Assert.AreNotEqual(DeploymentName, _objectUnderTest.DeploymentVersion);
        }
    }
}
