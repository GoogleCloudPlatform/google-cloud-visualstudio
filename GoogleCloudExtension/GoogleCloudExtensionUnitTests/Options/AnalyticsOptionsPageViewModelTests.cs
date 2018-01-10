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

using GoogleCloudExtension.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.ComponentModel;

namespace GoogleCloudExtensionUnitTests.Options
{
    [TestClass]
    public class AnalyticsOptionsPageViewModelTests
    {
        [TestMethod]
        public void TestInitialConditions()
        {
            var objectUnderTest = new AnalyticsOptionsPageViewModel();

            Assert.IsFalse(objectUnderTest.OptIn);
        }

        [TestMethod]
        public void TestSetOptIn()
        {
            var objectUnderTest = new AnalyticsOptionsPageViewModel();

            objectUnderTest.OptIn = true;

            Assert.IsTrue(objectUnderTest.OptIn);
        }

        [TestMethod]
        public void TestSetOptInRaisesPropertyChanged()
        {
            var objectUnderTest = new AnalyticsOptionsPageViewModel();
            var propertyChangedHandler = new Mock<PropertyChangedEventHandler>();
            objectUnderTest.PropertyChanged += propertyChangedHandler.Object;

            objectUnderTest.OptIn = true;

            propertyChangedHandler.Verify(
                h => h(
                    objectUnderTest,
                    It.Is<PropertyChangedEventArgs>(args => args.PropertyName == nameof(objectUnderTest.OptIn))),
                Times.Once);
        }
    }
}
