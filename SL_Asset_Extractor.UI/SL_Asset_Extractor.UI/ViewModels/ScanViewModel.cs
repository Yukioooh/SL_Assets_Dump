using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace SL_Asset_Extractor.UI.ViewModels
{
    public partial class ScanViewModel : BaseViewModel
    {
        public ObservableCollection<string> LogMessages { get; } = new();
        public ObservableCollection<string> SourceFolders { get; } = new();

        [ObservableProperty] private string _exportFolder = "";
        [ObservableProperty] private bool _extractTexture2D = true;
        [ObservableProperty] private bool _extractSprite = true;
        [ObservableProperty] private int _progress = 0;
        [ObservableProperty] private int _progressMax = 100;
        [ObservableProperty] private string _progressText = "";
        [ObservableProperty] private int _bundlesFound = 0;
        [ObservableProperty] private int _assetsExtracted = 0;
        [ObservableProperty] private int _newAssets = 0;
        [ObservableProperty] private int _skippedAssets = 0;

        private CancellationTokenSource? _cts;

        [RelayCommand]
        private void AddSourceFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Sélectionner un dossier source"
            };

            if (dialog.ShowDialog() == true)
            {
                if (!SourceFolders.Contains(dialog.FolderName))
                    SourceFolders.Add(dialog.FolderName);
                else
                    AddLog("Ce dossier est déjà dans la liste.");
            }
        }

        [RelayCommand]
        private void RemoveSourceFolder(string folder)
        {
            if (SourceFolders.Contains(folder))
                SourceFolders.Remove(folder);
        }

        [RelayCommand]
        private void BrowseExportFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Sélectionner le dossier d'export"
            };

            if (dialog.ShowDialog() == true)
                ExportFolder = dialog.FolderName;
        }

        [RelayCommand]
        private async Task StartExtraction()
        {
            if (SourceFolders.Count == 0)
            {
                AddLog("Veuillez ajouter au moins un dossier source.");
                return;
            }

            if (string.IsNullOrEmpty(ExportFolder))
            {
                AddLog("Veuillez sélectionner un dossier d'export.");
                return;
            }

            IsBusy = true;
            LogMessages.Clear();
            _cts = new CancellationTokenSource();

            AddLog($"Démarrage — {SourceFolders.Count} dossier(s) source(s)");
            StatusMessage = "Extraction en cours...";

            foreach (var folder in SourceFolders)
                AddLog($"Source : {folder}");

            await Task.Delay(1000, _cts.Token);

            StatusMessage = "Terminé";
            IsBusy = false;
            AddLog("Extraction terminée.");
        }

        [RelayCommand]
        private void Cancel()
        {
            _cts?.Cancel();
            StatusMessage = "Annulé";
            IsBusy = false;
            AddLog("Extraction annulée.");
        }

        private void AddLog(string message)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                LogMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            });
        }
    }
}