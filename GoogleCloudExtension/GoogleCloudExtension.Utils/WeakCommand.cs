﻿// Copyright 2016 Google Inc. All Rights Reserved.
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
    /// Implements the ICommand interface but keeping a weak reference back to the object
    /// that actually implements the command via a delegate, useful to avoid leaks of ViewModels.
    /// <typeparam name="T">The type of the object passed in to the handler.</typeparam>
    /// </summary>
    public class WeakCommand<T> : ICommand
    {
        private bool _canExecuteCommand;
        private readonly WeakAction<T> _delegate;

        /// <summary>
        /// Initializes the new instance of WeakCommand.
        /// </summary>
        /// <param name="handler">The action to execute when executing the command.</param>
        /// <param name="canExecuteCommand">Whether the command is enabled or not.</param>
        public WeakCommand(Action<T> handler, bool canExecuteCommand = true)
        {
            _delegate = new WeakAction<T>(handler);
            this.CanExecuteCommand = canExecuteCommand;
        }

        #region ICommand implementation.

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return CanExecuteCommand;
        }

        public void Execute(object parameter)
        {
            if (parameter is T)
            {
                _delegate.Invoke((T)parameter);
            }
        }

        #endregion

        /// <summary>
        /// Gets/sets whether the command can be executed.
        /// </summary>
        public bool CanExecuteCommand
        {
            get { return _canExecuteCommand; }
            set
            {
                if (_canExecuteCommand != value)
                {
                    _canExecuteCommand = value;
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    /// <summary>
    /// Implements the weak ICommand pattern for the case when there is no parameter to be
    /// passed to the handler.
    /// </summary>
    public class WeakCommand : ICommand
    {
        private bool _canExecuteCommand;
        private readonly WeakAction _delegate;

        /// <summary>
        /// Initializes a new instance of WeakCommand.
        /// </summary>
        /// <param name="handler">The action to execute when the command is executed.</param>
        /// <param name="canExecuteCommand">Whether the command is enabled or not.</param>
        public WeakCommand(Action handler, bool canExecuteCommand = true)
        {
            _delegate = new WeakAction(handler);
            this.CanExecuteCommand = canExecuteCommand;
        }

        #region ICommand implementation.

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => CanExecuteCommand;

        public void Execute(object parameter)
        {
            _delegate.Invoke();
        }

        #endregion

        /// <summary>
        /// Gets/sets whether the command can be executed or not.
        /// </summary>
        public bool CanExecuteCommand
        {
            get { return _canExecuteCommand; }
            set
            {
                if (_canExecuteCommand != value)
                {
                    _canExecuteCommand = value;
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
