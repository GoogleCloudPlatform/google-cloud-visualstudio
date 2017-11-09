using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Theming
{
    /// <summary>
    /// Interface to be implemented by those Windows that can be closed. Allows for mocking
    /// of the window.
    /// </summary>
    public interface ICloseable
    {
        /// <summary>
        /// Closes thew Window, or equivalent.
        /// </summary>
        void Close();
    }
}
