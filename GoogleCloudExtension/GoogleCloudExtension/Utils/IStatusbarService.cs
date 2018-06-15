using System;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Service interface for the <see cref="StatusbarHelper"/> service.
    /// </summary>
    public interface IStatusbarService
    {
        /// <summary>
        /// Freezes the status bar, which prevents updates from other parts of the VS shell.
        /// </summary>
        /// <returns>
        /// An implementation of <seealso cref="IDisposable"/> that will unfreeze the status bar on dispose.
        /// </returns>
        IDisposable Freeze();

        /// <summary>
        /// Change the text in the status bar. If the status bar is frozen no change is made.
        /// </summary>
        /// <param name="text">The text to display.</param>
        IDisposable FreezeText(string text);

        /// <summary>
        /// Change the text in the status bar. If the status bar is frozen no change is made.
        /// </summary>
        /// <param name="text">The text to display.</param>
        void SetText(string text);

        /// <summary>
        /// Shows an animation to show that a deploy action is being executed. This animation will only show
        /// if VS is showing all of the visual effects. The result of the method should stored in a variable in a
        /// using statement.
        /// </summary>
        /// <returns>
        /// An implementation of <seealso cref="IDisposable"/> that will stop the animation on dispose.
        /// </returns>
        IDisposable ShowDeployAnimation();

        /// <summary>
        /// Shows the progress bar indicator in the Visual Studio shell.
        /// </summary>
        /// <param name="label">The label to use for the progress indicator.</param>
        /// <returns>
        /// An instance of <seealso cref="ProgressBarHelper"/> which can be used to both update the progress bar
        /// and perform cleanup.
        /// </returns>
        ProgressBarHelper ShowProgressBar(string label);
    }
}