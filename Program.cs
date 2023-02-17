using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Serilog;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using HeightMapExtractor;
using ImageMagick;

class ImageTile
{
    public FVector Position;
    public MagickImage Image;
    public int ChannelIndex;
}

static class Program
{
    static Dictionary<string, FVector> infos = new();
    static Dictionary<string, int> weightMapChannelInfo = new();
    static List<string> AvailableLayers = new List<string>();
    static FVector imageBounds = new(0);
    static List<ImageTile> heightTiles = new();
    static Dictionary<string, List<ImageTile>> weightTiles = new();
    public static Config config;
    public static int ComponentSize = -1;
    public static string WorldName;

    static void DoThings(FPackageIndex Actor)
    {
        var LoadedActor = Actor.Load();
        if (LoadedActor != null)// && LoadedActor?.ExportType == "LandscapeStreamingProxy" || LoadedActor?.ExportType == "LevelStreamingAlwaysLoaded")
        {
            var LandscapeComps = LoadedActor.GetOrDefault("LandscapeComponents", Array.Empty<UObject>());
            foreach (var comp in LandscapeComps)
            {
                var SectionBaseX = comp.GetOrDefault("SectionBaseX", 0);
                var SectionBaseY = comp.GetOrDefault("SectionBaseY", 0);
                // var RelativeLocation  = comp.GetOrDefault("RelativeLocation", new FVector());
                var NumSections = comp.GetOrDefault("NumSubsections", 1);
                var pos = new FVector(SectionBaseX, SectionBaseY, 0);
                pos = pos * 1f / NumSections;

                var ComponentSizeQuads = comp.GetOrDefault("ComponentSizeQuads", 8);
                if (ComponentSize != -1)
                    Debug.Assert(ComponentSize == ComponentSizeQuads);
                ComponentSize = ComponentSizeQuads;

                imageBounds.X = Math.Max(imageBounds.X, Math.Abs(pos.X)+ComponentSize);
                imageBounds.Y = Math.Max(imageBounds.Y, Math.Abs(pos.Y)+ComponentSize);
                if (comp.TryGetValue<UTexture2D>(out var HeightmapTex, "HeightmapTexture"))
                {
                    var imagetile = HeightmapTex.Decode();
                    var path = new FileInfo(Path.Combine(config.ExportDirectory, "Tiles", HeightmapTex.Owner.Name.SubstringAfter("/"),
                        HeightmapTex.Name + ".png"));
                    if (config.ExportTiles)
                    {
                        path.Directory.Create();
                        imagetile.Write(path, MagickFormat.Png);
                    }
                    heightTiles.Add(new ImageTile { Image = imagetile, ChannelIndex = -1, Position = pos });
                }
                if (config.ExportWeightMaps && comp.TryGetValue<UTexture2D[]>(out var WeightmapTextures, "WeightmapTextures"))
                {
                    var WeightmapLayerAllocations = comp.GetOrDefault<FStructFallback[]>("WeightmapLayerAllocations");
                    foreach (var weightmapLayer in WeightmapLayerAllocations)
                    {
                        var layerInfo = weightmapLayer.GetOrDefault<UObject>("LayerInfo", null);
                        Debug.Assert(layerInfo != null);
                            // continue;

                        int WeightMapChannelIndex = weightmapLayer.GetOrDefault<byte>("WeightmapTextureChannel", 0);
                        int WeightMapIndex = weightmapLayer.GetOrDefault<byte>("WeightmapTextureIndex", 0);

                        var WeightmapTexture = WeightmapTextures[WeightMapIndex];

                        var imagetile = WeightmapTexture.Decode();
                        var path = new FileInfo(Path.Combine(config.ExportDirectory, "Tiles", WeightmapTexture.Owner.Name.SubstringAfter("/"),
                            WeightmapTexture.Name + ".png"));
                        if (config.ExportTiles)
                        {
                            path.Directory.Create();
                            imagetile.Write(path, MagickFormat.Png);
                        }

                        if (!weightTiles.ContainsKey(layerInfo.Name))
                            weightTiles[layerInfo.Name] = new List<ImageTile>();

                        weightTiles[layerInfo.Name].Add(new ImageTile { Image = imagetile, ChannelIndex = WeightMapChannelIndex, Position = pos});
                    }
                }
                // break;
            }
        }
    }

    static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Error()
            .WriteTo.Console()
            .CreateLogger();

        config = JsonConvert.DeserializeObject<Config>(File.OpenText("config.json").ReadToEnd());

