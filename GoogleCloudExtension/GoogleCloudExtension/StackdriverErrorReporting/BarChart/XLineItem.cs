﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Utils;
using System;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Bar chart shows y-scale lines
    /// </summary>
    public class XLineItem : Model
    {
        /// <summary>
        /// The scale value.
        /// </summary>
        public string CountScale { get; }

        /// <summary>
        /// Height of each row.
        /// A y-cale line is shown of each row.
        /// </summary>
        public int RowHeight => TimedCountBarChartControl.RowHeight;

        /// <summary>
        /// Initializes a new instance of <seealso cref="XLineItem"/> class.
        /// </summary>
        /// <param name="scale"></param>
        public XLineItem(double scale)
        {
            CountScale = scale == 0 ? null :
                String.Format(((Math.Round(scale) == scale) ? "{0:0}" : "{0:0.00}"), scale);
        }
    }
}