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

using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class ProtectedCommandBaseTests
    {
        /// <summary>
        /// Minimal concrete implementation of ProtectedCommandBase for testing.
        /// </summary>
        private class TestProtectedCommandBase : ProtectedCommandBase
        {
            public TestProtectedCommandBase(bool canExecuteCommand) : base(canExecuteCommand) { }
            public override void Execute(object argument) => throw new NotSupportedException();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void TestConstructor_SetsCanExecuteCommand(bool input)
        {
            var objectUnderTest = new TestProtectedCommandBase(input);

            Assert.AreEqual(input, objectUnderTest.CanExecuteCommand);
            Assert.AreEqual(input, objectUnderTest.CanExecute(null));
        }


        [TestMethod]
        public void TestSetCanExecuteCommand_UpdatesCanExecuteCommand()
        {
            var objectUnderTest = new TestProtectedCommandBase(false);

            objectUnderTest.CanExecuteCommand = true;

            Assert.IsTrue(objectUnderTest.CanExecuteCommand);
        }

        [TestMethod]
        public void TestSetCanExecuteCommand_UpdatesCanExecute()
        {
            var objectUnderTest = new TestProtectedCommandBase(false);

            objectUnderTest.CanExecuteCommand = true;

            Assert.IsTrue(objectUnderTest.CanExecute(null));
        }

        [TestMethod]
        public void TestSetCanExecuteCommand_InvokesCanExecuteChanged()
        {
            var objectUnderTest = new TestProtectedCommandBase(false);
            var eventHandlerMock = new Mock<Action<object, EventArgs>>();

            objectUnderTest.CanExecuteChanged += new EventHandler(eventHandlerMock.Object);
            objectUnderTest.CanExecuteCommand = true;

            eventHandlerMock.Verify(a => a(objectUnderTest, EventArgs.Empty), Times.Once);
        }

        [TestMethod]
        public void TestSetCanExecuteCommand_WithoutChangeDoesNotInvokeCanExecuteChanged()
        {
            var objectUnderTest = new TestProtectedCommandBase(false);
            var eventHandlerMock = new Mock<Action<object, EventArgs>>();

            objectUnderTest.CanExecuteChanged += new EventHandler(eventHandlerMock.Object);
            objectUnderTest.CanExecuteCommand = false;

            eventHandlerMock.Verify(a => a(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Never);
        }
    }
}
