using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Windowing;
using HeightMapExtractorGUI.ViewModels;

namespace HeightMapExtractorGUI.Views;

public partial class LoadingWindow : AppWindow
{
    public LoadingWindow()
    {
        InitializeComponent();
        
        TitleBar.ExtendsContentIntoTitleBar = true;
        Width = 300;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        DataContext = new LoadingViewModel();
    }
}