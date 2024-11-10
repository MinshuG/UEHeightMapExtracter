using System;
using System.IO;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;

namespace HeightMapExtractorGUI.Models;

public class AesKeyFormat
{
    private string _name;
    public string Name
    {
        get
        {
            if (EncryptionKeyGuid == new FGuid(0))
                return "Main Static Key";
            if (string.IsNullOrEmpty(_name))
                return "Unknown";
            return _name;
        }
        set => _name = value;
    } // Name of the pak 1st
    public string Key { get; set; }
    public FGuid EncryptionKeyGuid { get; set; }

    public AesKeyFormat(string name, string key, FGuid encryptionKeyGuid)
    {
        _name = name;
        Key = key;
        EncryptionKeyGuid = encryptionKeyGuid;
    }
}

public class Config
{
    public VersionContainer Version { get; set; } = new VersionContainer();
    public DirectoryInfo[] Directories { get; set; } = Array.Empty<DirectoryInfo>();
    public string UsmapPath { get; set; } = string.Empty;
    public AesKeyFormat[] AesKeys { get; set; } = Array.Empty<AesKeyFormat>();
}