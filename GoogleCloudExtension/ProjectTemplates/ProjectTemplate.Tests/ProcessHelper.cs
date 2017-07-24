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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace ProjectTemplate.Tests
{
    /// <summary>
    /// The methods of this class help to manage processes.
    /// </summary>
    public static class ProcessHelper
    {
        /// <summary>
        /// Kills the given process and all decendant processes.
        /// </summary>
        /// <param name="process">The parent process to kill.</param>
        /// <returns>A list of all of the <see cref="Process"/> objects of the killed processes.</returns>
        public static IList<Process> KillProcessTree(this Process process)
        {
            var query = new SelectQuery("Win32_Process", $"ParentProcessID={process.Id}");
            var searcher = new ManagementObjectSearcher(query);
            var exceptions = new List<Exception>();
            var killedProcesses = new List<Process>();
            ManagementObjectCollection childProcessIds = searcher.Get();

            try
            {
                process.Kill();
                killedProcesses.Add(process);
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }

            foreach (ManagementBaseObject mo in childProcessIds)
            {
                try
                {
                    int processId = Convert.ToInt32(mo["ProcessID"]);
                    Process childProcess = Process.GetProcessById(processId);
                    killedProcesses.AddRange(KillProcessTree(childProcess));
                }
                catch (AggregateException e)
                {
                    exceptions.AddRange(e.InnerExceptions);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count != 0)
            {
                throw new AggregateException(exceptions);
            }

            return killedProcesses;
        }

        /// <summary>
        /// Kills the given process and all decendant processes.
        /// </summary>
        /// <param name="process">The parent process to kill.</param>
        /// <param name="timeout">The amount of time to wait for the processes to exit.</param>
        public static void KillProcessTreeAndWait(this Process process, TimeSpan timeout)
        {
            IList<Process> killedProcesses = process.KillProcessTree();
            var tasks = new List<Task>();
            foreach (Process killedProcess in killedProcesses)
            {
                var taskSource = new TaskCompletionSource<bool>();
                killedProcess.Exited += (sender, args) => taskSource.SetResult(true);
                if (!killedProcess.HasExited)
                {
                    tasks.Add(taskSource.Task);
                }
            }
            Task.WaitAll(tasks.ToArray(), timeout);
        }
    }
}