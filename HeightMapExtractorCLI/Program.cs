using System.Diagnostics;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using HeightMapExtractor;
using Serilog.Core;
using Serilog.Events;


static class Program
{
    public static Config config;

    public static string ExportingWorldName { get; set; }
    
    static Stopwatch PerformanceTimer = new();

    static void Main(string[] args)
    {
        PerformanceTimer.Start();
        Utils.RegisterAssembly();
        
        var ls = new LoggingLevelSwitch();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(ls)
            .WriteTo.Console()
            .CreateLogger();

        ls.MinimumLevel = LogEventLevel.Information;
        Log.Information("Starting HeightMap Exporter...");
        config = GetConfig();

        OodleHelper.Initialize();

        if (!ZlibHelper.DownloadDll())
        {
            Log.Error("Failed to download zlib-ng2.dll");
            Log.Error("Please download it from {0} and place it in the same directory as this executable or working directory (export directory)", ZlibHelper.DOWNLOAD_URL);
        }
        else
        {
            ZlibHelper.Initialize(ZlibHelper.DLL_NAME);
        }

        var provider = new DefaultFileProvider(config.PaksDirectory, SearchOption.AllDirectories, true,
            new VersionContainer(config.Game, optionOverrides: config.OptionsOverrides));
        provider.Initialize();

        var keysToSubmit = new Dictionary<FGuid, FAesKey>();

        foreach (var entry in config.EncryptionKeys)
        {
            if (!string.IsNullOrEmpty(entry.FileName))
            {
                var foundGuid = provider.UnloadedVfs.FirstOrDefault(it => it.Name == entry.FileName);

                if (foundGuid != null)
                {
                    keysToSubmit[foundGuid.EncryptionKeyGuid] = new FAesKey(entry.Key);
                }
                else
                {
                    Log.Warning("PAK file not found: {0}", entry.FileName);
                }
            }
            else
            {
                keysToSubmit[entry.Guid] = new FAesKey(entry.Key);
            }
        }

        provider.SubmitKeys(keysToSubmit);
        
        provider.LoadVirtualPaths();
        provider.PostMount();

        var countVars = provider.LoadConsoleVariables();

        // if none loaded things might not work properly
        var cVarLogString = $"Loaded {countVars} ConsoleVariables." +
                            (countVars == 0 ? " Assets might not load properly" : "");
        if (countVars == 0)
            Log.Warning(cVarLogString);
        else
            Log.Information(cVarLogString);

        if (File.Exists(config.MappingsFile))
        {
            provider.MappingsContainer = new FileUsmapTypeMappingsProvider(config.MappingsFile);
        }

        var extractor = new HeightMapExtractor.HeightMapExtractor(provider, config.ExportDirectory, true);
        extractor.ProcessWorld(config.ExportPackage, null);

        if (extractor.LandscapeComps.Count == 0)
        {
            Log.Information("Time taken: {0}s", PerformanceTimer.Elapsed.TotalSeconds);
            Log.Information("No landscapes found. Press any key to exit.");
#if !DEBUG
            Console.ReadLine();
#endif
            return;
        }
        var saveTimer = Stopwatch.StartNew();
        extractor.Save();
        saveTimer.Stop();
        Log.Information("Time taken to save: {0}s", saveTimer.Elapsed.TotalSeconds);
        Log.Information("Time taken: {0}s", PerformanceTimer.Elapsed.TotalSeconds);
#if !DEBUG
        Console.ReadLine();
#endif
    }

    static Config GetConfig()
    {
        var file = new FileInfo("config.json");
        
        if (!file.Exists)
        {
            Log.Information("{0} not found in working directory.", file.FullName);
            Console.ReadLine();
            Environment.Exit(0);
        }

        var fileHandle = file.OpenText();
        Log.Information($"Reading Config: {file.FullName}");
        return JsonConvert.DeserializeObject<Config>(fileHandle.ReadToEnd()) ?? throw new InvalidOperationException();
    }
}

public class EncryptionKey {
    public FGuid Guid;
    public string FileName;
    public string Key;

    public EncryptionKey() {
        Guid = new();
        Key = String.Empty;
    }

    public EncryptionKey(FGuid guid, string key) {
        Guid = guid;
        Key = key;
    }
}

public class Config {
    public string PaksDirectory = "";
    [JsonProperty("UEVersion")]
    public EGame Game = EGame.GAME_UE4_LATEST;
    public Dictionary<string, bool> OptionsOverrides = new Dictionary<string, bool>();
    public List<EncryptionKey> EncryptionKeys = new();
    public bool bScanSubLevels = true;
    public string ExportPackage;
    public string ExportDirectory = "";
    public string? MappingsFile;
    public bool ExportWeightMaps = true;
    public bool ExportTiles = true;
}