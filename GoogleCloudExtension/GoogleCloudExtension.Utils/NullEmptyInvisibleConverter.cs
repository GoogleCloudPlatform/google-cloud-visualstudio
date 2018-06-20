// Copyright 2017 Google Inc. All Rights Reserved.
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

using System.Windows;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// If the object is null, or if it is string type and is empty or whitespace,
    /// set the visibility to <seealso cref="Visibility.Collapsed"/>.
    /// Otherwise, set the visibility as <seealso cref="Visibility.Visible"/>.
    /// Note: Only Convert is implemented, so this is not a bidirectional converter, do not use on TwoWay bindings.
    /// </summary>
    public class NullEmptyInvisibleConverter : NullEmptyConverter<Visibility>
    {
        protected override Visibility EmptyValue { get; } = Visibility.Collapsed;
        protected override Visibility NotEmptyValue { get; } = Visibility.Visible;
    }
}

