using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.DataSources
{
    [Serializable]
    internal class GrpcOperationException : Exception
    {
        public IDictionary<string, object> Error { get; }

        public GrpcOperationException()
        {
        }

        public GrpcOperationException(IDictionary<string, object> error) : base(nameof(GrpcOperationException))
        {
            Error = error;
        }

        public GrpcOperationException(string message) : base(message)
        {
        }

        public GrpcOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GrpcOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}