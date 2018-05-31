using System.Xml.Linq;

namespace GoogleCloudExtension.Services.FileSystem
{
    /// <summary>
    /// Interface for an xml document service that matches the static methods of <see cref="XDocument"/>.
    /// </summary>
    public interface IXDocument
    {
        /// <inheritdoc cref="XDocument.Load(string)"/>
        XDocument Load(string uri);
    }
}