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
using Microsoft.TeamFoundation.Controls;
using System;
using System.ComponentModel.Composition;
using static System.Diagnostics.Debug;

namespace GoogleCloudExtension.Team
{
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

        [ImportingConstructor]
        public Section(ISectionView sectionView)
        {
            SectionContent = sectionView.ThrowIfNull(nameof(sectionView));
            _viewModel = sectionView.ViewModel.ThrowIfNull(nameof(sectionView.ViewModel));
        }

        #region implement interface ITeamExplorerSection

        public void Initialize(object sender, SectionInitializeEventArgs e)
        {
            WriteLine($"CsrTeamExplorerSection.Initialize");
            _serviceProvider = e.ServiceProvider;
            _teamExploerServices = new TeamExplorerUtils(_serviceProvider);
            _viewModel.Initialize(_teamExploerServices);
        }

        public void Cancel()
        {
            WriteLine($"CsrTeamExplorerSection.Cancel");
        }

        public object GetExtensibilityService(Type serviceType)
        {
            WriteLine($"CsrTeamExplorerSection.GetExtensibilityService");
            return null;
        }

        public bool IsBusy
        {
            get
            {
                WriteLine($"CsrTeamExplorerSection.IsBusy get");
                return _isBusy;
            }
            private set
            {
                WriteLine($"CsrTeamExplorerSection.IsBusy set");
                SetValueAndRaise(out _isBusy, value);
            }
        }

        public bool IsExpanded
        {
            get
            {
                WriteLine($"CsrTeamExplorerSection.IsExpanded get");
                return _isExpanded;
            }
            set
            {
                WriteLine($"CsrTeamExplorerSection.IsExpanded set");
                SetValueAndRaise(out _isExpanded, value);
            }
        }

        public bool IsVisible
        {
            get
            {
                WriteLine($"CsrTeamExplorerSection.IsVisible get");
                return _isVisible;
            }
            set
            {
                WriteLine($"CsrTeamExplorerSection.IsVisible set");
                SetValueAndRaise(out _isVisible, value);
            }
        }

        public void Loaded(object sender, SectionLoadedEventArgs e)
        {
            WriteLine($"CsrTeamExplorerSection.Loaded {sender.GetType().Name} {sender.ToString()}, {e}");
        }

        public void Refresh()
        {
            WriteLine($"CsrTeamExplorerSection.Refresh");
            _viewModel.Refresh();
        }

        public void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
            WriteLine($"CsrTeamExplorerSection.SaveContext {sender.GetType().Name} {sender.ToString()}, {e}");
        }

        public object SectionContent
        {
            get
            {
                // When this get_SectionContent is called, Team Explorer is trying to refresh the section content.
                // This is the chance to update the section view with new active repo.
                WriteLine($"CsrTeamExplorerSection.SectionContent");
                string newActive = _teamExploerServices?.GetActiveRepository();
                if (String.Compare(newActive, _activeRepo, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    _viewModel.UpdateActiveRepo(newActive);
                    _activeRepo = newActive;
                }
                return _sectionContent;
            }
            private set { SetValueAndRaise(out _sectionContent, value); }
        }

        public string Title
        {
            get
            {
                return "Google Cloud Source Repositories";
            }
        }

        public void Dispose()
        { }

        #endregion
    }
}