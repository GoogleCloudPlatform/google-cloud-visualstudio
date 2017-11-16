using Google.Apis.Container.v1.Data;
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
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.GkeStep
{
    [TestClass]
    public class GkeStepViewModelTests
    {
        private const string ProjectId = "ProjectId";

        private static readonly List<Cluster> s_mockedClusters = new List<Cluster>
        {
            new Cluster { Name = "Bcluster" },
            new Cluster { Name = "Acluster" },
            new Cluster { Name = "Ccluster" },
        };
        
        private GkeStepViewModel _objectUnderTest;
        private Mock<IApiManager> _mockedApiManager;
        private Mock<IGkeDataSource> _mockedDataSource;
        private Mock<IPublishDialog> _mockedPublishDialog;
        private Mock<IParsedProject> _mockedProject;
        private TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        private TaskCompletionSource<IList<Cluster>> _clusterListTaskSource;

        [TestInitialize]
        public void Initialize()
        {
            _mockedApiManager = new Mock<IApiManager>();
            _mockedDataSource = new Mock<IGkeDataSource>();
            _mockedPublishDialog = new Mock<IPublishDialog>();
            _mockedProject = new Mock<IParsedProject>();

            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _clusterListTaskSource = new TaskCompletionSource<IList<Cluster>>();

            _mockedApiManager.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(() => _areServicesEnabledTaskSource.Task);
            _mockedDataSource.Setup(x => x.GetClusterListAsync()).Returns(() => _clusterListTaskSource.Task);

            _mockedProject.Setup(x => x.Name).Returns(ProjectId);

            _mockedPublishDialog.Setup(x => x.TrackTask(It.IsAny<Task>()));
            _mockedPublishDialog.Setup(x => x.Project).Returns(_mockedProject.Object);

            _objectUnderTest = GkeStepViewModel.CreateStep(_mockedDataSource.Object, _mockedApiManager.Object);
        }

        [TestMethod]
        public void TestInitialState()
        {
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public async Task TestPositiveProjectValidation()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            Assert.IsNotNull(_objectUnderTest.LoadingProjectTask);

            // Check the state before the validation.
            Assert.IsTrue(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);

            _areServicesEnabledTaskSource.SetResult(true);
            _clusterListTaskSource.SetResult(s_mockedClusters);

            // Wait for project load to finish.
            await _objectUnderTest.LoadingProjectTask;

            // Check the result after validation.
            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsTrue(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
            Assert.AreEqual(ProjectId.ToLower(), _objectUnderTest.DeploymentName);
        }

        [TestMethod]
        public async Task TestNeedsApiValidation()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            Assert.IsNotNull(_objectUnderTest.LoadingProjectTask);

            // Check the state before the validation.
            Assert.IsTrue(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);

            _areServicesEnabledTaskSource.SetResult(false);
            _clusterListTaskSource.SetResult(null);

            // Wait for the project load to finish.
            await _objectUnderTest.LoadingProjectTask;

            // Check the result after validation.
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsTrue(_objectUnderTest.NeedsApiEnabled);
        }
    }
}
