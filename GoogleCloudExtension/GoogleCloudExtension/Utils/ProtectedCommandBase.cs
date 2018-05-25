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
using System.Windows.Input;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Base class for all protected commands. Provides the <see cref="CanExecute"/> implementation.
    /// </summary>
    public abstract class ProtectedCommandBase : ICommand
    {
        private bool _canExecuteCommand;

        /// <summary>
        /// Gets/sets whether the command can be executed.
        /// </summary>
        public bool CanExecuteCommand
        {
            get => _canExecuteCommand;
            set
            {
                if (_canExecuteCommand != value)
                {
                    _canExecuteCommand = value;
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Sets the inital value of <see cref="CanExecuteCommand"/>
        /// </summary>
        /// <param name="canExecuteCommand"></param>
        protected ProtectedCommandBase(bool canExecuteCommand)
        {
            CanExecuteCommand = canExecuteCommand;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command can execute.
        /// </summary>
        public virtual event EventHandler CanExecuteChanged;

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        /// <param name="_">Unused.</param>
        public bool CanExecute(object _) => CanExecuteCommand;

        /// <summary>The method called when the command is invoked.</summary>
        /// <param name="argument">
        /// Data used by the command. If the command does not require data, this parameter can be set to null.
        /// </param>
        public abstract void Execute(object argument);
    }
}