using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    public class ProtectedDelegate
    {
        private readonly Delegate _delegate;

        public ProtectedDelegate(Delegate src)
        {
            _delegate = src;
        }

        public void Invoke(params object[] args)
        {
            ErrorHandlerUtils.HandleExceptions(() => _delegate.Method.Invoke(_delegate.Target, args));
        }
    }
}
