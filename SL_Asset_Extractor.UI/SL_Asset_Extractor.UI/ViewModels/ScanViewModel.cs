using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using SL_Asset_Extractor.Core.Scanner;
using SL_Asset_Extractor.Core.Extractor;
using SL_Asset_Extractor.Core.Database;
using SL_Asset_Extractor.Core.Classifier;
using SL_Asset_Extractor.Core.Models;
using SL_Asset_Extractor.Core.Settings;
using SL_Asset_Extractor.UI.Views;

namespace SL_Asset_Extractor.UI.ViewModels
{
    public partial class ScanViewModel : BaseViewModel
    {
        public ObservableCollection<string> LogMessages { get; } = new();
        public ObservableCollection<string> SourceFolders { get; } = new();
        public ObservableCollection<string> CharactersList { get; } = new();

        [ObservableProperty] private string _exportFolder = "";
        [ObservableProperty] private bool _extractTexture2D = true;
        [ObservableProperty] private bool _extractSprite = true;
        [ObservableProperty] private int _progress = 0;
        [ObservableProperty] private int _progressMax = 1;
        [ObservableProperty] private string _progressText = "";
        [ObservableProperty] private int _bundlesFound = 0;
        [ObservableProperty] private int _assetsExtracted = 0;
        [ObservableProperty] private int _newAssets = 0;
        [ObservableProperty] private int _skippedAssets = 0;
        [ObservableProperty] private string _timeRemaining = "";

        private CancellationTokenSource? _cts;
        private DateTime _extractionStartTime;
        private int _bundlesProcessed = 0;

        private static string CliPath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Tools",
            "AssetStudioModCLI",
            "AssetStudioModCLI.exe");

