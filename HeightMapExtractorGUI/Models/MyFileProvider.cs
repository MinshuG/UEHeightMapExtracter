using HeightMapExtractorGUI.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;


namespace HeightMapExtractorGUI.Models;

public sealed class MyFileProvider: DefaultFileProvider
{
    public static MyFileProvider? Instance { get; private set; }

    private Config ProviderConfig { get; }
    
    public static MyFileProvider Create(Config config)
    {
        if (Instance != null)
        {
            return Instance;
        }
        // thank you officer more adding one more step to the process
        if (!ZlibHelper.DownloadDll()) 
        {
            Log.Error("Failed to download zlib-ng2.dll");
            Log.Error("Please download it from {0} and place it in the same directory as this executable or working directory (export directory)", ZlibHelper.DOWNLOAD_URL);
        }
        else {
            ZlibHelper.Initialize(ZlibHelper.DLL_NAME);    
        }
        var provider = new MyFileProvider(config);
        Instance = provider;
        return provider;
    }
    
    public static MyFileProvider GetInstance()
    {
        if (Instance == null)
            throw new Exception("Instance is null");
        return Instance;
    }
    
    public static void Destroy()
    {
        Instance?.Dispose();
        Instance = null;
    }

    private MyFileProvider(Config config) 
        : base(config.Directories[0], config.Directories.Skip(1).ToArray(), SearchOption.AllDirectories, true, new VersionContainer(config.UnrealVersion))
    {
        Trace.Assert(Instance == null);
        // Instance = this;
        ProviderConfig = config;
        

        if (!string.IsNullOrEmpty(config.UsmapPath))
            MappingsContainer = new FileUsmapTypeMappingsProvider(config.UsmapPath);
    }

    public override void Initialize()
    {
        base.Initialize();
    }
 
    public async Task<int> SubmitKeysAsync2()
    {
        var keysDict = GetAesKeys();
        
        var ret = await SubmitKeysAsync(keysDict);
        PostMount();
        return ret;
    }

    private Dictionary<FGuid, FAesKey> GetAesKeys()
    {
        var keys = GetRequiredAesKeys();
        var dict = new Dictionary<FGuid, FAesKey>();
        foreach (var key in keys)
        {
            if (string.IsNullOrEmpty(key.Key)) continue;
            dict[key.EncryptionKeyGuid] = new FAesKey(key.Key);
        }

        return dict;
    }

    private string GetAesKeyString(FGuid keyGuid)
    {
        var key = ProviderConfig.AesKeys.FirstOrDefault(x => x.EncryptionKeyGuid == keyGuid); 
        if (string.IsNullOrEmpty(key?.Key))
        {
            var vfses = UnloadedVfs.ToList();
            vfses.AddRange(MountedVfs);
            var vfs = vfses.FirstOrDefault(x => x.EncryptionKeyGuid == keyGuid);
            if (vfs != null)
            {
                return vfs.AesKey?.KeyString ?? "";
            }
        }
        return key?.Key ?? "";
    }

    public AesKeyFormat[] GetRequiredAesKeys()
    {
        var keys = new List<AesKeyFormat>();
        
        foreach (var vfs in UnloadedVfs)
        {
            if (vfs is not { IsEncrypted: true } || keys.Any(x => x.EncryptionKeyGuid == vfs.EncryptionKeyGuid)) continue;
            
            var key = GetAesKeyString(vfs.EncryptionKeyGuid);
            
            keys.Add(new AesKeyFormat(vfs.Name, key, vfs.EncryptionKeyGuid));
        }

        var mainKey = keys.FirstOrDefault(x => x.EncryptionKeyGuid == new FGuid(0));
        
        // make sure the main key is always first
        if (mainKey != null)
        {
            keys.Remove(mainKey);
            keys.Insert(0, mainKey);

            if (string.IsNullOrEmpty(mainKey.Key))
            {
                mainKey.Key = "0x0000000000000000000000000000000000000000000000000000000000000000";
            }
        }
        else
        {
            // false sense
            // if we are here, then game is probably not encrypted
            keys.Add(new AesKeyFormat("", "0x0000000000000000000000000000000000000000000000000000000000000000", new FGuid(0)));
        }

        return keys.ToArray();
    }

    public void SetAesKeys(AesKeyFormat[] keys)
    {
        ProviderConfig.AesKeys = keys;
    }

    public void Dispose()
    {
        base.Dispose();
        Instance = null;
    }

    internal static UObject StolenConstructObject(UStruct? struc)
    {
        UObject? obj = null;
        var current = struc;
        while (current != null) // Traverse up until a known one is found
        {
            if (current is UClass scriptClass)
            {
                // We know this is a class defined in code at this point
                obj = scriptClass.ConstructObject();
                if (obj != null)
                    break;
            }

            current = current.SuperStruct?.Load<UStruct>();
        }

        obj ??= new UObject();
        obj.Class = struc;
        obj.Flags |= EObjectFlags.RF_WasLoaded;
        return obj;
    }
    
}