using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GoogleCloudExtension.GcsFileProgressDialog
{
    public class GcsFileOperation : Model, IGcsFileOperation
    {
        private readonly SynchronizationContext _context;
        private double _progress = 0;
        private bool _isError;

        public double Progress
        {
            get { return _progress; }
            set { SetValueAndRaise(ref _progress, value); }
        }

        public bool IsError
        {
            get { return _isError; }
            set { SetValueAndRaise(ref _isError, value); }
        }

        public string Source { get; }

        public string SourceName => Path.GetFileName(Source);

        public string Bucket { get; }

        public string Destination { get; }

        public event EventHandler Completed;

        public GcsFileOperation(
            string source,
            string bucket,
            string destination)
        {
            _context = SynchronizationContext.Current;

            Source = source;
            Bucket = bucket;
            Destination = destination;
        }

        #region IUploadOperation implementation.

        void IGcsFileOperation.Progress(double value)
        {
            _context.Send((x) => Progress = value, null);
        }

        void IGcsFileOperation.Completed()
        {
            _context.Send((x) =>
            {
                Progress = 1.0;
                Completed?.Invoke(this, EventArgs.Empty);
            }, null);
        }

        void IGcsFileOperation.Cancelled()
        {
            Debug.WriteLine($"Operation or {Source} cancelled.");
        }

        void IGcsFileOperation.Error(DataSourceException ex)
        {
            IsError = true;
        }

        #endregion
    }
}