        private static string DbPath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "sl_assets.db");

        private static string RulesPath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "rules.json");

        private static string SettingsPath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "settings.json");

        private readonly SettingsService _settingsService;
        private readonly RulesService _rulesService;

        public ScanViewModel()
        {
            _settingsService = new SettingsService(SettingsPath);
            _rulesService = new RulesService(RulesPath);
            LoadSettings();
        }

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
                {
                    SourceFolders.Add(dialog.FolderName);
                    SaveSettings();
                }
                else
                {
                    AddLog("Ce dossier est déjà dans la liste.");
                }
            }
        }

        [RelayCommand]
        private void RemoveSourceFolder(string folder)
        {
            if (SourceFolders.Contains(folder))
            {
                SourceFolders.Remove(folder);
                SaveSettings();
            }
        }

        [RelayCommand]
        private void BrowseExportFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Sélectionner le dossier d'export"
            };

            if (dialog.ShowDialog() == true)
            {
                ExportFolder = dialog.FolderName;
                SaveSettings();
            }
        }

        [RelayCommand]
        private void AddCharacter()
        {
            var dialog = new AddCharacterDialog
            {
                Owner = App.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                var name = dialog.CharacterName;
                if (!CharactersList.Contains(name))
                {
                    CharactersList.Add(name);
                    _rulesService.AddCharacter(name);
                    AddLog($"Personnage ajouté : {name}");
                }
                else
                {
                    AddLog($"Personnage déjà dans la liste : {name}");
                }
            }
        }

        [RelayCommand]
        private void RemoveCharacter(string character)
        {
            if (CharactersList.Contains(character))
            {
                CharactersList.Remove(character);
                _rulesService.RemoveCharacter(character);
                AddLog($"Personnage supprimé : {character}");
            }
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

            if (!File.Exists(CliPath))
            {
                AddLog($"CLI introuvable : {CliPath}");
                return;
            }

            if (!File.Exists(RulesPath))
            {
                AddLog($"rules.json introuvable : {RulesPath}");
                return;
            }

            IsBusy = true;
            LogMessages.Clear();
            BundlesFound = 0;
            AssetsExtracted = 0;
            NewAssets = 0;
            SkippedAssets = 0;
            TimeRemaining = "";
            _cts = new CancellationTokenSource();

            var scanner = new BundleScanner();
            var database = new DatabaseService(DbPath);
            var classifier = new AssetClassifier(RulesPath);
            var extractor = new AssetExtractor(CliPath, ExportFolder);

            extractor.LogReceived += msg => AddLog(msg);

            try
            {
                AddLog("Scan des dossiers sources...");
                StatusMessage = "Scan en cours...";

                var allBundles = new List<BundleInfo>();

                void OnBundleFound(string name, int current, int total)
                {
                    UpdateUI(() =>
                    {
                        Progress = current;
                        ProgressMax = total;
                        ProgressText = $"{current} / {total}";
                        StatusMessage = $"Scan : {name}";
                    });
                }

                scanner.BundleFound += OnBundleFound;

                foreach (var sourceFolder in SourceFolders)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    AddLog($"Scan de : {sourceFolder}");

                    var bundles = await scanner.ScanDirectoryAsync(
                        sourceFolder, _cts.Token);

                    allBundles.AddRange(bundles);
                }

                scanner.BundleFound -= OnBundleFound;

                BundlesFound = allBundles.Count;
                AddLog($"{allBundles.Count} bundle(s) trouvé(s).");

                AddLog("Vérification de la base de données...");
                StatusMessage = "Vérification...";

                var toExtract = new List<BundleInfo>();

                foreach (var bundle in allBundles)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var existing = await database.GetBundleByPathAsync(bundle.FullPath);

                    if (existing == null)
                    {
                        AddLog($"Nouveau : {bundle.FileName}");
                        toExtract.Add(bundle);
                    }
                    else if (existing.Hash != bundle.Hash)
                    {
                        AddLog($"Modifié : {bundle.FileName}");
                        toExtract.Add(bundle);
                    }
                    else
                    {
                        SkippedAssets++;
                    }
                }

                AddLog($"{toExtract.Count} bundle(s) à extraire, {SkippedAssets} ignoré(s).");

                if (toExtract.Count == 0)
                {
                    AddLog("Aucun nouveau bundle détecté.");
                    StatusMessage = "Terminé — aucun changement.";
                    return;
                }

                AddLog("Démarrage de l'extraction...");
                Progress = 0;
                ProgressMax = toExtract.Count;
                _extractionStartTime = DateTime.Now;
                _bundlesProcessed = 0;

                for (int i = 0; i < toExtract.Count; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var bundle = toExtract[i];
                    UpdateUI(() =>
                    {
                        Progress = i + 1;
                        ProgressText = $"{i + 1} / {toExtract.Count}";
                        StatusMessage = $"Extraction : {bundle.FileName}";
                    });

                    AddLog($"Extraction : {bundle.FileName}");

                    var success = await extractor.ExtractFromFolderAsync(
                        bundle.FullPath, _cts.Token);

                    if (success)
                    {
                        await ClassifyAndMoveAssetsAsync(
                            ExportFolder, bundle, classifier, database);

                        await database.SaveBundleAsync(bundle);
                        AssetsExtracted++;
                        _bundlesProcessed++;

                        var elapsed = DateTime.Now - _extractionStartTime;
                        var avgPerBundle = elapsed.TotalSeconds / _bundlesProcessed;
                        var remaining = avgPerBundle * (toExtract.Count - _bundlesProcessed);
                        var remainingSpan = TimeSpan.FromSeconds(remaining);

                        UpdateUI(() =>
                        {
                            TimeRemaining = _bundlesProcessed < toExtract.Count
                                ? $"Temps restant : {remainingSpan:mm\\:ss}"
                                : "";
                        });
                    }
                    else
                    {
                        AddLog($"Echec extraction : {bundle.FileName}");
                    }
                }

                StatusMessage = $"Terminé — {NewAssets} nouveaux assets.";
                AddLog($"Extraction terminée ! {NewAssets} nouveaux assets extraits.");
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Annulé.";
                AddLog("Extraction annulée.");
            }
            catch (Exception ex)
            {
                StatusMessage = "Erreur.";
                AddLog($"Erreur : {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                _cts?.Dispose();
                _cts = null;
                ProgressText = "";
                TimeRemaining = "";
            }
        }

        private async Task ClassifyAndMoveAssetsAsync(
            string exportFolder,
            BundleInfo bundle,
            AssetClassifier classifier,
            DatabaseService database)
        {
            var subFolders = new[] { "Texture2D", "Sprite" };

            foreach (var sub in subFolders)
            {
                var folderPath = Path.Combine(exportFolder, sub);
                if (!Directory.Exists(folderPath)) continue;

                var pngFiles = Directory.GetFiles(folderPath, "*.png");

                foreach (var pngFile in pngFiles)
                {
                    var assetName = Path.GetFileNameWithoutExtension(pngFile);
                    var uniqueKey = $"{bundle.FileName}_{assetName}";

                    var exists = await database.AssetExistsAsync(uniqueKey);
                    if (exists)
                    {
                        SkippedAssets++;
                        continue;
                    }

                    var result = classifier.Classify(assetName);

                    var destFolder = Path.Combine(exportFolder, result.FullPath);
                    Directory.CreateDirectory(destFolder);

                    var destFile = Path.Combine(destFolder, Path.GetFileName(pngFile));
                    File.Move(pngFile, destFile, overwrite: true);

                    var assetInfo = new AssetInfo
                    {
                        PathId = 0,
                        Name = assetName,
                        Type = AssetType.Texture2D,
                        BundleName = bundle.FileName,
                        ExportedPath = destFile,
                        Category = result.Category,
                        ExtractedAt = DateTime.UtcNow
                    };

                    await database.SaveAssetAsync(assetInfo);
                    NewAssets++;
                    AddLog($"Asset : {assetName} → {result.FullPath}");
                }
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _cts?.Cancel();
            StatusMessage = "Annulé";
            IsBusy = false;
            TimeRemaining = "";
            AddLog("Extraction annulée.");
        }

        private void LoadSettings()
        {
            var settings = _settingsService.Load();
            ExportFolder = settings.ExportFolder;
            foreach (var folder in settings.SourceFolders)
                SourceFolders.Add(folder);

            var characters = _rulesService.GetCharacters();
            foreach (var character in characters)
                CharactersList.Add(character);
        }

        private void SaveSettings()
        {
            _settingsService.Save(new AppSettings
            {
                SourceFolders = SourceFolders.ToList(),
                ExportFolder = ExportFolder
            });
        }

        private void AddLog(string message)
        {
            UpdateUI(() => LogMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}"));
        }

        private void UpdateUI(Action action)
        {
            App.Current.Dispatcher.Invoke(action);
        }
    }
}