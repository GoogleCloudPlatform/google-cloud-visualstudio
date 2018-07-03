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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GoogleCloudExtension.GCloud.UnitTests
{
    [TestClass]
    public class GCloudValidationResultTests
    {
        [TestMethod]
        public void TestNotInstalled_IsNotValid()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.NotInstalled;
            Assert.IsFalse(objectUnderTest.IsValid);
        }

        [TestMethod]
        public void TestNotInstalled_IsNotInstalled()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.NotInstalled;
            Assert.IsFalse(objectUnderTest.IsCloudSdkInstalled);
        }

        [TestMethod]
        public void TestNotInstalled_IsNotUpdated()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.NotInstalled;
            Assert.IsTrue(objectUnderTest.IsObsolete);
        }

        [TestMethod]
        public void TestNotInstalled_HasNoVersion()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.NotInstalled;
            Assert.IsNull(objectUnderTest.CloudSdkVersion);
        }

        [TestMethod]
        public void TestGetNotUpdated_IsNotValid()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.GetObsoleteVersion(null);
            Assert.IsFalse(objectUnderTest.IsValid);
        }

        [TestMethod]
        public void TestGetNotUpdated_IsInstalled()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.GetObsoleteVersion(null);
            Assert.IsTrue(objectUnderTest.IsCloudSdkInstalled);
        }

        [TestMethod]
        public void TestGetNotUpdated_IsNotUpdated()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.GetObsoleteVersion(null);
            Assert.IsTrue(objectUnderTest.IsObsolete);
        }

        [TestMethod]
        public void TestGetNotUpdated_HasExpectedVersion()
        {
            var expectedVersion = new Version(5, 3, 4);
            GCloudValidationResult objectUnderTest = GCloudValidationResult.GetObsoleteVersion(expectedVersion);
            Assert.AreEqual(expectedVersion, objectUnderTest.CloudSdkVersion);
        }

        [TestMethod]
        public void TestMissingComponent_IsNotValid()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.MissingComponent;
            Assert.IsFalse(objectUnderTest.IsValid);
        }

        [TestMethod]
        public void TestMissingComponent_IsInstalled()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.MissingComponent;
            Assert.IsTrue(objectUnderTest.IsCloudSdkInstalled);
        }

        [TestMethod]
        public void TestMissingComponent_IsUpdated()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.MissingComponent;
            Assert.IsFalse(objectUnderTest.IsObsolete);
        }

        [TestMethod]
        public void TestValid_IsValid()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.Valid;
            Assert.IsTrue(objectUnderTest.IsValid);
        }

        [TestMethod]
        public void TestValid_IsInstalled()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.Valid;
            Assert.IsTrue(objectUnderTest.IsCloudSdkInstalled);
        }

        [TestMethod]
        public void TestValid_IsUpdated()
        {
            GCloudValidationResult objectUnderTest = GCloudValidationResult.Valid;
            Assert.IsFalse(objectUnderTest.IsObsolete);
        }
    }
}
