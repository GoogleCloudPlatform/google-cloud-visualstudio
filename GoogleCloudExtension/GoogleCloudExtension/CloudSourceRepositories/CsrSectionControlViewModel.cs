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

using GoogleCloudExtension.TeamExplorerExtension;
using GoogleCloudExtension.Utils;
using System.Diagnostics;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// View model to <seealso cref="CsrSectionControl"/>.
    /// </summary>
    [Export(typeof(ISectionViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CsrSectionControlViewModel : ViewModelBase, ISectionViewModel
    {
        private ContentControl _content;

        /// <summary>
        /// The content for the section control.
        /// </summary>
        public ContentControl Content
        {
            get { return _content; }
            private set { SetValueAndRaise(out _content, value); }
        }

        public CsrSectionControlViewModel()
        { }

        #region implement interface ISectionViewModel

        void ISectionViewModel.Refresh()
        {
            Debug.WriteLine("CsrSectionControlViewModel.Refresh");
        }

        void ISectionViewModel.Initialize(ITeamExplorerUtils teamExplorerService)
        {
            Debug.WriteLine("CsrSectionControlViewModel.Initialize");
        }

        void ISectionViewModel.UpdateActiveRepo(string newRepoLocalPath)
        {
            Debug.WriteLine($"CsrSectionControlViewModel.UpdateActiveRepo {newRepoLocalPath}");
        }

        #endregion
    }
}
