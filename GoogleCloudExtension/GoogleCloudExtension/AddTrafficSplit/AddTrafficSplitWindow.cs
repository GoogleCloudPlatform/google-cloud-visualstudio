﻿using GoogleCloudExtension.Theming;
using System.Collections.Generic;

namespace GoogleCloudExtension.AddTrafficSplit
{
    public class AddTrafficSplitWindow : CommonDialogWindowBase
    {
        public AddTrafficSplitViewModel ViewModel { get; }

        private AddTrafficSplitWindow(IEnumerable<string> versions) : base("Add Traffic Split")
        {
            ViewModel = new AddTrafficSplitViewModel(this, versions);
            Content = new AddTrafficSplitWindowContent
            {
                DataContext = ViewModel
            };
        }

        public static AddTrafficSplitResult PromptUser(IEnumerable<string> versions)
        {
            var dialog = new AddTrafficSplitWindow(versions);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
