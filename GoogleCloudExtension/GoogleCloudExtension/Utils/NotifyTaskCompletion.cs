using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    public class NotifyTaskCompletion : INotifyPropertyChanged
    {
        /// <summary>
        /// Is set to true if the task threw an error.
        /// </summary>
        public bool IsFaulted => Task.IsFaulted;

        /// <summary>
        /// Gets a relevant exception error message, if it exists.
        /// </summary>
        public string ErrorMessage =>
            Task.Exception?.InnerException?.Message ??
            Task.Exception?.InnerExceptions.FirstOrDefault()?.Message ??
            Task.Exception?.Message;

        /// <summary>
        /// True if the task ended execution due to being canceled.
        /// </summary>
        public bool IsCanceled => Task.IsCanceled;

        /// <summary>
        /// True if the task has completed.
        /// </summary>
        public bool IsCompleted => Task.IsCompleted;

        /// <summary>
        /// True if the task completed without cancelation or exception.
        /// </summary>
        public bool IsSuccess => IsCompleted && !IsCanceled && !IsFaulted;

        private Task Task { get; }

        public NotifyTaskCompletion(Task task)
        {
            Task = task;
            WaitTask(task);
        }

        private async void WaitTask(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                // Check exceptions using task.
            }
            NotifyPropertyChanged(null);
        }

        /// <summary>
        /// Triggered on property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Trigger a property changed event.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property that changed. null for all properties.
        /// </param>
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
