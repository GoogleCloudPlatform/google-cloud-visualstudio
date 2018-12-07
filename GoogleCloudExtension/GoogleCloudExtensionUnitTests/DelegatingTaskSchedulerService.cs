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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleCloudExtensionUnitTests
{
    public class DelegatingTaskSchedulerService : IVsTaskSchedulerService, IVsTaskSchedulerService2
    {
        private static IVsTaskSchedulerService CurrentService =>
            (IVsTaskSchedulerService)ServiceProvider.GlobalProvider.GetService(typeof(SVsTaskSchedulerService));

        private static IVsTaskSchedulerService2 CurrentService2 =>
            (IVsTaskSchedulerService2)ServiceProvider.GlobalProvider.GetService(typeof(SVsTaskSchedulerService));

        public IVsTask CreateTask(uint context, IVsTaskBody taskBody) => CurrentService.CreateTask(context, taskBody);

        public IVsTask CreateTaskEx(uint context, uint options, IVsTaskBody taskBody, object asyncState) =>
            CurrentService.CreateTaskEx(context, options, taskBody, asyncState);

        public IVsTask ContinueWhenAllCompleted(
            uint context,
            uint tasks,
            IVsTask[] dependentTasks,
            IVsTaskBody taskBody)
        {
            return CurrentService.ContinueWhenAllCompleted(context, tasks, dependentTasks, taskBody);
        }

        public IVsTask ContinueWhenAllCompletedEx(
            uint context,
            uint tasks,
            IVsTask[] dependentTasks,
            uint options,
            IVsTaskBody taskBody,
            object asyncState)
        {
            return CurrentService.ContinueWhenAllCompletedEx(
                context,
                tasks,
                dependentTasks,
                options,
                taskBody,
                asyncState);
        }

        public IVsTaskCompletionSource CreateTaskCompletionSource() => CurrentService.CreateTaskCompletionSource();

        public IVsTaskCompletionSource CreateTaskCompletionSourceEx(uint options, object asyncState) =>
            CurrentService.CreateTaskCompletionSourceEx(options, asyncState);

        /// <summary>Gets the shell's instance of joinable task context. The functionality in this method is intended to be exposed by helper classes in MPF and not to be directly consumed by users.</summary>
        /// <returns>The HRESULT.</returns>
        public object GetAsyncTaskContext() => CurrentService2.GetAsyncTaskContext();

        /// <summary>Gets the task scheduler instance used for the context specified. This returnc a <see cref="T:System.Threading.Tasks.TaskScheduler" /> type. The functionality in this method is intended to be exposed by helper classes in MPF and not to be directly consumed by users.</summary>
        /// <param name="context">The context.</param>
        /// <returns>The HRESULT.</returns>
        public object GetTaskScheduler(uint context) => CurrentService2.GetTaskScheduler(context);
    }
}