using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.ApiManagement
{
    public interface IApiManager
    {
        /// <summary>
        /// This method will check that all of the given service names are enabled.
        /// </summary>
        /// <param name="serviceNames">The list of services to check.</param>
        /// <returns>A task that will be true if all services are enabled, false otherwise.</returns>
        Task<bool> AreServicesEnabledAsync(IList<string> serviceNames);

        /// <summary>
        /// This method will check that all given services are enabled and if not will prompt the user to enable the
        /// necessary services.
        /// </summary>
        /// <param name="serviceNames">The services to check.</param>
        /// <param name="prompt">The prompt to use in the prompt dialog to ask the user for permission to enable the services.</param>
        /// <returns>A task that will be true if all services where enabled, false if the user cancelled or if the operation failed.</returns>
        Task<bool> EnsureAllServicesEnabledAsync(IEnumerable<string> serviceNames, string prompt);

        /// <summary>
        /// This method will enable the list of services given.
        /// </summary>
        /// <param name="serviceNames">The list of services to enable.</param>
        /// <returns>A task that will be completed once the operation finishes.</returns>
        Task EnableServicesAsync(IEnumerable<string> serviceNames);

    }
}
