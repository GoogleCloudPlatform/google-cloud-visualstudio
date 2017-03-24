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
using GoogleCloudExtension.SourceBrowsing;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// An adaptor to LogEntry so as to provide properties for data binding.
    /// </summary>
    internal class LogItem : Model
    {
        private const string JsonPayloadMessageFieldName = "message";
        private const string AnyIconPath = "StackdriverLogsViewer/Resources/ic_log_level_any_12.png";
        private const string DebugIconPath = "StackdriverLogsViewer/Resources/ic_log_level_debug_12.png";
        private const string ErrorIconPath = "StackdriverLogsViewer/Resources/ic_log_level_error_12.png";
        private const string FatalIconPath = "StackdriverLogsViewer/Resources/ic_log_level_fatal_12.png";
        private const string InfoIconPath = "StackdriverLogsViewer/Resources/ic_log_level_info_12.png";
        private const string WarningIconPath = "StackdriverLogsViewer/Resources/ic_log_level_warning_12.png";

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

        /// <summary>
        /// The regex parses the log entry function field.
        /// Example:  [Log4NetSample.Program, Log4NetExample, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null].WriteRandomSeverityLog
        /// This regex extracts the assembly name "Log4NetExample" and version "1.0.0.0".
        /// </summary>
        private static readonly Regex s_FunctionRegex = new Regex($@"^\[(.*),\s*(.*),\s*Version\s*=\s*(.*)\s*,(.*),(.*)\]\.([\w\-. ]+)$");
        private readonly Lazy<List<ObjectNodeTree>> _treeViewObjects;

        /// <summary>
        /// The function field of source location.
        /// </summary>
        public string Function { get; }

        /// <summary>
        /// Log entry source assembly name.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Log entry source assembly version.
        /// </summary>
        public string AssemblyVersion { get; }

        /// <summary>
        /// Log entry source line number.
        /// </summary>
        public long? SourceLine { get; }

        /// <summary>
        /// Source location file path.
        /// </summary>
        public string SourceFilePath { get; }

        /// <summary>
        /// Indicates if the source link is shown or hidden.
        /// </summary>
        public bool SourceLinkVisible { get; }

        /// <summary>
        /// Gets the time stamp of the selected time zone.
        /// </summary>
        public DateTime TimeStamp { get; private set; }

        /// <summary>
        /// Gets a log entry object.
        /// </summary>
        public LogEntry Entry { get; }

        /// <summary>
        /// Gets the log item timestamp Date string in local time. Data binding to a view property.
        /// </summary>
        public string Date => TimeStamp.ToString(Resources.LogViewerLogItemDateFormat);

        /// <summary>
        /// Gets a log item timestamp in local time. Data binding to a view property.
        /// </summary>
        public string Time => TimeStamp.ToString(Resources.LogViewerLogItemTimeFormat);

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
        /// Gets the list of ObjectNodeTree for detail tree view.
        /// </summary>
        public List<ObjectNodeTree> TreeViewObjects => _treeViewObjects.Value;

        /// <summary>
        /// Gets the formated source location as content of data grid column.
        /// </summary>
        public string SourceLinkCaption { get; }

        /// <summary>
        /// Command responses to source link button click event.
        /// </summary>
        public ProtectedCommand OnNavigateToSourceCommand { get; }

        /// <summary>
        /// Log severity level.
        /// </summary>
        public LogSeverity LogLevel { get; }

        /// <summary>
        /// Gets the log item severity level. The data binding source to severity column in the data grid.
        /// </summary>
        public ImageSource SeverityLevel
        {
            get
            {
                switch (LogLevel)
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
        /// <param name="timeZoneInfo">The current selected timezone.</param>
        public LogItem(LogEntry logEntry, TimeZoneInfo timeZoneInfo)
        {
            if (logEntry == null)
            {
                return;
            }

            Entry = logEntry;
            TimeStamp = ConvertTimestamp(logEntry.Timestamp, timeZoneInfo);
            Message = ComposeMessage();

            LogSeverity severity;
            if (String.IsNullOrWhiteSpace(Entry.Severity) ||
                !Enum.TryParse<LogSeverity>(Entry.Severity, ignoreCase: true, result: out severity))
            {
                severity = LogSeverity.Default;
            }
            LogLevel = severity;

            _treeViewObjects = new Lazy<List<ObjectNodeTree>>(() => new LogEntryNode(Entry).Children);

            Function = Entry.SourceLocation?.Function;
            SourceFilePath = Entry?.SourceLocation?.File;
            SourceLine = Entry.SourceLocation?.Line;
            if (Function != null && SourceFilePath != null && SourceLine.HasValue)
            {
                // Example:  [Log4NetSample.Program, Log4NetExample, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null].WriteRandomSeverityLog
                Match match = s_FunctionRegex.Match(Function);
                if (match.Success)
                {
                    AssemblyName = match.Groups[2].Value;
                    AssemblyVersion = match.Groups[3].Value;
                }

                SourceLinkVisible = true;
                OnNavigateToSourceCommand = new ProtectedCommand(NavigateToSourceLineCommand);
                var tmp = $"{SourceFilePath}:{SourceLine}";
                SourceLinkCaption = tmp.Length <= 20 ? tmp : $"...{tmp.Substring(tmp.Length - 17)}";
            }
        }

        /// <summary>
        /// Change time zone of log item.
        /// </summary>
        /// <param name="newTimeZone">The new time zone.</param>
        public void ChangeTimeZone(TimeZoneInfo newTimeZone)
        {
            TimeStamp = TimeZoneInfo.ConvertTime(TimeStamp, newTimeZone);
            RaisePropertyChanged(nameof(Time));
        }

        private string GetSourceLocationField(string fieldName)
        {
            return Entry.Labels.ContainsKey(fieldName) ? Entry.Labels[fieldName] : null;
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
                if (Entry.JsonPayload.ContainsKey(JsonPayloadMessageFieldName))
                {
                    message = Entry.JsonPayload[JsonPayloadMessageFieldName].ToString();
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

            // http://stackoverflow.com/questions/11654190/ienumerablechar-to-string
            // The discussion here suggests to use new string() that performs well.
            return new string(message?.Select(x => (x == '\r' || x == '\n') ? ' ' : x).ToArray<char>());
        }

        private DateTime ConvertTimestamp(object timestamp, TimeZoneInfo timeZoneInfo)
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

            return TimeZoneInfo.ConvertTime(datetime, timeZoneInfo);
        }

        /// <summary>
        /// Open the source file, move to the source line and show tooltip.
        /// </summary>
        private void NavigateToSourceLineCommand()
        {
            var project = this.FindOrOpenProject();
            if (project == null)
            {
                Debug.WriteLine($"Failed to find project of {AssemblyName}");
                return;
            }

            var projectSourceFile = project.FindSourceFile(SourceFilePath);
            if (projectSourceFile == null)
            {
                SourceVersionUtils.FileItemNotFoundPrompt(SourceFilePath);
                return;
            }

            var window = ShellUtils.Open(projectSourceFile.ProjectItem);
            if (null == window)
            {
                SourceVersionUtils.FailedToOpenFilePrompt(SourceFilePath);
                return;
            }

            this.ShowToolTip(window);
        }
    }
}
