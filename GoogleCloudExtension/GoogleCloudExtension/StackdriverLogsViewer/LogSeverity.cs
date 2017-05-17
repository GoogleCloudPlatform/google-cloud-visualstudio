﻿// Copyright 2016 Google Inc. All Rights Reserved.
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

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Define log severity that matches to corresponding icons.
    /// Note, the LogEntry contains the severity as string not a defined enum.
    /// Refer to Stackdriver Logging API, LogEntry LogSeverity definition.
    /// </summary>
    public enum LogSeverity
    {
        Default,
        Debug,
        Info,
        Notice,
        Warning,
        Error,
        Critical,
        Alert,
        Emergency,
        All
    }
}
