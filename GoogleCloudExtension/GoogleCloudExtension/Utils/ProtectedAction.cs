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

using System;

namespace GoogleCloudExtension.Utils
{
    public class ProtectedAction
    {
        private readonly Action _action;

        public ProtectedAction(Action action)
        {
            _action = action;
        }

        public void Invoke()
        {
            ErrorHandlerUtils.HandleExceptions(() => _action());
        }
    }

    public class ProtectedAction<TIn>
    {
        private readonly Action<TIn> _action;

        public ProtectedAction(Action<TIn> action)
        {
            _action = action;
        }

        public void Invoke(TIn param1)
        {
            ErrorHandlerUtils.HandleExceptions(() => _action.Invoke(param1));
        }
    }

    public class ProtectedAction<TIn1, TIn2>
    {
        private readonly Action<TIn1, TIn2> _action;

        public ProtectedAction(Action<TIn1, TIn2> action)
        {
            _action = action;
        }

        public void Invoke(TIn1 param1, TIn2 param2)
        {
            ErrorHandlerUtils.HandleExceptions(() => _action.Invoke(param1, param2));
        }
    }
}
