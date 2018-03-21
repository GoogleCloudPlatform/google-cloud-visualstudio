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

using GoogleCloudExtension.StackdriverErrorReporting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using EventTimeRangePeriodEnum =
    Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.EventsResource.ListRequest.TimeRangePeriodEnum;
using GroupTimeRangePeriodEnum =
    Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;

namespace GoogleCloudExtensionUnitTests.StackdriverErrorReporting.TimeRangeButtons
{
    [TestClass]
    public class TimeRangeItemTests
    {
        [TestMethod]
        public void TestInitalConditions()
        {
            const string durationString = "duration";
            const string captionString = "caption";

            var objectUnderTest = new TimeRangeItem(
                captionString, durationString, GroupTimeRangePeriodEnum.PERIOD30DAYS, EventTimeRangePeriodEnum.PERIOD1WEEK);

            Assert.AreEqual(captionString, objectUnderTest.Caption);
            Assert.AreEqual(GroupTimeRangePeriodEnum.PERIOD30DAYS, objectUnderTest.GroupTimeRange);
            Assert.AreEqual(EventTimeRangePeriodEnum.PERIOD1WEEK, objectUnderTest.EventTimeRange);
            Assert.AreEqual(durationString, objectUnderTest.TimedCountDuration);
            Assert.IsFalse(objectUnderTest.IsCurrentSelection);
        }

        [TestMethod]
        public void TestIsCurrentSelectionProperty()
        {
            var propertiesChanged = new List<string>();
            var objectUnderTest = new TimeRangeItem("", "", 0, 0);
            objectUnderTest.PropertyChanged += (sender, args) => propertiesChanged.Add(args.PropertyName);

            objectUnderTest.IsCurrentSelection = true;

            Assert.IsTrue(objectUnderTest.IsCurrentSelection);
            CollectionAssert.AreEqual(new[] { nameof(objectUnderTest.IsCurrentSelection) }, propertiesChanged);
        }

        [TestMethod]
        public void TestEquals()
        {
            var objectUnderTest = new TimeRangeItem("", "", 0, 0);
            var equalObject = new TimeRangeItem("", "", 0, 0);
            var selectedEqualObject = new TimeRangeItem("", "", 0, 0) { IsCurrentSelection = true };
            var unequalCaption = new TimeRangeItem("unequal", "", 0, 0);
            var unequalDuration = new TimeRangeItem("", "unequal", 0, 0);
            var unequalGroupTimePeriod = new TimeRangeItem("", "", GroupTimeRangePeriodEnum.PERIOD1HOUR, 0);
            var unequalEventTimePeriod = new TimeRangeItem("", "", 0, EventTimeRangePeriodEnum.PERIOD1HOUR);

            // ReSharper disable once EqualExpressionComparison
            Assert.IsTrue(objectUnderTest.Equals(objectUnderTest));
            Assert.IsTrue(objectUnderTest.Equals(equalObject));
            Assert.IsTrue(selectedEqualObject.Equals(equalObject));

            Assert.IsFalse(objectUnderTest.Equals(null));
            Assert.IsFalse(objectUnderTest.Equals(new object()));
            Assert.IsFalse(objectUnderTest.Equals(unequalCaption));
            Assert.IsFalse(objectUnderTest.Equals(unequalDuration));
            Assert.IsFalse(objectUnderTest.Equals(unequalGroupTimePeriod));
            Assert.IsFalse(objectUnderTest.Equals(unequalEventTimePeriod));
        }

        [TestMethod]
        public void TestGetHashCode()
        {
            var objectUnderTest = new TimeRangeItem("", "", 0, 0);
            var equalObject = new TimeRangeItem("", "", 0, 0);
            var selectedEqualObject = new TimeRangeItem("", "", 0, 0) { IsCurrentSelection = true };
            var unequalCaption = new TimeRangeItem("unequal", "", 0, 0);
            var unequalDuration = new TimeRangeItem("", "unequal", 0, 0);
            var unequalGroupTimePeriod = new TimeRangeItem("", "", GroupTimeRangePeriodEnum.PERIOD1HOUR, 0);
            var unequalEventTimePeriod = new TimeRangeItem("", "", 0, EventTimeRangePeriodEnum.PERIOD1HOUR);

            Assert.AreEqual(objectUnderTest.GetHashCode(), objectUnderTest.GetHashCode());
            Assert.AreEqual(equalObject.GetHashCode(), objectUnderTest.GetHashCode());
            Assert.AreEqual(selectedEqualObject.GetHashCode(), objectUnderTest.GetHashCode());
            Assert.AreNotEqual(objectUnderTest.GetHashCode(), unequalCaption.GetHashCode());
            Assert.AreNotEqual(objectUnderTest.GetHashCode(), unequalDuration.GetHashCode());
            Assert.AreNotEqual(objectUnderTest.GetHashCode(), unequalGroupTimePeriod.GetHashCode());
            Assert.AreNotEqual(objectUnderTest.GetHashCode(), unequalEventTimePeriod.GetHashCode());
            Assert.AreNotEqual(0, objectUnderTest.GetHashCode());
        }
    }
}
