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

namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// This event is sent every time that there's an unhandled exception.
    /// </summary>
    internal static class UnhandledExceptionEvent
    {
        private const string UnhandledExceptionEventName = "unhandledException";
        private const string ExceptionProperty = "exception";

        public static AnalyticsEvent Create(Exception ex)
        {
            if (ex is AggregateException)
            {
                return new AnalyticsEvent(
                    UnhandledExceptionEventName,
                    ExceptionProperty, ex.InnerException.GetType().Name);
            }
            else
            {
                return new AnalyticsEvent(
                    UnhandledExceptionEventName,
                    ExceptionProperty, ex.GetType().Name);
            }
        }
    }
}
