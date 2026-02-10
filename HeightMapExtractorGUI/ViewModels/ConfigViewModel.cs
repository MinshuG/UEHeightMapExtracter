using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Versions;
using HeightMapExtractor;
using HeightMapExtractorGUI.Models;
using HeightMapExtractorGUI.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace HeightMapExtractorGUI.ViewModels;

public partial class ConfigViewModel : ViewModelBase
{
    [ObservableProperty] private string? _unrealVersion;
    [ObservableProperty] private ObservableCollection<string> _directories;
    [ObservableProperty] private string? _selectedDirectory;
    [ObservableProperty] private string? _usmapPath;
    [ObservableProperty] private bool _bOverridePackageVer; 
    [ObservableProperty] private string? _ue5Ver;
    [ObservableProperty] private string? _ue4Ver;
    
    [ObservableProperty] private string? _platform;

    [ObservableProperty] private bool _validConfig; // is valid config
    [ObservableProperty] private bool _configErrorBar;
    [ObservableProperty] private string _configErrors;
    
    private MainWindowViewModel _mainWindowViewModel = null!;

    public ConfigViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        Directories = new ObservableCollection<string>();
        _validConfig = IsValidConfig();
        _configErrorBar = false;
        _configErrors = "";
        _bOverridePackageVer = false;

#if DEBUG
        UnrealVersion = "GAME_UE5_0";
        Directories.Add("F:\\Fortnite Versions\\19.10\\FortniteGame\\Content\\Paks");
        UsmapPath = "C:\\Users\\Minshu\\Downloads\\19.10_oo.usmap";
        _ue4Ver = "522 - CORRECT_LICENSEE_FLAG";
        _ue5Ver = "1000 - INITIAL_VERSION";
        _platform = "0 - DesktopMobile";
#endif
    }

    public ConfigViewModel()
    {
        Debug.Assert(Design.IsDesignMode);
        Directories = new ObservableCollection<string>();
        _validConfig = IsValidConfig();
        _configErrorBar = false;
        _configErrors = "";

        if (Design.IsDesignMode) // hmm
        {
            Directories.Add("C:\\Games\\Fortnite\\FortniteGame\\Content\\Paks");
            UnrealVersion = "GAME_UE5_4";
        }
    }

    [RelayCommand]
    private async Task AddExtraDirectory()
    {
        var folder = await FileFolder.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            { AllowMultiple = false, Title = "Select a folder" });
        if (folder != null)
        {
            var path = folder.Path.LocalPath;
            if (Directories.Contains(path))
            {
                // TODO: Add logic to display error message
                return;
            }

            Directories.Add(folder.Path.LocalPath);
            SelectedDirectory = folder.Path.LocalPath;
        }

        return;
    }

    [RelayCommand]
    private void RemoveExtraDirectory()
    {
        if (SelectedDirectory != null)
        {
            // remove the select and select the next one
            var index = Directories.IndexOf(SelectedDirectory);
            Directories.Remove(SelectedDirectory);
            if (index < Directories.Count)
            {
                SelectedDirectory = Directories[index];
            }
            else if (Directories.Count > 0)
            {
                SelectedDirectory = Directories[^1];
            }
        }
    }

    [RelayCommand]
    private async Task SelectMappings()
    {
        var file = await FileFolder.OpenFilePickerAsync(
            new FilePickerOpenOptions()
            {
                Title = "Select a .usmap file",
                FileTypeFilter = new List<FilePickerFileType>()
                {
                    new FilePickerFileType("Unreal Engine Mappings")
                    {
                        Patterns = new List<string>() { "*.usmap" }
                    }
                }
            });
        if (file != null)
        {
            UsmapPath = file.Path.LocalPath;
        }
    }

    [RelayCommand]
    private async Task Done()
    {
        if (IsValidConfig())
        {
            // var config = ToConfig();
            // _mainWindowViewModel?.Window.Hide();
            var loadingviewWindow = new LoadingWindow();
            loadingviewWindow.ShowInTaskbar = false;

            loadingviewWindow.ShowDialog(AppHelper.MainWindow).ConfigureAwait(false);
            var context = (LoadingViewModel)loadingviewWindow.DataContext!;
            if (Design.IsDesignMode) return;

            var config = ToConfig();
            context.SetProgressText("Starting...");

            MyFileProvider fileProvider;
            try
            {
                fileProvider = MyFileProvider.Create(config);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to create file provider");
                var box = MessageBoxManager
                    .GetMessageBoxStandard("Error", "Failed to create file provider. Check logs for details.", ButtonEnum.Ok);
                var _ = await box.ShowAsync();
                loadingviewWindow.Close();
                return;
            }

            context.SetProgressText("Initializing...");
            await Task.Run(() =>fileProvider.Initialize()).ConfigureAwait(true);
            context.SetProgressText(fileProvider.UnloadedVfs.Count > 0 ? $"Found: {fileProvider.UnloadedVfs.Count} Containers" : "No Container Found.");
            await Task.Delay(300);
            if (fileProvider.UnloadedVfs.Count > 0)
            {
                // var aesKeys = fileProvider.GetRequiredAesKeys();
                context.SetProgressText($"Waiting for AES Key...");
                var aesKeyDemandView = new AesKeyDemandWindow();

                await aesKeyDemandView.ShowDialog(loadingviewWindow).ConfigureAwait(true);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (!(aesKeyDemandView.DataContext as AesKeyDemandViewModel).WasOkClicked)
                {
                    var box = MessageBoxManager
                        .GetMessageBoxStandard("welp", "sigh", ButtonEnum.Ok);
                    var  _ = await box.ShowAsync();
                    fileProvider.Dispose();
                    loadingviewWindow.Close();
                    return;
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }

            context.SetProgressText("Mounting...");
            var result = await fileProvider.SubmitKeysAsync2();
            fileProvider.PostMount();
            
            fileProvider.LoadVirtualPaths();
            var countVars = fileProvider.LoadConsoleVariables();

            context.SetProgressText($"Mounted {result} containers.");

            context.SetProgressText("Populating TreeView...");
            {
                var mainView = new MainAppViewModel();
                // await Task.Run(() => mainView.PopulateTreeView(fileProvider.Files));
                _mainWindowViewModel.ContentView = mainView;
            }
            context.SetProgressText("Done.");
            loadingviewWindow.Close();
            await Task.Delay(500);
        }
        else
        {
            var errors = ValidateConfigAndGetErrors();
            ConfigErrors = string.Join("\n", errors);
            ConfigErrorBar = true;
        }
    }

    private string[] ValidateConfigAndGetErrors()
    {
        var errors = new List<string>();
        
        if (UnrealVersion == null)
        {
            errors.Add("Unreal Engine version is not selected.");
        }
        
        // if (Game == null)
        // {
        //     errors.Add("Game is not selected.");
        // }
        
        if (!string.IsNullOrEmpty(UsmapPath) && !System.IO.File.Exists(UsmapPath))
        {
            errors.Add("Mappings file does not exist.");
        }
        
        if (Directories.Count == 0)
        {
            errors.Add("No pak directories are added.");
        }
        else if (Directories.Any(x => !System.IO.Directory.Exists(x)))
        {
            errors.Add("One or more pak folders do not exist.");
        }
        else if (BOverridePackageVer) 
        {
            if (Ue4Ver == null || Ue5Ver == null)
            {
                errors.Add("UE4 or UE5 version is not selected. Please select a version or disable package version override.");
            }
        }

        return errors.ToArray();
    }
    
    private bool IsValidConfig()
    {
        // return true;
        if (ConfigErrorBar)
        {
            var errors = ValidateConfigAndGetErrors();
            ConfigErrors = string.Join("\n", errors);
        }

        if (Platform == null)
        {
            return false;
        }

        if (UnrealVersion == null)
        {
            return false;
        }
        
        if (!string.IsNullOrEmpty(UsmapPath) && !System.IO.File.Exists(UsmapPath))
        {
            return false;
        }

        if (Directories.Count == 0 || Directories.Any(x => !System.IO.Directory.Exists(x)))
        {
            return false;
        }

        if (BOverridePackageVer)
        {
            if (Ue4Ver == null || Ue5Ver == null)
            {
                return false;
            }
        }

        // ConfigErrorBar = false; // if we got here, the config is valid
        return true;
    }

    public Config ToConfig()
    {
        Trace.Assert(IsValidConfig());
        VersionContainer version;

        var platform = int.Parse(Platform!.Split(" - ")[0]);

        if (BOverridePackageVer)
        {
            var UE4Ver = Ue4Ver == "AUTOMATIC_VERSION" ? "0" : Ue4Ver!.Split(" - ")[0];
            var UE5Ver = Ue5Ver == "AUTOMATIC_VERSION" ? "0" : Ue5Ver!.Split(" - ")[0];
            
            var v = new FPackageFileVersion(int.Parse(UE4Ver), int.Parse(UE5Ver));
            version = new VersionContainer((EGame)Enum.Parse(typeof(EGame), UnrealVersion!),
                (ETexturePlatform)platform, v);
        }
        else
        {
            version = new VersionContainer((EGame)Enum.Parse(typeof(EGame), UnrealVersion), (ETexturePlatform)platform);
        }

        return new Config()
        {
#pragma warning disable CS8604 // Possible null reference argument.
            Version = version,
#pragma warning restore CS8604 // Possible null reference argument.
            Directories = Directories.Select(x => new System.IO.DirectoryInfo(x)).ToArray(),
            UsmapPath = UsmapPath ?? ""
        };
    }
}