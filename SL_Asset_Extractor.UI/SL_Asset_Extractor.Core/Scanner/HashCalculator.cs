// Calcule l'empreinte SHA256 d'un fichier.

using System.Security.Cryptography;

namespace SLAssetExtractor.Core.Scanner { 
    public static class HashCalculator
    {
        public static async Task<string> ComputeFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();

            // read only 
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 81920,      
                useAsync: true);        

            var hashBytes = await sha256.ComputeHashAsync(stream);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public static string ComputeBytesHash(byte[] data)
        {
    
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}