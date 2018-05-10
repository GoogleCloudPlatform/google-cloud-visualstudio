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

using Google.Apis.Clouderrorreporting.v1beta1;
using Google.Apis.Clouderrorreporting.v1beta1.Data;
using GoogleCloudExtension.StackdriverErrorReporting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.StackdriverErrorReporting
{
    /// <summary>
    /// Summary description for ErrorItemTests
    /// </summary>
    [TestClass]
    public class ErrorGroupItemTests
    {
        [TestMethod]
        [ExpectedException(typeof(ErrorReportingException))]
        public void TestConstructorErrorOnNullErrorGroup()
        {
            try
            {
                var objectUnderTest = new ErrorGroupItem(null, null);
                Assert.Fail(objectUnderTest.ToString());
            }
            catch (ErrorReportingException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));
                throw;
            }
        }

        [TestMethod]
        public void TestInitalConditions()
        {
            var errorGroupStats = new ErrorGroupStats();

            var objectUnderTest = new ErrorGroupItem(errorGroupStats, null);

            Assert.IsNull(objectUnderTest.ParsedException);
            Assert.AreEqual(errorGroupStats, objectUnderTest.ErrorGroup);
            Assert.AreEqual(0, objectUnderTest.ErrorCount);
            Assert.IsNull(objectUnderTest.SeenIn);
            Assert.IsNull(objectUnderTest.Status);
            Assert.IsNull(objectUnderTest.ErrorMessage);
            Assert.IsNull(objectUnderTest.FirstStackFrameSummary);
            Assert.IsNull(objectUnderTest.FirstStackFrame);
            Assert.AreEqual(DateTime.MinValue, DateTime.Parse(objectUnderTest.FirstSeenTime));
            Assert.AreEqual(DateTime.MinValue, DateTime.Parse(objectUnderTest.LastSeenTime));
            Assert.IsNull(objectUnderTest.GroupTimeRange);
            Assert.IsNull(objectUnderTest.TimedCountList);
            Assert.IsNull(objectUnderTest.RawErrorMessage);
            Assert.AreEqual("-", objectUnderTest.AffectedUsersCount);
        }

        [TestMethod]
        public void TestPopulatedInitalConditions()
        {
            var firstTime = new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Local);
            var lastTime = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Local);
            const int affectedUsersCount = 3;
            const int count = 10;
            const string testServiceName = "TestService";
            const int responseStatusCode = 200;
            const string testMessage = "TestMessage";
            var timedCounts = new List<TimedCount> { new TimedCount() };
            var errorGroupStats = new ErrorGroupStats
            {
                AffectedServices = new List<ServiceContext> { new ServiceContext { Service = testServiceName } },
                AffectedUsersCount = affectedUsersCount,
                Count = count,
                FirstSeenTime = firstTime,
                LastSeenTime = lastTime,
                NumAffectedServices = 2,
                Representative = new ErrorEvent
                {
                    Context = new ErrorContext { HttpRequest = new HttpRequestContext { ResponseStatusCode = responseStatusCode } },
                    Message = testMessage
                },
                Group = new ErrorGroup(),
                TimedCounts = timedCounts
            };
            var timeRangeItem = new TimeRangeItem(
                "testTimeRangeCaption", "testTimedCountDuration",
                ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum.PERIOD1DAY,
                ProjectsResource.EventsResource.ListRequest.TimeRangePeriodEnum.PERIOD1DAY);

            var objectUnderTest = new ErrorGroupItem(errorGroupStats, timeRangeItem);

            Assert.IsNotNull(objectUnderTest.ParsedException);
            Assert.AreEqual(errorGroupStats, objectUnderTest.ErrorGroup);
            Assert.AreEqual(count, objectUnderTest.ErrorCount);
            Assert.AreEqual(testServiceName, objectUnderTest.SeenIn);
            Assert.AreEqual(responseStatusCode, objectUnderTest.Status);
            Assert.AreEqual(testMessage, objectUnderTest.ErrorMessage);
            Assert.IsNull(objectUnderTest.FirstStackFrameSummary);
            Assert.IsNull(objectUnderTest.FirstStackFrame);
            Assert.AreEqual(firstTime, DateTime.Parse(objectUnderTest.FirstSeenTime));
            Assert.AreEqual(lastTime, DateTime.Parse(objectUnderTest.LastSeenTime));
            Assert.AreEqual(timeRangeItem, objectUnderTest.GroupTimeRange);
            CollectionAssert.AreEquivalent(timedCounts, objectUnderTest.TimedCountList.ToList());
            Assert.AreEqual(testMessage, objectUnderTest.RawErrorMessage);
            Assert.AreEqual(affectedUsersCount.ToString(), objectUnderTest.AffectedUsersCount);
        }

        [TestMethod]
        public void TestSetCountEmpty()
        {
            var errorGroupStats = new ErrorGroupStats
            {
                AffectedServices = new List<ServiceContext> { new ServiceContext() },
                AffectedUsersCount = 3,
                NumAffectedServices = 2,
                TimedCounts = new List<TimedCount> { new TimedCount() }
            };
            var objectUnderTest = new ErrorGroupItem(errorGroupStats, null);

            objectUnderTest.SetCountEmpty();

            Assert.AreEqual(0, objectUnderTest.ErrorCount);
            Assert.IsNull(objectUnderTest.SeenIn);
            Assert.AreEqual("-", objectUnderTest.AffectedUsersCount);
            Assert.IsNull(objectUnderTest.TimedCountList);
            Assert.AreEqual(0, objectUnderTest.ErrorGroup.Count);
            Assert.IsNull(objectUnderTest.ErrorGroup.NumAffectedServices);
            Assert.IsNull(objectUnderTest.ErrorGroup.AffectedUsersCount);
            Assert.IsNull(objectUnderTest.ErrorGroup.TimedCounts);
        }
    }
}
