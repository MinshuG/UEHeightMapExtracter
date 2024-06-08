namespace HeightMapExtractorGUI.Models;

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;


public static class FileFolder
{
    // folder picker
    // no multiple folder selection
    public static async Task<IStorageFolder?> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        // folder browser dialog
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        var folders = await provider.OpenFolderPickerAsync(options);
        
        return folders.Count >= 1 ? folders[0] : null;
    }

    // file picker
    // no multiple file selection
    public static async Task<IStorageFile?> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        // file browser dialog
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        var files = await provider.OpenFilePickerAsync(options);
        
        return files.Count >= 1 ? files[0] : null;
    }
}