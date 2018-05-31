using System.ComponentModel.Composition;
using System.Xml.Linq;

namespace GoogleCloudExtension.Services.FileSystem
{
    /// <summary>
    /// An <see cref="IXDocument"/> service implementation that delegates to <see cref="XDocument"/>.
    /// </summary>
    [Export(typeof(IXDocument))]
    public class LinqXDocumentService : IXDocument
    {
        /// <inheritdoc cref="XDocument.Load(string)"/>
        public XDocument Load(string uri) => XDocument.Load(uri);
    }
}