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
using System.Diagnostics;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// View model for stackdriver log viewer <seealso cref="DateTimePicker"/>
    /// </summary>
    public class DateTimePickerViewModel : ViewModelBase
    {
        /// <summary>
        /// The current selected time zone. 
        /// </summary>
        private TimeZoneInfo _timeZone;

        private DateTime _uiElementDate;
        private int _uiSelectedOrderIndex;
        private bool _isDropDownOpen = false;

        /// <summary>   
        /// Gets or sets if using descending order by timestamp. 
        /// true: descending order,  false: ascending order.
        /// Go button reset it. Cancel button leave the value not changed.
        /// </summary>
        public bool IsDescendingOrder { get; set; }

        /// <summary>
        /// Gets or sets the datetime in UTC.  
        /// The current active date time.
        /// Go button reset it. Cancel button leave the value not changed.        
        /// </summary>
        public DateTime DateTimeUtc { get; set; }

        /// <summary>
        /// The command to execute to accept the changes.
        /// </summary>
        public ProtectedCommand GoCommand { get; }

        /// <summary>
        /// Gets the time box view model.
        /// </summary>
        public TimeBoxViewModel TimeBoxModel { get; }

        /// <summary>
        /// Gets or sets the UI control date box value of <seealso cref="_timeZone"/>
        /// </summary>
        public DateTime UiElementDate
        {
            get { return _uiElementDate; }
            set
            {
                _uiElementDate = value > Now ? Now : value;
                RaisePropertyChanged(nameof(UiElementDate));
            }
        }

        /// <summary>
        /// Gets or sets the selected order index of the combo box.
        /// </summary>
        public int SelectedOrderIndex
        {
            get { return _uiSelectedOrderIndex; }
            set { SetValueAndRaise(ref _uiSelectedOrderIndex, value); }
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
                    DateTime dt = TimeZoneInfo.ConvertTimeFromUtc(DateTimeUtc, _timeZone);
                    UiElementDate = dt.Date;
                    TimeBoxModel.Time = dt.TimeOfDay;
                    SelectedOrderIndex = IsDescendingOrder ? 0 : 1;
                }

                SetValueAndRaise(ref _isDropDownOpen, value);
            }
        }

        /// <summary>
        /// Create a instance of <seealso cref="DateTimePickerViewModel"/> class.
        /// </summary>
        /// <param name="timeZone">Current selected time zone.</param>
        /// <param name="dateTimeUtc">Initial UTC date time.</param>
        /// <param name="isDescendingOrder">Initial time stamp sort order.</param>
        public DateTimePickerViewModel(TimeZoneInfo timeZone, DateTime dateTimeUtc, bool isDescendingOrder)
        {
            _timeZone = timeZone;
            DateTimeUtc = dateTimeUtc;
            IsDescendingOrder = isDescendingOrder;
            GoCommand = new ProtectedCommand(OnGoButtonCommand);
            TimeBoxModel = new TimeBoxViewModel();
        }

        /// <summary>
        /// Change the control time zone.
        /// </summary>
        /// <param name="timeZone">The time zone to be changed to.</param>
        public void ChangeTimeZone(TimeZoneInfo timeZone)
        {
            _timeZone = timeZone;
        }

        /// <summary>
        /// Go button command handler.
        /// </summary>
        private void OnGoButtonCommand()
        {
            DateTime newDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(_uiElementDate.Date.Add(TimeBoxModel.Time));
            bool newOrder = _uiSelectedOrderIndex == 0;
            if (DateTimeUtc != newDateTimeUtc || newOrder != IsDescendingOrder)
            {
                Debug.WriteLine("OnChangedDateTime Invoke DateTimeFilterChange , changed");
                DateTimeUtc = newDateTimeUtc;
                IsDescendingOrder = newOrder;
                DateTimeFilterChange?.Invoke(this, new EventArgs());
            }
            else
            {
                Debug.WriteLine("OnChangedDateTime, No Change");
            }

            IsDropDownOpen = false;
        }

        /// <summary>
        /// The current time of the  <seealso cref="_timeZone"/>.
        /// </summary>
        private DateTime Now
        {
            get { return TimeZoneInfo.ConvertTime(DateTime.UtcNow, _timeZone); }
        } 
    }
}
