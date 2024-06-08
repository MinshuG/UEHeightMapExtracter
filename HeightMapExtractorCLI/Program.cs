using CUE4Parse_Conversion;
using CUE4Parse_Extensions;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using HeightMapExtractor;
using Serilog.Core;
using Serilog.Events;


static class Program
{
    public static Config config;

    private static ILogger Log = Serilog.Log.ForContext("SourceContext", "Program.cs");
    public static string ExportingWorldName { get; set; }

    static void Main(string[] args)
    {
        Utils.RegisterAssembly();

        var ls = new LoggingLevelSwitch();

        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(ls)
            .WriteTo.Console()
            .CreateLogger();

        ls.MinimumLevel = LogEventLevel.Information;
        Log.Information("Starting HeightMap Exporter...");
        config = GetConfig();
        // ObjectTypeRegistry.RegisterEngine(typeof(ALandscape).Assembly);

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
            Log.Information("No landscapes found. Press any key to exit.");
#if !DEBUG
            Console.ReadLine();
#endif
            return;
        }
        extractor.Save();
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


class InstalledData
{
    public AppData[] InstallationList;

    public class AppData
    {
        public string InstallLocation;
        public string NamespaceId;
    }
}

class BenbotAES
{
    public string mainKey;
    public KeyEntry[] dynamicKeys;

    public class KeyEntry
    {
        public string fileName;
        public string guid;
        public string key;
    }
}