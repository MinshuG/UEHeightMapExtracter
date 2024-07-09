using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.Utils;
using DynamicData;
using HeightMapExtractorGUI.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using Serilog;

namespace HeightMapExtractorGUI.ViewModels;


public partial class MyTreeViewItem : ViewModelBase, IEquatable<string>
{
    [ObservableProperty]
    private string _content;
    [ObservableProperty] private GameFile _gameFile;
    [ObservableProperty] private long _fileSize;

    public MyTreeViewItem? Parent { get; set; }

    public MyTreeViewItem(GameFile file)
    {
        GameFile = file;
        Content = file.Path;
        FileSize = file.Size;
    }

    public MyTreeViewItem(string path)
    {
        Debug.Assert(Design.IsDesignMode);
        Content = path;
        GameFile = null!;
        FileSize = new Random(path.GetHashCode()).Next();
    }

    public string GetPath()
    {
        return GameFile.Path;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MyTreeViewItem)obj);
    }
    
    public override int GetHashCode()
    {
        return GameFile.GetHashCode();
    }

    // == and != operators
    public static bool operator ==(MyTreeViewItem left, MyTreeViewItem right)
    {
        return ReferenceEquals(left, right);
    }

    public static bool operator !=(MyTreeViewItem left, MyTreeViewItem right)
    {
        return !(left == right);
    }

    protected bool Equals(MyTreeViewItem? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return GameFile == other.GameFile;
    }

    public bool Equals(string? other)
    {
        return GameFile.Name == other;
    }
}


public partial class MainAppViewModel: ViewModelBase
{
    [ObservableProperty]
    private BulkObservableCollection<MyTreeViewItem> _gameFiles;

    [ObservableProperty]
    private BulkObservableCollection<MyTreeViewItem> _filteredGameFiles;

    [ObservableProperty]
    private MyTreeViewItem? _selectedGameFile;
    
    [ObservableProperty]
    private string _searchText;
    
    [ObservableProperty]
    private bool _isBusy;
    
    [ObservableProperty]
    private bool _isExtracting;

    [ObservableProperty]
    private bool _canClickExtract;

    public MainAppViewModel()
    {
        GameFiles = new BulkObservableCollection<MyTreeViewItem>();
        FilteredGameFiles = new BulkObservableCollection<MyTreeViewItem>();
        SelectedGameFile = null;
        SearchText = "";
        IsBusy = false;
        
        CanOpenConsole = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        if (Design.IsDesignMode)
        {
            var paths = new string[]
            {
                "C:/Folder1/File1.txt",
                "C:/Folder1/File1.txt",
                "C:/Folder1/Folder2/File2.txt",
                "C:/Folder2/File3.txt",
                "C:/Folder2/Subfolder1/File4.txt",
                "C:/Folder2/Subfolder1/Filew4.umap",
                "C:/Folder2/Subfolder1/Fiweqle4.umap",
                "C:/Folder2/Subfowqeqlder1/Fiqwewqle4.umap",
                "C:/Folder2/Subfolder1/File4.umap",
                "C:/Folder2/Subfqesdolder1/File4.umap",
                "C:/Folder2/Subfoalder1/File4.umap",
                "C:/Folder2/Subsdafolder1/File4.umap",
                "C:/Folder2/Subfolder1/File4.umap",
                "C:/Folder2/Subfolder1/File4.umap",
                "C:/Folder2/SubfuolGZder1/File4.umap",
                "C:/Folder2/Subfuo324lGZder1/File4.umap",
                "C:/Folder2/Sub434fuolGZder1/File4.umap",
                "C:/Folder2/SubfuolGZ2421der1/File4.umap",
                "C:/Folder2/SubfuolGZder1/File4.umap",
                "C:/Folder2/SubfuolGZ34der1/File4.umap",
                "C:/Folder2/SubfuolGZde341/File4.umap",
                "C:/Folder2/SubfuolGZder1/File4.umap",
                "C:/Folder2/SubfuolGZder1/File4.umap",
                "C:/Folder2/SubfuolGZd3er1/File4.umap",
                "C:/Folder2/Subtrfolder1/File4.uexp"
            };
            
            var items2 = new List<MyTreeViewItem>();
            foreach (var path in paths)
            {
                items2.Add(new MyTreeViewItem(path));
            }
            GameFiles.AddRange(items2);
            FilteredGameFiles.AddRange(items2);
            return;
        }

        var items = new List<MyTreeViewItem>();
        var allowedExtensions = new[] { "umap" };
        foreach (var keyValuePair in MyFileProvider.GetInstance().Files)
        {
            if (allowedExtensions.Contains(keyValuePair.Value.Extension, StringComparer.InvariantCultureIgnoreCase))
                items.Add(new MyTreeViewItem(keyValuePair.Value));
        }
        // items.Sort((x, y) => string.Compare(x.GetPath(), y.GetPath(), StringComparison.Ordinal)); // done by DataGrid
        GameFiles.AddRange(items);

        this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(UpdateFilter!);
        UpdateFilter(SearchText);
    }

    partial void OnIsExtractingChanged(bool value)
    {
        CanClickExtract = !value && SelectedGameFile != null;
    }

    partial void OnSelectedGameFileChanged(MyTreeViewItem? value)
    {
        CanClickExtract = value is not null && !IsExtracting;
    }

    private async void UpdateFilter(string search)
    {
        IsBusy = true;
        if (string.IsNullOrEmpty(search))
        {
            FilteredGameFiles.Clear();
            FilteredGameFiles.AddRange(GameFiles);
            IsBusy = false;
            return;
        }

        var filterText = search.ToLower().Split(" ");
        var filteredItems = GameFiles.Where(x => filterText.All(y => x.GetPath().Contains(y, StringComparison.InvariantCultureIgnoreCase))).ToArray();
        FilteredGameFiles.Clear();
        // FilteredGameFiles.Add();
        FilteredGameFiles.AddRange(filteredItems);
        IsBusy = false;
    }

    [RelayCommand]
    private void ExtractHeightmap()
    {
        if (SelectedGameFile == null) return;
        
        // var saveFolder = new OpenFolderDialog();

        IsExtracting = true;
        var folder = FileFolder.OpenFolderPickerAsync(new FolderPickerOpenOptions() { AllowMultiple = false, Title = "Select Export Folder" }).ContinueWith(
            task =>
            {
                var folder = task.Result;
                if (folder is null) return;

                var file = SelectedGameFile.GetPath();
                {
                    try
                    {
                        var extractor = new HeightMapExtractor.HeightMapExtractor(MyFileProvider.GetInstance(), folder.Path.LocalPath, true);
                        extractor.ProcessWorld(file.SubstringBeforeLast("."), null);
                        extractor.Save();
                    }
                    catch (Exception e)
                    {
                        if (e is OperationCanceledException) return;
                        Log.Error(e, "Failed to export {GameFile}", file);
                    }    
                }
            }).ContinueWith(task2 => { IsExtracting = false; } ).ConfigureAwait(false);
    } 
    
    [ObservableProperty] private bool _consoleStatus;
    [ObservableProperty] private bool _canOpenConsole; // win only
    [RelayCommand]
    private void ConsoleToggle()
    {
        if (Design.IsDesignMode) return;
        if (ConsoleStatus)
        {
            ConsoleHelper.ShowConsole();
        }
        else
        {
            ConsoleHelper.HideConsole();
        }
    }
}