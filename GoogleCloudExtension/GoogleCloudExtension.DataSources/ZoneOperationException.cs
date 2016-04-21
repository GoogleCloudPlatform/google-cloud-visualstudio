// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Exception thrown when there's an error waiting for a ZoneOperation operation
    /// to complete.
    /// </summary>
    [Serializable]
    public class ZoneOperationException : Exception
    {
        public Google.Apis.Compute.v1.Data.Operation.ErrorData Error { get; }

        public ZoneOperationException()
        {
        }

        public ZoneOperationException(string message) : base(message)
        {
        }

        public ZoneOperationException(Google.Apis.Compute.v1.Data.Operation.ErrorData error) : base("Zone failed")
        {
            this.Error = error;
        }

        public ZoneOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ZoneOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}