        var provider = new DefaultFileProvider(config.PaksDirectory, SearchOption.AllDirectories, true, new VersionContainer(config.Game, optionOverrides: config.OptionsOverrides));
        provider.Initialize();
        provider.MappingsContainer = new FileUsmapTypeMappingsProvider(
            @"C:\Users\Minshu\Documents\BlenderUmap\mappings\FortniteRelease-23.40-CL-24087481-Android_oo.usmap");

        // provider.SubmitKey(new FGuid(0), new FAesKey("0x0000000000000000000000000000000000000000000000000000000000000000"));

        var keysToSubmit = new Dictionary<FGuid, FAesKey>();

        foreach (var entry in config.EncryptionKeys) {
            if (!string.IsNullOrEmpty(entry.FileName)) {
                var foundGuid = provider.UnloadedVfs.FirstOrDefault(it => it.Name == entry.FileName);

                if (foundGuid != null) {
                    keysToSubmit[foundGuid.EncryptionKeyGuid] = new FAesKey(entry.Key);
                } else {
                    Log.Warning("PAK file not found: {0}", entry.FileName);
                }
            } else {
                keysToSubmit[entry.Guid] = new FAesKey(entry.Key);
            }
        }

        provider.SubmitKeys(keysToSubmit);

        if (File.Exists(config.MappingsFile))
        {
            provider.MappingsContainer = new FileUsmapTypeMappingsProvider(config.MappingsFile);
        }

        var world = provider.LoadObject<UWorld>(config.ExportPackage);
        WorldName = world.Name;
        config.ExportDirectory = Path.Join(config.ExportDirectory, WorldName);

        var actors = world.PersistentLevel.Load<ULevel>()!.Actors;
        for (var i = 0; i < actors.Length; i++)
        {
            var actor = actors[i];
            ProgressBar(WorldName, i + 1, actors.Length);
            DoThings(actor);
        }
        Console.WriteLine();

        for (var i = 0; i < world.StreamingLevels.Length; i++)
        {
            var level = world.StreamingLevels[i];
            var uWorld = level.Load().Get<UWorld>("WorldAsset");
            var persLevel = uWorld.PersistentLevel.Load<ULevel>();
            for (var j = 0; j < persLevel.Actors.Length; j++)
            {
                ProgressBar($"{i+1}/{world.StreamingLevels.Length}: {uWorld.Name}", j+1, persLevel.Actors.Length);
                var actor = persLevel.Actors[j];
                DoThings(actor);
            }
            Console.WriteLine();
        }

        Console.WriteLine($"Found {heightTiles.Count} height tiles");
        foreach (var weightTile in weightTiles)
        {
            Console.WriteLine($"{weightTile.Key} : {weightTile.Value.Count} tiles");
        }

        provider.Dispose();
        var dir = new DirectoryInfo(config.ExportDirectory);
        dir.Create();

        // imageBounds *= 2;

        foreach (var weightTile in weightTiles)
        {
            var image = new MagickImage(MagickColors.Transparent, (int)imageBounds.X, (int)imageBounds.Y);
            foreach (var tile in weightTile.Value)
            {
                var channelImage = tile.ChannelIndex switch
                {
                    0 => tile.Image.Separate(Channels.Red),
                    1 =>  tile.Image.Separate(Channels.Green),
                    2 =>  tile.Image.Separate(Channels.Blue),
                    3 =>  tile.Image.Separate(Channels.Alpha),
                    _ => throw new ArgumentOutOfRangeException()
                };

                image.Composite(new MagickImage(channelImage.First()), (int)tile.Position.X, (int)tile.Position.Y, CompositeOperator.Over);
            }

            var f = File.OpenWrite(Path.Join(dir.ToString(), string.Concat(weightTile.Key, ".png")));
            image.Write(f, MagickFormat.Png48);
            f.Close();
        }

        if (heightTiles.Count == 0)
        {
            Console.WriteLine("No height tiles found");
            Environment.Exit(1);
        }

