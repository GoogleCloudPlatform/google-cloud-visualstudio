// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Exception to be thrown from the data sources in this project.
    /// </summary>
    public class DataSourceException : Exception
    {
        public DataSourceException(string message) : base(message)
        {
        }

        public DataSourceException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}