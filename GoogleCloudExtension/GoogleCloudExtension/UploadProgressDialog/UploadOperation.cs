using Google.Apis.Upload;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.UploadProgressDialog
{
    public class UploadOperation : Model, IUploadOperation
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

        public string FullGcsPath => $"gs://{Bucket}/{Destination}";

        public event EventHandler Completed;

        public UploadOperation(
            SynchronizationContext context,
            string source,
            string bucket,
            string destination)
        {
            _context = context;

            Source = source;
            Bucket = bucket;
            Destination = destination;
        }

        #region IUploadOperation implementation.

        void IUploadOperation.Progress(double value)
        {
            _context.Send((x) => Progress = value, null);
        }

        void IUploadOperation.Completed()
        {
            _context.Send((x) => Completed?.Invoke(this, EventArgs.Empty), null);
        }

        void IUploadOperation.Cancelled()
        {
            Debug.WriteLine($"Operation or {Source} cancelled.");
        }

        void IUploadOperation.Error(DataSourceException ex)
        {
            IsError = true;
        }

        #endregion
    }
}
