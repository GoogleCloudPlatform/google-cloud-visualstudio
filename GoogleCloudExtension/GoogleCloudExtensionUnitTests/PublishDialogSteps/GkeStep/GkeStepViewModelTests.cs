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
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PublishDialogSteps.GkeStep;
using GoogleCloudExtensionUnitTests.PublishDialog;
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
    public class GkeStepViewModelTests : PublishDialogStepBaseTestsBase<GkeStepViewModel>
    {
        private const string InvalidVersion = "-Invalid Version Name!";
        private const string ValidVersion = "valid-version-name";
        private const string InvalidDeploymentName = "-Invalid Deployment Name!";
        private const string ValidDeploymentName = "valid-deployment-name";
        private const string ValidReplicas = "2";
        private const string ZeroReplicas = "0";
        private const string NegativeReplicas = "-3";

        private static readonly Cluster s_selectedCluster = new Cluster { Name = "Acluster" };
        private static readonly List<Cluster> s_mockedClusters = new List<Cluster>
        {
            new Cluster { Name = "Bcluster" },
            s_selectedCluster,
            new Cluster { Name = "Ccluster" },
        };

        private static readonly Regex s_validNamePattern = new Regex(@"^(?!-)[a-z\d\-]{1,100}$");

        private Mock<IGkeDataSource> _dataSourceMock;
        private TaskCompletionSource<IList<Cluster>> _clusterListTaskSource;

        private string _expectedDeploymentName;
        private IEnumerable<Cluster> _expectedClusters;
        private Cluster _expectedSelectedCluster;
        private string _expectedReplicas;
        private bool _expectedRefreshClusterListCanExecute;
        private bool _expectedCreateClusterCommandCanExecute;

        protected override int RequieredAPIsForStep => 2;

        protected override GkeStepViewModel CreateStep()
        {
            return GkeStepViewModel.CreateStep(apiManager: _apiManagerMock.Object, pickProjectPrompt: _pickProjectPromptMock.Object, dataSource: _dataSourceMock.Object);
        }

        [TestInitialize]
        public override void BeforeEach()
        {
            _dataSourceMock = new Mock<IGkeDataSource>();
            _dataSourceMock.Setup(ds => ds.GetClusterListAsync()).Returns(() => _clusterListTaskSource.Task);

            base.BeforeEach();
        }

        [TestMethod]
        public async Task TestOnVisibleNoClusters()
        {
            await GoToNoClustersDefaultState();

            SetNoClustersDefaultStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToNoClustersSelectCommmand()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetNoClustersTargetStateExpectedValues();

            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(new List<Cluster>());
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNullVersionValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidVersionStateExpectedValues();

            await GoToVersionState(null);

            AssertVersionExpectedState(null);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNullVersionInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToVersionState(null);
            SetInvalidVersionStateExpectedValues();

            AssertVersionExpectedState(null);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEmptyVersionValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidVersionStateExpectedValues();

            await GoToVersionState(string.Empty);

            AssertVersionExpectedState(string.Empty);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEmptyVersionInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToVersionState(string.Empty);
            SetInvalidVersionStateExpectedValues();

            AssertVersionExpectedState(string.Empty);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestInvalidVersionValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidVersionStateExpectedValues();

            await GoToVersionState(InvalidVersion);

            AssertVersionExpectedState(InvalidVersion);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestInvalidVersionInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToVersionState(InvalidVersion);
            SetInvalidVersionStateExpectedValues();

            AssertVersionExpectedState(InvalidVersion);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestValidVersionValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetValidVersionStateExpectedValues();

            await GoToVersionState(ValidVersion);

            AssertVersionExpectedState(ValidVersion);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestValidVersionInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToVersionState(ValidVersion);
            SetValidVersionStateExpectedValues();

            AssertVersionExpectedState(ValidVersion);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNullDeploymentNameValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidDeploymentNameStateExpectedValues(null);

            await GoToDeploymentNameState(null);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNullDeploymentNameInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToDeploymentNameState(null);
            SetInvalidDeploymentNameStateExpectedValues(null);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEmptyDeploymentNameValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidDeploymentNameStateExpectedValues(string.Empty);

            await GoToDeploymentNameState(string.Empty);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEmptyDeploymentNameInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToDeploymentNameState(string.Empty);
            SetInvalidDeploymentNameStateExpectedValues(string.Empty);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestInvalidDeploymentNameValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidDeploymentNameStateExpectedValues(InvalidDeploymentName);

            await GoToDeploymentNameState(InvalidDeploymentName);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestInvalidDeploymentNameInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToDeploymentNameState(InvalidDeploymentName);
            SetInvalidDeploymentNameStateExpectedValues(InvalidDeploymentName);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestValidDeploymentNameValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetValidDeploymentNameStateExpectedValues();

            await GoToDeploymentNameState(ValidDeploymentName);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestValidDeploymentNameInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToDeploymentNameState(ValidDeploymentName);
            SetValidDeploymentNameStateExpectedValues();

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNullReplicasValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidReplicasStateExpectedValues(null);

            await GoToReplicasState(null);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNullReplicasInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToReplicasState(null);
            SetInvalidReplicasStateExpectedValues(null);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEmptyReplicasValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidReplicasStateExpectedValues(string.Empty);

            await GoToReplicasState(string.Empty);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEmptyReplicasInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToReplicasState(string.Empty);
            SetInvalidReplicasStateExpectedValues(string.Empty);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestZeroReplicasValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidReplicasStateExpectedValues(ZeroReplicas);

            await GoToReplicasState(ZeroReplicas);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestZeroReplicasInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToReplicasState(ZeroReplicas);
            SetInvalidReplicasStateExpectedValues(ZeroReplicas);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNegativeReplicasValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidReplicasStateExpectedValues(NegativeReplicas);

            await GoToReplicasState(NegativeReplicas);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNegativeReplicasInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToReplicasState(NegativeReplicas);
            SetInvalidReplicasStateExpectedValues(NegativeReplicas);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestValidReplicasValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetValidReplicasStateExpectedValues();

            await GoToReplicasState(ValidReplicas);

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestValidReplicasInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetClusterListMock(s_mockedClusters);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToReplicasState(ValidReplicas);
            SetValidReplicasStateExpectedValues();

            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestRefreshClustersCommandSuccess()
        {
            await GoToNoClustersDefaultState();
            _changedProperties.Clear();

            RunRefreshClustersCommandSuccess();

            SetValidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestRefreshClustersCommandFailure()
        {
            await GoToNoClustersDefaultState();
            _changedProperties.Clear();

            RunRefreshClustersCommandFailure();

            SetNoClustersDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        //protected override void InitPositiveValidationMocks()
        //{
        //    InitAreServicesEnabledMock(true);
        //    InitGetClusterListMock(s_mockedClusters);
        //}

        //protected override void InitNegativeValidationMocks()
        //{
        //    InitAreServicesEnabledMock(false);
        //    InitGetClusterListMock(s_mockedClusters);
        //}

        //protected override void InitLongRunningValidationMocks()
        //{
        //    InitLongRunningAreServicesEnabledMock();
        //    InitLongRunningGetClusterMock();
        //}

        //protected override void InitErrorValidationMocks()
        //{
        //    InitErrorAreServicesEnabledMock();
        //    InitErrorGetClusterMock();
        //}

        private void InitGetClusterListMock(IList<Cluster> result)
        {
            _clusterListTaskSource = new TaskCompletionSource<IList<Cluster>>();
            _clusterListTaskSource.SetResult(result);
        }

        private void InitLongRunningGetClusterMock()
        {
            _clusterListTaskSource = new TaskCompletionSource<IList<Cluster>>();
        }

        private void InitErrorGetClusterMock()
        {
            _clusterListTaskSource = new TaskCompletionSource<IList<Cluster>>();
            _clusterListTaskSource.SetException(new DataSourceException());
        }

        protected void SetInitialStateExpectedValues()
        {
            Assert.IsNull(_objectUnderTest.PublishDialog);
            Assert.IsNull(_objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);

            _expectedDeploymentName = null;
            _expectedClusters = Enumerable.Empty<Cluster>();
            _expectedSelectedCluster = null;
            _expectedReplicas = GkeStepViewModel.ReplicasDefaultValue;
            _expectedRefreshClusterListCanExecute = false;
            _expectedCreateClusterCommandCanExecute = false;
        }

        protected override void SetNoProjectStateExpectedValues()
        {
            base.SetNoProjectStateExpectedValues();

            _expectedDeploymentName = VisualStudioProjectName.ToLower();
            _expectedClusters = Enumerable.Empty<Cluster>();
            _expectedSelectedCluster = null;
            _expectedReplicas = GkeStepViewModel.ReplicasDefaultValue;
            _expectedRefreshClusterListCanExecute = false;
            _expectedCreateClusterCommandCanExecute = false;
        }

        protected override void SetValidProjectStateExpectedValues()
        {
            base.SetValidProjectStateExpectedValues();

            _expectedCanPublish = true;

            _expectedDeploymentName = VisualStudioProjectName.ToLower();
            _expectedClusters = s_mockedClusters;
            _expectedSelectedCluster = s_selectedCluster;
            _expectedReplicas = GkeStepViewModel.ReplicasDefaultValue;
            _expectedRefreshClusterListCanExecute = true;
            _expectedCreateClusterCommandCanExecute = true;
        }

        protected override void SetInvalidProjectStateExpectedValues()
        {
            base.SetInvalidProjectStateExpectedValues();

            _expectedNeedsApiEnabled = true;
            _expectedEnableApiCommandCanExecute = true;
            _expectedShowInputControls = false;

            _expectedDeploymentName = VisualStudioProjectName.ToLower();
            _expectedClusters = Enumerable.Empty<Cluster>();
            _expectedSelectedCluster = null;
            _expectedReplicas = GkeStepViewModel.ReplicasDefaultValue;
            _expectedRefreshClusterListCanExecute = false;
            _expectedCreateClusterCommandCanExecute = false;
        }

        protected override void SetErrorInValidationServicesExpectedValues()
        {
            base.SetErrorInValidationServicesExpectedValues();

            _expectedGeneralError = true;
            _expectedShowInputControls = false;

            _expectedDeploymentName = VisualStudioProjectName.ToLower();
            _expectedClusters = Enumerable.Empty<Cluster>();
            _expectedSelectedCluster = null;
            _expectedReplicas = GkeStepViewModel.ReplicasDefaultValue;
            _expectedRefreshClusterListCanExecute = false;
            _expectedCreateClusterCommandCanExecute = false;
        }

        protected override void SetLongRunningValidationServicesExpectedValues()
        {
            base.SetLongRunningValidationServicesExpectedValues();

            _expectedLoadingProject = true;
            _expectedShowInputControls = false;

            _expectedDeploymentName = VisualStudioProjectName.ToLower();
            _expectedClusters = Enumerable.Empty<Cluster>();
            _expectedSelectedCluster = null;
            _expectedReplicas = GkeStepViewModel.ReplicasDefaultValue;
            _expectedRefreshClusterListCanExecute = false;
            _expectedCreateClusterCommandCanExecute = false;
        }

        private void SetValidVersionStateExpectedValues()
        {
            _expectedInputHasErrors = false;
        }

        private void SetInvalidVersionStateExpectedValues()
        {
            _expectedInputHasErrors = true;
            _expectedCanPublish = false;
        }

        private void SetValidDeploymentNameStateExpectedValues()
        {
            _expectedInputHasErrors = false;
            _expectedDeploymentName = ValidDeploymentName;
        }

        private void SetInvalidDeploymentNameStateExpectedValues(string invalidValue)
        {
            _expectedInputHasErrors = true;
            _expectedCanPublish = false;
            _expectedDeploymentName = invalidValue;
        }

        private void SetValidReplicasStateExpectedValues()
        {
            _expectedInputHasErrors = false;
            _expectedReplicas = ValidReplicas;
        }

        private void SetInvalidReplicasStateExpectedValues(string invalidValue)
        {
            _expectedInputHasErrors = true;
            _expectedCanPublish = false;
            _expectedReplicas = invalidValue;
        }

        private void SetNoClustersDefaultStateExpectedValues()
        {
            SetValidDefaultStateExpectedValues();

            SetNoClustersStateExpectedValues();
        }

        private void SetNoClustersTargetStateExpectedValues()
        {
            SetValidTargetStateExpectedValues();

            SetNoClustersStateExpectedValues();
        }

        private void SetNoClustersStateExpectedValues()
        {
            _expectedCanPublish = false;

            _expectedClusters = GkeStepViewModel.s_placeholderList;
            _expectedSelectedCluster = GkeStepViewModel.s_placeholderCluster;
        }

        private async Task GoToNoClustersDefaultState()
        {
            InitAreServicesEnabledMock(true);
            InitGetClusterListMock(new List<Cluster>());
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        private async Task GoToVersionState(string goToValue)
        {
            _objectUnderTest.DeploymentVersion = goToValue;
            await _objectUnderTest.ValidationDelayTask;
        }

        private async Task GoToDeploymentNameState(string goToValue)
        {
            _objectUnderTest.DeploymentName = goToValue;
            await _objectUnderTest.ValidationDelayTask;
        }

        private async Task GoToReplicasState(string goToValue)
        {
            _objectUnderTest.Replicas = goToValue;
            await _objectUnderTest.ValidationDelayTask;
        }

        //protected override Task RunEnableApiCommandFailure()
        //{
        //    InitGetClusterListMock(s_mockedClusters);
        //    InitAreServicesEnabledMock(false);            
        //    InitEnableApiMock();
        //    await RunEnableApiCommand();
        //    return base.RunEnableApiCommandFailure();
        //}

        private void RunRefreshClustersCommandSuccess()
        {
            InitGetClusterListMock(s_mockedClusters);
            RunRefreshClustersCommand();
        }

        private void RunRefreshClustersCommandFailure()
        {
            InitGetClusterListMock(new List<Cluster>());
            RunRefreshClustersCommand();
        }

        private void RunRefreshClustersCommand()
        {
            _objectUnderTest.RefreshClustersListCommand.Execute(null);
        }

        protected override void AssertInitialState()
        {
            base.AssertInitialState();

            Assert.IsNotNull(_objectUnderTest.DeploymentVersion);
            Assert.IsTrue(s_validNamePattern.IsMatch(_objectUnderTest.DeploymentVersion));
        }

        private void AssertVersionExpectedState(string expected)
        {
            Assert.AreEqual(expected, _objectUnderTest.DeploymentVersion);
        }

        protected override void AssertAgainstExpected()
        {
            base.AssertAgainstExpected();

            Assert.AreEqual(_expectedDeploymentName, _objectUnderTest.DeploymentName);
            CollectionAssert.AreEquivalent(_expectedClusters.ToList(), _objectUnderTest.Clusters.ToList());
            Assert.AreEqual(_expectedSelectedCluster, _objectUnderTest.SelectedCluster);
            Assert.AreEqual(_expectedReplicas, _objectUnderTest.Replicas);
            Assert.AreEqual(_expectedRefreshClusterListCanExecute, _objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.AreEqual(_expectedCreateClusterCommandCanExecute, _objectUnderTest.CreateClusterCommand.CanExecuteCommand);
        }
    }
}
