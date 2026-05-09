using Dapper;
using Microsoft.Data.Sqlite;
using SL_Asset_Extractor.Core.Models;

namespace SL_Asset_Extractor.Core.Database
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            InitializeDatabase();
        }

        private SqliteConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        private void InitializeDatabase()
        {
            using var connection = CreateConnection();
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Bundles (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    FileName    TEXT    NOT NULL,
                    FullPath    TEXT    NOT NULL UNIQUE,
                    Hash        TEXT    NOT NULL,
                    FileSize    INTEGER NOT NULL,
                    LastScanned TEXT    NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_bundles_filename ON Bundles(FileName);

                CREATE TABLE IF NOT EXISTS Assets (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    PathId       INTEGER NOT NULL,
                    Name         TEXT    NOT NULL,
                    Type         TEXT    NOT NULL,
                    BundleName   TEXT    NOT NULL,
                    UniqueKey    TEXT    NOT NULL UNIQUE,
                    ExportedPath TEXT,
                    Category     TEXT    NOT NULL DEFAULT 'Unclassified',
                    ExtractedAt  TEXT    NOT NULL,
                    ImageHash    TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_assets_uniquekey ON Assets(UniqueKey);
                CREATE INDEX IF NOT EXISTS idx_assets_category ON Assets(Category);

                CREATE TABLE IF NOT EXISTS ExtractionLogs (
                    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    StartedAt           TEXT    NOT NULL,
                    FinishedAt          TEXT,
                    BundlesScanned      INTEGER NOT NULL DEFAULT 0,
                    NewBundles          INTEGER NOT NULL DEFAULT 0,
                    ModifiedBundles     INTEGER NOT NULL DEFAULT 0,
                    SkippedBundles      INTEGER NOT NULL DEFAULT 0,
                    NewAssetsExtracted  INTEGER NOT NULL DEFAULT 0,
                    DuplicatesSkipped   INTEGER NOT NULL DEFAULT 0,
                    ErrorMessage        TEXT
                );
            ");
        }

        public async Task<BundleInfo?> GetBundleByPathAsync(string fullPath)
        {
            using var connection = CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<BundleInfo>(
                "SELECT * FROM Bundles WHERE FullPath = @FullPath",
                new { FullPath = fullPath });
        }

        public async Task SaveBundleAsync(BundleInfo bundle)
        {
            using var connection = CreateConnection();
            await connection.ExecuteAsync(@"
                INSERT INTO Bundles (FileName, FullPath, Hash, FileSize, LastScanned)
                VALUES (@FileName, @FullPath, @Hash, @FileSize, @LastScanned)
                ON CONFLICT(FullPath) DO UPDATE SET
                    Hash        = excluded.Hash,
                    FileSize    = excluded.FileSize,
                    LastScanned = excluded.LastScanned",
                bundle);
        }

        public async Task<bool> AssetExistsAsync(string uniqueKey)
        {
            using var connection = CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Assets WHERE UniqueKey = @UniqueKey",
                new { UniqueKey = uniqueKey });
            return count > 0;
        }

        public async Task SaveAssetAsync(AssetInfo asset)
        {
            using var connection = CreateConnection();
            await connection.ExecuteAsync(@"
                INSERT OR IGNORE INTO Assets 
                    (PathId, Name, Type, BundleName, UniqueKey,
                     ExportedPath, Category, ExtractedAt, ImageHash)
                VALUES 
                    (@PathId, @Name, @Type, @BundleName, @UniqueKey,
                     @ExportedPath, @Category, @ExtractedAt, @ImageHash)",
                new
                {
                    asset.PathId,
                    asset.Name,
                    Type = asset.Type.ToString(),
                    asset.BundleName,
                    asset.UniqueKey,
                    asset.ExportedPath,
                    asset.Category,
                    ExtractedAt = asset.ExtractedAt.ToString("O"),
                    asset.ImageHash
                });
        }

        public async Task<List<AssetInfo>> GetAssetsByCategoryAsync(string category)
        {
            using var connection = CreateConnection();
            var results = await connection.QueryAsync<dynamic>(
                "SELECT * FROM Assets WHERE Category = @Category ORDER BY Name",
                new { Category = category });

            return results.Select(r => new AssetInfo
            {
                Id = (int)r.Id,
                PathId = (long)r.PathId,
                Name = (string)r.Name,
                Type = Enum.Parse<AssetType>((string)r.Type),
                BundleName = (string)r.BundleName,
                ExportedPath = (string?)r.ExportedPath,
                Category = (string)r.Category,
                ExtractedAt = DateTime.Parse((string)r.ExtractedAt),
                ImageHash = (string?)r.ImageHash
            }).ToList();
        }

        public async Task<int> SaveExtractionLogAsync(ExtractionLog log)
        {
            using var connection = CreateConnection();
            return await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO ExtractionLogs 
                    (StartedAt, BundlesScanned, NewBundles, ModifiedBundles,
                     SkippedBundles, NewAssetsExtracted, DuplicatesSkipped)
                VALUES 
                    (@StartedAt, @BundlesScanned, @NewBundles, @ModifiedBundles,
                     @SkippedBundles, @NewAssetsExtracted, @DuplicatesSkipped);
                SELECT last_insert_rowid();",
                new
                {
                    StartedAt = log.StartedAt.ToString("O"),
                    log.BundlesScanned,
                    log.NewBundles,
                    log.ModifiedBundles,
                    log.SkippedBundles,
                    log.NewAssetsExtracted,
                    log.DuplicatesSkipped
                });
        }

        public async Task UpdateExtractionLogAsync(ExtractionLog log)
        {
            using var connection = CreateConnection();
            await connection.ExecuteAsync(@"
                UPDATE ExtractionLogs SET
                    FinishedAt           = @FinishedAt,
                    NewAssetsExtracted   = @NewAssetsExtracted,
                    DuplicatesSkipped    = @DuplicatesSkipped,
                    ErrorMessage         = @ErrorMessage
                WHERE Id = @Id",
                new
                {
                    FinishedAt = log.FinishedAt?.ToString("O"),
                    log.NewAssetsExtracted,
                    log.DuplicatesSkipped,
                    log.ErrorMessage,
                    log.Id
                });
        }
    }
}