using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Exception thrown when there's an error waiting for a gRPC operation.
    /// </summary>
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