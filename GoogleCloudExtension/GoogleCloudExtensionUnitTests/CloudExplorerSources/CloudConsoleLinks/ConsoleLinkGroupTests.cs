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

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.CloudExplorerSources.CloudConsoleLinks;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.CloudConsoleLinks
{
    [TestClass]
    public class ConsoleLinkGroupTests
    {
        private static readonly ICloudSourceContext s_mockedContext = Mock.Of<ICloudSourceContext>();
        private const string DefaultCaption = "DefaultCaption";

        [TestMethod]
        public void TestConstructor_SetsCaption()
        {
            const string testCaption = "Test Caption";
            var objectUnderTest = new ConsoleLinkGroup(
                testCaption, s_mockedContext, new LinkInfo[] { });

            Assert.AreEqual(testCaption, objectUnderTest.Caption);
        }

        [TestMethod]
        public void TestConstructor_AddsConsoleLinkChilderen()
        {
            var firstLinkInfo = new LinkInfo("first url", "first caption");
            var secondLinkInfo = new LinkInfo("second url", "second caption");
            LinkInfo[] linkInfos = { firstLinkInfo, secondLinkInfo };
            var objectUnderTest = new ConsoleLinkGroup(
                DefaultCaption, s_mockedContext, linkInfos);

            CollectionAssert.AllItemsAreInstancesOfType(objectUnderTest.Children.ToList(), typeof(ConsoleLink));
            CollectionAssert.AreEqual(
                linkInfos.Select(l => l.Caption).ToList(), objectUnderTest.Children.Select(l => l.Caption).ToList());
        }
    }
}
