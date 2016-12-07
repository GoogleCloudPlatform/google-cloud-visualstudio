using System.Windows.Controls;

namespace GoogleCloudExtension.NamePrompt
{
    /// <summary>
    /// Interaction logic for NamePromptContent.xaml
    /// </summary>
    public partial class NamePromptContent : UserControl
    {
        public NamePromptContent()
        {
            InitializeComponent();

            _nameBox.Focus();
        }
    }
}
