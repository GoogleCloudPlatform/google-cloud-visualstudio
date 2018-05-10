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

using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.CloudExplorerSources.PubSub;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.PubSub
{
    [TestClass]
    public class OrphanedSubscriptionsViewModelTests
    {
        private const string MockProjectId = "mock-project-id";

        [TestMethod]
        public void TestInitialConditions()
        {
            var rootViewModelMock = new Mock<IPubsubSourceRootViewModel>();
            rootViewModelMock.SetupGet(vm => vm.Context.CurrentProject.ProjectId).Returns(MockProjectId);
            var objectUnderTest = new OrphanedSubscriptionsViewModel(
                rootViewModelMock.Object, Enumerable.Empty<Subscription>());

            List<MenuItem> menuItems = objectUnderTest.ContextMenu.ItemsSource.Cast<MenuItem>().ToList();
            var expectedMenuHeaders = new[] { Resources.UiOpenOnCloudConsoleMenuHeader };
            CollectionAssert.AreEquivalent(expectedMenuHeaders, menuItems.Select(mi => mi.Header).ToList());
        }

        [TestMethod]
        public void TestOpenCloudConsoleCommand()
        {
            var rootViewModelMock = new Mock<IPubsubSourceRootViewModel>();
            rootViewModelMock.SetupGet(vm => vm.Context.CurrentProject.ProjectId).Returns(MockProjectId);
            var objectUnderTest = new OrphanedSubscriptionsViewModel(
                rootViewModelMock.Object, Enumerable.Empty<Subscription>());

            List<MenuItem> menuItems = objectUnderTest.ContextMenu.ItemsSource.Cast<MenuItem>().ToList();
            menuItems.Single(mi => mi.Header.Equals(Resources.UiOpenOnCloudConsoleMenuHeader)).Command.Execute(null);

            string expectedUrl = string.Format(
                OrphanedSubscriptionsViewModel.PubSubConsoleSubscriptionsUrlFormat, MockProjectId);
            rootViewModelMock.Verify(vm => vm.OpenBrowser(expectedUrl));
        }
    }
}
