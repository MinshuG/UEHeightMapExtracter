using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Objects.Core.Misc;
using HeightMapExtractorGUI.Internet;
using HeightMapExtractorGUI.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace HeightMapExtractorGUI.ViewModels;

public partial class AesKeyDemandViewModel: ViewModelBase
{
    [ObservableProperty] private ObservableCollection<AesKeysViewModel> _aesKeys;
    
    public bool WasOkClicked { get; private set; }

    [ObservableProperty] private bool _isFortnite;
    
    public AesKeyDemandViewModel(AesKeyFormat[] keys) // for testing
    {
        AesKeys = new ObservableCollection<AesKeysViewModel>();
        foreach (var key in keys)
        {
            AesKeys.Add(new AesKeysViewModel(key));
        }
        _isFortnite = true;
    }

    public AesKeyDemandViewModel()
    {
        AesKeys = new ObservableCollection<AesKeysViewModel>();
        var provider = MyFileProvider.GetInstance();
        foreach (var key in provider.GetRequiredAesKeys())
        {
            AesKeys.Add(new AesKeysViewModel(key));
        }
        _isFortnite = provider.UnloadedVfs.Any(x => x.Name.Contains("FortniteGame"));
    }

    // [RelayCommand]
    public void Ok()
    {
        if (Design.IsDesignMode) return;
        var provider = MyFileProvider.GetInstance();
        var keys = AesKeys.ToArray().Select(x=> x.ToAesKeyFormat()).ToArray();
        provider.SetAesKeys(keys);
        WasOkClicked = true;
        // _view.Close();
    }

    // [RelayCommand]
    public async void FetchFNButton(string? version)
    {
        if (Design.IsDesignMode) return;

        try
        {
            // check if version is valid (must be in parseable to float)
            if (!string.IsNullOrEmpty(version) && !float.TryParse(version, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                var box = MessageBoxManager
                    .GetMessageBoxStandard("Error", "invalid version entered.", ButtonEnum.Ok);
                var  _ = await box.ShowAsync();
                return;
            }
            
            var keysResp = await GenxGames.GetAesKeysAsync(version);
            
            foreach (var key in AesKeys)
            {
                var newKey = keysResp.DynamicKeys.FirstOrDefault(x => new FGuid(x.Guid) == new FGuid(key.EncryptionKeyGuid));
                if (newKey != null)
                {
                    key.Key = newKey.Key;
                }
            }

            var mainkey = AesKeys.FirstOrDefault(x => new FGuid(x.EncryptionKeyGuid) == new FGuid(0));
            
            if (mainkey != null && !string.IsNullOrEmpty(keysResp.MainKey))
            {
                mainkey.Key = keysResp.MainKey;
            }

            var box2 = MessageBoxManager.GetMessageBoxStandard("Success", "Keys fetched successfully.", ButtonEnum.Ok);
            _ = await box2.ShowAsync();

        }
        catch (Exception e)
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("Error", e.Message, ButtonEnum.Ok);
            var  _ = await box.ShowAsync();
        }
    }

    // dummy keys
    public static AesKeyFormat[] GetDummyKeys()
    {
        var keys = new AesKeyFormat[6];
        return new[]
        {
            new AesKeyFormat("xxxxxxxxxxxxxx.pak", "0x2615E4242EF7903730AE011FF6F49DA42B0EF62A101D63928D79A9BB8F245ABB", new FGuid(0)),
            new AesKeyFormat("pakchunk1000.pak", "0xB9D9532FD69B83C2C81B2B56A4444C1951293EB8FD8A8DB74761DB20D1ED177A", new FGuid(069268)),
            new AesKeyFormat("pakchunk1003xxxxxx.pak", "", new FGuid("2CEE3C1783B9E41EB66238BAD32EFF23")),
            new AesKeyFormat("xxxxxxxxxxxxxx.pak", "", new FGuid("3486B038CFC458884D3375743DBF5D53")),
            new AesKeyFormat("pakchunk1016.pak", "0x139D8B77682D33330550C91D3298CD5A6107120AF5AA71FE8A5887DA16B6EDD4", new FGuid(90942972))
        };
    }
}