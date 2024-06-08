using CommunityToolkit.Mvvm.ComponentModel;

namespace HeightMapExtractorGUI.ViewModels;

public partial class LoadingViewModel: ViewModelBase
{
    [ObservableProperty] private string? _loadingText;
    
    public LoadingViewModel()
    {
        LoadingText = "Loading...";
    }

    public void SetProgressText(string text)
    {
        LoadingText = text;
    }
}