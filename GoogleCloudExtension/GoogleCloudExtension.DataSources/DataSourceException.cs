// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

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