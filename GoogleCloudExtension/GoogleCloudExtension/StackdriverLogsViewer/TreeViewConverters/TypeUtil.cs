// Copyright 2016 Google Inc. All Rights Reserved.
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
using System.Collections;
using System.Collections.Generic;

namespace GoogleCloudExtension.StackdriverLogsViewer.TreeViewConverters
{
    /// <summary>
    /// Utility methods primarily used by ObjectNodeTree 
    /// </summary>
    public static class TypeUtil
    {
        /// <summary>
        /// Check if the type is a IList generic type.
        /// </summary>
        public static bool IsListType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        /// <summary>
        /// Check if the type is IDictionary type.
        /// </summary>
        public static bool IsDictionaryType(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }

        /// <summary>
        /// Check if the object is a IList type
        /// </summary>
        public static bool IsListObject(this object obj)
        {
            return obj != null && obj is IList && obj.GetType().IsListType();
        }

        /// <summary>
        /// Check if the object is IDictionary
        /// </summary>
        public static bool IsDictionaryObject(this object obj)
        {
            return obj != null && obj.GetType().IsDictionaryType();
        }

        /// <summary>
        /// Check if the object is Numeric type
        /// </summary>
        public static bool IsNumericType(this object obj)
        {
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
