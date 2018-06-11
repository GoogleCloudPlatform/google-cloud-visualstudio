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
        public static IVsWindowFrame GetMockedWindowFrame() => GetWindowFrameMock().Object;

        /// <summary>
        /// Gets a <see cref="Mock"/> of an <see cref="IVsWindowFrame"/>.
        /// </summary>
        /// <param name="mockBehavior"></param>
        /// <returns>Gets a <see cref="Mock"/> of an <see cref="IVsWindowFrame"/>.</returns>
        public static Mock<IVsWindowFrame> GetWindowFrameMock(MockBehavior mockBehavior = MockBehavior.Loose)
        {
            var mock = new Mock<IVsWindowFrame>(mockBehavior);
            mock.As<IVsWindowFrame2>();
            mock.Setup(f => f.Show()).Returns(VSConstants.S_OK);

            // ReSharper disable once RedundantAssignment
            object outProperty = null;
            mock.Setup(f => f.GetProperty(It.IsAny<int>(), out outProperty)).Returns(VSConstants.S_OK);
            return mock;
        }
    }
}