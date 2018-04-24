using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;

namespace GoogleCloudExtensionUnitTests
{
    /// <summary>
    /// Static class for creating mocks of <see cref="IVsWindowFrame"/>s.
    /// </summary>
    public static class VsWindowFrameMocks
    {

        /// <summary>
        /// Gets an <see cref="IVsWindowFrame"/> backed by a <see cref="Mock{T}"/>.
        /// </summary>
        /// <returns>A mocked insance of <see cref="IVsWindowFrame"/>.</returns>
        public static IVsWindowFrame GetMockedWindowFrame()
        {
            // ReSharper disable once RedundantAssignment
            object outProperty = null;
            return Mock.Of<IVsWindowFrame>(
                f => f.Show() == VSConstants.S_OK &&
                    f.GetProperty(It.IsAny<int>(), out outProperty) == VSConstants.S_OK);
        }

        /// <summary>
        /// Gets a <see cref="Mock"/> of an <see cref="IVsWindowFrame"/>.
        /// </summary>
        /// <returns>Gets a <see cref="Mock"/> of an <see cref="IVsWindowFrame"/>.</returns>
        public static Mock<IVsWindowFrame> GetWindowFrameMock()
        {
            return Mock.Get(GetMockedWindowFrame());
        }
    }
}