// Copyright 2016 Google Inc. All Rights Reserved.
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

using Google.Apis.Logging.v2.Data;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Media;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// An adaptor to LogEntry so as to provide properties for data binding.
    /// </summary>
    internal class LogItem : Model
    {
        private const string JasonPayloadMessageFieldName = "message";
        private const string AnyIconPath = "StackdriverLogsViewer/Resources/ic_log_level_any.png";
        private const string DebugIconPath = "StackdriverLogsViewer/Resources/ic_log_level_debug.png";
        private const string ErrorIconPath = "StackdriverLogsViewer/Resources/ic_log_level_error.png";
        private const string FatalIconPath = "StackdriverLogsViewer/Resources/ic_log_level_fatal.png";
        private const string InfoIconPath = "StackdriverLogsViewer/Resources/ic_log_level_info.png";
        private const string WarningIconPath = "StackdriverLogsViewer/Resources/ic_log_level_warning.png";

        private static readonly Lazy<ImageSource> s_anyIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(AnyIconPath));
        private static readonly Lazy<ImageSource> s_debugIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(DebugIconPath));
        private static readonly Lazy<ImageSource> s_errorIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(ErrorIconPath));
        private static readonly Lazy<ImageSource> s_fatalIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(FatalIconPath));
        private static readonly Lazy<ImageSource> s_infoIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(InfoIconPath));
        private static readonly Lazy<ImageSource> s_warningIcon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(WarningIconPath));

        private DateTime _timestamp;

        /// <summary>
        /// Gets a log entry object.
        /// </summary>
        public LogEntry Entry { get; }

        /// <summary>
        /// Gets the log item timestamp Date string in local time. Data binding to a view property.
        /// </summary>
        public string Date => _timestamp.ToString(Resources.LogViewerLogItemDateFormat);

        /// <summary>
        /// Gets a log item timestamp in local time. Data binding to a view property.
        /// </summary>
        public string Time => _timestamp.ToString(Resources.LogViewerLogItemTimeFormat);

        /// <summary>
        /// Gets the log message to be displayed at top level.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the log severity tooltip. Data binding to the severity icon tool tip.
        /// </summary>
        public string SeverityTip => String.IsNullOrWhiteSpace(Entry?.Severity) ? 
            Resources.LogViewerAnyOtherSeverityLevelTip : Entry.Severity;

        /// <summary>
        /// Gets the log item severity level. The data binding source to severity column in the data grid.
        /// </summary>
        public ImageSource SeverityLevel
        {
            get
            {
                LogSeverity logLevel;
                if (String.IsNullOrWhiteSpace(Entry?.Severity) ||
                    !Enum.TryParse<LogSeverity>(Entry?.Severity, ignoreCase: true, result: out logLevel))
                {
                    return s_anyIcon.Value;
                }

                switch (logLevel)
                {
                    // EMERGENCY, CRITICAL, Alert all map to fatal icon.
                    case LogSeverity.Alert:
                    case LogSeverity.Critical:
                    case LogSeverity.Emergency:
                        return s_fatalIcon.Value;

                    case LogSeverity.Debug:
                        return s_debugIcon.Value;

                    case LogSeverity.Error:
                        return s_errorIcon.Value;

                    case LogSeverity.Notice:
                    case LogSeverity.Info:
                        return s_infoIcon.Value;

                    case LogSeverity.Warning:
                        return s_warningIcon.Value;

                    default:
                        return s_anyIcon.Value;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="LogItem"/> class.
        /// </summary>
        /// <param name="logEntry">A log entry.</param>
        public LogItem(LogEntry logEntry)
        {
            Entry = logEntry;
            Message = ComposeMessage();
            _timestamp = ConvertTimestamp(logEntry.Timestamp);
        }

        /// <summary>
        /// Change time zone of log item.
        /// </summary>
        /// <param name="newTimeZone">The new time zone.</param>
        public void ChangeTimeZone(TimeZoneInfo newTimeZone)
        {
            _timestamp = TimeZoneInfo.ConvertTime(_timestamp, newTimeZone);
        }

        private string ComposeDictionaryPayloadMessage(IDictionary<string, object> dictPayload)
        {
            if (dictPayload == null)
            {
                return "";
            }

            StringBuilder text = new StringBuilder();
            foreach (var kv in dictPayload)
            {
                text.AppendFormat(Resources.LogViewerDictionaryPayloadFormatString, kv.Key, kv.Value);
            }

            return text.ToString();
        }

        private string ComposeMessage()
        {
            string message = null;
            if (Entry?.JsonPayload != null)
            {
                // If the JsonPload has message filed, display this field.
                if (Entry.JsonPayload.ContainsKey(JasonPayloadMessageFieldName))
                {
                    message = Entry.JsonPayload[JasonPayloadMessageFieldName].ToString();
                }
                else
                {
                    message = ComposeDictionaryPayloadMessage(Entry.JsonPayload);
                }
            }
            else if (Entry?.ProtoPayload != null)
            {
                message = ComposeDictionaryPayloadMessage(Entry.ProtoPayload);
            }
            else if (Entry?.TextPayload != null)
            {
                message = Entry.TextPayload;
            }
            else if (Entry?.Labels != null)
            {
                message = String.Join(";", Entry?.Labels.Values);
            }
            else if (Entry?.Resource?.Labels != null)
            {
                message = String.Join(";", Entry?.Resource.Labels);
            }

            return message?.Replace(Environment.NewLine, "\\n").Replace("\t", "\\t");
        }

        private DateTime ConvertTimestamp(object timestamp)
        {
            DateTime datetime;
            if (timestamp == null)
            {
                Debug.Assert(false, "Entry Timestamp is null");
                datetime = DateTime.MaxValue;
            }
            else if (timestamp is DateTime)
            {
                datetime = (DateTime)timestamp;
            }
            else
            {
                // From Stackdriver Logging API reference,
                // A timestamp in RFC3339 UTC "Zulu" format, accurate to nanoseconds. 
                // Example: "2014-10-02T15:01:23.045123456Z".
                if (!DateTime.TryParse(timestamp.ToString(), 
                    CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out datetime))
                {
                    datetime = DateTime.MaxValue;
                }
            }

            return datetime.ToLocalTime();
        }
    }
}
