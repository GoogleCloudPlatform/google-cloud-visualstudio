// Copyright 2018 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;

namespace GoogleCloudExtension.MenuBarControls
{
    public interface IGcpMenuBarControl : IVsUIElement, IVsUIWpfElement { }

    /// <summary>
    /// Interaction logic for GcpMenuBarControl.xaml
    /// </summary>
    [Export(typeof(IGcpMenuBarControl))]
    public partial class GcpMenuBarControl : UserControl, IGcpMenuBarControl, INonClientArea
    {
        /// <summary>
        /// The response to WM_NCHITTEST that defers to the client.
        /// </summary>
        /// <seealso cref="http://docs.microsoft.com/en-us/windows/desktop/inputdev/wm-nchittest"/>
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        internal const int HTCLIENT = 1;

        private IVsUISimpleDataSource _vsDataSource;

        [ImportingConstructor]
        public GcpMenuBarControl(IGcpUserProjectViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        /// <summary>Gets the data source for this element.</summary>
        /// <param name="ppDataSource">[out] The data source.</param>
        /// <returns>If the method succeeds, it returns <see cref="VSConstants.S_OK" />. If it fails, it returns an error code.</returns>
        public int get_DataSource(out IVsUISimpleDataSource ppDataSource)
        {
            ppDataSource = _vsDataSource;
            return VSConstants.S_OK;
        }

        /// <summary>Binds the specified data source to this element.</summary>
        /// <param name="pDataSource">The data source.</param>
        /// <returns>If the method succeeds, it returns <see cref="VSConstants.S_OK" />. If it fails, it returns an error code.</returns>
        public int put_DataSource(IVsUISimpleDataSource pDataSource)
        {
            _vsDataSource = pDataSource;
            return VSConstants.S_OK;
        }

        /// <summary>Translates keyboard accelerators.</summary>
        /// <returns>If the method succeeds, it returns <see cref="VSConstants.S_OK" />. If it fails, it returns an error code.</returns>
        public int TranslateAccelerator(IVsUIAccelerator _) => VSConstants.S_OK;

        /// <summary>
        /// Gets the implementation-specific object
        /// (e.g. <see cref="IVsUIWpfElement" />, <see cref="IVsUIWin32Element" />).
        /// </summary>
        /// <param name="uiObject">[out] The UI object.</param>
        /// <returns>
        /// If the method succeeds, it returns <see cref="VSConstants.S_OK" />. If it fails, it returns an error code.
        /// </returns>
        public int GetUIObject(out object uiObject)
        {
            uiObject = this;
            return VSConstants.S_OK;
        }

        /// <summary>Creates a Windows Presentation Foundation user interface element.</summary>
        /// <param name="frameworkElement">[out] Location to return the interface for the new element.</param>
        /// <returns>Returns S_OK if the element was created.</returns>
        public int CreateFrameworkElement(out object frameworkElement)
        {
            frameworkElement = this;
            return VSConstants.S_FALSE;
        }

        /// <summary>Returns an interface to the Windows Presentation Foundation user interface element.</summary>
        /// <param name="frameworkElement">[out] Location to return the interface.</param>
        /// <returns>Returns S_OK if the element's interface was returned.</returns>
        public int GetFrameworkElement(out object frameworkElement)
        {
            frameworkElement = this;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Given a point, determines what the hit test result should be for
        /// WM_NCHITTEST.
        /// </summary>
        /// <returns>HTCLIENT</returns>
        /// <remarks>This method makes the control interactive when placed on the title bar.</remarks>
        /// <seealso cref="http://docs.microsoft.com/en-us/windows/desktop/inputdev/wm-nchittest"/>
        int INonClientArea.HitTest(Point _)
        {
            return HTCLIENT;
        }
    }
}
