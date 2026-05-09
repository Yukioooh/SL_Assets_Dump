using CommunityToolkit.Mvvm.ComponentModel;

namespace SL_Asset_Extractor.UI.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = "Prêt";
    }
}