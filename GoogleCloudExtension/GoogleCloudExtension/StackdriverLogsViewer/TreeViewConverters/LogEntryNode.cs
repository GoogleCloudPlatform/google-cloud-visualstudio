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

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    internal class LogEntryNode : ObjectNodeTree
    {
        /// <summary>
        /// Create an instance of the <seealso cref="LogEntryNode"/> class.
        /// </summary>
        /// <param name="logEntry">A <seealso cref="LogEntry"/> object.</param>
        public LogEntryNode(LogEntry logEntry) : base("", logEntry, null) { }

        protected override void ParseObjectTree(object obj)
        {
            var log = obj as LogEntry;
            AddChildren("resource", log.Resource);
            AddChildren("httpRequest", log.HttpRequest);
            AddChildren("operation", log.Operation);
            AddChildren("sourceLocation", log.SourceLocation);
            AddChildren("insertId", log.InsertId);
            AddChildren("jsonPayload", log.JsonPayload);
            AddChildren("labels", log.Labels);
            AddChildren("logName", log.LogName);
            AddChildren("protoPayload", log.ProtoPayload);
            AddChildren("severity", log.Severity);
            AddChildren("textPayload", log.TextPayload);
            AddChildren("timestamp", log.Timestamp);
            AddChildren("trace", log.Trace);
        }
    }
}
