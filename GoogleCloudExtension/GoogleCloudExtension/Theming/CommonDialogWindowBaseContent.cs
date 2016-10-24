using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.Theming
{
    public class CommonDialogWindowBaseContent : ContentControl
    {
        // Dependency property registration for the buttons property, to allow template binding to work.
        public static readonly DependencyProperty ButtonsProperty =
            DependencyProperty.Register(
                nameof(Buttons),
                typeof(ObservableCollection<DialogButtonInfo>),
                typeof(CommonDialogWindowBaseContent));

        /// <summary>
        /// The list of buttons to show in the dialog.
        /// </summary>
        public ObservableCollection<DialogButtonInfo> Buttons
        {
            get { return (ObservableCollection<DialogButtonInfo>)GetValue(ButtonsProperty); }
            set { SetValue(ButtonsProperty, value); }
        }

        public CommonDialogWindowBaseContent()
        {
            Initialize();
            Buttons = new ObservableCollection<DialogButtonInfo>();
        }

        private void Initialize()
        {
            ResourceDictionary resources = new ResourceDictionary();
            resources.Source = ResourceUtils.GetResourceUri("Theming/ThemingResources.xaml");
            this.Resources = resources;
        }
    }
}
