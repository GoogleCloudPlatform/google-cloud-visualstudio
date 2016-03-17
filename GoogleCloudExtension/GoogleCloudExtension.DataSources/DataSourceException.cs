using System;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.DataSources
{
    [Serializable]
    public class DataSourceException : Exception
    {
        public DataSourceException()
        {
        }

        public DataSourceException(string message) : base(message)
        {
        }

        public DataSourceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DataSourceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}