        var height = new MagickImage(MagickColors.Black, (int)imageBounds.X, (int)imageBounds.Y);
        var normal = new MagickImage(MagickColors.Black, (int)imageBounds.X, (int)imageBounds.Y);
        foreach (var heightTile in heightTiles)
        {
            // extract r and b channels
            var rgbas = heightTile.Image.GetPixels().ToByteArray(PixelMapping.RGBA);

            // height = (((uint16)TexData.R) << 8) | TexData.G;
            var heightData = new ushort[rgbas.Length / 4];
            // Normal data R G B (255)
            var normalData = new byte[(rgbas.Length/4) * 3];
            // RGBA
            for (var i = 0; i < rgbas.Length; i += 4)
            {
                heightData[i / 4] = (ushort)((rgbas[i] << 8) + rgbas[i + 1]); // see FLandscapeEditDataInterface::GetHeightDataTemplFast

                normalData[i / 4 * 3] = rgbas[i + 2]; // B-> R
                normalData[i / 4 * 3 + 1] = rgbas[i + 3]; // A-> G
                normalData[i / 4 * 3 + 2] = BCDecoder.GetZNormal(rgbas[i + 2], rgbas[i + 3]); // B
            }

            var readSettings = new MyPixelReadSettings
            {
                StorageType = StorageType.Short,
                Mapping = "R",
                ReadSettings = new MagickReadSettings()
                {
                    Format = MagickFormat.Gray,
                    Width = heightTile.Image.Height,
                    Height = heightTile.Image.Width,
                    ColorSpace = ColorSpace.sRGB
                }
            };

            // handle negative positions
            var posx = (int)heightTile.Position.X; //(int)(Math.Floor(imageBounds.X / 2) - heightTile.Position.X);
            var posy = (int)heightTile.Position.Y; //(int)(Math.Floor(imageBounds.Y / 2) - heightTile.Position.Y);
            // int posx = (int)(Math.Floor(imageBounds.X / 2) - heightTile.Position.X);
            // int posy = (int)(Math.Floor(imageBounds.Y / 2) - heightTile.Position.Y);
            // Debug.Assert(posy > 0 && posx > 0);

            byte[] heightBuffer = new byte[heightData.Length * 2];
            Buffer.BlockCopy(heightData, 0, heightBuffer, 0, heightData.Length * 2);
            height.Composite(new MagickImage(heightBuffer, readSettings), (int)posx, posy, CompositeOperator.Over);

            var normalReadSettings = new MyPixelReadSettings
            {
                StorageType = StorageType.Char,
                Mapping = "RGB",
                ReadSettings = new MagickReadSettings()
                {
                    Format = MagickFormat.Rgb,
                    Width = heightTile.Image.Height,
                    Height = heightTile.Image.Width,
                    ColorSpace = ColorSpace.Undefined
                }
            };

            // byte[] normalBuffer = new byte[normalData.Length * 2 * 3];
            // Buffer.BlockCopy(normalData, 0, normalBuffer, 0, normalData.Length * 2*3);
            // Console.WriteLine($"X:{posx}, Y:{posy}");
            normal.Composite(new MagickImage(normalData, normalReadSettings), posx, posy, CompositeOperator.Over);
        }

        var f2 = File.OpenWrite(Path.Join(dir.ToString(), string.Concat("Height", ".png")));
        height.Write(f2, MagickFormat.Png64);
        f2.Close();
        var f3 = File.OpenWrite(Path.Join(dir.ToString(), string.Concat("Normal", ".png")));
        normal.Write(f3, MagickFormat.Png64);
        f3.Close();

        // var infos_s = JsonConvert.SerializeObject(infos, Formatting.Indented);
        // var infos_tex_s = JsonConvert.SerializeObject(weightMapChannelInfo, Formatting.Indented);

        // File.WriteAllText(@"C:\WindowsOld\Users\Minshu\Desktop\Games\aes\HeightMapExtractor\Test\infos.json", infos_s);
        // File.WriteAllText(@"L:\Coding Stuff\UmapTracker\weightMapChannelInfo.json", infos_tex_s);
        return;
    }

    private static void ProgressBar(string message, int progress, int tot)
    {
        //draw empty progress bar
        Console.CursorLeft = 0;
        Console.Write($"{message} ["); //start
        Console.CursorLeft = message.Length+32;
        Console.Write("]"); //end
        Console.CursorLeft = message.Length+2;
        float onechunk = 30.0f / tot;

        //draw filled part
        int position = message.Length+2;
        for (int i = 0; i < onechunk * progress; i++)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.CursorLeft = position++;
            Console.Write(" ");
        }

        // draw unfilled part
         for (int i = position; i <= 31; i++)
         {
             Console.BackgroundColor = ConsoleColor.Gray;
             Console.CursorLeft = position++;
             Console.Write(" ");
         }

        //draw totals
        Console.CursorLeft = message.Length + 35;
        Console.BackgroundColor = ConsoleColor.Black;
        Console.Write(progress.ToString() + " of " + tot.ToString() + "    "); //blanks at the end remove any excess
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
    public string PaksDirectory = "C:\\Program Files\\Epic Games\\Fortnite\\FortniteGame\\Content\\Paks";
    [JsonProperty("UEVersion")]
    public EGame Game = EGame.GAME_UE4_LATEST;
    public Dictionary<string, bool> OptionsOverrides = new Dictionary<string, bool>();
    public List<EncryptionKey> EncryptionKeys = new();
    public string ExportPackage;
    public string ExportDirectory;
    public string MappingsFile;
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