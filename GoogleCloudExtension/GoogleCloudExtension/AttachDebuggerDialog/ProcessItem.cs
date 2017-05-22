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

using EnvDTE80;
using GoogleCloudExtension.Utils;
using System;
using System.IO;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// Represents a process on a remote debugging target machine.
    /// </summary>
    public class ProcessItem : Model
    {
        /// <summary>
        /// The <seealso cref="Process2"/> interface.
        /// </summary>
        public Process2 Process { get; }

        /// <summary>
        /// The display process name.
        /// </summary>
        public string Name
        {
            get
            {
                try
                {
                    return Path.GetFileName(Process.Name);
                }
                catch (ArgumentException)
                {
                    // The error is not likely to happen.
                    // Default to fullname.
                    return Process.Name;
                }
            }
        }

        /// <summary>
        /// The process id.
        /// </summary>
        public int PID => Process.ProcessID;

        /// <summary>
        /// The username that starts the process.
        /// </summary>
        public string User => Process.UserName;

        public ProcessItem(Process2 process)
        {
            Process = process;
        }
    }
}
