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

using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Google.Apis.Logging.v2.Data;
using GoogleCloudExtension.StackdriverLogsViewer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.StackdriverLogsViewer
{
    [TestClass]
    public class LogItemTests
    {
        [TestMethod]
        public void TestInitialConditions()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 3);

            Assert.IsNull(objectUnderTest.Function);
            Assert.IsNull(objectUnderTest.AssemblyName);
            Assert.IsNull(objectUnderTest.AssemblyVersion);
            Assert.IsNull(objectUnderTest.SourceLine);
            Assert.IsNull(objectUnderTest.SourceFilePath);
            Assert.IsFalse(objectUnderTest.SourceLinkVisible);
            Assert.AreEqual(entryTimestamp, objectUnderTest.TimeStamp);
            Assert.AreEqual(logEntry, objectUnderTest.Entry);
            Assert.AreEqual("01-01-2011", objectUnderTest.Date);
            Assert.AreEqual("01:01:01.000", objectUnderTest.Time);
            Assert.AreEqual("", objectUnderTest.Message);
            Assert.AreEqual("Any", objectUnderTest.SeverityTip);
            Assert.IsNull(objectUnderTest.SourceLinkCaption);
            Assert.IsNull(objectUnderTest.OnNavigateToSourceCommand);
            Assert.AreEqual(LogSeverity.Default, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_any_12.png");
            Assert.AreEqual(3, objectUnderTest.ParentToolWindowId);
        }

        [TestMethod]
        public void TestMessageFromJsonField()
        {
            const string jsonPayloadMessage = "jsonPayloadMessage";
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                JsonPayload = new Dictionary<string, object> { { LogItem.JsonPayloadMessageFieldName, jsonPayloadMessage } },
                ProtoPayload = new Dictionary<string, object> { { "protoKey", "protoValue" } },
                TextPayload = "textPayloadMessage",
                Labels = new Dictionary<string, string> { { "Label1", "Value1" }, { "Label2", "Value2" } },
                Resource = new MonitoredResource
                {
                    Labels = new Dictionary<string, string>
                    {
                        {"ResourceLabel1", "ResourceValue1"},
                        {"ResourceLabel2", "ResourceValue2"}
                    }
                }
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(jsonPayloadMessage, objectUnderTest.Message);
        }

        [TestMethod]
        public void TestMessageFromJsonPayload()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                JsonPayload = new Dictionary<string, object> { { "jsonKey", "jsonValue" } },
                ProtoPayload = new Dictionary<string, object> { { "protoKey", "protoValue" } },
                TextPayload = "textPayloadMessage",
                Labels = new Dictionary<string, string> { { "Label1", "Value1" }, { "Label2", "Value2" } },
                Resource = new MonitoredResource
                {
                    Labels = new Dictionary<string, string>
                    {
                        {"ResourceLabel1", "ResourceValue1"},
                        {"ResourceLabel2", "ResourceValue2"}
                    }
                }
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual("jsonKey: jsonValue  ", objectUnderTest.Message);
        }

        [TestMethod]
        public void TestMessageFromProtoPayload()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                ProtoPayload = new Dictionary<string, object> { { "protoKey", "protoValue" } },
                TextPayload = "textPayloadMessage",
                Labels = new Dictionary<string, string> { { "Label1", "Value1" }, { "Label2", "Value2" } },
                Resource = new MonitoredResource
                {
                    Labels = new Dictionary<string, string>
                    {
                        {"ResourceLabel1", "ResourceValue1"},
                        {"ResourceLabel2", "ResourceValue2"}
                    }
                }
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual("protoKey: protoValue  ", objectUnderTest.Message);
        }

        [TestMethod]
        public void TestMessageFromTextPayload()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string textPayloadMessage = "textPayloadMessage";
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                TextPayload = textPayloadMessage,
                Labels = new Dictionary<string, string> { { "Label1", "Value1" }, { "Label2", "Value2" } },
                Resource = new MonitoredResource
                {
                    Labels = new Dictionary<string, string>
                    {
                        {"ResourceLabel1", "ResourceValue1"},
                        {"ResourceLabel2", "ResourceValue2"}
                    }
                }
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(textPayloadMessage, objectUnderTest.Message);
        }

        [TestMethod]
        public void TestMessageFromLabels()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Labels = new Dictionary<string, string> { { "Label1", "Value1" }, { "Label2", "Value2" } },
                Resource = new MonitoredResource
                {
                    Labels = new Dictionary<string, string>
                    {
                        {"ResourceLabel1", "ResourceValue1"},
                        {"ResourceLabel2", "ResourceValue2"}
                    }
                }
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual("Value1;Value2", objectUnderTest.Message);
        }

        [TestMethod]
        public void TestMessageFromResourceLabels()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Resource = new MonitoredResource
                {
                    Labels = new Dictionary<string, string>
                    {
                        {"ResourceLabel1", "ResourceValue1"},
                        {"ResourceLabel2", "ResourceValue2"}
                    }
                }
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(
                "[ResourceLabel1, ResourceValue1];[ResourceLabel2, ResourceValue2]", objectUnderTest.Message);
        }

        [TestMethod]
        public void TestMessageWithLineBreaks()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                TextPayload = "text\nPayload\rMessage"
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual("text Payload Message", objectUnderTest.Message);
        }

        [TestMethod]
        public void TestIncludingSourceLocation()
        {
            const string sourceFilePath = @"c:\path\to\file.cs";
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string fullFunctionName =
                "[Full.Class.Name, Assembly.Name, Version=4, Culture=en-us, PublicKeyToken=pk].FunctionName";
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                SourceLocation = new LogEntrySourceLocation
                {
                    Function =
                        fullFunctionName,
                    Line = 1138,
                    File = sourceFilePath
                }
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(fullFunctionName, objectUnderTest.Function);
            Assert.AreEqual("Assembly.Name", objectUnderTest.AssemblyName);
            Assert.AreEqual("4", objectUnderTest.AssemblyVersion);
            Assert.AreEqual(sourceFilePath, objectUnderTest.SourceFilePath);
            Assert.AreEqual(1138, objectUnderTest.SourceLine);
            Assert.IsTrue(objectUnderTest.OnNavigateToSourceCommand.CanExecuteCommand);
            Assert.IsTrue(objectUnderTest.SourceLinkVisible);
            Assert.AreEqual("(file.cs:1138)", objectUnderTest.SourceLinkCaption);
        }

        [TestMethod]
        public void TestIncludingSourceLocationInvalidFunctionSyntax()
        {
            const string sourceFilePath = @"c:\path\to\file.cs";
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                SourceLocation = new LogEntrySourceLocation
                {
                    Function = "InvalidFunction",
                    File = sourceFilePath,
                    Line = 1138
                }
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.IsNull(objectUnderTest.AssemblyName);
            Assert.IsNull(objectUnderTest.AssemblyVersion);
            Assert.IsTrue(objectUnderTest.SourceLinkVisible);
            Assert.IsTrue(objectUnderTest.OnNavigateToSourceCommand.CanExecuteCommand);
            Assert.AreEqual(sourceFilePath, objectUnderTest.SourceFilePath);
            Assert.AreEqual(1138, objectUnderTest.SourceLine);
            Assert.AreEqual("(file.cs:1138)", objectUnderTest.SourceLinkCaption);
        }

        [TestMethod]
        public void TestIncludingSourceLocationMissingLine()
        {
            const string sourceFilePath = @"c:\path\to\file.cs";
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                SourceLocation = new LogEntrySourceLocation
                {
                    Function = "InvalidFunction",
                    File = sourceFilePath
                }
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.IsNull(objectUnderTest.AssemblyName);
            Assert.IsNull(objectUnderTest.AssemblyVersion);
            Assert.IsFalse(objectUnderTest.SourceLinkVisible);
            Assert.IsNull(objectUnderTest.OnNavigateToSourceCommand);
            Assert.AreEqual(sourceFilePath, objectUnderTest.SourceFilePath);
            Assert.IsNull(objectUnderTest.SourceLine);
            Assert.IsNull(objectUnderTest.SourceLinkCaption);
        }

        [TestMethod]
        public void TestIncludingSourceLocationMissingFile()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                SourceLocation = new LogEntrySourceLocation
                {
                    Function = "InvalidFunction",
                    Line = 1138
                }
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.IsNull(objectUnderTest.AssemblyName);
            Assert.IsNull(objectUnderTest.AssemblyVersion);
            Assert.IsFalse(objectUnderTest.SourceLinkVisible);
            Assert.IsNull(objectUnderTest.OnNavigateToSourceCommand);
            Assert.IsNull(objectUnderTest.SourceFilePath);
            Assert.AreEqual(1138, objectUnderTest.SourceLine);
            Assert.IsNull(objectUnderTest.SourceLinkCaption);
        }

        [TestMethod]
        public void TestIncludingSourceLocationMissingFunction()
        {
            const string sourceFilePath = @"c:\path\to\file.cs";
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                SourceLocation = new LogEntrySourceLocation
                {
                    File = sourceFilePath,
                    Line = 1138
                }
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.IsNull(objectUnderTest.AssemblyName);
            Assert.IsNull(objectUnderTest.AssemblyVersion);
            Assert.IsFalse(objectUnderTest.SourceLinkVisible);
            Assert.IsNull(objectUnderTest.OnNavigateToSourceCommand);
            Assert.AreEqual(1138, objectUnderTest.SourceLine);
            Assert.AreEqual(sourceFilePath, objectUnderTest.SourceFilePath);
            Assert.IsNull(objectUnderTest.SourceLinkCaption);
        }

        [TestMethod]
        public void TestUnknownSeverity()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string unknownSeverity = "unknown";
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Severity = unknownSeverity
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(unknownSeverity, objectUnderTest.SeverityTip);
            Assert.AreEqual(LogSeverity.Default, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_any_12.png");
        }

        [TestMethod]
        public void TestDefaultSeverity()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string severityString = nameof(LogSeverity.Default);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Severity = severityString
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(severityString, objectUnderTest.SeverityTip);
            Assert.AreEqual(LogSeverity.Default, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_any_12.png");
        }

        [TestMethod]
        public void TestDebugSeverity()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string severityString = nameof(LogSeverity.Debug);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Severity = severityString
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(severityString, objectUnderTest.SeverityTip);
            Assert.AreEqual(LogSeverity.Debug, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_debug_12.png");
        }

        [TestMethod]
        public void TestInfoSeverity()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string severityString = nameof(LogSeverity.Info);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Severity = severityString
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(severityString, objectUnderTest.SeverityTip);
            Assert.AreEqual(LogSeverity.Info, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_info_12.png");
        }

        [TestMethod]
        public void TestNoticeSeverity()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string severityString = nameof(LogSeverity.Notice);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Severity = severityString
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(severityString, objectUnderTest.SeverityTip);
            Assert.AreEqual(LogSeverity.Notice, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_info_12.png");
        }

        [TestMethod]
        public void TestWarningSeverity()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string severityString = nameof(LogSeverity.Warning);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Severity = severityString
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(severityString, objectUnderTest.SeverityTip);
            Assert.AreEqual(LogSeverity.Warning, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_warning_12.png");
        }

        [TestMethod]
        public void TestErrorSeverity()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string severityString = nameof(LogSeverity.Error);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Severity = severityString
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(severityString, objectUnderTest.SeverityTip);
            Assert.AreEqual(LogSeverity.Error, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_error_12.png");
        }

        [TestMethod]
        public void TestCriticalSeverity()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string severityString = nameof(LogSeverity.Critical);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Severity = severityString
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(severityString, objectUnderTest.SeverityTip);
            Assert.AreEqual(LogSeverity.Critical, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_fatal_12.png");
        }

        [TestMethod]
        public void TestAlertSeverity()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string severityString = nameof(LogSeverity.Alert);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Severity = severityString
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(severityString, objectUnderTest.SeverityTip);
            Assert.AreEqual(LogSeverity.Alert, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_fatal_12.png");
        }

        [TestMethod]
        public void TestEmergencySeverity()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string severityString = nameof(LogSeverity.Emergency);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Severity = severityString
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(severityString, objectUnderTest.SeverityTip);
            Assert.AreEqual(LogSeverity.Emergency, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_fatal_12.png");
        }

        [TestMethod]
        public void TestAllSeverity()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            const string severityString = nameof(LogSeverity.All);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp,
                Severity = severityString
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);

            Assert.AreEqual(severityString, objectUnderTest.SeverityTip);
            Assert.AreEqual(LogSeverity.All, objectUnderTest.LogLevel);
            StringAssert.EndsWith(
                ((BitmapImage)objectUnderTest.SeverityLevel).UriSource.AbsoluteUri, "ic_log_level_any_12.png");
        }

        [TestMethod]
        public void TestChangeTimeZone()
        {
            var entryTimestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            var logEntry = new LogEntry
            {
                Timestamp = entryTimestamp
            };
            var objectUnderTest = new LogItem(logEntry, TimeZoneInfo.Utc, 0);
            var propertiesChanges = new List<string>();
            objectUnderTest.PropertyChanged += (sender, args) => propertiesChanges.Add(args.PropertyName);

            TimeZoneInfo newTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                "TestTimeZone", TimeSpan.FromHours(-2), "TestTimeZone", "TestTimeZone");
            objectUnderTest.ChangeTimeZone(newTimeZone);

            Assert.AreEqual("23:01:01.000", objectUnderTest.Time);
            Assert.AreEqual("12-31-2010", objectUnderTest.Date);
            CollectionAssert.Contains(propertiesChanges, nameof(LogItem.Date));
            CollectionAssert.Contains(propertiesChanges, nameof(LogItem.Time));
        }
    }
}
