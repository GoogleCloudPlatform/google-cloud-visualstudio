using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.Theming
{
    public class ButtonsList : ItemsControl
    {
        // Ensure that all items get the ItemTemplate applied to them.
        protected override bool IsItemItsOwnContainerOverride(object item) => false;
    }
}
