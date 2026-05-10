namespace SL_Asset_Extractor.Core.Models
{
    public class AppSettings
    {
        public List<string> SourceFolders { get; set; } = new();
        public string ExportFolder { get; set; } = "";
    }
}