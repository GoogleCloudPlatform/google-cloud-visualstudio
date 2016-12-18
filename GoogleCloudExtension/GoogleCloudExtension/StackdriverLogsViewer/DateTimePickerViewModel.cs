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

using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Diagnostics;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// View model for stackdriver log viewer date time picker. <seealso cref="DateTimePicker"/>
    /// </summary>
    public class DateTimePickerViewModel : ViewModelBase
    {
        /// <summary>
        /// Remembers the current active date time.
        /// Go button reset it. Cancel button leave the value not changed.
        /// </summary>
        public DateTime _goodStateDateTime;

        /// <summary>
        /// Remembers the current active list log entry time order.
        /// true: descending order,  false: ascending order.
        /// Go button reset it. Cancel button leave the value not changed.
        /// </summary>
        private bool _goodStateIsDescendingOrder;

        /// <summary>
        /// The current selected time zone. 
        /// </summary>
        private TimeZoneInfo _timeZone;

        private DateTime _uiElementDate;
        private TimeSpan _uiElementtime;
        private int _uiSelectedOrder = 0;
        private bool _isDropDownOpen = false;

        /// <summary>
        /// The command to execute to accept the changes.
        /// </summary>
        public ProtectedCommand GoCommand { get; }

        /// <summary>
        /// Gets or sets the UI control date box value.
        /// </summary>
        public DateTime UiElementDate
        {
            get
            {
                // time zone ?
                if (_uiElementDate > Now)
                {
                    _uiElementDate = Now.Date;
                }

                return _uiElementDate;
            }

            set
            {
                // ?? better way
                var timeString = value.Date.ToString("s");
                _uiElementDate = DateTime.Parse(timeString);
                RaisePropertyChanged(nameof(UiElementDate));
            }
        }

        /// <summary>
        /// Gets or sets the UI control time box value.
        /// </summary>
        public TimeSpan UiElementTime
        {
            get { return _uiElementtime; }
            set { SetValueAndRaise(ref _uiElementtime, value); }
        }

        /// <summary>   
        /// False:  Ascending order 
        /// </summary>
        public bool IsDecendingOrder
        {
            get { return _goodStateIsDescendingOrder; }
            set
            {
                _goodStateIsDescendingOrder = value;
                SelectedDescendingOrder = value;
            }
        }

        /// <summary>
        /// Gets or sets the selected order index of the combo box.
        /// </summary>
        public int SelectedOrderIndex
        {
            get { return _uiSelectedOrder; }
            set { SetValueAndRaise(ref _uiSelectedOrder, value); }
        }

        public DateTime DateTimeUtc
        {
            get { return TimeZoneInfo.ConvertTimeToUtc(_goodStateDateTime); }

            set
            {
                DateTime dt = TimeZoneInfo.ConvertTimeFromUtc(value, _timeZone);
                _goodStateDateTime = dt;
                _uiElementDate = dt.Date;
                _uiElementtime = dt.TimeOfDay;

                RaisePropertyChanged(nameof(UiElementDate));
                RaisePropertyChanged(nameof(UiElementTime));
            }
        }

        /// <summary>
        /// The event handler that notifies the date time or the time order is changed.
        /// </summary>
        public event EventHandler DateTimeFilterChange;

        /// <summary>
        /// Gets or sets if the combo box drop down is open.
        /// </summary>
        public bool IsDropDownOpen
        {
            get
            {
                Debug.WriteLine($"Get IsDropDownOpen {_isDropDownOpen}");
                return _isDropDownOpen;
            }
            set
            {
                Debug.WriteLine($"Set IsDropDownOpen {value}");
                if (value)  // Now it is open
                {
                    UiElementDate = _goodStateDateTime.Date;
                    UiElementTime = _goodStateDateTime.TimeOfDay;
                    SelectedDescendingOrder = _goodStateIsDescendingOrder;
                }

                SetValueAndRaise(ref _isDropDownOpen, value);
            }
        }

        /// <summary>
        /// Create a instance of <seealso cref="DateTimePickerViewModel"/> class.
        /// </summary>
        /// <param name="timeZone">Current selected time zone.</param>
        public DateTimePickerViewModel(TimeZoneInfo timeZone)
        {
            _timeZone = timeZone;
            GoCommand = new ProtectedCommand(OnGoButtonCommand);
        }

        /// <summary>
        /// Change the control time zone.
        /// </summary>
        /// <param name="timeZone">The time zone to be changed to</param>
        public void ChangeTimeZone(TimeZoneInfo timeZone)
        {
            _timeZone = timeZone;
            _goodStateDateTime = TimeZoneInfo.ConvertTime(_goodStateDateTime, timeZone);
        }

        /// <summary>
        /// Go button is clicked.
        /// </summary>
        private void OnGoButtonCommand()
        {
            DateTime newDateTime = _uiElementDate.Date.Add(_uiElementtime);
            if (_goodStateDateTime != newDateTime || _goodStateIsDescendingOrder != SelectedDescendingOrder)
            {
                Debug.WriteLine("OnChangedDateTime Invoke DateTimeFilterChange , changed");
                _goodStateDateTime = newDateTime;
                _goodStateIsDescendingOrder = SelectedDescendingOrder;
                DateTimeFilterChange?.Invoke(this, new EventArgs());
            }
            else
            {
                Debug.WriteLine("OnChangedDateTime, No Change");
            }

            IsDropDownOpen = false;
        }

        private bool SelectedDescendingOrder
        {
            get { return _uiSelectedOrder == 0; }
            set { SelectedOrderIndex = value ? 0 : 1; }
        }

        private DateTime Now
        {
            get { return TimeZoneInfo.ConvertTime(DateTime.UtcNow, _timeZone); }
        }

 
    }
}
