// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Reflection;
using System.Windows.Input;

namespace GoogleCloudExtension.Utils
{
    public delegate void WeakCommandHandler(object parameter);

    /// <summary>
    /// Implements the ICommand interface but keeping a weak reference back to the object
    /// that actually implements the command via a delegate, useful to avoid leaks of ViewModels.
    /// </summary>
    public class WeakCommand : ICommand
    {
        private readonly WeakReference _target;
        private readonly MethodInfo _method;

        public WeakCommand(WeakCommandHandler handler, bool canExecuteCommand = true)
        {
            _target = new WeakReference(handler.Target);
            _method = handler.Method;
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
            object target = _target.Target;
            if (target != null && this.CanExecute(parameter))
            {
                _method.Invoke(target, new object[] { parameter });
            }
        }
    }
}
