using CommunityToolkit.Mvvm.ComponentModel;

namespace HeightMapExtractorGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _contentView;

    public MainWindowViewModel()
    {
        ContentView = new ConfigViewModel(this);
        // ContentView = new AesKeyDemandViewModel(AesKeyDemandViewModel.GetDummyKeys());
    }
}