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

using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Container.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps.Gke;
using GoogleCloudExtension.Services.VsProject;
using GoogleCloudExtension.Utils;
using GoogleCloudExtensionUnitTests.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly Project s_defaultProject = new Project { ProjectId = "TestProjectId" };

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
        private IPublishDialog _mockedPublishDialog;
        private Mock<Func<Project>> _pickProjectPromptMock;
        private List<string> _changedProperties;
        private int _canPublishChangedCount;
        private Mock<Func<string, Process>> _startProcessMock;
        private Mock<IVsProjectPropertyService> _propertyServiceMock;
        private FakeParsedProject _parsedProject;

        [ClassInitialize]
        public static void BeforeAll(TestContext context) =>
            GcpPublishStepsUtils.NowOverride = DateTime.Parse("2088-12-23 01:01:01");

        [ClassCleanup]
        public static void AfterAll() => GcpPublishStepsUtils.NowOverride = null;

        protected override void BeforeEach()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _propertyServiceMock = new Mock<IVsProjectPropertyService>();
            PackageMock.Setup(p => p.GetService<IVsProjectPropertyService>()).Returns(_propertyServiceMock.Object);

            _parsedProject = new FakeParsedProject { Name = VisualStudioProjectName };
            _mockedPublishDialog = Mock.Of<IPublishDialog>(pd => pd.Project == _parsedProject);

            _pickProjectPromptMock = new Mock<Func<Project>>();
            _changedProperties = new List<string>();

            var mockedApiManager = Mock.Of<IApiManager>(
                x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>()) == Task.FromResult(true) &&
                    x.EnableServicesAsync(It.IsAny<IEnumerable<string>>()) == Task.FromResult(true));
            _getClusterListTaskSource = new TaskCompletionSource<IList<Cluster>>();
            var mockedDataSource = Mock.Of<IGkeDataSource>(ds => ds.GetClusterListAsync() == _getClusterListTaskSource.Task);

            _objectUnderTest = new GkeStepViewModel(
                mockedDataSource, mockedApiManager, _pickProjectPromptMock.Object, _mockedPublishDialog);
            _objectUnderTest.MillisecondsDelay = 0;
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
            _objectUnderTest.PublishCommand.CanExecuteChanged += (sender, args) => _canPublishChangedCount++;
            _startProcessMock = new Mock<Func<string, Process>>();
            _objectUnderTest._startProcessOverride = _startProcessMock.Object;
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
        public void TestSetSelectedCluster_EnablesPublish()
        {
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);
            _objectUnderTest.OnVisible();
            _objectUnderTest.SelectedCluster = null;
            _canPublishChangedCount = 0;

            _objectUnderTest.SelectedCluster = s_aCluster;

            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.AreEqual(1, _canPublishChangedCount);
        }

        [TestMethod]
        public void TestSetSelectedCluster_ToNullDisablesPublish()
        {
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);
            _objectUnderTest.OnVisible();
            _objectUnderTest.SelectedCluster = s_aCluster;
            _canPublishChangedCount = 0;

            _objectUnderTest.SelectedCluster = null;

            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.AreEqual(1, _canPublishChangedCount);
        }

        [TestMethod]
        public void TestSetSelectedCluster_ToPlaceholderDisablesPublish()
        {
            _getClusterListTaskSource.SetResult(s_outOfOrderClusters);
            _objectUnderTest.OnVisible();
            _objectUnderTest.SelectedCluster = s_aCluster;
            _canPublishChangedCount = 0;

            _objectUnderTest.SelectedCluster = GkeStepViewModel.s_placeholderCluster;

            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.AreEqual(1, _canPublishChangedCount);
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
        public void TestSetExposePublicService()
        {
            _objectUnderTest.ExposePublicService = true;

            Assert.IsTrue(_objectUnderTest.ExposePublicService);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.ExposePublicService));
        }

        [TestMethod]
        public void TestSetOpenWebsite()
        {
            _objectUnderTest.OpenWebsite = true;

            Assert.IsTrue(_objectUnderTest.OpenWebsite);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.OpenWebsite));
        }

        [TestMethod]
        public void TestCreateClusterCommand()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.CreateClusterCommand.Execute(null);

            _startProcessMock.Verify(
                f => f(string.Format(GkeStepViewModel.GkeAddClusterUrlFormat, s_defaultProject.ProjectId)), Times.Once);
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
            Mock.Get(_mockedPublishDialog).Setup(pd => pd.Project.Name).Returns("VisualStudioProjectName");
            _getClusterListTaskSource.SetResult(null);

            _objectUnderTest.OnVisible();

            Assert.AreEqual("visual-studio-project-name", _objectUnderTest.DeploymentName);
        }

        [TestMethod]
        public void TestValidateProjectAsync_MissingProjectDisablesCommands()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            _objectUnderTest.RefreshClustersListCommand.CanExecuteCommand = true;
            _objectUnderTest.CreateClusterCommand.CanExecuteCommand = true;

            _objectUnderTest.OnVisible();

            Assert.IsFalse(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestValidateProjectAsync_EnablesCommands()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
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
    }
}
