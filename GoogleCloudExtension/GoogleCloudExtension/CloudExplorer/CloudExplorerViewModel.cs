// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// This class contains the view specific logic for the AppEngineAppsToolWindow view.
    /// </summary>
    internal class CloudExplorerViewModel : ViewModelBase
    {
        private const string RefreshImagePath = "CloudExplorer/Resources/refresh.png";
        private static readonly Lazy<ImageSource> s_refreshIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(RefreshImagePath));

        private readonly IList<ICloudExplorerSource> _sources;
        private readonly List<ButtonDefinition> _buttons;

        /// <summary>
        /// The list of module and version combinations for the current project.
        /// </summary>
        public IEnumerable<TreeHierarchy> Roots
        {
            get
            {
                foreach (var source in _sources)
                {
                    yield return source.GetRoot();
                }
            }
        }

        public IList<ButtonDefinition> Buttons => _buttons;

        public CloudExplorerViewModel(IEnumerable<ICloudExplorerSource> sources)
        {
            _sources = new List<ICloudExplorerSource>(sources);
            _buttons = new List<ButtonDefinition>()
            {
                new ButtonDefinition
                {
                    Icon = s_refreshIcon.Value,
                    ToolTip = "Refresh",
                    Command = new WeakCommand(this.OnRefresh),
                }
            };

            foreach (var source in _sources)
            {
                var sourceButtons = source.GetButtons();
                _buttons.AddRange(sourceButtons);
            }
        }

        private void OnRefresh()
        {
            foreach (var source in _sources)
            {
                source.Refresh();
            }
            RaisePropertyChanged(nameof(Roots));
        }
    }
}
