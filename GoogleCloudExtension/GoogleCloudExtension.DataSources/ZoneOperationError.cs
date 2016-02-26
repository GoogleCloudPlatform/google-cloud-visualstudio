using GoogleCloudExtension.DataSources.Models;
using System;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.DataSources
{
    [Serializable]
    public class ZoneOperationError : Exception
    {
        public Error Error { get; }

        public ZoneOperationError()
        {
        }

        public ZoneOperationError(string message) : base(message)
        {
        }

        public ZoneOperationError(Error error) : base("Zone failed")
        {
            this.Error = error;
        }

        public ZoneOperationError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ZoneOperationError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}