using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// <summary>
        /// The children for this item.
        /// </summary>
        public ObservableCollection<TreeNode> Children { get; }

        /// <summary>
        /// Initialize the item from an <c>IEnumerable</c> source.
        /// </summary>
        /// <param name="children">The children of the item.</param>
        public TreeHierarchy(IEnumerable<TreeNode> children)
        {
            Children = new ObservableCollection<TreeNode>(children);
        }
    }
}
