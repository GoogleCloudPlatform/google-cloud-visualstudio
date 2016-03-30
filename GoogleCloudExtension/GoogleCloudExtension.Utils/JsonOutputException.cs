// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Exception thrown when there's a problem parsing json output from a process.
    /// </summary>
    [Serializable]
    public class JsonOutputException : Exception
    {
        public JsonOutputException()
        {
        }

        public JsonOutputException(string message) : base(message)
        {
        }

        public JsonOutputException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected JsonOutputException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}