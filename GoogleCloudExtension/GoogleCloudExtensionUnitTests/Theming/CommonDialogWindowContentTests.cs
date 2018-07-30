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

using GoogleCloudExtension.Theming;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.Theming
{
    [TestClass]
    public class CommonDialogWindowContentTests
    {
        private const string DefaultTitle = "Default Title";

        [TestMethod]
        public void TestConstructor_SetsTitle()
        {
            const string expectedTitle = "Expected Title";

            var objectUnderTest = new TestCommonWindowContent(new object(), expectedTitle);

            Assert.AreEqual(expectedTitle, objectUnderTest.Title);
        }

        [TestMethod]
        public void TestConstructor_SetsViewModel()
        {
            var expectedViewModel = new object();

            var objectUnderTest = new TestCommonWindowContent(expectedViewModel, DefaultTitle);

            Assert.AreEqual(expectedViewModel, objectUnderTest.ViewModel);
        }

        [TestMethod]
        public void TestConstructor_SetsDataContext()
        {
            var expectedDataContext = new object();

            var objectUnderTest = new TestCommonWindowContent(expectedDataContext, DefaultTitle);

            Assert.AreEqual(expectedDataContext, objectUnderTest.DataContext);
        }

        private class TestCommonWindowContent : CommonWindowContent<object>
        {
            public TestCommonWindowContent(object viewModel, string title) : base(viewModel, title) { }
        }
    }
}
