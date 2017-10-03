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

using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.CloudSourceRepositories.v1.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.CloudSourceRepositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.CloudSourceRepository
{
    [TestClass]
    public class CsrAddRepoWindowViewModelTests
    {
        private IList<Repo> _testRepos;
        private CsrAddRepoWindowViewModel _testViewModel;
        private Mock<CsrAddRepoWindow> _addRepoWindowMock;
        private Mock<Project> _projectMock;

        [TestInitialize]
        public void Initialize()
        {
            _addRepoWindowMock = new Mock<CsrAddRepoWindow>();
            _testRepos = new List<Repo>();
            _projectMock = new Mock<Project>();
            _testViewModel = new CsrAddRepoWindowViewModel(
                _addRepoWindowMock.Object, _testRepos, _projectMock.Object);
        }

        [TestMethod]
        public void InitialStateTest()
        {
            Assert.IsFalse(_testViewModel.OkCommand.CanExecuteCommand);
            Assert.IsFalse(_testViewModel.HasErrors);
            Assert.IsTrue(_testViewModel.IsReady);
            Assert.IsNull(_testViewModel.RepositoryName);
        }

        [TestMethod]
        public void RepoNameLengthTooShortValidationTest()
        {
            TestRepoNameLengthOutofRange("vv");
        }

        [TestMethod]
        public void RepoNameLengthTooLongValidationTest()
        {
            string longName = new string('v', 63);
            TestValidRepoName(longName);
            TestRepoNameLengthOutofRange(longName + 'w');   // 64 characters
        }


        [TestMethod]
        public void InvalidRepoNameFirstCharTest()
        {
            _testViewModel.RepositoryName = '-' + "valid";
            Assert.IsFalse(_testViewModel.OkCommand.CanExecuteCommand);
            var error = _testViewModel.ValidateRepoName()?.FirstOrDefault();
            Assert.IsNotNull(error);
            Assert.AreEqual(Resources.CsrRepoNameFirstCharacterExtraRuleMessage, error.ErrorContent);
        }

        [TestMethod]
        public void ValidRepoNameValidationTest()
        {
            TestValidRepoName("valid_name");

            TestRepoNameValidRange('a', 'z');
            TestRepoNameValidRange('A', 'Z');
            TestRepoNameValidRange('0', '9');
            TestValidRepoName("_This-is_valid");    // _ and - character
        }

        public void InvalidRepoNameTest()
        {
            TestRepoNameInvalidRange(Convert.ToChar(0), Convert.ToChar(44));    // 45 is -
            TestRepoNameInvalidRange(Convert.ToChar(46), Convert.ToChar(47));   // 48 is 0
            TestRepoNameInvalidRange(Convert.ToChar(58), Convert.ToChar(64));   // 57 is 9, 66 is A
            TestRepoNameInvalidRange(Convert.ToChar(91), Convert.ToChar(94));   // 90 is Z, 95 is _
            TestRepoNameInvalidRange(Convert.ToChar(96), Convert.ToChar(96));   // 97 is a,  122 is z
            TestRepoNameInvalidRange(Convert.ToChar(123), Convert.ToChar(255));
        }

        private void TestRepoNameLengthOutofRange(string name)
        {
            _testViewModel.RepositoryName = name;
            Assert.IsFalse(_testViewModel.OkCommand.CanExecuteCommand);
            var error = _testViewModel.ValidateRepoName()?.FirstOrDefault();
            Assert.IsNotNull(error);
            Assert.AreEqual(Resources.CsrRepoNameLengthLimitMessage, error.ErrorContent);
        }

        private void TestRepoNameValidRange(char low, char high)
        {
            for (char ch = low; ch <= high; ++ch)
            {
                TestValidRepoName(ch.ToString() + "valid_name" + ch.ToString());
            }
        }

        private void TestRepoNameInvalidRange(char low, char high)
        {
            for (char ch = low; ch <= high; ++ch)
            {
                TestInvalidRepoNameChar(ch);
            }
        }

        private void TestValidRepoName(string name)
        {
            _testViewModel.RepositoryName = name;
            Assert.IsTrue(_testViewModel.OkCommand.CanExecuteCommand);
            Assert.IsFalse(_testViewModel.HasErrors);
        }

        private void TestInvalidRepoNameChar(char ch)
        {
            _testViewModel.RepositoryName = "valid" + ch.ToString() + "valid";
            Assert.IsFalse(_testViewModel.OkCommand.CanExecuteCommand);
            var error = _testViewModel.ValidateRepoName()?.FirstOrDefault();
            Assert.IsNotNull(error);
            Assert.AreEqual(Resources.CsrRepoNameRuleMessage, error.ErrorContent);
        }
    }
}
