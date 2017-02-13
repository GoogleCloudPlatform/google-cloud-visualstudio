using System;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// Exception thrown from the methods implemented in this project.
    /// </summary>
    [Serializable]
    internal class DeploymentException : Exception
    {
        public DeploymentException()
        {
        }

        public DeploymentException(string message) : base(message)
        {
        }

        public DeploymentException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeploymentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}