using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Diagnostics;
using System.Collections.ObjectModel;


namespace GoogleCloudExtension.StackdriverLogsViewer
{
    //public class MyMenu : Menu
    //{
    //    public MyMenu()
    //    {
    //        // Should get the default style & template since styles are not inherited
    //        // Style = FindResource(typeof(Menu)) as Style;
    //    }

    //    protected override DependencyObject GetContainerForItemOverride()
    //    {
    //        var container = new SearchMenuItem();
    //        return container;
    //    }
    //}

    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:MenuSelectionWithSearch"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:MenuSelectionWithSearch;assembly=MenuSelectionWithSearch"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:SearchMenuItem/>
    ///
    /// </summary>

    [TemplatePart(Name = "PART_searchTextBox", Type = typeof(TextBox))]
    public class SearchMenuItem : MenuItem
    {
        static SearchMenuItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchMenuItem), new FrameworkPropertyMetadata(typeof(SearchMenuItem)));
        }

        public SearchMenuItem()
        {
            // Style = FindResource(typeof(MenuItem)) as Style;            
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            var container = new SearchMenuItem();
            return container;
        }

        public static DependencyProperty OnSubmenuOpenCommandProperty
            = DependencyProperty.Register(
                "OnSubmenuOpenCommand",
                typeof(ICommand),
                typeof(SearchMenuItem));

        public ICommand OnSubmenuOpenCommand
        {
            get
            {
                return (ICommand)GetValue(OnSubmenuOpenCommandProperty);
            }

            set
            {
                SetValue(OnSubmenuOpenCommandProperty, value);
            }
        }

        public static DependencyProperty IsSubmenuPopulatedProperty
            = DependencyProperty.Register(
                "IsSubmenuPopulated",
                typeof(bool),
                typeof(SearchMenuItem),
                new FrameworkPropertyMetadata()
                {
                    DefaultValue = false,
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

        public bool IsSubmenuPopulated
        {
            get
            {
                return (bool)GetValue(IsSubmenuPopulatedProperty);
            }

            set
            {
                Debug.WriteLine($"{Header} SetValue(IsSubmenuPopulatedProperty, {value}");
                SetValue(IsSubmenuPopulatedProperty, value);
            }
        }
        private TextBox _searchBox;

        /// Create bindings and event handlers on named items.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _searchBox = Template.FindName("PART_searchTextBox", this) as TextBox;
            if (_searchBox != null)
            {
                _searchBox.TextChanged += _searchBox_TextChanged;
            }

            this.SubmenuOpened += SearchMenuItem_SubmenuOpened;
        }

        private void SearchMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"{Header} submenu opened");
            // OnInit = true;
            Debug.WriteLine($"{Header} adding sub menu");
            if (OnSubmenuOpenCommand != null && OnSubmenuOpenCommand.CanExecute(null) && !IsSubmenuPopulated)
            {
                OnSubmenuOpenCommand.Execute(null);
            }
        }

        private void _searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Debug.WriteLine("_searchBox_TextChanged");
            if (sender != _searchBox)
            {
                Debug.WriteLine("Expect sender is _searchBox");
                return;
            }

            var prefix = _searchBox.Text.Trim();

            for (int i = 0; i < Items.Count; i++)
            {
                MenuItem menuItem =
                    ItemContainerGenerator.ContainerFromIndex(i) as MenuItem;
                string label = menuItem?.Header?.ToString();
                if (label == null)
                {
                    continue;
                }

                menuItem.Visibility = prefix == "" || label.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase) ?
                    Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
