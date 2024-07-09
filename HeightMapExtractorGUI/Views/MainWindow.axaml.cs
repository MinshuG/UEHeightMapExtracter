using Avalonia.Controls;
using FluentAvalonia.UI.Windowing;

namespace HeightMapExtractorGUI.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        Title = $"HeightMapExtractor v{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}";
        InitializeComponent();
    }
}