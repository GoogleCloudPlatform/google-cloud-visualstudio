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

using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Threading;

namespace TestingHelpers
{
    public class FakeSyncContext : SynchronizationContext
    {
        private readonly List<object> _postStates = new List<object>();
        public IReadOnlyList<object> PostStates => _postStates;

        public static IDisposable CreateCurrent() => CreateCurrent(out _);

        public static IDisposable CreateCurrent(out FakeSyncContext newCurrent)
        {
            SynchronizationContext oldSyncContext = Current;
            newCurrent = new FakeSyncContext();
            SetSynchronizationContext(newCurrent);
            return new Disposable(() => SetSynchronizationContext(oldSyncContext));
        }

        public override void OperationCompleted() => throw new NotSupportedException();

        public override void OperationStarted() { }

        public override void Post(SendOrPostCallback d, object state)
        {
            _postStates.Add(state);
            d(state);
        }

        public override void Send(SendOrPostCallback d, object state) => throw new NotSupportedException();

        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout) => throw new NotSupportedException();
    }
}