// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

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
        private readonly WeakDelegate<T> _delegate;

        public WeakCommand(Action<T> handler, bool canExecuteCommand = true)
        {
            _delegate = new WeakDelegate<T>(handler);
            this.CanExecuteCommand = canExecuteCommand;
        }

        public event EventHandler CanExecuteChanged;

        private bool _CanExecuteCommand;
        public bool CanExecuteCommand
        {
            get { return _CanExecuteCommand; }
            set
            {
                if (_CanExecuteCommand != value)
                {
                    _CanExecuteCommand = value;
                    if (CanExecuteChanged != null)
                    {
                        CanExecuteChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public bool CanExecute(object parameter)
        {
            return this.CanExecuteCommand;
        }

        public void Execute(object parameter)
        {
            if (parameter == null)
            {
                // Need an object to execute the command, the parameter can be null
                // while the bindings are being computed.
                return;
            }
            if (CanExecuteCommand)
            {
                _delegate.Invoke((T)parameter);
            }
        }
    }

    /// <summary>
    /// Implements the weak ICommand pattern for the case when there is no parameter to be
    /// passed to the handler.
    /// </summary>
    public class WeakCommand : ICommand
    {
        private bool _CanExecuteCommand;
        private readonly WeakDelegate _delegate;

        public WeakCommand(Action handler, bool canExecuteCommand = true)
        {
            _delegate = new WeakDelegate(handler);
            this.CanExecuteCommand = canExecuteCommand;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecuteCommand
        {
            get { return _CanExecuteCommand; }
            set
            {
                if (_CanExecuteCommand != value)
                {
                    _CanExecuteCommand = value;
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool CanExecute(object parameter) => CanExecuteCommand;

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                _delegate.Invoke();
            }
        }
    }
}
