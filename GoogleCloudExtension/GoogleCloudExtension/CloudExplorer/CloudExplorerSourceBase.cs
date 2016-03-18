// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.CloudExplorer
{
    public abstract class CloudExplorerSourceBase<TRootViewModel> : ICloudExplorerSource where TRootViewModel: SourceRootViewModelBase, new()
    {
        private readonly TRootViewModel _root;
        private readonly IList<ButtonDefinition> _buttons = new List<ButtonDefinition>();

        public TreeHierarchy Root => _root;

        public IEnumerable<ButtonDefinition> Buttons => _buttons;

        protected TRootViewModel ActualRoot => _root;

        protected IList<ButtonDefinition> ActualButtons => _buttons;

        public CloudExplorerSourceBase()
        {
            _root = new TRootViewModel();
            _root.Initialize();
        }

        public void Refresh()
        {
            _root.Refresh();
        }
    }
}
