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
using Microsoft.TeamFoundation.Controls;
using System;
using System.Diagnostics;
using System.ComponentModel.Composition;

namespace GoogleCloudExtension.TeamExplorerExtension
{
    /// <summary>
    /// Expose a <seealso cref="ITeamExplorerSection"/> for Team Explorer Connect tab.
    /// MEF will create instances of this class.
    /// </summary>
    [TeamExplorerSection(Guid, TeamExplorerPageIds.Connect, 5)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Section : Model, ITeamExplorerSection
    {
        private const string Guid = "2625FA1D-22DD-440E-87C0-1EB42DB7C5A4";

        private ISectionViewModel _viewModel;
        private IServiceProvider _serviceProvider;
        private TeamExplorerUtils _teamExploerServices;
        private bool _isBusy;
        private bool _isExpanded = true;
        private bool _isVisible = true;
        private object _sectionContent;
        private string _activeRepo;

        #region implement interface ITeamExplorerSection

        public object SectionContent
        {
            get
            {
                // When this get_SectionContent is called, Team Explorer is trying to refresh the section content.
                // This is the chance to update the section view with new active repo.
                Debug.WriteLine($"CsrTeamExplorerSection.SectionContent");
                string newActive = _teamExploerServices?.GetActiveRepository();
                if (String.Compare(newActive, _activeRepo, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    _viewModel.UpdateActiveRepo(newActive);
                    _activeRepo = newActive;
                }
                return _sectionContent;
            }
            private set { SetValueAndRaise(ref _sectionContent, value); }
        }

        public string Title { get; }

        public bool IsBusy
        {
            get
            {
                Debug.WriteLine($"CsrTeamExplorerSection.IsBusy get");
                return _isBusy;
            }
            private set
            {
                Debug.WriteLine($"CsrTeamExplorerSection.IsBusy set");
                SetValueAndRaise(ref _isBusy, value);
            }
        }

        public bool IsExpanded
        {
            get
            {
                Debug.WriteLine($"CsrTeamExplorerSection.IsExpanded get");
                return _isExpanded;
            }
            set
            {
                Debug.WriteLine($"CsrTeamExplorerSection.IsExpanded set");
                SetValueAndRaise(ref _isExpanded, value);
            }
        }

        public bool IsVisible
        {
            get
            {
                Debug.WriteLine($"CsrTeamExplorerSection.IsVisible get");
                return _isVisible;
            }
            set
            {
                Debug.WriteLine($"CsrTeamExplorerSection.IsVisible set");
                SetValueAndRaise(ref _isVisible, value);
            }
        }

        #endregion

        /// <summary>
        /// <seealso cref="ImportingConstructorAttribute"/> tells the MEF framework to 
        /// use this as default constructor.
        /// </summary>
        /// <param name="sectionView">
        /// An <seealso cref="ISectionView"/> interface.
        /// This asks MEF to create an object of ISectionView type and  use it to 
        /// create an instance of Section class.
        /// </param>
        [ImportingConstructor]
        public Section(ISectionView sectionView)
        {
            SectionContent = sectionView.ThrowIfNull(nameof(sectionView));
            _viewModel = sectionView.ViewModel.ThrowIfNull(nameof(sectionView.ViewModel));
            Title = sectionView.Title;
        }

        #region implement interface ITeamExplorerSection

        public void Loaded(object sender, SectionLoadedEventArgs e)
        {
            Debug.WriteLine($"CsrTeamExplorerSection.Loaded {sender.GetType().Name} {sender.ToString()}, {e}");
        }

        public void Initialize(object sender, SectionInitializeEventArgs e)
        {
            Debug.WriteLine($"CsrTeamExplorerSection.Initialize");
            _serviceProvider = e.ServiceProvider;
            _teamExploerServices = new TeamExplorerUtils(_serviceProvider);
            _viewModel.Initialize(_teamExploerServices);
        }

        public void Cancel()
        {
            Debug.WriteLine($"CsrTeamExplorerSection.Cancel");
        }

        public object GetExtensibilityService(Type serviceType)
        {
            Debug.WriteLine($"CsrTeamExplorerSection.GetExtensibilityService");
            return null;
        }

        public void Refresh()
        {
            Debug.WriteLine($"CsrTeamExplorerSection.Refresh");
            _viewModel.Refresh();
        }

        public void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
            Debug.WriteLine($"CsrTeamExplorerSection.SaveContext {sender.GetType().Name} {sender.ToString()}, {e}");
        }

        public void Dispose()
        { }

        #endregion
    }
}