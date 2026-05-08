// Représente un fichier .bundle scanné 

namespace SL_Asset_Extractor.Core.Models { 
    public class BundleInfo 
    {
        // id unique en bdd 
        public int Id { get; set; }
        
        //nom du bundle 
        public required string FileName { get; set; }
        
        // chemin complet 
        public required string FullPath { get; set; }
        
        // hash du bundle 
        public required string Hash { get; set; }
        
        // taille du fichier 
        public long FileSize { get; set; }

        // date du scan 
        public DateTime LastScanned { get; set; }

        //nb d'asset scannés 
        public int ExtractedAssetsCount { get; set; }

    }
}