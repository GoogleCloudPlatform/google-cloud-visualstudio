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

using GoogleCloudExtension.AppEngineManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Theming;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.AppEngineManagement
{
    /// <summary>
    /// Test class for <seealso cref="AppEngineManagementViewModel"/>
    /// </summary>
    [TestClass]
    public class AppEngineManagementViewModelTests
    {
        private const string TestProjectId = "TestProjectId";

        private static readonly List<string> s_mockFlexLocations = new List<string>
        {
            "us-something",
            "mars-other",
            "antartica-west-1d",
            AppEngineManagementViewModel.DefaultRegionName
        };
        private static readonly List<string> s_sortedMockFlexLocations = s_mockFlexLocations.OrderBy(x => x).ToList();

        private TaskCompletionSource<IList<string>> _flexLocationsSource;
        private Mock<IGaeDataSource> _mockedGaeDataSource;
        private Mock<ICloseable> _mockedWindow;
        private AppEngineManagementViewModel _testedViewModel;

        [TestInitialize]
        public void Initialize()
        {
            _flexLocationsSource = new TaskCompletionSource<IList<string>>();

            _mockedGaeDataSource = new Mock<IGaeDataSource>();
            _mockedGaeDataSource.Setup(ds => ds.GetFlexLocationsAsync()).Returns(() => _flexLocationsSource.Task);

            _mockedWindow = new Mock<ICloseable>();

            _testedViewModel = new AppEngineManagementViewModel(_mockedWindow.Object, TestProjectId, _mockedGaeDataSource.Object);
        }

        [TestMethod]
        public void InitialStateTests()
        {
            Assert.IsTrue(_testedViewModel.Locations.IsPending);
            Assert.AreEqual(TestProjectId, _testedViewModel.ProjectId);
            Assert.AreEqual(AppEngineManagementViewModel.s_loadingPlaceholder.First(), _testedViewModel.SelectedLocation);
            CollectionAssert.AreEqual(AppEngineManagementViewModel.s_loadingPlaceholder.ToList(), _testedViewModel.Locations.Value.ToList());
        }

        [TestMethod]
        public async Task LocationsLoadedTest()
        {
            _flexLocationsSource.SetResult(s_mockFlexLocations);
            await _testedViewModel.Locations.ValueTask;

            Assert.AreEqual(AppEngineManagementViewModel.DefaultRegionName, _testedViewModel.SelectedLocation);
            CollectionAssert.AreEqual(s_sortedMockFlexLocations, _testedViewModel.Locations.Value.ToList());
        }

        [TestMethod]
        public async Task CanExecuteTest()
        {
            _flexLocationsSource.SetResult(s_mockFlexLocations);
            await _testedViewModel.Locations.ValueTask;
            _testedViewModel.SelectedLocation = s_mockFlexLocations.First();

            Assert.IsTrue(_testedViewModel.ActionCommand.CanExecute(null));
        }

        [TestMethod]
        public async Task ResultTest()
        {
            _flexLocationsSource.SetResult(s_mockFlexLocations);
            await _testedViewModel.Locations.ValueTask;
            _testedViewModel.SelectedLocation = s_mockFlexLocations.First();

            _testedViewModel.ActionCommand.Execute(null);

            Assert.AreEqual(s_mockFlexLocations.First(), _testedViewModel.Result);
            _mockedWindow.Verify(x => x.Close(), Times.Once, "Failed to close the window on action.");
        }
    }
}
