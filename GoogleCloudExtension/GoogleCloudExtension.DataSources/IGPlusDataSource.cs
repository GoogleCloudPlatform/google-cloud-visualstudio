using System.Threading.Tasks;
using Google.Apis.Plus.v1.Data;

namespace GoogleCloudExtension.DataSources {
    /// <summary>
    /// Interface of the <see cref="GPlusDataSource"/>
    /// </summary>
    public interface IGPlusDataSource {
        /// <summary>
        /// Fetches the profile for the authenticated user.
        /// </summary>
        Task<Person> GetProfileAsync();
    }
}