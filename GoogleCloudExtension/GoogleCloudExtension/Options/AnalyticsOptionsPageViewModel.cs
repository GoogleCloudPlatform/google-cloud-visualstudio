using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.Options
{
    public class AnalyticsOptionsPageViewModel : ViewModelBase
    {
        private bool _optIn;

        public bool OptIn
        {
            get { return _optIn; }
            set { SetValueAndRaise(ref _optIn, value); }
        }
    }
}