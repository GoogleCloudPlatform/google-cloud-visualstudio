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

using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps.CoreGceWarning;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.PublishDialog.Steps.CoreGceWarning
{
    [TestClass]
    public class CoreGceWarningContentTests
    {
        private CoreGceWarningStepContent _objectUnderTest;
        private IPublishDialog _publishDialog;

        [TestInitialize]
        public void BeforeEach()
        {
            _publishDialog = Mock.Of<IPublishDialog>(pd => pd.Project.Name == "SomeVsProjectName");
        }

        [TestMethod]
        public void TestConstructor_Initalizes()
        {
            _objectUnderTest = new CoreGceWarningStepContent(_publishDialog);

            Assert.IsTrue(_objectUnderTest.IsInitialized);
        }

        [TestMethod]
        public void TestConstructor_SetsViewModel()
        {
            _objectUnderTest = new CoreGceWarningStepContent(_publishDialog);

            Assert.IsNotNull(_objectUnderTest.ViewModel);
        }

        [TestMethod]
        public void TestConstructor_SetsDataContextToViewModel()
        {
            _objectUnderTest = new CoreGceWarningStepContent(_publishDialog);

            Assert.AreEqual(_objectUnderTest.ViewModel, _objectUnderTest.DataContext);
        }
    }
}
