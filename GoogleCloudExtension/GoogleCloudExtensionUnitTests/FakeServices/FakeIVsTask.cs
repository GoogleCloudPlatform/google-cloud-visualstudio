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
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.FakeServices
{
    public class FakeIVsTask : IVsTask, IVsTaskEvents, IVsTaskJoinableTask
    {
        private readonly Task<object> _t;
        private readonly CancellationTokenSource _cancelSource;

        public FakeIVsTask(Task<object> t, CancellationTokenSource cancelSource = null)
        {
            _t = t;
            _cancelSource = cancelSource;
        }

        private FakeIVsTask(
            FakeIVsTask parent,
            __VSTASKRUNCONTEXT context,
            __VSTASKCONTINUATIONOPTIONS options,
            IVsTaskBody body,
            object asyncState)
        {
            TaskScheduler scheduler = GetSchedulerFromContext(context);
            TaskContinuationOptions continuationOptions = GetTaskContinuationOptions(options);
            _t = parent._t.ContinueWith(
                (task, state) =>
                {
                    body.DoWork(this, 1, new IVsTask[] { parent }, out object result);
                    return result;
                },
                asyncState,
                default(CancellationToken),
                continuationOptions,
                scheduler);
            parent.OnMarkedAsBlocking?.Invoke(parent, new BlockingTaskEventArgs(parent, this));
        }

        [SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")]
        public static TaskContinuationOptions GetTaskContinuationOptions(__VSTASKCONTINUATIONOPTIONS vsTaskOptions) =>
            (TaskContinuationOptions)vsTaskOptions;

        /// <summary>Appends the provided action to this task to be run after the task is run to completion. The action is invoked on the context provided.</summary>
        /// <param name="context">[in] Where to run this task. Values are from <see cref="T:Microsoft.VisualStudio.Shell.Interop.__VSTASKRUNCONTEXT" />.</param>
        /// <param name="pTaskBody">[in] Action to be executed.</param>
        /// <returns>A new <see cref="T:Microsoft.VisualStudio.Shell.Interop.IVsTask" /> instance that has the current task as its parent.</returns>
        public IVsTask ContinueWith(uint context, IVsTaskBody pTaskBody) => ContinueWithEx(context, 0, pTaskBody, null);

        /// <summary>Appends the provided action (using the specified options) to this task to be run after the task is run to completion. The action is invoked on the context provided.</summary>
        /// <param name="context">[in] Where to run this task. Values are from <see cref="T:Microsoft.VisualStudio.Shell.Interop.__VSTASKRUNCONTEXT" />.</param>
        /// <param name="options">[in] Allows setting task continuation options. Values are from <see cref="T:Microsoft.VisualStudio.Shell.Interop.__VSTASKCONTINUATIONOPTIONS" />.</param>
        /// <param name="pTaskBody">[in] Action to be executed.</param>
        /// <param name="pAsyncState">[in] The asynchronous state of the task.</param>
        /// <returns>A new <see cref="T:Microsoft.VisualStudio.Shell.Interop.IVsTask" /> instance that has the current task as its parent.</returns>
        public IVsTask ContinueWithEx(uint context, uint options, IVsTaskBody pTaskBody, object pAsyncState) =>
            new FakeIVsTask(
                this,
                (__VSTASKRUNCONTEXT)context,
                (__VSTASKCONTINUATIONOPTIONS)options,
                pTaskBody,
                pAsyncState);

        /// <summary>Starts the task.</summary>
        public void Start() => _t.Start();

        /// <summary>Cancels the task group. An antecedent task and all of its children share the same cancellation token, so cancelling any of the tasks cancels the whole task group.</summary>
        public void Cancel() => _cancelSource?.Cancel();

        /// <summary>Waits for the task to complete (not including any continuations) and returns the result set by the task. If the task returns an error code or an exception, this method returns the same error code.</summary>
        /// <returns>The result set by the task.</returns>
        [SuppressMessage("", "VSTHRD002", Justification = "Synchronous wait required by interface.")]
        public object GetResult()
        {
            OnBlockingWaitBegin?.Invoke(this, null);
            object result = _t.Result;
            OnBlockingWaitEnd?.Invoke(this, null);
            return result;
        }

        /// <summary>Aborts the task if the task has been cancelled. Use this method to return from a cancelled task.</summary>
        public void AbortIfCanceled() { }

        /// <summary>Waits for the task to complete (not including any continuations). If the task returns an error code or an exception, this method returns the same error code.</summary>
        public void Wait() => WaitEx(-1, 0);

        /// <summary>Waits for the task to complete (not including any continuations). You can either specify a timeout (or INFINITE) or set the option to abort on task cancellation.</summary>
        /// <param name="millisecondsTimeout">The timeout (in milliseconds) or INFINITE.</param>
        /// <param name="options">Values are of type <see cref="T:Microsoft.VisualStudio.Shell.Interop.__VSTASKWAITOPTIONS" />. Set to VSTWO_AbortOnTaskCancellation to abort if a cancellation occurs.</param>
        /// <returns>
        /// <see langword="true" /> if the task completed successfully before <paramref name="millisecondsTimeout" />, otherwise <see langword="false" />.</returns>
        [SuppressMessage("", "VSTHRD002", Justification = "Synchronous wait required by interface.")]
        public bool WaitEx(int millisecondsTimeout, uint options)
        {
            OnBlockingWaitBegin?.Invoke(this, null);
            bool waitResult = _t.Wait(millisecondsTimeout);
            OnBlockingWaitEnd?.Invoke(this, null);
            return waitResult;
        }

        /// <summary>Gets whether the task completed with an exception. If <see langword="true" />, an exception occurred.</summary>
        public bool IsFaulted => _t.IsFaulted;

        /// <summary>Gets whether the task result is available. If <see langword="true" />, the task result is available. If <see langword="false" />, a <see cref="M:Microsoft.VisualStudio.Shell.Interop.IVsTask.GetResult" /> call is blocked until the task is completed.</summary>
        public bool IsCompleted => _t.IsCompleted;

        /// <summary>Gets whether the task group is cancelled. If <see langword="true" />, the task group is cancelled.</summary>
        public bool IsCanceled => _t.IsCanceled;

        /// <summary>Gets the asynchronous state object that was given when the task was created.</summary>
        public object AsyncState => _t.AsyncState;

        /// <summary>Gets or sets the description for the text that is displayed for component diagnostics.</summary>
        public string Description { get; set; }

        /// <summary>
        /// Raised when a blocking wait call made to IVsTask instance on main thread of Visual Studio
        /// </summary>
        public event EventHandler OnBlockingWaitBegin;

        /// <summary>
        /// Raised when a blocking wait call to IVsTask is finished on main thread of Visual Studio
        /// </summary>
        public event EventHandler OnBlockingWaitEnd;

        /// <summary>
        /// Raised when this task is marked as a blocking task for a wait on main thread of Visual Studio
        /// </summary>
        public event EventHandler<BlockingTaskEventArgs> OnMarkedAsBlocking;

        /// <summary>
        /// Indicates that this IVsTask instance acts as a wrapper around the specified JoinableTask.
        /// </summary>
        public void AssociateJoinableTask(object joinableTask)
        {
        }

        /// <summary>Gets the cancellation token used for this task.</summary>
        public CancellationToken CancellationToken => default(CancellationToken);

        public static TaskScheduler GetSchedulerFromContext(__VSTASKRUNCONTEXT context)
        {
            switch (context)
            {
                case __VSTASKRUNCONTEXT.VSTC_BACKGROUNDTHREAD:
                case __VSTASKRUNCONTEXT.VSTC_BACKGROUNDTHREAD_LOW_IO_PRIORITY:
                    return TaskScheduler.Default;
                case __VSTASKRUNCONTEXT.VSTC_UITHREAD_BACKGROUND_PRIORITY:
                case __VSTASKRUNCONTEXT.VSTC_UITHREAD_IDLE_PRIORITY:
                case __VSTASKRUNCONTEXT.VSTC_UITHREAD_NORMAL_PRIORITY:
                case __VSTASKRUNCONTEXT.VSTC_UITHREAD_SEND:
                case __VSTASKRUNCONTEXT.VSTC_CURRENTCONTEXT:
                    return AssemblyInitialize.ApplicationTaskScheduler;
                default:
                    return TaskScheduler.Current;
            }
        }
    }
}