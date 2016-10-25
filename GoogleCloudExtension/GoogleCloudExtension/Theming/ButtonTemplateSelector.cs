using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.Theming
{
    public class ButtonTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var buttonInfo = (DialogButtonInfo)item;
            var element = (FrameworkElement)container;

            if (buttonInfo.Caption.Length > 6)
            {
                return (DataTemplate)element.FindResource("DialogButtonTemplateWide");
            }
            else
            {
                return (DataTemplate)element.FindResource("DialogButtonTemplateStandard");
            }
        }
    }
}
