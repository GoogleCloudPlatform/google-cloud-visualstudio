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

using GoogleCloudExtension.SolutionUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtensionUnitTests
{
    /// <summary>
    /// Test class for solution options
    /// </summary>
    public class TestOptions
    {
        [SolutionSettingKey("google_string_1")]
        public string StringOption { get; set; } = "string option 1";

        [SolutionSettingKey("google_string_2")]
        public string StringOption2 { get; set; } = "string option 2";

        public string NonOption { get; set; }

        public string NonOption2 => "Test string";
    }

    /// <summary>
    /// Test class for solution options
    /// </summary>
    public class TestOptionsValid2
    {
        [SolutionSettingKey("google_string_3")]
        public string StringOption { get; set; } = "string option 3";

        [SolutionSettingKey("google_string_4")]
        public string StringOption2 { get; set; } = "string option 4";

        public int NonOption { get; set; }

        public string NonOption2 => "Test string";
    }

    /// <summary>
    /// Test class for solution options
    /// </summary>
    public class TestOptionsNonStringType
    {
        [SolutionSettingKey("google_int")]
        public int IntOption { get; set; }

        [SolutionSettingKey("google_string_2")]
        public string StringOption2 { get; set; }
    }

    /// <summary>
    /// Test class for solution options
    /// </summary>
    public class TestOptionsDuplicatekey
    {
        [SolutionSettingKey("google_string_1")]
        public string StringOption { get; set; } = "string option 1";

        [SolutionSettingKey("google_string_1")]
        public string StringOption2 { get; set; } = "string option 2";
    }

    /// <summary>
    /// Test class for solution options
    /// </summary>
    public class TestOptionsPrivateGet
    {
        [SolutionSettingKey("google_string_1")]
        public string StringOption { private get; set; } = "string option 1";
    }

    /// <summary>
    /// Test class for solution options
    /// </summary>
    public class TestOptionsPrivateSet
    {
        [SolutionSettingKey("google_string_1")]
        public string StringOption { get; private set; } = "string option 1";
    }

    [TestClass]
    public class SolutionUserOptionsTests
    {
        [TestMethod]
        public void ValidOptions()
        {
            var testOptions = new TestOptions();
            var userOptionsOntestOptions = new SolutionUserOptions(testOptions);
            List<SolutionUserOptions> options = new List<SolutionUserOptions>
            {
                userOptionsOntestOptions,
                new SolutionUserOptions(new TestOptionsValid2()),
            };

            List<string> expectedResults = new List<string>
            {
                "google_string_1",
                "google_string_2",
                "google_string_3",
                "google_string_4",
            };
            List<string> optionKeys = new List<string>();

            foreach (var key in options.SelectMany(setting => setting.Keys))
            {
                optionKeys.Add(key);
            }

            CollectionAssert.AreEqual(expectedResults, optionKeys);

            string testKey = "google_string_1";
            Assert.AreEqual("string option 1", userOptionsOntestOptions.Read(testKey));
            string newValue = "string opiton new value";
            userOptionsOntestOptions.Set(testKey, newValue);
            Assert.AreEqual(newValue, userOptionsOntestOptions.Read(testKey));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException), AllowDerivedTypes = false)]
        public void TestOptionsNonStringType()
        {
            var options = new SolutionUserOptions(new TestOptionsNonStringType());
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException), AllowDerivedTypes = false)]
        public void TestOptionsPrivateSet()
        {
            var options = new SolutionUserOptions(new TestOptionsPrivateSet());
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException), AllowDerivedTypes = false)]
        public void TestOptionsPrivateGet()
        {
            var options = new SolutionUserOptions(new TestOptionsPrivateGet());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "An item with the same key has already been added.", AllowDerivedTypes = false)]
        public void TestOptionsDuplicatekey()
        {
            var options = new SolutionUserOptions(new TestOptionsDuplicatekey());
        }
    }
}
