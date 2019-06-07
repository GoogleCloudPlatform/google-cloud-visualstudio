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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GoogleCloudExtension.Utils.Async;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class implements the <seealso cref="System.Windows.Input.ICommand"/> interface and wraps the action for the command
    /// in <seealso cref="ErrorHandlerUtils.HandleExceptionsAsync"/>
    /// to handle the exceptions that could escape.
    /// </summary>
    public class ProtectedAsyncCommand : ProtectedCommandBase, INotifyPropertyChanged
    {
        private readonly Func<Task> _asyncAction;
        private AsyncProperty _latestExecution;

        /// <summary>
        /// The latest task executed for this command.
        /// </summary>
        public AsyncProperty LatestExecution
        {
            get => _latestExecution;
            private set => SetValueAndRaise(out _latestExecution, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of ProtectedAsyncCommand.
        /// </summary>
        /// <param name="asyncAction">The async task to execute when the command is executed.</param>
        /// <param name="canExecuteCommand">Whether the command is enabled or not.</param>
        public ProtectedAsyncCommand(Func<Task> asyncAction, bool canExecuteCommand = true) : base(canExecuteCommand)
        {
            _asyncAction = asyncAction;
            LatestExecution = new AsyncProperty(Task.CompletedTask);
        }

        /// <summary>Defines the method to be called when the command is invoked.</summary>
        /// <param name="argument">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public override void Execute(object argument)
        {
            Task actionTask = _asyncAction();
            ErrorHandlerUtils.HandleExceptionsAsync(() => actionTask);
            LatestExecution = new AsyncProperty(actionTask);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void SetValueAndRaise<T>(out T field, T value, [CallerMemberName] string propertyName = null)
        {
            field = value;
            OnPropertyChanged(propertyName);
        }
    }
    /// <summary>
    /// This class implements the <seealso cref="System.Windows.Input.ICommand"/> interface and wraps the action for the command
    /// in <seealso cref="ErrorHandlerUtils.HandleExceptionsAsync"/>
    /// to handle the exceptions that could escape.
    /// </summary>
    public class ProtectedAsyncCommand<T> : ProtectedCommandBase, INotifyPropertyChanged
    {
        private readonly Func<T, Task> _asyncAction;
        private AsyncProperty _latestExecution;

        /// <summary>
        /// The latest task executed for this command.
        /// </summary>
        public AsyncProperty LatestExecution
        {
            get => _latestExecution;
            private set => SetValueAndRaise(out _latestExecution, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of ProtectedAsyncCommand.
        /// </summary>
        /// <param name="asyncAction">The async task to execute when the command is executed.</param>
        /// <param name="canExecuteCommand">Whether the command is enabled or not.</param>
        public ProtectedAsyncCommand(Func<T, Task> asyncAction, bool canExecuteCommand = true) : base(canExecuteCommand)
        {
            _asyncAction = asyncAction;
            LatestExecution = new AsyncProperty(Task.CompletedTask);
        }

        /// <summary>Defines the method to be called when the command is invoked.</summary>
        /// <param name="argument">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public override void Execute(object argument)
        {
            Task actionTask = _asyncAction((T)argument);
            ErrorHandlerUtils.HandleExceptionsAsync(() => actionTask);
            LatestExecution = new AsyncProperty(actionTask);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void SetValueAndRaise<TField>(
            out TField field,
            TField value,
            [CallerMemberName] string propertyName = null)
        {
            field = value;
            OnPropertyChanged(propertyName);
        }
    }
}