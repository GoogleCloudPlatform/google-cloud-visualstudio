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

using GoogleCloudExtension.Utils;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Define AM/PM as enums.
    /// </summary>
    [Serializable]
    public enum TimeType { AM, PM }

    /// <summary>
    /// The view model for TimeBox user control.
    /// </summary>
    public class TimeBoxViewModel : ViewModelBase, IDataErrorInfo
    {
        private int _hour;
        private int _minute;
        private int _second;
        private TimeType _timeType;

        /// <summary>
        /// Gets or sets hour value.
        /// </summary>
        public int Hour
        {
            get { return _hour; }
            set { SetTimePartBoxValue(ref _hour, value, 12); }
        }

        /// <summary>
        /// Gets or sets minute value.
        /// </summary>
        public int Minute
        {
            get { return _minute; }
            set { SetTimePartBoxValue(ref _minute, value, 60); }
        }

        /// <summary>
        /// Gets or sets second value.
        /// </summary>
        public int Second
        {
            get { return _second; }
            set { SetTimePartBoxValue(ref _second, value, 60); }
        }

        /// <summary>
        /// Gets the selected TimeType binded to view.
        /// </summary>
        public TimeType TimeType
        {
            get { return _timeType; }
            set { SetValueAndRaise(ref _timeType, value); }
        }

        /// <summary>
        /// Gets or sets the time box time.
        /// </summary>
        public TimeSpan Time
        {
            get
            {
                var h = Hour;
                var hour = TimeType == TimeType.AM ? h : (h % 12) + 12;
                return new TimeSpan(hour, Minute, Second);
            }

            set
            {
                TimeType = TimeType.AM;
                var h = value.Hours;
                if (h >= 12)
                {
                    h = value.Hours - 12;
                    TimeType = TimeType.PM;
                }
                if (h == 0)
                {
                    h = 12;
                }

                Hour = h;
                Minute = value.Minutes;
                Second = value.Seconds;
            }
        }

        #region IDataErrorInfo implementation
        /// <summary>
        /// Add empty implementation of Error method of <seealso cref="IDataErrorInfo"/> interface.
        /// </summary>
        public string Error
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Implement <seealso cref="IDataErrorInfo"/> interface to perform binding data validation.
        /// </summary>
        /// <param name="columnName">The binding variable name</param>
        /// <returns>
        /// null if there validation passes.
        /// Any non empty string if it fails validation.
        /// </returns>
        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(Hour))
                {
                    if (Hour >= 1 && Hour <= 12) return null;
                }
                if (columnName == nameof(Minute))
                {
                    if (Minute >= 0 && Minute <= 60) return null;
                }
                if (columnName == nameof(Second))
                {
                    if (Second >= 0 && Second <= 60) return null;
                }

                return "Invalid input";
            }
        }
        #endregion

        /// <summary>
        /// Sets the value in the given reference and raises the property changed event for the property.
        /// If the value is not within the range of [0, upperLimit], the value is not set.
        /// </summary>
        /// <param name="storage">The field that the value is set to.</param>
        /// <param name="value">The new value.</param>
        /// <param name="upperLimit">The valid value upper bound.</param>
        /// <param name="propertyName">The name of the property that is changing.</param>
        private void SetTimePartBoxValue(
            ref int storage, int value, int upperLimit, [CallerMemberName] string propertyName = "")
        {
            if (value < 0 || value > upperLimit)
            {
                return;
            }

            SetValueAndRaise(ref storage, value, propertyName);
        }
    }
}
