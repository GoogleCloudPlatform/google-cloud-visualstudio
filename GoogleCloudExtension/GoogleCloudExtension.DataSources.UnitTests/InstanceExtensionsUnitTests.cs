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

using Google.Apis.Compute.v1.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.UnitTests
{
    /// <summary>
    /// Tests for the <seealso cref="InstanceExtensions"/> class.
    /// </summary>
    [TestClass]
    public class InstanceExtensionsUnitTests
    {
        private static readonly Instance s_sampleInstance = new Instance
        {
            NetworkInterfaces = new List<NetworkInterface>
            {
                new NetworkInterface
                {
                    AccessConfigs = new List<AccessConfig>
                    {
                        new AccessConfig { NatIP = "1.2.3.4" }
                    }
                }
            }
        };

        private const string ExpectedSampleInstanceFQDN = "4.3.2.1.bc.googleusercontent.com";

        /// <summary>
        /// This test ensures that we get the right FQDN from a chosen instance.
        /// </summary>
        [TestMethod]
        public void GetFQDNTest()
        {
            var actual = s_sampleInstance.GetFullyQualifiedDomainName();
            Assert.AreEqual(ExpectedSampleInstanceFQDN, actual);
        }

        /// <summary>
        /// This test ensure that we get an empty string from an instnace that doesn't have an IP.
        /// </summary>
        [TestMethod]
        public void GetNullFQDNTest()
        {
            Instance noPublicIpInstance = new Instance();
            var actual = noPublicIpInstance.GetFullyQualifiedDomainName();
            Assert.AreEqual("", actual);
        }
    }
}
