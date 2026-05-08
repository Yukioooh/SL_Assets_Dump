
namespace SL_Asset_Extractor.Core.Models
{
    public class ExtractionLog
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int BundlesScanned { get; set; }
        public int NewBundles { get; set; }
        public int ModifiedBundles { get; set; }
        public int SkippedBundles { get; set; }
        public int NewAssetsExtracted { get; set; }
        public int DuplicatesSkipped { get; set; }


        public string? ErrorMessage { get; set; }

        // Propriété calculée : durée totale
        public TimeSpan? Duration => FinishedAt - StartedAt;
    }
}