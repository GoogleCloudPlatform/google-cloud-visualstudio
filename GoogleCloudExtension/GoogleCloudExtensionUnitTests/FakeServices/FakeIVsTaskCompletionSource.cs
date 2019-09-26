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

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleCloudExtensionUnitTests.FakeServices
{
    public class FakeIVsTaskCompletionSource : IVsTaskCompletionSource
    {
        private readonly TaskCompletionSource<object> _source;

        public FakeIVsTaskCompletionSource() : this(new TaskCompletionSource<object>())
        {
        }

        public FakeIVsTaskCompletionSource(uint options, object asyncState) : this(
            new TaskCompletionSource<object>(asyncState, (TaskCreationOptions)options))
        {
        }

        private FakeIVsTaskCompletionSource(TaskCompletionSource<object> source)
        {
            _source = source;
            Task = new FakeIVsTask(_source.Task);
        }

        /// <summary>Sets the task owned by this source to completed state with the result.</summary>
        /// <param name="result">The result to be set.</param>
        public void SetResult(object result) => _source.SetResult(result);

        /// <summary>Sets the task owned by this source to cancelled state, also cancelling the task.</summary>
        public void SetCanceled() => _source.SetCanceled();

        /// <summary>Sets the task owned by this source to the faulted state (with the given HRESULT code).</summary>
        /// <param name="hr">The error code to set in the faulted state.</param>
        public void SetFaulted(int hr) => _source.SetException(Marshal.GetExceptionForHR(hr));

        /// <summary>Adds the specified task to the task completion sources dependent task list. Then if <see cref="M:Microsoft.VisualStudio.Shell.Interop.IVsTask.Wait" /> is called on <see langword="IVsTaskCompletionSource.Task" />, the UI can be unblocked correctly.</summary>
        /// <param name="pTask">The task to add to the list.</param>
        public void AddDependentTask(IVsTask pTask) => throw new NotSupportedException();

        /// <summary>Gets the task owned by this source.</summary>
        public IVsTask Task { get; }
    }
}