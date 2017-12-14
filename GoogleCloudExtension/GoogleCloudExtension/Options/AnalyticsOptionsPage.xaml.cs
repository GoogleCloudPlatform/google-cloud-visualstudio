using System.Windows.Controls;

namespace GoogleCloudExtension.Options
{
    /// <summary>
    /// Interaction logic for AnalyticsOptionsPage.xaml
    /// </summary>
    public partial class AnalyticsOptionsPage : UserControl
    {
        internal AnalyticsOptionsPageViewModel ViewModel { get; }

        internal AnalyticsOptionsPage()
        {
            ViewModel = new AnalyticsOptionsPageViewModel();
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
