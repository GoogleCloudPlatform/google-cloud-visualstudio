using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// Interaction logic for ImageToggleButton.xaml
    /// </summary>
    public partial class ImageToggleButton : ToggleButton
    {
        public ImageToggleButton()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty CheckedImageProperty =
            DependencyProperty.Register(
                nameof(CheckedImage),
                typeof(ImageSource),
                typeof(ImageToggleButton));

        public static readonly DependencyProperty UncheckedImageProperty =
            DependencyProperty.Register(
                nameof(UncheckedImage),
                typeof(ImageSource),
                typeof(ImageToggleButton));

        public static readonly DependencyProperty MouseOverBackgroundProperty =
            DependencyProperty.Register(
                nameof(MouseOverBackground),
                typeof(Brush),
                typeof(ImageToggleButton),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(224, 224, 224))));

        public static readonly DependencyProperty MouseOverForegroudProperty =
            DependencyProperty.Register(
                nameof(MouseOverForeground),
                typeof(Brush),
                typeof(ImageToggleButton),
                new PropertyMetadata(Brushes.Blue));

        /// <summary>
        /// The image to show in the checked state.
        /// </summary>
        public ImageSource CheckedImage
        {
            get { return (ImageSource)GetValue(CheckedImageProperty); }
            set { SetValue(CheckedImageProperty, value); }
        }

        /// <summary>
        /// The image to show in the unchecked state.
        /// </summary>  
        public ImageSource UncheckedImage
        {
            get { return (ImageSource)GetValue(UncheckedImageProperty); }
            set { SetValue(UncheckedImageProperty, value); }
        }

        /// <summary>
        /// The brush of background in the mouse over state.
        /// </summary>
        public Brush MouseOverBackground
        {
            get { return (Brush)GetValue(MouseOverBackgroundProperty); }
            set { SetValue(MouseOverBackgroundProperty, value); }
        }

        /// <summary>
        /// The brush of foreground in the mouse over state.
        /// </summary>
        public Brush MouseOverForeground
        {
            get { return (Brush)GetValue(MouseOverForegroudProperty); }
            set { SetValue(MouseOverForegroudProperty, value); }
        }
    }
}
