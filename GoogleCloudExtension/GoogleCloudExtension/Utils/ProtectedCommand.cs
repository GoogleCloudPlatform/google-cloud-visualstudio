using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.Utils
{
    public class ProtectedCommand : ICommand
    {
        private bool _canExecuteCommand;
        private readonly ProtectedAction _action;

        /// <summary>
        /// Initializes a new instance of WeakCommand.
        /// </summary>
        /// <param name="handler">The action to execute when the command is executed.</param>
        /// <param name="canExecuteCommand">Whether the command is enabled or not.</param>
        public ProtectedCommand(Action handler, bool canExecuteCommand = true)
        {
            _action = new ProtectedAction(handler);
            this.CanExecuteCommand = canExecuteCommand;
        }

        #region ICommand implementation.

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => CanExecuteCommand;

        public void Execute(object parameter)
        {
            _action.Invoke();
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

    public class ProtectedCommand<T> : ICommand
    {
        private bool _canExecuteCommand;
        private readonly ProtectedAction<T> _action;

        /// <summary>
        /// Initializes the new instance of WeakCommand.
        /// </summary>
        /// <param name="handler">The action to execute when executing the command.</param>
        /// <param name="canExecuteCommand">Whether the command is enabled or not.</param>
        public ProtectedCommand(Action<T> handler, bool canExecuteCommand = true)
        {
            _action = new ProtectedAction<T>(handler);
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
                _action.Invoke((T)parameter);
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
}
