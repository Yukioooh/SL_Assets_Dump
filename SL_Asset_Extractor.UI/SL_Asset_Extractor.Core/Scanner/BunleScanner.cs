using SL_Asset_Extractor.Core.Models;
using SLAssetExtractor.Core.Scanner;

namespace SL_Asset_Extractor.Core.Scanner
{
    public class BundleScanner
    {
        public event Action<string, int, int>? BundleFound;
        public event Action<int>? ScanCompleted;

        public async Task<List<BundleInfo>> ScanDirectoryAsync(
            string directoryPath,
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Le dossier '{directoryPath}' n'existe pas.");

            var bundleFiles = Directory.GetFiles(directoryPath, "*.bundle", SearchOption.AllDirectories);
            var results = new List<BundleInfo>();

            for (int i = 0; i < bundleFiles.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var filePath = bundleFiles[i];
                var fileName = Path.GetFileName(filePath);

                BundleFound?.Invoke(fileName, i + 1, bundleFiles.Length);

                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var hash = await HashCalculator.ComputeFileHashAsync(filePath);

                    results.Add(new BundleInfo
                    {
                        FileName = fileName,
                        FullPath = filePath,
                        Hash = hash,
                        FileSize = fileInfo.Length,
                        LastScanned = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur bundle {fileName} : {ex.Message}");
                }
            }

            ScanCompleted?.Invoke(results.Count);
            return results;
        }
    }
}