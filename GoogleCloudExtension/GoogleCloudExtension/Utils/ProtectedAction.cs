using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    class ProtectedAction<TIn1, TIn2>
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
