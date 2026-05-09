// Représente un asset extrait d'un bundle 

namespace SL_Asset_Extractor.Core.Models { 
        public enum AssetType
        {
            Texture2D,
            Sprite
        }

        public class AssetInfo
        {
            
            public int Id { get; set; }
            // id du bundle d'origine
            public long PathId { get; set; }
            // nom de l'asset 
            public required string Name { get; set; }
            // type
            public AssetType Type { get; set; }
            // bundle ou se trouve l'asset
            public required string BundleName { get; set; }
            // clé pour éviter les doublons en bdd (bundle + pathid)
            public string UniqueKey => $"{BundleName}_{PathId}";
            // chemin d'exportation de l'asset (null si pas encore exporté)
            public string? ExportedPath { get; set; }
            // catégorie de l'asset (ex: "UI", "Gameplay", etc.)
            public string Category { get; set; } = "Unclassified";
            // date d'extraction de l'asset
            public DateTime ExtractedAt { get; set; }
            // hash de l'image (pour éviter les doublons d'export)
            public string? ImageHash { get; set; }
        
        }
    }
