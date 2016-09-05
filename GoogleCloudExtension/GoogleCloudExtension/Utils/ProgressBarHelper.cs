using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    public class ProgressBarHelper : IProgress<double>, IDisposable
    {
        private const uint Total = 1000;

        private readonly IVsStatusbar _statusbar;
        private uint _cookie;

        public ProgressBarHelper(IVsStatusbar statusbar)
        {
            _statusbar = statusbar;
            _statusbar.Progress(ref _cookie, 1, "", 0, 0);
        }

        #region IDisposable

        public void Dispose()
        {
            _statusbar.Progress(ref _cookie, 0, "", 0, 0);
        }

        #endregion

        #region IProgress<double>

        void IProgress<double>.Report(double value)
        {
            _statusbar.Progress(ref _cookie, 1, "", (uint)(value * Total), Total);
        }

        #endregion
    }
}
