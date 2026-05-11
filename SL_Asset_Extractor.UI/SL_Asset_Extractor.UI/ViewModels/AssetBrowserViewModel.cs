using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace SL_Asset_Extractor.UI.ViewModels
{
    public class AssetItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string Category { get; set; } = "";
        public string SubFolder { get; set; } = "";
        public string SubSubFolder { get; set; } = "";
        public BitmapImage? Thumbnail { get; set; }
    }

    public partial class AssetBrowserViewModel : ObservableObject
    {
        private List<AssetItem> _allAssets = new();
        private List<AssetItem> _filteredList = new();
        private string _exportFolder = "";

        public ObservableCollection<AssetItem> FilteredAssets { get; } = new();
        public ObservableCollection<string> Categories { get; } = new();
        public ObservableCollection<string> SubFolders { get; } = new();
        public ObservableCollection<string> SubSubFolders { get; } = new();

        [ObservableProperty] private string _selectedCategory = "Toutes";
        [ObservableProperty] private string _selectedSubFolder = "Tous";
        [ObservableProperty] private string _selectedSubSubFolder = "Tous";
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private AssetItem? _selectedAsset;
        [ObservableProperty] private BitmapImage? _previewImage;
        [ObservableProperty] private int _totalAssets = 0;
        [ObservableProperty] private bool _isLoading = false;
        [ObservableProperty] private int _currentPage = 1;
        [ObservableProperty] private int _totalPages = 1;
        [ObservableProperty] private string _pageInfo = "Page 1 / 1";
        private const int PageSize = 50;

        partial void OnSelectedCategoryChanged(string value)
        {
            UpdateSubFolders();
            UpdateSubSubFolders();
            ApplyFilters();
        }

        partial void OnSelectedSubFolderChanged(string value)
        {
            UpdateSubSubFolders();
            ApplyFilters();
        }

        partial void OnSelectedSubSubFolderChanged(string value) => ApplyFilters();
        partial void OnSearchTextChanged(string value) => ApplyFilters();

        partial void OnSelectedAssetChanged(AssetItem? value)
        {
            if (value == null)
            {
                PreviewImage = null;
                return;
            }

            try
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(value.FullPath);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                PreviewImage = img;
            }
            catch
            {
                PreviewImage = null;
            }
        }

        public void SetExportFolder(string folder)
        {
            _exportFolder = folder;
        }

        public async Task LoadAssetsAsync(string exportFolder)
        {
            if (!Directory.Exists(exportFolder)) return;

            IsLoading = true;
            _allAssets.Clear();
            App.Current.Dispatcher.Invoke(() => FilteredAssets.Clear());

            await Task.Run(() =>
            {
                var pngFiles = Directory
                    .GetFiles(exportFolder, "*.png", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("_temp"))
                    .ToList();

                foreach (var file in pngFiles)
                {
                    var relativePath = Path.GetRelativePath(exportFolder, file);
                    var parts = relativePath.Split(Path.DirectorySeparatorChar);

                    var category = parts.Length >= 1 ? parts[0] : "Unclassified";
                    var subFolder = parts.Length >= 3 ? parts[1] : "";
                    var subSubFolder = parts.Length >= 4 ? parts[2] : "";

                    _allAssets.Add(new AssetItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        FullPath = file,
                        Category = category,
                        SubFolder = subFolder,
                        SubSubFolder = subSubFolder,
                        Thumbnail = null
                    });
                }
            });

            App.Current.Dispatcher.Invoke(() =>
            {
                UpdateCategories();
                UpdateSubFolders();
                UpdateSubSubFolders();
                ApplyFilters();
                TotalAssets = _allAssets.Count;
            });

            IsLoading = false;
        }

        private static BitmapImage LoadThumbnail(string path, int size)
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(path);
            img.DecodePixelWidth = size;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();
            return img;
        }

        private void UpdateCategories()
        {
            Categories.Clear();
            Categories.Add("Toutes");
            foreach (var cat in _allAssets.Select(a => a.Category).Distinct().OrderBy(c => c))
                Categories.Add(cat);
            SelectedCategory = "Toutes";
        }

        private void UpdateSubFolders()
        {
            SubFolders.Clear();
            SubFolders.Add("Tous");

            var filtered = SelectedCategory == "Toutes"
                ? _allAssets
                : _allAssets.Where(a => a.Category == SelectedCategory);

            foreach (var sub in filtered
                .Select(a => a.SubFolder)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct().OrderBy(s => s))
                SubFolders.Add(sub);

            SelectedSubFolder = "Tous";
        }

        private void UpdateSubSubFolders()
        {
            SubSubFolders.Clear();
            SubSubFolders.Add("Tous");

            var filtered = _allAssets.AsEnumerable();

            if (SelectedCategory != "Toutes")
                filtered = filtered.Where(a => a.Category == SelectedCategory);

            if (SelectedSubFolder != "Tous")
                filtered = filtered.Where(a => a.SubFolder == SelectedSubFolder);

            foreach (var sub in filtered
                .Select(a => a.SubSubFolder)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct().OrderBy(s => s))
                SubSubFolders.Add(sub);

            SelectedSubSubFolder = "Tous";
        }

        private void ApplyFilters()
        {
            var filtered = _allAssets.AsEnumerable();

            if (SelectedCategory != "Toutes")
                filtered = filtered.Where(a => a.Category == SelectedCategory);

            if (SelectedSubFolder != "Tous")
                filtered = filtered.Where(a => a.SubFolder == SelectedSubFolder);

            if (SelectedSubSubFolder != "Tous")
                filtered = filtered.Where(a => a.SubSubFolder == SelectedSubSubFolder);

            if (!string.IsNullOrEmpty(SearchText))
                filtered = filtered.Where(a =>
                    a.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            _filteredList = filtered.ToList();
            CurrentPage = 1;
            TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredList.Count / (double)PageSize));
            PageInfo = $"Page {CurrentPage} / {TotalPages}";

            LoadCurrentPage();
        }

        private void LoadCurrentPage()
        {
            FilteredAssets.Clear();

            var results = _filteredList
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Task.Run(() =>
            {
                foreach (var asset in results)
                {
                    if (asset.Thumbnail == null)
                    {
                        try
                        {
                            asset.Thumbnail = LoadThumbnail(asset.FullPath, 120);
                        }
                        catch { }
                    }
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var asset in results)
                        FilteredAssets.Add(asset);

                    PageInfo = $"Page {CurrentPage} / {TotalPages}";
                });
            });
        }

        [RelayCommand]
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadCurrentPage();
            }
        }

        [RelayCommand]
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadCurrentPage();
            }
        }

        [RelayCommand]
        private void SelectAsset(AssetItem asset)
        {
            SelectedAsset = asset;
        }

        [RelayCommand]
        private void OpenFolder()
        {
            if (SelectedAsset == null) return;
            var folder = Path.GetDirectoryName(SelectedAsset.FullPath);
            if (folder != null && Directory.Exists(folder))
                System.Diagnostics.Process.Start("explorer.exe", folder);
        }

        [RelayCommand]
        private async Task Refresh()
        {
            if (!string.IsNullOrEmpty(_exportFolder))
                await LoadAssetsAsync(_exportFolder);
        }
    }
}