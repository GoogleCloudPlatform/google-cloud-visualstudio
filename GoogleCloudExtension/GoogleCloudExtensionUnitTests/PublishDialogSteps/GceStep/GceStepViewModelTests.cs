﻿using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.GceStep;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.GceStep
{
    /// <summary>
    /// Unit tests for <seealso cref="GceStepViewModel"/>
    /// </summary>
    [TestClass]
    public class GceStepViewModelTests
    {
        private const string WindowsLicenseUrl = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2016-dc";

        private static readonly AttachedDisk s_windowsDisk = new AttachedDisk
        {
            Licenses = new string[] { WindowsLicenseUrl },
            Boot = true
        };

        private static readonly IList<Instance> s_mockedInstances = new List<Instance>
        {
            new Instance { Name="Ainstance", Disks = new AttachedDisk[] { s_windowsDisk }, Status = "RUNNING" }
        };

        private GceStepViewModel _objectUnderTest;
        private Mock<IApiManager> _mockedApiManager;
        private Mock<IGceDataSource> _mockedDataSource;
        private Mock<IPublishDialog> _mockedPublishDialog;
        private TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        private TaskCompletionSource<IList<Instance>> _getInstanceListTaskSource;

        [TestInitialize]
        public void InitializeTest()
        {
            _mockedApiManager = new Mock<IApiManager>();
            _mockedDataSource = new Mock<IGceDataSource>();
            _mockedPublishDialog = new Mock<IPublishDialog>();

            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _getInstanceListTaskSource = new TaskCompletionSource<IList<Instance>>();

            _mockedApiManager.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(_areServicesEnabledTaskSource.Task);
            _mockedDataSource.Setup(x => x.GetInstanceListAsync()).Returns(_getInstanceListTaskSource.Task);

            _objectUnderTest = GceStepViewModel.CreateStep(_mockedDataSource.Object, _mockedApiManager.Object);
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
            _getInstanceListTaskSource.SetResult(s_mockedInstances);

            // Wait for project load to finish.
            await _objectUnderTest.LoadingProjectTask;

            // Validate the state after the load.
            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsTrue(_objectUnderTest.Instances.IsCompleted);
            Assert.AreEqual(s_mockedInstances.Count, _objectUnderTest.Instances.Value.Count());

            // Verify the expected methods were called.
            _mockedApiManager.Verify(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>()), Times.AtLeastOnce);
            _mockedDataSource.Verify(x => x.GetInstanceListAsync(), Times.AtLeastOnce);
            _mockedPublishDialog.Verify(x => x.TrackTask(It.IsAny<Task>()), Times.AtLeastOnce);
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
            _getInstanceListTaskSource.SetResult(null);

            // Wait for the project load to finish.
            await _objectUnderTest.LoadingProjectTask;

            // Validate the state after the load.
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsTrue(_objectUnderTest.NeedsApiEnabled);

            // Verify that the expected methods were called.
            _mockedApiManager.Verify(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>()), Times.AtLeastOnce);
            _mockedDataSource.Verify(x => x.GetInstanceListAsync(), Times.Never);
            _mockedPublishDialog.Verify(x => x.TrackTask(It.IsAny<Task>()), Times.AtLeastOnce);
        }
    }
}
