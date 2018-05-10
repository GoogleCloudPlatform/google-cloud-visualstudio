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

using GoogleCloudExtension.TemplateWizards;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace GoogleCloudExtensionUnitTests.TemplateWizards
{
    /// <summary>
    /// Class for testing <see cref="GoogleTemplateWizardHelper"/>.
    /// </summary>
    [TestClass]
    public class GoogleTemplateWizardHelperTests
    {
        private string _sandboxDir;
        private Dictionary<string, string> _replacements;
        private string _solutionDir;
        private string _projectDir;
        private string _nonExistantDir;

        [TestInitialize]
        public void BeforeEach()
        {
            _sandboxDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _solutionDir = Path.Combine(_sandboxDir, "SolutionDir");
            _projectDir = Path.Combine(_solutionDir, "ProjectDir");
            _nonExistantDir = Path.Combine(_sandboxDir, "NonExistantDir");

            Directory.CreateDirectory(_sandboxDir);
            Directory.CreateDirectory(_solutionDir);
            Directory.CreateDirectory(_projectDir);
            File.WriteAllText(Path.Combine(_projectDir, Path.GetRandomFileName()), @"Test File");

            _replacements = new Dictionary<string, string>
            {
                {ReplacementsKeys.DestinationDirectoryKey, _projectDir},
                {ReplacementsKeys.SolutionDirectoryKey, _solutionDir }
            };
        }

        [TestCleanup]
        public void AfterEach()
        {
            Directory.Delete(_sandboxDir, true);
        }

        [TestMethod]
        public void TestCleanupDirectoriesExclusive()
        {
            _replacements[ReplacementsKeys.ExclusiveProjectKey] = bool.TrueString;

            GoogleTemplateWizardHelper.CleanupDirectories(_replacements);

            Assert.IsFalse(Directory.Exists(_projectDir));
            Assert.IsFalse(Directory.Exists(_solutionDir));
        }

        [TestMethod]
        public void TestCleanupDirectoriesNonExclusive()
        {
            _replacements[ReplacementsKeys.ExclusiveProjectKey] = bool.FalseString;

            GoogleTemplateWizardHelper.CleanupDirectories(_replacements);

            Assert.IsFalse(Directory.Exists(_projectDir));
            Assert.IsTrue(Directory.Exists(_solutionDir));
        }

        [TestMethod]
        public void TestCleanupDirectoriesNonExistant()
        {
            _replacements[ReplacementsKeys.DestinationDirectoryKey] = _nonExistantDir;
            _replacements[ReplacementsKeys.SolutionDirectoryKey] = _nonExistantDir;

            GoogleTemplateWizardHelper.CleanupDirectories(_replacements);
        }
    }
}
