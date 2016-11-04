using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.Controls
{
    public class IconButton : Button
    {
        public static readonly DependencyProperty NormalIconProperty =
            DependencyProperty.Register(
                nameof(NormalIcon),
                typeof(ImageSource),
                typeof(IconButton));

        public static readonly DependencyProperty MouseOverIconProperty =
            DependencyProperty.Register(
                nameof(MouseOverIcon),
                typeof(ImageSource),
                typeof(IconButton));

        public ImageSource NormalIcon
        {
            get { return (ImageSource)GetValue(NormalIconProperty); }
            set { SetValue(NormalIconProperty, value); }
        }

        public ImageSource MouseOverIcon
        {
            get { return (ImageSource)GetValue(MouseOverIconProperty); }
            set { SetValue(MouseOverIconProperty, value); }
        }
    }
}
