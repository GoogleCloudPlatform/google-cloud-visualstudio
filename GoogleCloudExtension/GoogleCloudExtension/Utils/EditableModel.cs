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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// An <see cref="IEditableObject"/> for a single object.
    /// </summary>
    /// <typeparam name="T">The type this EditableModel contains.</typeparam>
    public class EditableModel<T> : Model, IEditableObject
    {
        private T _value;
        private T _uneditedValue;
        private bool _isEdit;

        /// <summary>
        /// The current value of this <see cref="EditableModel{T}"/>.
        /// </summary>
        public T Value
        {
            get { return _value; }
            set { SetValueAndRaise(ref _value, value); }
        }

        /// <summary>
        /// Creates new <see cref="EditableModel{T}"/> wrapping the given value.
        /// </summary>
        /// <param name="value"></param>
        public EditableModel(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new <see cref="EditableModel{T}"/> with a default value.
        /// </summary>
        /// <remarks>
        /// Used by <see cref="System.Windows.Controls.DataGrid"/> when adding new rows.
        /// </remarks>
        public EditableModel() : this(default(T)) { }

        /// <inheritdoc />
        public override string ToString()
        {
            return Value?.ToString() ?? "";
        }

        /// <inheritdoc />
        public void BeginEdit()
        {
            if (!_isEdit)
            {
                _uneditedValue = Value;
                _isEdit = true;
            }
        }

        /// <inheritdoc />
        public void EndEdit()
        {
            _uneditedValue = default(T);
            _isEdit = false;
        }

        /// <inheritdoc />
        public void CancelEdit()
        {
            if (_isEdit)
            {
                Value = _uneditedValue;
                _isEdit = false;
            }
        }
    }

    /// <summary>
    /// Helper functions for dealing with <see cref="EditableModel{T}"/>
    /// </summary>
    public static class EditableModel
    {
        /// <summary>
        /// Creates a new <see cref="EditableModel{T}"/>
        /// </summary>
        /// <typeparam name="T">The type of the object to wrap.</typeparam>
        /// <param name="input">The object to wrap.</param>
        /// <returns>A new <see cref="EditableModel{T}"/> wrapping the input.</returns>
        public static EditableModel<T> Of<T>(T input) => new EditableModel<T>(input);

        /// <summary>
        /// Wraps the elements of an <see cref="IEnumerable{T}"/> in <see cref="EditableModel{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type to wrap.</typeparam>
        /// <param name="input">The objects to wrap.</param>
        public static IEnumerable<EditableModel<T>> ToEditableModels<T>(this IEnumerable<T> input) =>
            input?.Select(Of);

        /// <summary>
        /// Unwraps an IEnumerable &lt;EditableModel&lt;<typeparamref name="T"/>&gt;&gt;
        /// to a plain IEnumerable &lt;<typeparamref name="T"/>&gt;.
        /// </summary>
        /// <typeparam name="T">The type unwraped from <see cref="EditableModel{T}"/>.</typeparam>
        public static IEnumerable<T> Values<T>(this IEnumerable<EditableModel<T>> input) =>
            input?.Select(model => model.Value);
    }
}