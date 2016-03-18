using System;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.CloudExplorer
{
    [Serializable]
    internal class CloudExplorerSourceException : Exception
    {
        public CloudExplorerSourceException()
        {
        }

        public CloudExplorerSourceException(string message) : base(message)
        {
        }

        public CloudExplorerSourceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CloudExplorerSourceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}