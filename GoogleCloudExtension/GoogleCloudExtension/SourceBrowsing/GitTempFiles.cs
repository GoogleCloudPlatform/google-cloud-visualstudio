using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.SourceBrowsing
{
    internal class GitTempFiles
    {
        // Git SHA, repository, relative file path
        // Return a file : create new or open an existing one.
        // In the end, clean up.

        // (a) Windows temp files won't be cleaned up automatically.
        // (b) Clean up on VS exit event.
        //  http://stackoverflow.com/questions/14679217/how-to-detect-if-visual-studio-ide-is-closing-using-vspackage
        // (c) create a folder to hold tmp files on dates
        // (d) clean up old tmp files periodically -- 2 days ago. 

        private const string TempSubFolder = "GoogleToolsForVS";

        private Lazy<string> _folder = new Lazy<string>(
            () => Path.Combine(Path.GetTempPath(), TempSubFolder));

        private Dictionary<string, string> _tmpFilesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Use () => new GitTempFiles() because the constructor is private.
        private static Lazy<GitTempFiles> s_instance = new Lazy<GitTempFiles>(() => new GitTempFiles());

        public static GitTempFiles Current => s_instance.Value;

        private GitTempFiles()
        {
            if (!File.Exists(_folder.Value))
            {
                Directory.CreateDirectory(_folder.Value);
            }
            Clean();
            ShellUtils.RegisterShutdownEventHandler(Clean);
        }

        public string GetOrSave(string gitSha, string relativePath, Action<string> save)
        {
            gitSha.ThrowIfNullOrEmpty(nameof(gitSha));
            relativePath.ThrowIfNullOrEmpty(nameof(relativePath));
            save.ThrowIfNull(nameof(save));

            var key = $"{gitSha}/{relativePath}";
            if (_tmpFilesMap.ContainsKey(key))
            {
                return _tmpFilesMap[key];
            }
            else
            {
                var filePath = NewTempFileName(Path.GetFileName(relativePath));
                save(filePath);
                _tmpFilesMap[key] = filePath;
                return filePath;
            }
        }

        public void Clean()
        {
            try
            {
                Array.ForEach(Directory.GetFiles(_folder.Value), File.Delete);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                || ex is DirectoryNotFoundException || ex is PathTooLongException)
            { }
        }

        private string NewTempFileName(string name) => 
            Path.Combine(_folder.Value, $"{Guid.NewGuid()}_{name}");
    }
}
