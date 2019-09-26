// Copyright 2018 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.Theming
{
    public class CommonDialogWindow<T> : CommonDialogWindowBase where T : ICloseSource
    {
        private readonly ICommonWindowContent<T> _commonWindowContent;

        /// <summary>
        /// The view model of the content.
        /// </summary>
        public T ViewModel => _commonWindowContent.ViewModel;

        public CommonDialogWindow(ICommonWindowContent<T> commonWindowContent) : base(commonWindowContent.Title)
        {
            Content = _commonWindowContent = commonWindowContent;
            _commonWindowContent.ViewModel.Close += Close;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _commonWindowContent.ViewModel.Close -= Close;
        }
    }
}
