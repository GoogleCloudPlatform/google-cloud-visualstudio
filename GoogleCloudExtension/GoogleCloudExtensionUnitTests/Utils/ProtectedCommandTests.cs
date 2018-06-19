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
    public static class ProtectedCommandTests
    {
        [TestClass]
        public class ProtectedCommand0Tests : ExtensionTestBase
        {

            [TestMethod]
            public void TestConstrutor_DefaultsCanExecuteCommandToTrue()
            {
                var objectUnderTest = new ProtectedCommand(() => { });

                Assert.IsTrue(objectUnderTest.CanExecuteCommand);
            }

            [TestMethod]
            public void TestConstrutor_OverridesDefaultCanExecuteCommandWithParameter()
            {
                var objectUnderTest = new ProtectedCommand(() => { }, false);

                Assert.IsFalse(objectUnderTest.CanExecuteCommand);
            }

            [TestMethod]
            public void TestExecute_InvokesProvidedAction()
            {
                var actionMock = new Mock<Action>();
                var objectUnderTest = new ProtectedCommand(actionMock.Object);

                objectUnderTest.Execute(null);

                actionMock.Verify(f => f(), Times.Once);
            }

            [TestMethod]
            public void TestExecute_DoesNotThrowWhenActionErrors()
            {
                var objectUnderTest = new ProtectedCommand(() => throw new Exception());

                objectUnderTest.Execute(null);
            }
        }

        [TestClass]
        public class ProtectedCommand1Tests : ExtensionTestBase
        {

            [TestMethod]
            public void TestConstrutor_DefaultsCanExecuteCommandToTrue()
            {
                var objectUnderTest = new ProtectedCommand<object>(_ => { });

                Assert.IsTrue(objectUnderTest.CanExecuteCommand);
            }

            [TestMethod]
            public void TestConstrutor_OverridesDefaultCanExecuteCommandWithParameter()
            {
                var objectUnderTest = new ProtectedCommand<object>(_ => { }, false);

                Assert.IsFalse(objectUnderTest.CanExecuteCommand);
            }

            [TestMethod]
            public void TestExecute_InvokesProvidedActionWithArgument()
            {
                var actionMock = new Mock<Action<object>>();
                var objectUnderTest = new ProtectedCommand<object>(actionMock.Object);

                var argument = new object();
                objectUnderTest.Execute(argument);

                actionMock.Verify(f => f(argument), Times.Once);
            }

            [TestMethod]
            public void TestExecute_DoesNotThrowWhenActionErrors()
            {
                var objectUnderTest = new ProtectedCommand<object>(_ => throw new Exception());

                objectUnderTest.Execute(null);
            }
        }
    }
}
