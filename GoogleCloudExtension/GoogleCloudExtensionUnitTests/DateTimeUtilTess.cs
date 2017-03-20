﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using static GoogleCloudExtension.Utils.DateTimeUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;

namespace GoogleCloudExtensionUnitTests
{
    [TestClass]
    public class DateTimeUtilTess
    {
        [TestMethod]
        public void MinTest()
        {
            DateTime t1 = DateTime.Now;
            DateTime t2 = DateTime.Now;
            Assert.AreEqual(t1, Min(t1, DateTime.UtcNow));
            Assert.AreEqual(t1, Min(DateTime.UtcNow, t1));
            Assert.AreEqual(t1, Min(t1, t2));
            Assert.AreEqual(t2, Min(t2, DateTime.MaxValue));
            Assert.AreEqual(DateTime.MinValue, Min(t2, DateTime.MinValue));
        }

        [TestMethod]
        public void MaxTest()
        {
            DateTime t1 = DateTime.Now;
            DateTime t2 = DateTime.UtcNow;
            Assert.AreEqual(t1, Max(t1, DateTime.UtcNow.AddHours(-1)));
            Assert.AreEqual(t1, Max(DateTime.UtcNow.AddHours(-1), t1));
            Assert.AreEqual(t2, Max(t1, t2));
            Assert.AreEqual(t2, Max(t2, t1));
            Assert.AreEqual(t2, Max(t2, DateTime.MinValue));
            Assert.AreEqual(DateTime.MaxValue, Max(t2, DateTime.MaxValue));
        }
    }
}
