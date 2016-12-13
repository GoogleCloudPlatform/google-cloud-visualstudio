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
    internal class LogItem : ViewModelBase
    {
        private const string JasonPayloadMessageFieldName = "message";
        private const string AnyIconPath = "StackdriverLogsViewer/Resources/ic_log_level_any.png";
        private const string DebugIconPath = "StackdriverLogsViewer/Resources/ic_log_level_debug.png";
        private const string ErrorIconPath = "StackdriverLogsViewer/Resources/ic_log_level_error.png";
        private const string FatalIconPath = "StackdriverLogsViewer/Resources/ic_log_level_fatal.png";
        private const string InfoIconPath = "StackdriverLogsViewer/Resources/ic_log_level_info.png";
        private const string WarningIconPath = "StackdriverLogsViewer/Resources/ic_log_level_warning.png";

        private static readonly Lazy<ImageSource> s_any_icon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(AnyIconPath));
        private static readonly Lazy<ImageSource> s_debug_icon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(DebugIconPath));
        private static readonly Lazy<ImageSource> s_error_icon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(ErrorIconPath));
        private static readonly Lazy<ImageSource> s_fatal_icon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(FatalIconPath));
        private static readonly Lazy<ImageSource> s_info_icon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(InfoIconPath));
        private static readonly Lazy<ImageSource> s_warning_icon =
            new Lazy<ImageSource>(() => ResourceUtils.LoadImage(WarningIconPath));

        private readonly DateTime _timestamp;

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
        public string SeverityTip => string.IsNullOrWhiteSpace(Entry?.Severity) ? 
            Resources.LogViewerAnyOtherSeverityLevelTip : Entry.Severity;

        /// <summary>
        /// Gets the log item severity level. The data binding source to severity column in the data grid.
        /// </summary>
        public ImageSource SeverityLevel
        {
            get
            {
                LogSeverity logLevel;
                if (string.IsNullOrWhiteSpace(Entry?.Severity) ||
                    !Enum.TryParse<LogSeverity>(Entry?.Severity, out logLevel))
                {
                    return s_any_icon.Value;
                }

                switch (logLevel)
                {
                    // EMERGENCY, CRITICAL both map to fatal icon.
                    case LogSeverity.CRITICAL:
                    case LogSeverity.EMERGENCY:
                        return s_fatal_icon.Value;
                    case LogSeverity.DEBUG:
                        return s_debug_icon.Value;
                    case LogSeverity.ERROR:
                        return s_error_icon.Value;
                    case LogSeverity.INFO:
                        return s_info_icon.Value;
                    case LogSeverity.WARNING:
                        return s_warning_icon.Value;
                    default:
                        return s_any_icon.Value;
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

        private string ComposeDictionaryPayloadMessage(IDictionary<string, object> dictPayload)
        {
            Debug.Assert(dictPayload != null);
            if (dictPayload == null)
            {
                return String.Empty;
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
                message = string.Join(";", Entry?.Labels.Values);
            }
            else if (Entry?.Resource?.Labels != null)
            {
                message = string.Join(";", Entry?.Resource.Labels);
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
