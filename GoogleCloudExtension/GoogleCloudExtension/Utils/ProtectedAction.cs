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
    /// <summary>
    /// This class wraps an <seealso cref="Action"/> with the <seealso cref="ErrorHandlerUtils.HandleExceptions(Action)"/>
    /// method to correclty handle all exceptions that escape the action.
    /// </summary>
    public class ProtectedAction
    {
        private readonly Action _action;

        public ProtectedAction(Action action)
        {
            _action = action;
        }

        /// <summary>
        /// Invokes the action handling all of the exceptions that escape the action.
        /// </summary>
        public void Invoke()
        {
            ErrorHandlerUtils.HandleExceptions(() => _action());
        }
    }

    /// <summary>
    /// This class wraps an <seealso cref="Action{T}"/> with the <seealso cref="ErrorHandlerUtils.HandleExceptions(Action)"/>
    /// method to correclty handle all exceptions that escape the action.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    public class ProtectedAction<TIn>
    {
        private readonly Action<TIn> _action;

        public ProtectedAction(Action<TIn> action)
        {
            _action = action;
        }

        /// <summary>
        /// Invokes the action handling all of the exceptions that escape the action.
        /// </summary>
        public void Invoke(TIn param1)
        {
            ErrorHandlerUtils.HandleExceptions(() => _action.Invoke(param1));
        }
    }

    /// <summary>
    /// This class wraps an <seealso cref="Action{T1, T2}"/> with the <seealso cref="ErrorHandlerUtils.HandleExceptions(Action)"/>
    /// method to correclty handle all exceptions that escape the action.
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    public class ProtectedAction<TIn1, TIn2>
    {
        private readonly Action<TIn1, TIn2> _action;

        public ProtectedAction(Action<TIn1, TIn2> action)
        {
            _action = action;
        }

        /// <summary>
        /// Invokes the action handling all of the exceptions that escape the action.
        /// </summary>
        public void Invoke(TIn1 param1, TIn2 param2)
        {
            ErrorHandlerUtils.HandleExceptions(() => _action.Invoke(param1, param2));
        }
    }
}
