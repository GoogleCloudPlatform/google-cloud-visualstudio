using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using stdole;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace GoogleCloudExtensionUnitTests
{
    public static class ServiceProviderMockExtensions
    {
        // Use a static field to keep garbage collection from collecting the mocks.
        private static readonly Dictionary<Mock<IServiceProvider>, Dictionary<Type, object>> s_mocks =
            new Dictionary<Mock<IServiceProvider>, Dictionary<Type, object>>();

        /// <summary>
        /// Setup a service provider to mock a service.
        /// </summary>
        /// <typeparam name="SVsType">The type used to request the service.</typeparam>
        /// <typeparam name="IVsType">The type the service is used as.</typeparam>
        /// <param name="serviceProviderMock">The mock of the service provider.</param>
        /// <returns>The mock of the service.</returns>
        public static Mock<IVsType> SetupService<SVsType, IVsType>(
            this Mock<IServiceProvider> serviceProviderMock) where IVsType : class
        {
            var serviceMock = new Mock<IVsType>();
            serviceProviderMock.SetupService<SVsType, IVsType>(serviceMock);
            return serviceMock;
        }

        /// <summary>
        /// Sets up a mocked service object to be provided by a mock service provider.
        /// </summary>
        /// <typeparam name="SVsType">The type used to request the service.</typeparam>
        /// <typeparam name="IVsType">The type the service is used as.</typeparam>
        /// <param name="serviceProviderMock">The mock of the service provider.</param>
        /// <param name="serviceMock">The mock of the service.</param>
        public static void SetupService<SVsType, IVsType>(
            this Mock<IServiceProvider> serviceProviderMock,
            IMock<IVsType> serviceMock) where IVsType : class
        {
            Guid serviceGuid = typeof(SVsType).GUID;
            Guid iUnknownGuid = typeof(IUnknown).GUID;
            // ReSharper disable once RedundantAssignment
            IntPtr interfacePtr = Marshal.GetIUnknownForObject(serviceMock.Object);
            serviceProviderMock.Setup(x => x.QueryService(ref serviceGuid, ref iUnknownGuid, out interfacePtr))
                .Returns(0);
            if (!s_mocks.ContainsKey(serviceProviderMock))
            {
                s_mocks[serviceProviderMock] = new Dictionary<Type, object>();
            }
            s_mocks[serviceProviderMock][typeof(SVsType)] = serviceMock;
        }

        /// <summary>
        /// Sets up services required for a service provider to be the global service provider.
        /// </summary>
        /// <param name="serviceProviderMock">The mock of the service provider.</param>
        public static void SetupDefaultServices(this Mock<IServiceProvider> serviceProviderMock)
        {
            Mock<IVsActivityLog> activityLogMock = serviceProviderMock.SetupService<SVsActivityLog, IVsActivityLog>();
            activityLogMock.Setup(al => al.LogEntry(It.IsAny<uint>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(VSConstants.S_OK);
        }

        /// <summary>
        /// Sets the mock of the given service provider to be the global service provider.
        /// </summary>
        /// <param name="serviceProviderMock">The mock of the service provider.</param>
        public static void SetAsGlobalProvider(this Mock<IServiceProvider> serviceProviderMock)
        {
            // Remove the old GlobalProvider if it exists.
            ServiceProvider.GlobalProvider?.Dispose();
            // This sets the ServiceProvider.GlobalProvider
            // and causes it to use the mocked IServiceProvider.
            ServiceProvider.CreateFromSetSite(serviceProviderMock.Object);
        }

        /// <summary>
        /// Dispose of the GlobalProvider and allow garbage collection of the services.
        /// </summary>
        /// <param name="serviceProviderMock">The service provider mock to dispose.</param>
        public static void Dispose(this Mock<IServiceProvider> serviceProviderMock)
        {
            serviceProviderMock.Reset();
            ServiceProvider.GlobalProvider?.Dispose();
            s_mocks.Remove(serviceProviderMock);
            Assert.AreEqual(s_mocks.Count, 0, UnitTestResources.MultipleServiceProvidersWarning);
        }
    }
}