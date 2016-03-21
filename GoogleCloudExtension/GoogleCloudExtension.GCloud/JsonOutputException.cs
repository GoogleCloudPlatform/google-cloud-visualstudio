// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.GCloud
{
    [Serializable]
    internal class JsonOutputException : Exception
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