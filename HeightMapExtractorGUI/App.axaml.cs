using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HeightMapExtractor;
using HeightMapExtractorGUI.Models;
using HeightMapExtractorGUI.ViewModels;
using HeightMapExtractorGUI.Views;
using Serilog;
using Serilog.Events;

namespace HeightMapExtractorGUI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Utils.RegisterAssembly(); // register export types
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(AppHelper.LoggingLevelSwitch)
            .WriteTo.Console(LogEventLevel.Information)
            .CreateLogger();
        
        AppHelper.LoggingLevelSwitch.MinimumLevel = LogEventLevel.Information;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
            // desktop.MainWindow = new LoadingWindow()
            // {
            //     // DataContext = new MainWindowViewModel(),
            //     // DataContext = new AesKeyDemandViewModel(AesKeyDemandViewModel.GetDummyKeys()),
            // };
            AppHelper.MainWindow = desktop.MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}