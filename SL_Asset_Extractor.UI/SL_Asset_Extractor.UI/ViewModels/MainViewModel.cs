using System.IO;

namespace SL_Asset_Extractor.UI.ViewModels
{
    public class MainViewModel
    {
        public ScanViewModel ScanVM { get; }
        public AssetBrowserViewModel BrowserVM { get; }

        public MainViewModel()
        {
            ScanVM = new ScanViewModel();
            BrowserVM = new AssetBrowserViewModel();

            ScanVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ScanVM.ExportFolder))
                {
                    BrowserVM.SetExportFolder(ScanVM.ExportFolder);
                    if (!string.IsNullOrEmpty(ScanVM.ExportFolder))
                        _ = BrowserVM.LoadAssetsAsync(ScanVM.ExportFolder);
                }

                if (e.PropertyName == nameof(ScanVM.IsBusy) && !ScanVM.IsBusy)
                    _ = BrowserVM.LoadAssetsAsync(ScanVM.ExportFolder);
            };

            if (!string.IsNullOrEmpty(ScanVM.ExportFolder))
            {
                BrowserVM.SetExportFolder(ScanVM.ExportFolder);
                _ = BrowserVM.LoadAssetsAsync(ScanVM.ExportFolder);
            }
        }
    }
}