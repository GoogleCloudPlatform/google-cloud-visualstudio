﻿// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Runtime.Serialization;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Exception thrown when there's an issue in a data source.
    /// </summary>
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