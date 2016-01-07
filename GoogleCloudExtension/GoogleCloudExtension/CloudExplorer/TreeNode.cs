// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    public class TreeNode : Model
    {
        private ImageSource _icon;
        private object _content;
        private ContextMenu _contextMenu;

        /// <summary>
        /// The icon to use in the UI for this item.
        /// </summary>
        public ImageSource Icon
        {
            get { return _icon; }
            set { SetValueAndRaise(ref _icon, value); }
        }

        /// <summary>
        /// The content to display for this item.
        /// </summary>
        public object Content
        {
            get { return _content; }
            set { SetValueAndRaise(ref _content, value); }
        }

        /// <summary>
        /// The context menu for this item.
        /// </summary>
        public ContextMenu ContextMenu
        {
            get { return _contextMenu; }
            set { SetValueAndRaise(ref _contextMenu, value); }
        }
    }

    public class TreeLeaf : TreeNode
    { }

    public class TreeHierarchy : TreeNode
    {
        private bool _isExpanded;

        /// <summary>
        /// The children for this item.
        /// </summary>
        public ObservableCollection<TreeNode> Children { get; } = new ObservableCollection<TreeNode>();

        /// <summary>
        /// Returns whether the hierarchy is expanded or not.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    SetValueAndRaise(ref _isExpanded, value);
                    OnIsExpandedChanged(value);
                }
            }
        }

        /// <summary>
        /// Initialize the item from an <c>IEnumerable</c> source.
        /// </summary>
        /// <param name="children">The children of the item.</param>
        public TreeHierarchy(IEnumerable<TreeNode> children)
        {
            foreach (var child in children)
            {
                Children.Add(child);
            }
        }

        public TreeHierarchy()
        { }

        /// <summary>
        /// This method will be called every time the value of IsExpanded property changes.
        /// </summary>
        /// <param name="newValue">The new value of the property.</param>
        protected virtual void OnIsExpandedChanged(bool newValue)
        { }
    }
}
