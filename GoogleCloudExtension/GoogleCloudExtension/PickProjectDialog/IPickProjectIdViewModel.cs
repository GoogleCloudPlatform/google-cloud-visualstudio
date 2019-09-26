using System;
using System.Collections.Generic;
using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;

namespace GoogleCloudExtension.PickProjectDialog
{
    /// <summary>
    /// Interface of the view model used by the <see cref="PickProjectIdWindowContent"/>
    /// </summary>
    public interface IPickProjectIdViewModel : IViewModelBase<Project>
    {
        /// <summary>
        /// Command to open the manage users dialog.
        /// </summary>
        ProtectedCommand ChangeUserCommand { get; }

        /// <summary>
        /// Command to confirm the selection of a project id.
        /// </summary>
        ProtectedCommand OkCommand { get; }

        /// <summary>
        /// Command to execute when refreshing the list of projects.
        /// </summary>
        ProtectedCommand RefreshCommand { get; }

        /// <summary>
        /// The list of projects available to the current user.
        /// </summary>
        IEnumerable<Project> Projects { get; }

        /// <summary>
        /// The project selected from the list of current projects.
        /// </summary>
        Project SelectedProject { get; set; }

        /// <summary>
        /// The property that surfaces task completion information for the Load Projects task.
        /// </summary>
        AsyncProperty LoadTask { get; set; }

        bool HasAccount { get; }
        string Filter { get; set; }
        bool AllowAccountChange { get; }
        string HelpText { get; }
        Predicate<object> ItemFilter { get; }

        bool FilterItem(object item);
    }
}