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

using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Represents the log id selector items source.
    /// </summary>
    public class LogIdsList : Model
    {
        private string _selectedLogIDShortName;
        private readonly Dictionary<string, string> _logIDs = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _logShortNameToIdLookup = new Dictionary<string, string>();

        /// <summary>
        /// Gets the list of log id short name as the selector items source. 
        /// </summary>
        public List<string> LogIDs { get; }

        /// <summary>
        /// Gets the current selection full name.
        /// </summary>
        public string SelectedLogIdFullName
        {
            get
            {
                if (_selectedLogIDShortName == null || _selectedLogIDShortName == Resources.LogViewerLogIdSelectAllLabel)
                {
                    return null;
                }

                return _logShortNameToIdLookup[_selectedLogIDShortName];
            }
        }

        /// <summary>
        /// Gets or sets the selected log id short name.
        /// </summary>
        public string SelectedLogId
        {
            get { return _selectedLogIDShortName; }
            set { SetValueAndRaise(ref _selectedLogIDShortName, value); }
        }

        /// <summary>
        /// Instantialize a new instance of <seealso cref="LogIdsList"/> class.
        /// </summary>
        }

        /// <summary>
        /// Instantialize a new instance of <seealso cref="LogIdsList"/> class.
        /// </summary>
        public LogIdsList(IList<string> logIds)
        {
            LogIDs = new List<string>();
            foreach (var id in logIds)
            {
                AddLogId(id);
            }

            LogIDs.Add(Resources.LogViewerLogIdSelectAllLabel);
            _selectedLogIDShortName = Resources.LogViewerLogIdSelectAllLabel;
        }

        private void AddLogId(string logId)
        {
            if (String.IsNullOrWhiteSpace(logId))
            {
                return;
            }

            var lowerCaseLogID = logId.ToLower();
            if (_logIDs.ContainsKey(lowerCaseLogID))
            {
                return;
            }

            var splits = logId.Split(new string[] { "/", "%2F", "%2f" }, StringSplitOptions.RemoveEmptyEntries);
            string shortName = splits.LastOrDefault();
            _logIDs[lowerCaseLogID] = shortName;
            if (_logShortNameToIdLookup.ContainsKey(shortName))
            {
                Debug.Assert(false, $"Found same short name of {_logShortNameToIdLookup[shortName]} and {logId}");
                return;
            }

            LogIDs.Add(shortName);
            _logShortNameToIdLookup[shortName] = logId;
        }
    }
}
