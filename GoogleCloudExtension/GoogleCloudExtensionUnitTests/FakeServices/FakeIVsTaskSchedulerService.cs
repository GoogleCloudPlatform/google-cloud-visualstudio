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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleCloudExtensionUnitTests.FakeServices
{
    public class FakeIVsTaskSchedulerService : IVsTaskSchedulerService, IVsTaskSchedulerService2, SVsTaskSchedulerService
    {
        /// <summary>Creates a task that is run on the given context.</summary>
        /// <param name="context">[in] Where to run this task. Values are from <see cref="T:Microsoft.VisualStudio.Shell.Interop.__VSTASKRUNCONTEXT" />.</param>
        /// <param name="taskBody">[in] Action to be executed.</param>
        /// <returns>The task to be run.</returns>
        public IVsTask CreateTask(uint context, IVsTaskBody taskBody) =>
            new FakeIVsTask(Task.FromResult(new object())).ContinueWith(context, taskBody);

        /// <summary>Creates a task with the specified options that is run on the given context.</summary>
        /// <param name="context">[in] Where to run this task. Values are from <see cref="T:Microsoft.VisualStudio.Shell.Interop.__VSTASKRUNCONTEXT" />.</param>
        /// <param name="options">[in] The creation options set for the task. Values are from <see cref="T:Microsoft.VisualStudio.Shell.Interop.__VSTASKCREATIONOPTIONS" />.</param>
        /// <param name="taskBody">[in] Action to be executed.</param>
        /// <param name="asyncState">[in] The asynchronous state of the task.</param>
        /// <returns>The new task instance.</returns>
        public IVsTask CreateTaskEx(uint context, uint options, IVsTaskBody taskBody, object asyncState) =>
            new FakeIVsTask(Task.FromResult(new object())).ContinueWithEx(context, options, taskBody, asyncState);

        /// <summary>Creates an asynchrous task that is run after all the provided tasks have either finished running or have been cancelled.</summary>
        /// <param name="context">[in] Where to run this task.</param>
        /// <param name="dwTasks">[in] The number of tasks to wait.</param>
        /// <param name="dependentTasks">[in] An array of tasks to wait.</param>
        /// <param name="taskBody">[in] Worker method for the task.</param>
        /// <returns>The created task that runs after all of the other tasks have completed.</returns>
        public IVsTask ContinueWhenAllCompleted(
            uint context,
            uint dwTasks,
            IVsTask[] dependentTasks,
            IVsTaskBody taskBody)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            Task<object> waitTask = Task.Factory.StartNew<object>(
                () =>
                {
                    foreach (IVsTask dependentTask in dependentTasks)
                    {
                        dependentTask.Wait();
                    }

                    return null;
                },
                cancellationTokenSource.Token,
                TaskCreationOptions.None,
                TaskScheduler.Default);
            return new FakeIVsTask(waitTask, cancellationTokenSource).ContinueWith(context, taskBody);
        }

        /// <summary>Creates a task (using the specified options) that is run after all the given tasks are completed.</summary>
        /// <param name="context">[in] Where to run this task.</param>
        /// <param name="dwTasks">[in] The number of tasks to wait.</param>
        /// <param name="dependentTasks">[in] An array of tasks to wait.</param>
        /// <param name="options">[in] The continuation options set for the task.</param>
        /// <param name="taskBody">[in] Worker method for the task.</param>
        /// <param name="asyncState">[in] Asynchronous state for the task.</param>
        /// <returns>The created task that runs after all of the other tasks have completed.</returns>
        public IVsTask ContinueWhenAllCompletedEx(
            uint context,
            uint dwTasks,
            IVsTask[] dependentTasks,
            uint options,
            IVsTaskBody taskBody,
            object asyncState)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            Task<object> waitTask = Task.Factory.StartNew<object>(
                () =>
                {
                    foreach (IVsTask dependentTask in dependentTasks)
                    {
                        dependentTask.Wait();
                    }

                    return null;
                },
                cancellationTokenSource.Token,
                TaskCreationOptions.None,
                TaskScheduler.Default);
            return new FakeIVsTask(waitTask, cancellationTokenSource).ContinueWithEx(
                context,
                options,
                taskBody,
                asyncState);
        }

        /// <summary>Creates a task completion source instance that can be used to start a task, or can cancel or append continuations.</summary>
        /// <returns>The task completion source instance.</returns>
        public IVsTaskCompletionSource CreateTaskCompletionSource() => new FakeIVsTaskCompletionSource();

        /// <summary>Creates a task completion source instance with the specified options.</summary>
        /// <param name="options">[in] Task creation options for the task controlled by the completion source.</param>
        /// <param name="asyncState">[in] Asynchronous state that will be stored by the task controlled by the completion source.</param>
        /// <returns>The task completion source instance.</returns>
        public IVsTaskCompletionSource CreateTaskCompletionSourceEx(uint options, object asyncState) =>
            new FakeIVsTaskCompletionSource(options, asyncState);

        /// <summary>Gets the shell's instance of joinable task context. The functionality in this method is intended to be exposed by helper classes in MPF and not to be directly consumed by users.</summary>
        /// <returns>The HRESULT.</returns>
        public object GetAsyncTaskContext() => AssemblyInitialize.JoinableApplicationContext;

        /// <summary>Gets the task scheduler instance used for the context specified. This returnc a <see cref="T:System.Threading.Tasks.TaskScheduler" /> type. The functionality in this method is intended to be exposed by helper classes in MPF and not to be directly consumed by users.</summary>
        /// <param name="context">The context.</param>
        /// <returns>The HRESULT.</returns>
        public object GetTaskScheduler(uint context) =>
            FakeIVsTask.GetSchedulerFromContext((__VSTASKRUNCONTEXT)context);
    }
}
