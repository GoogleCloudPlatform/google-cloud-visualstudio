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

using System.Windows.Input;
using System.Windows.Media.Imaging;
using GoogleCloudExtension.PublishDialog.Steps.Choice;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.PublishDialog.Steps.Choice
{
    [TestClass]
    public class ChoiceTests
    {
        [TestMethod]
        public void TestConstructor_SetsId()
        {

            const ChoiceType id = ChoiceType.Gce;
            var objectUnderTest = new GoogleCloudExtension.PublishDialog.Steps.Choice.Choice(id, null, null, null, null);

            Assert.AreEqual(id, objectUnderTest.Id);
        }

        [TestMethod]
        public void TestConstructor_SetsName()
        {
            const string name = "Test Name";

            var objectUnderTest =
                new GoogleCloudExtension.PublishDialog.Steps.Choice.Choice(ChoiceType.None, name, null, null, null);

            Assert.AreEqual(name, objectUnderTest.Name);
        }

        [TestMethod]
        public void TestConstructor_SetsToolTip()
        {
            const string toolTip = "Test ToolTip";

            var objectUnderTest =
                new GoogleCloudExtension.PublishDialog.Steps.Choice.Choice(ChoiceType.None, null, toolTip, null, null);

            Assert.AreEqual(toolTip, objectUnderTest.ToolTip);
        }

        [TestMethod]
        public void TestConstructor_SetsIcon()
        {
            var icon = new BitmapImage();

            var objectUnderTest =
                new GoogleCloudExtension.PublishDialog.Steps.Choice.Choice(ChoiceType.None, null, null, icon, null);

            Assert.AreEqual(icon, objectUnderTest.Icon);
        }

        [TestMethod]
        public void TestConstructor_SetsCommand()
        {
            var command = Mock.Of<ICommand>();

            var objectUnderTest =
                new GoogleCloudExtension.PublishDialog.Steps.Choice.Choice(ChoiceType.None, null, null, null, command);

            Assert.AreEqual(command, objectUnderTest.Command);
        }
    }
}
