using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Objects.Core.Misc;
using HeightMapExtractorGUI.Models;

namespace HeightMapExtractorGUI.ViewModels;

public partial class AesKeysViewModel: ViewModelBase // wrapper for AesKeyFormat
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _key;
    [ObservableProperty] private string _encryptionKeyGuid;

    public AesKeysViewModel(string name, string key, string encryptionKeyGuid)
    {
        Name = name;
        Key = key;
        EncryptionKeyGuid = encryptionKeyGuid;
    }

    public AesKeysViewModel(AesKeyFormat aesKeyFormat)
    {
        Name = aesKeyFormat.Name;
        Key = aesKeyFormat.Key;
        EncryptionKeyGuid = aesKeyFormat.EncryptionKeyGuid.ToString();
    }

    public AesKeyFormat ToAesKeyFormat()
    {
        return new AesKeyFormat(Name, Key, new FGuid(EncryptionKeyGuid));
    }
}