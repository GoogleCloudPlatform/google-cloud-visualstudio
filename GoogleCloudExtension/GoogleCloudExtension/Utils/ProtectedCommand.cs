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
    /// This class implements the <seealso cref="System.Windows.Input.ICommand"/>
    /// interface and wraps the action for the command in <seealso cref="ErrorHandlerUtils.HandleExceptions(Action)"/>
    /// to handle the exceptions that could escape.
    /// </summary>
    public class ProtectedCommand : ProtectedCommandBase
    {
        private readonly Action _action;

        /// <summary>
        /// Initializes a new instance of <see cref="ProtectedCommand"/>.
        /// </summary>
        /// <param name="action">The action to invoke when the command is executed.</param>
        /// <param name="canExecuteCommand">Whether the command is enabled or not.</param>
        public ProtectedCommand(Action action, bool canExecuteCommand = true) : base(canExecuteCommand)
        {
            _action = action;
        }

        /// <summary>
        /// Safely executes the supplied action.
        /// </summary>
        public override void Execute(object _) => ErrorHandlerUtils.HandleExceptions(_action);
    }

    /// <summary>
    /// This class implements the <seealso cref="System.Windows.Input.ICommand"/>
    /// interface and wraps the action for the command in <seealso cref="ErrorHandlerUtils.HandleExceptions(Action)"/>
    /// to handle the exceptions that could escape.
    /// </summary>
    public class ProtectedCommand<T> : ProtectedCommandBase
    {
        private readonly Action<T> _action;

        /// <summary>
        /// Initializes the new instance of <see cref="ProtectedCommand{T}"/>.
        /// </summary>
        /// <param name="action">The action to invoke when executing the command.</param>
        /// <param name="canExecuteCommand">Whether the command is enabled or not.</param>
        public ProtectedCommand(Action<T> action, bool canExecuteCommand = true) : base(canExecuteCommand)
        {
            _action = action;
        }

        /// <summary>
        /// Safely executes the supplied action.
        /// </summary>
        /// <param name="argument">The argument to the action.</param>
        public override void Execute(object argument)
        {
            if (argument is T typedArgument)
            {
                ErrorHandlerUtils.HandleExceptions(() => _action.Invoke(typedArgument));
            }
        }
    }
}
