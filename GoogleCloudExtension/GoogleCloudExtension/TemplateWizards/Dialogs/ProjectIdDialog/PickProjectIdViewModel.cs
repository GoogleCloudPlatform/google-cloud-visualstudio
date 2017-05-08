using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.ProjectIdDialog
{
    public class PickProjectIdViewModel : ViewModelBase
    {
        public string Result { get; private set; }

        public ProtectedCommand ChangeUserCommand { get; }

        public ProtectedCommand SelectProjectCommand { get; }

        public ProtectedCommand SkipProjectInputCommand { get; }

        public IList<Project> Projects
        {
            get { return _projects; }
            set { SetValueAndRaise(ref _projects, value); }
        }

        public Project SelectedProject
        {
            get { return _selectedProject; }
            set
            {
                SetValueAndRaise(ref _selectedProject, value);
                if (SelectedProject != null)
                {
                    ProjectId = SelectedProject.ProjectId;
                }
            }
        }

        public string ProjectId
        {
            get { return _projectId; }
            set
            {
                SetValueAndRaise(ref _projectId, value);
                SkipProjectInputCommand.CanExecuteCommand = string.IsNullOrEmpty(ProjectId);
                SelectProjectCommand.CanExecuteCommand = !string.IsNullOrEmpty(ProjectId);
            }
        }

        public NotifyTaskCompletion LoadTask
        {
            get { return _loadTask; }
            set { SetValueAndRaise(ref _loadTask, value); }
        }

        /// Property backing field.
        private IList<Project> _projects;
        private Project _selectedProject;
        private string _projectId;
        private NotifyTaskCompletion _loadTask;

        private readonly IPickProjectIdWindow _owner;
        private readonly Func<IResourceManagerDataSource> _resourceManagerDataSourceFactory;
        private readonly Action _promptAccountManagement;

        public PickProjectIdViewModel(IPickProjectIdWindow owner)
            : this(owner, DataSourceUtils.CreateResourceManagerDataSource, ManageAccountsWindow.PromptUser) { }

        /// <summary>
        /// For Testing.
        /// </summary>
        /// <param name="owner">The window that owns this ViewModel.</param>
        /// <param name="dataSourceFactory">The factory of the source of projects.</param>
        /// <param name="promptAccountManagement">Action to prompt managing accounts.</param>
        internal PickProjectIdViewModel(
            IPickProjectIdWindow owner, Func<IResourceManagerDataSource> dataSourceFactory, Action promptAccountManagement)
        {
            _owner = owner;
            _resourceManagerDataSourceFactory = dataSourceFactory;
            _promptAccountManagement = promptAccountManagement;
            ChangeUserCommand = new ProtectedCommand(OnChangeUser);
            SelectProjectCommand = new ProtectedCommand(OnSelectProject, false);
            SkipProjectInputCommand = new ProtectedCommand(OnSkip);
            LoadTask = new NotifyTaskCompletion(LoadProjectsAsync());
        }

        private async Task LoadProjectsAsync()
        {
            Projects = await _resourceManagerDataSourceFactory().GetProjectsListAsync();
            if (string.IsNullOrEmpty(ProjectId) || ProjectId.Equals(SelectedProject?.ProjectId))
            {
                // Updates ProjectId within the property.
                SelectedProject =
                    Projects.FirstOrDefault(p => p.ProjectId == CredentialsStore.Default.CurrentProjectId) ??
                    Projects.FirstOrDefault();
            }
            else
            {
                SelectedProject = null;
            }
        }

        private void OnChangeUser()
        {
            _promptAccountManagement();
            LoadTask = new NotifyTaskCompletion(LoadProjectsAsync());
        }

        private void OnSelectProject()
        {
            Result = ProjectId;
            _owner.Close();
        }

        private void OnSkip()
        {
            Result = "";
            _owner.Close();
        }
    }
}