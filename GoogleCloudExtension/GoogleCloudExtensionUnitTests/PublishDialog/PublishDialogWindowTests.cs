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
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.PublishDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtensionUnitTests.PublishDialog
{
    [TestClass]
    public class PublishDialogWindowTests : WpfTestBase<PublishDialogWindow>
    {
        private const string ExpectedProjectName = "Expected Project Name";
        private PublishDialogWindow _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            var packageMock = new Mock<IGoogleCloudExtensionPackage> { DefaultValueProvider = DefaultValueProvider.Mock };
            GoogleCloudExtensionPackage.Instance = packageMock.Object;
            _objectUnderTest = new PublishDialogWindow(Mock.Of<IParsedDteProject>(p => p.Name == ExpectedProjectName));
        }

        [TestMethod]
        [DataRow(KnownProjectTypes.WebApplication)]
        [DataRow(KnownProjectTypes.NetCoreWebApplication)]
        public void TestCanPublish_True(KnownProjectTypes validKnownProjectType)
        {
            bool result = PublishDialogWindow.CanPublish(
                Mock.Of<IParsedProject>(p => p.ProjectType == validKnownProjectType));

            Assert.IsTrue(result);
        }

        [TestMethod]
        [DataRow(KnownProjectTypes.None)]
        [DataRow(3000)]
        public void TestCanPublish_False(KnownProjectTypes invalidKnownProjectType)
        {
            bool result = PublishDialogWindow.CanPublish(
                Mock.Of<IParsedProject>(p => p.ProjectType == invalidKnownProjectType));

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestConstructor_SetsDataContext()
        {
            Assert.AreEqual(_objectUnderTest.DataContext, ((FrameworkElement)_objectUnderTest.Content).DataContext);
        }

        [TestMethod]
        public async Task TestTitle_DataBound()
        {
            await GetWindowAsync(() => _objectUnderTest.ShowModal());
            StringAssert.Contains(_objectUnderTest.Title, ExpectedProjectName);
        }

        /// <summary>
        /// Implementers must register the given handler to an event that is fired when the window to test is activated.
        /// </summary>
        /// <param name="handler">The event handler that will close the window.</param>
        protected override void RegisterActivatedEvent(EventHandler handler) => _objectUnderTest.Activated += handler;

        /// <summary>
        /// Implementers must use this to unregister the given handler from the event registered in
        /// <see cref="WpfTestBase{TWindow}.RegisterActivatedEvent"/>.
        /// </summary>
        /// <param name="handler">The event handler to unregister from the event.</param>
        protected override void UnregisterActivatedEvent(EventHandler handler) => _objectUnderTest.Activated -= handler;
    }
}
