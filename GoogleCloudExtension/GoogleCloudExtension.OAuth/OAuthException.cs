// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.OAuth
{
    /// <summary>
    /// Exception thrown when encountering issues with oauth servers.
    /// </summary>
    [Serializable]
    public class OAuthException : Exception
    {
        public OAuthException()
        {
        }

        public OAuthException(string message) : base(message)
        {
        }

        public OAuthException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected OAuthException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}