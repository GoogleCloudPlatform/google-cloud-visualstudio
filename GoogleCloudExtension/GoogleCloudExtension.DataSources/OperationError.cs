using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.DataSources
{
    [Serializable]
    internal class OperationError : Exception
    {
        public IDictionary<string, object> Error { get; }

        public OperationError()
        {
        }

        public OperationError(IDictionary<string, object> error)
        {
            Error = error;
        }

        public OperationError(string message) : base(message)
        {
        }

        public OperationError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected OperationError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}