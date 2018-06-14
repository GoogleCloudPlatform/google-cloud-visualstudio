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
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class StatusbarHelperTests : ExtensionTestBase
    {
        private const string StatusbarMessage = "Message On Statusbar";
        private Mock<IVsStatusbar> _vsStatusbarMock;

        protected override void BeforeEach()
        {
            _vsStatusbarMock = new Mock<IVsStatusbar>();
            PackageMock.Setup(p => p.GetService<SVsStatusbar, IVsStatusbar>()).Returns(_vsStatusbarMock.Object);
            SetHelperStatusBarToDefault();
        }

        protected override void AfterEach() => SetHelperStatusBarToDefault();

        private static void SetHelperStatusBarToDefault() => StatusbarHelper.s_statusbar =
            new Lazy<IVsStatusbar>(GoogleCloudExtensionPackage.Instance.GetService<SVsStatusbar, IVsStatusbar>);

        [TestMethod]
        public void TestSetText_DoesNotSetWhenFrozen()
        {
            // ReSharper disable once RedundantAssignment
            int isFrozenTrue = Convert.ToInt32(true);
            _vsStatusbarMock.Setup(sb => sb.IsFrozen(out isFrozenTrue)).Returns(VSConstants.S_OK);

            StatusbarHelper.SetText("Message On Statusbar");

            _vsStatusbarMock.Verify(sb => sb.SetText(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestSetText_SetsWhenNotFrozen()
        {
            // ReSharper disable once RedundantAssignment
            int isFrozenFalse = Convert.ToInt32(false);
            _vsStatusbarMock.Setup(sb => sb.IsFrozen(out isFrozenFalse)).Returns(VSConstants.S_OK);

            StatusbarHelper.SetText(StatusbarMessage);

            _vsStatusbarMock.Verify(sb => sb.SetText(StatusbarMessage), Times.Once);
        }

        [TestMethod]
        public void TestFreezeText_DoesNothingWhenAlreadyFrozen()
        {
            // ReSharper disable once RedundantAssignment
            int isFrozenTrue = Convert.ToInt32(true);
            _vsStatusbarMock.Setup(sb => sb.IsFrozen(out isFrozenTrue)).Returns(VSConstants.S_OK);

            StatusbarHelper.FreezeText(StatusbarMessage);

            _vsStatusbarMock.Verify(sb => sb.SetText(It.IsAny<string>()), Times.Never);
            _vsStatusbarMock.Verify(sb => sb.FreezeOutput(It.IsAny<int>()), Times.Never);

        }

        [TestMethod]
        public void TestFreezeText_Dispose_DoesNothingWhenAlreadyFrozen()
        {
            // ReSharper disable once RedundantAssignment
            int isFrozenTrue = Convert.ToInt32(true);
            _vsStatusbarMock.Setup(sb => sb.IsFrozen(out isFrozenTrue)).Returns(VSConstants.S_OK);

            StatusbarHelper.FreezeText(StatusbarMessage).Dispose();

            _vsStatusbarMock.Verify(sb => sb.SetText(It.IsAny<string>()), Times.Never);
            _vsStatusbarMock.Verify(sb => sb.FreezeOutput(It.IsAny<int>()), Times.Never);

        }

        [TestMethod]
        public void TestFreezeText_SetsText()
        {
            // ReSharper disable once RedundantAssignment
            int isFrozenFalse = Convert.ToInt32(false);
            _vsStatusbarMock.Setup(sb => sb.IsFrozen(out isFrozenFalse)).Returns(VSConstants.S_OK);

            StatusbarHelper.FreezeText(StatusbarMessage);

            _vsStatusbarMock.Verify(sb => sb.SetText(StatusbarMessage), Times.Once);
        }

        [TestMethod]
        public void TestFreezeText_FreezesStatusBar()
        {
            // ReSharper disable once RedundantAssignment
            int isFrozenFalse = Convert.ToInt32(false);
            _vsStatusbarMock.Setup(sb => sb.IsFrozen(out isFrozenFalse)).Returns(VSConstants.S_OK);

            StatusbarHelper.FreezeText(StatusbarMessage);

            _vsStatusbarMock.Verify(sb => sb.FreezeOutput(Convert.ToInt32(true)), Times.Once);
        }

        [TestMethod]
        public void TestFreezeText_Dispose_ResetsText()
        {
            // ReSharper disable once RedundantAssignment
            int isFrozenFalse = Convert.ToInt32(false);
            _vsStatusbarMock.Setup(sb => sb.IsFrozen(out isFrozenFalse)).Returns(VSConstants.S_OK);
            // ReSharper disable once RedundantAssignment
            var oldText = "Old Statusbar Message";
            _vsStatusbarMock.Setup(sb => sb.GetText(out oldText)).Returns(VSConstants.S_OK);

            StatusbarHelper.FreezeText(StatusbarMessage).Dispose();

            _vsStatusbarMock.Verify(sb => sb.SetText(oldText), Times.Once);
        }

        [TestMethod]
        public void TestFreezeText_Dispose_UnfreezesStatusBar()
        {
            // ReSharper disable once RedundantAssignment
            int isFrozenFalse = Convert.ToInt32(false);
            _vsStatusbarMock.Setup(sb => sb.IsFrozen(out isFrozenFalse)).Returns(VSConstants.S_OK);

            StatusbarHelper.FreezeText(StatusbarMessage).Dispose();

            _vsStatusbarMock.Verify(sb => sb.FreezeOutput(Convert.ToInt32(false)), Times.Once);
        }
    }
}
