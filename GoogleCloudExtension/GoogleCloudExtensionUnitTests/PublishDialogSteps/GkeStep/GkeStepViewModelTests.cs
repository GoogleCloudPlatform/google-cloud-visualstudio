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

using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Container.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.GkeStep;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.GkeStep
{
    [TestClass]
    public class GkeStepViewModelTests : ExtensionTestBase
    {
        private const string DefaultProjectId = "DefaultProjectId";
        private const string TargetProjectId = "TargetProjectId";
        private const string VisualStudioProjectName = "VisualStudioProjectName";
        private const string InvalidVersion = "-Invalid Version Name!";
        private const string ValidVersion = "valid-version-name";
        private const string InvalidDeploymentName = "-Invalid Deployment Name!";
        private const string ValidDeploymentName = "valid-deployment-name";
        private const string ValidReplicas = "2";
        private const string ZeroReplicas = "0";
        private const string NegativeReplicas = "-3";

        private static readonly Regex s_validNamePattern = new Regex(@"^(?!-)[a-z\d\-]{1,100}$");

        private static readonly Project s_targetProject = new Project { ProjectId = TargetProjectId };
        private static readonly Project s_defaultProject = new Project { ProjectId = DefaultProjectId };

        private static readonly Cluster s_selectedCluster = new Cluster { Name = "Acluster" };
        private static readonly Cluster s_BCluster = new Cluster { Name = "Bcluster" };
        private static readonly Cluster s_CCluster = new Cluster { Name = "Ccluster" };
        private static readonly List<Cluster> s_mockedClusters = new List<Cluster>
        {
            s_BCluster, s_selectedCluster, s_CCluster,
        };
        private static readonly List<Cluster> s_expectedClusters = new List<Cluster>
        {
            s_selectedCluster, s_BCluster, s_CCluster,
        };

        private GkeStepViewModel _objectUnderTest;
        private Mock<IApiManager> _apiManagerMock;
        private TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        private TaskCompletionSource<object> _enableServicesTaskSource;
        private Mock<IGkeDataSource> _dataSourceMock;
        private TaskCompletionSource<IList<Cluster>> _clusterListTaskSource;
        private IPublishDialog _mockedPublishDialog;
        private Mock<Func<Project>> _pickProjectPromptMock;
        private List<string> _changedProperties;

        protected override void BeforeEach()
        {
            base.BeforeEach();

            IParsedProject mockedProject = Mock.Of<IParsedProject>(p => p.Name == VisualStudioProjectName);

            Mock<IPublishDialog> publishDialogMock = new Mock<IPublishDialog>();
            publishDialogMock.Setup(pd => pd.Project).Returns(mockedProject);
            publishDialogMock.Setup(pd => pd.TrackTask(It.IsAny<Task>()));
            _mockedPublishDialog = publishDialogMock.Object;

            _pickProjectPromptMock = new Mock<Func<Project>>();
            _changedProperties = new List<string>();

            _apiManagerMock = new Mock<IApiManager>();
            _apiManagerMock.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(() => _areServicesEnabledTaskSource.Task);
            _apiManagerMock.Setup(x => x.EnableServicesAsync(It.IsAny<IEnumerable<string>>())).Returns(() => _enableServicesTaskSource.Task);

            _dataSourceMock = new Mock<IGkeDataSource>();
            _dataSourceMock.Setup(ds => ds.GetClusterListAsync()).Returns(() => _clusterListTaskSource.Task);

            _objectUnderTest = GkeStepViewModel.CreateStep(apiManager: _apiManagerMock.Object, pickProjectPrompt: _pickProjectPromptMock.Object, dataSource: _dataSourceMock.Object);
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        protected override void AfterEach()
        {
            _objectUnderTest.OnFlowFinished();

            base.AfterEach();
        }

        [TestMethod]
        public void TestInitialState()
        {
            AssertInitialState();
        }

        [TestMethod]
        public void TestInitialStateNoProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);

            AssertSelectedProjectUnchanged();
            AssertInitialState();
        }

        [TestMethod]
        public void TestInitialStateProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnVisibleNoProject()
        {
            await OnVisibleWithProject(null);

            AssertSelectedProjectChanged();
            AssertNoProjectInitialState();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisiblePositiveValidation()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestOnVisibleNegativeValidation()
        {
            InitAreServicesEnabledMock(false);

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public void TestOnVisibleLongRunningValidation()
        {
            InitLongRunningAreServicesEnabledMock();

            Task onVisibleTask = OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisibleErrorInValidation()
        {
            InitErrorAreServicesEnabledMock();

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisibleNoClusters()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(new List<Cluster>());

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertValidProjectPlaceholderClusters(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNoExternal()
        {
            await OnVisibleWithProject(null);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectUnchanged();
            AssertNoProjectInitialState();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToPositiveValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNegativeValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToLongRunningValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToErrorInValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToNoExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectChanged();
            AssertNoProjectInitialState();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToPositiveValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNegativeValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToErrorInValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToNoClustersSelectCommmand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            InitGetClusterListMock(new List<Cluster>());
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectPlaceholderClusters(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNoExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectChanged();
            AssertNoProjectInitialState();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToPositiveValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNegativeValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToLongRunningValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToErrorInValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToNoSelectCommand()
        {
            await OnVisibleWithProject(null);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertNoProjectInitialState();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToPositiveValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNegativeValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToLongRunningValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToErrorInValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToNoSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertValidProjectInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNegativeValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToErrorInValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToSameValidSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertValidProjectInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToSameInvalidSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidProjectInvalidVersionToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            await SetVersion(InvalidVersion);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInvalidVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidProjectInvalidNameToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            await SetDeploymentName(InvalidDeploymentName);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInvalidDeploymentName(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidProjectInvalidReplicasToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            await SetReplicas(NegativeReplicas);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectNegativeReplicas(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNoSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNegativeValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToLongRunningValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToErrorInValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToSamePositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertValidProjectInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToSameNegativeValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidProjectAndVersionToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            await SetVersion(InvalidVersion);

            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInvalidVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidProjectAndNameToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            await SetDeploymentName(InvalidDeploymentName);

            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInvalidDeploymentName(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidProjectAndReplicasToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            await SetReplicas(NegativeReplicas);

            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectNegativeReplicas(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToNoSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertErrorInValidationInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToPositiveValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToNegativeValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToLongRunningServiceValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToErrorInValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestEnableApisCommandSuccess()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            InitEnableApiMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await RunEnableApiCommand();

            AssertSelectedProjectUnchanged();
            AssertValidProjectInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Once());
            AssertEnableServicesCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestEnableApisCommandFailure()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitEnableApiMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await RunEnableApiCommand();

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetClusterListCalled(Times.Never());
            AssertEnableServicesCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestRefreshClustersCommandSuccess()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(new List<Cluster>());
            await OnVisibleWithProject(s_defaultProject);

            InitGetClusterListMock(s_mockedClusters);
            _changedProperties.Clear();
            ResetMockCalls();

            await RunRefreshClustersListCommand();

            AssertSelectedProjectUnchanged();
            AssertValidProjectInitialState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestRefreshClustersCommandFailure()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(new List<Cluster>());
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await RunRefreshClustersListCommand();

            AssertSelectedProjectUnchanged();
            AssertValidProjectPlaceholderClusters(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetClusterListCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestValidProjectNullVersion()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(null);

            AssertValidProjectNullVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectNullVersion()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(null);

            AssertInvalidProjectNullVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectEmptyVersion()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(string.Empty);

            AssertValidProjectEmptyVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectEmptyVersion()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(string.Empty);

            AssertInvalidProjectEmptyVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectInvalidVersion()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(InvalidVersion);

            AssertValidProjectInvalidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectInvalidVersion()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(InvalidVersion);

            AssertInvalidProjectInvalidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectValidVersion()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(ValidVersion);

            AssertValidProjectValidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectValidVersion()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(ValidVersion);

            AssertInvalidProjectValidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestFromValidProjectInvalidVersionToValidVersion()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            await SetVersion(InvalidVersion);

            await SetVersion(ValidVersion);

            AssertValidProjectValidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestFromInvalidProjectInvalidVersionToValidVersion()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            await SetVersion(InvalidVersion);

            await SetVersion(ValidVersion);

            AssertInvalidProjectValidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectNullDeploymentName()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetDeploymentName(null);

            AssertValidProjectNullDeploymentName(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectNullDeploymentName()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetDeploymentName(null);

            AssertInvalidProjectNullDeploymentName(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectEmptyDeploymentName()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetDeploymentName(string.Empty);

            AssertValidProjectEmptyDeploymentName(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectEmptyDeploymentName()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetDeploymentName(string.Empty);

            AssertInvalidProjectEmptyDeploymentName(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectInvalidDeploymentName()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetDeploymentName(InvalidDeploymentName);

            AssertValidProjectInvalidDeploymentName(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectInvalidDeploymentName()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetDeploymentName(InvalidDeploymentName);

            AssertInvalidProjectInvalidDeploymentName(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectValidDeploymentName()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetDeploymentName(ValidDeploymentName);

            AssertValidProjectValidDeploymentName(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectValidDeploymentName()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetDeploymentName(ValidDeploymentName);

            AssertInvalidProjectValidDeploymentName(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestFromValidProjectInvalidNameToValidName()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            await SetDeploymentName(InvalidDeploymentName);

            await SetDeploymentName(ValidDeploymentName);

            AssertValidProjectValidDeploymentName(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestFromInvalidProjectInvalidNameToValidName()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            await SetDeploymentName(InvalidDeploymentName);

            await SetDeploymentName(ValidDeploymentName);

            AssertInvalidProjectValidDeploymentName(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectNullReplicas()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetReplicas(null);

            AssertValidProjectNullReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectNullReplicas()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetReplicas(null);

            AssertInvalidProjectNullReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectEmptyReplicas()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetReplicas(string.Empty);

            AssertValidProjectEmptyReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectEmptyReplicas()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetReplicas(string.Empty);

            AssertInvalidProjectEmptyReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectZeroReplicas()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetReplicas(ZeroReplicas);

            AssertValidProjectZeroReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectZeroReplicas()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetReplicas(ZeroReplicas);

            AssertInvalidProjectZeroReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectNegativeReplicas()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetReplicas(NegativeReplicas);

            AssertValidProjectNegativeReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectNegativeReplicas()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetReplicas(NegativeReplicas);

            AssertInvalidProjectNegativeReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectValidReplicas()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            await SetReplicas(ValidReplicas);

            AssertValidProjectValidReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectValidReplicas()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            await SetReplicas(ValidReplicas);

            AssertInvalidProjectValidReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestFromValidProjectInvalidReplicasToValidReplicas()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            await SetReplicas(NegativeReplicas);

            await SetReplicas(ValidReplicas);

            AssertValidProjectValidReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestFromInvalidProjectInvalidReplicasToValidReplicas()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            await SetReplicas(NegativeReplicas);

            await SetReplicas(ValidReplicas);

            AssertInvalidProjectValidReplicas(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestExposeServiceInvalidates()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _objectUnderTest.ExposeService = true;
            _objectUnderTest.ExposePublicService = true;
            _objectUnderTest.OpenWebsite = true;

            _objectUnderTest.ExposeService = false;

            AssertValidProjectInitialState(DefaultProjectId);
            AssertDontExposeService();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromValidState()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromValidEventHandling()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromInvalidState()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromInvalidEventHandling()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromErrorState()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromErrorEventHandling()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        private void InitAreServicesEnabledMock(bool servicesEnabled)
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _areServicesEnabledTaskSource.SetResult(servicesEnabled);
        }

        private void InitLongRunningAreServicesEnabledMock()
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
        }

        private void InitErrorAreServicesEnabledMock()
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _areServicesEnabledTaskSource.SetException(new DataSourceException());
        }

        private void InitGetClusterListMock(IList<Cluster> result)
        {
            _clusterListTaskSource = new TaskCompletionSource<IList<Cluster>>();
            _clusterListTaskSource.SetResult(result);
        }

        private void InitEnableApiMock()
        {
            _enableServicesTaskSource = new TaskCompletionSource<object>();
            _enableServicesTaskSource.SetResult(null);
        }

        private void ResetMockCalls()
        {
            _apiManagerMock.ResetCalls();
            _dataSourceMock.ResetCalls();
        }

        private async Task OnVisibleWithProject(Project project)
        {
            CredentialsStore.Default.UpdateCurrentProject(project);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        private async Task OnProjectChangedExternally(Project changedTo)
        {
            CredentialsStore.Default.UpdateCurrentProject(changedTo);
            await _objectUnderTest.AsyncAction;
        }

        private async Task OnProjectChangedSelectProjectCommand(Project changedTo)
        {
            _pickProjectPromptMock.Setup(f => f()).Returns(changedTo);
            _objectUnderTest.SelectProjectCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        private async Task RunEnableApiCommand()
        {
            _objectUnderTest.EnableApiCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        private async Task RunRefreshClustersListCommand()
        {
            _objectUnderTest.RefreshClustersListCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        private async Task SetVersion(string version)
        {
            _objectUnderTest.DeploymentVersion = version;
            await _objectUnderTest.ValidationDelayTask;
        }

        private async Task SetDeploymentName(string name)
        {
            _objectUnderTest.DeploymentName = name;
            await _objectUnderTest.ValidationDelayTask;
        }

        private async Task SetReplicas(string replicas)
        {
            _objectUnderTest.Replicas = replicas;
            await _objectUnderTest.ValidationDelayTask;
        }

        private void RaiseFlowFinished()
        {
            Mock<IPublishDialog> publishDialogMock = Mock.Get(_mockedPublishDialog);
            publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
        }

        private void AssertInitialState()
        {
            AssertInitialVersionState();
            AssertDefaultClustersState();

            Assert.IsNotNull(_objectUnderTest.AsyncAction);
            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsNull(_objectUnderTest.PublishDialog);
            Assert.IsNull(_objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsNull(_objectUnderTest.DeploymentName);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertNoProjectInitialState()
        {
            AssertInvariantsAfterVisible();
            AssertInitialVersionState();
            AssertDefaultClustersState();
            AssertDefaultDeploymentName();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsNull(_objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertValidProjectInitialState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultDeploymentName();
            AssertValidClustersState();

            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsTrue(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectNullVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertDefaultDeploymentName();
            AssertValidClustersState();

            Assert.IsNull(_objectUnderTest.DeploymentVersion);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectEmptyVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertDefaultDeploymentName();
            AssertValidClustersState();

            Assert.AreEqual(string.Empty, _objectUnderTest.DeploymentVersion);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectInvalidVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertDefaultDeploymentName();
            AssertValidClustersState();

            Assert.AreEqual(InvalidVersion, _objectUnderTest.DeploymentVersion);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectValidVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertDefaultDeploymentName();
            AssertValidClustersState();

            Assert.AreEqual(ValidVersion, _objectUnderTest.DeploymentVersion);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsTrue(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectNullDeploymentName(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertValidClustersState();

            Assert.IsNull(_objectUnderTest.DeploymentName);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectEmptyDeploymentName(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertValidClustersState();

            Assert.AreEqual(string.Empty, _objectUnderTest.DeploymentName);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectInvalidDeploymentName(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertValidClustersState();

            Assert.AreEqual(InvalidDeploymentName, _objectUnderTest.DeploymentName);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectValidDeploymentName(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertValidClustersState();

            Assert.AreEqual(ValidDeploymentName, _objectUnderTest.DeploymentName);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsTrue(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectNullReplicas(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertValidClustersState();
            AssertDefaultDeploymentName();

            Assert.IsNull(_objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectEmptyReplicas(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertValidClustersState();
            AssertDefaultDeploymentName();

            Assert.AreEqual(string.Empty, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectZeroReplicas(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertValidClustersState();
            AssertDefaultDeploymentName();

            Assert.AreEqual(ZeroReplicas, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectNegativeReplicas(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertValidClustersState();
            AssertDefaultDeploymentName();

            Assert.AreEqual(NegativeReplicas, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectValidReplicas(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertValidClustersState();
            AssertDefaultDeploymentName();

            Assert.AreEqual(ValidReplicas, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsTrue(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectPlaceholderClusters(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultDeploymentName();
            AssertPlaceholderClustersState();

            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProject(string expectedProjectId)
        {
            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsTrue(_objectUnderTest.ShowInputControls);
        }

        private void AssertInvalidProjectInitialState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultDeploymentName();
            AssertDefaultClustersState();

            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectNullVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertDefaultDeploymentName();
            AssertDefaultClustersState();

            Assert.IsNull(_objectUnderTest.DeploymentVersion);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectEmptyVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertDefaultDeploymentName();
            AssertDefaultClustersState();

            Assert.AreEqual(string.Empty, _objectUnderTest.DeploymentVersion);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectInvalidVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertDefaultDeploymentName();
            AssertDefaultClustersState();

            Assert.AreEqual(InvalidVersion, _objectUnderTest.DeploymentVersion);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectValidVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertDefaultDeploymentName();
            AssertDefaultClustersState();

            Assert.AreEqual(ValidVersion, _objectUnderTest.DeploymentVersion);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectNullDeploymentName(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultClustersState();

            Assert.IsNull(_objectUnderTest.DeploymentName);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectEmptyDeploymentName(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultClustersState();

            Assert.AreEqual(string.Empty, _objectUnderTest.DeploymentName);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectInvalidDeploymentName(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultClustersState();

            Assert.AreEqual(InvalidDeploymentName, _objectUnderTest.DeploymentName);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectValidDeploymentName(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultClustersState();

            Assert.AreEqual(ValidDeploymentName, _objectUnderTest.DeploymentName);
            Assert.AreEqual(GkeStepViewModel.ReplicasDefaultValue, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectNullReplicas(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultClustersState();
            AssertDefaultDeploymentName();

            Assert.IsNull(_objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectEmptyReplicas(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultClustersState();
            AssertDefaultDeploymentName();

            Assert.AreEqual(string.Empty, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectZeroReplicas(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultClustersState();
            AssertDefaultDeploymentName();

            Assert.AreEqual(ZeroReplicas, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectNegativeReplicas(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultClustersState();
            AssertDefaultDeploymentName();

            Assert.AreEqual(NegativeReplicas, _objectUnderTest.Replicas);
            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProjectValidReplicas(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInvalidProject(expectedProjectId);
            AssertInitialVersionState();
            AssertDefaultClustersState();
            AssertDefaultDeploymentName();

            Assert.AreEqual(ValidReplicas, _objectUnderTest.Replicas);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidProject(string expectedProjectId)
        {
            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsTrue(_objectUnderTest.NeedsApiEnabled);
            Assert.IsTrue(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertLongRunningValidationInitialState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInitialVersionState();
            AssertDefaultDeploymentName();
            AssertDefaultClustersState();

            Assert.IsFalse(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsTrue(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertErrorInValidationInitialState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertInitialVersionState();
            AssertDefaultDeploymentName();
            AssertDefaultClustersState();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertInitialVersionState()
        {
            Assert.IsNotNull(_objectUnderTest.DeploymentVersion);
            Assert.IsTrue(s_validNamePattern.IsMatch(_objectUnderTest.DeploymentVersion));
        }

        private void AssertDefaultDeploymentName()
        {
            Assert.AreEqual(VisualStudioProjectName.ToLower(), _objectUnderTest.DeploymentName);
        }

        private void AssertDefaultClustersState()
        {
            CollectionAssert.AreEquivalent(Enumerable.Empty<Cluster>().ToList(), _objectUnderTest.Clusters.ToList());
            Assert.IsNull(_objectUnderTest.SelectedCluster);
        }

        private void AssertValidClustersState()
        {
            CollectionAssert.AreEqual(s_expectedClusters, _objectUnderTest.Clusters.ToList());
            Assert.AreEqual(s_selectedCluster, _objectUnderTest.SelectedCluster);
        }

        private void AssertPlaceholderClustersState()
        {
            CollectionAssert.AreEqual(GkeStepViewModel.s_placeholderList.ToList(), _objectUnderTest.Clusters.ToList());
            Assert.AreEqual(GkeStepViewModel.s_placeholderCluster, _objectUnderTest.SelectedCluster);
        }

        private void AssertDontExposeService()
        {
            Assert.IsFalse(_objectUnderTest.ExposeService);
            Assert.IsFalse(_objectUnderTest.ExposePublicService);
            Assert.IsFalse(_objectUnderTest.OpenWebsite);
        }

        private void AssertSelectedProjectChanged()
        {
            CollectionAssert.Contains(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }

        private void AssertSelectedProjectUnchanged()
        {
            CollectionAssert.DoesNotContain(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }

        private void AssertAreServicesEnabledCalled(Times times)
        {
            _apiManagerMock.Verify(api => api.AreServicesEnabledAsync(It.IsAny<IList<string>>()), times);
        }

        private void AssertGetClusterListCalled(Times times)
        {
            _dataSourceMock.Verify(src => src.GetClusterListAsync(), times);
        }

        private void AssertEnableServicesCalled(Times times)
        {
            _apiManagerMock.Verify(api => api.EnableServicesAsync(It.IsAny<IEnumerable<string>>()), times);
        }

        private void AssertInvariantsAfterVisible()
        {
            Assert.IsNotNull(_objectUnderTest.AsyncAction);
            Assert.AreEqual(_mockedPublishDialog, _objectUnderTest.PublishDialog);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsTrue(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
        }
    }
}
