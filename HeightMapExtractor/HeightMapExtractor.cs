using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse_Extensions;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Serilog;

namespace HeightMapExtractor;

public class HeightMapExtractor
{
    public readonly Dictionary<FGuid, List<FPackageIndex>>
        LandscapeComps = new(); // FGuid -> ULandscapeComponent

    public readonly Dictionary<FGuid, List<ALandscapeProxy>>
        LandscapeActors = new(); // FGuid -> ULandscapeComponent

    public string ExportingWorldName { get; set; }
    public string ExportDirectory { get; set; }
    public bool bScanSubLevels;
    
    private readonly DefaultFileProvider _provider;

    public HeightMapExtractor(DefaultFileProvider provider, string exportDirectory, bool scanSubLevels = true)
    {
        _provider = provider;
        ExportDirectory = exportDirectory;
        bScanSubLevels = scanSubLevels;
    }

    public void ProcessWorld(string worldPath, TreeNode<string>? loadedLevels)
    {
        if (!_provider.TryLoadObject(worldPath, out UWorld world)) return;
        var worldName = _provider.CompactFilePath(world.GetPathName());
        if (loadedLevels == null)
        {
            ExportingWorldName = world.Name;
            ExportDirectory = Path.Join(ExportDirectory, ExportingWorldName);
            loadedLevels = new TreeNode<string>(worldName);
        }
        else
        {
            var node = loadedLevels.FindTreeNodeInParentNodes(worldName);
            if (node != null)
            {
                Log.Information("level loop detected");

                var sb = new System.Text.StringBuilder();
                var current = node;
                while (current != null)
                {
                    sb.Append(current.Data);
                    current = current.Parent;
                    if (current != null) sb.Append(" -> ");
                }

                Log.Information(sb.ToString());
                // Environment.Exit(0);
                return;
            }

            loadedLevels = loadedLevels.AddChild(worldName);
        }

        var actors = world.PersistentLevel.Load<ULevel>()!.Actors;
        for (var i = 0; i < actors.Length; i++)
        {
            var actor = actors[i];
            Utils.ProgressBar(ExportingWorldName, i + 1, actors.Length);
            ProcessActor(actor, loadedLevels);
        }

        foreach (var level in world.StreamingLevels)
        {
            var uWorld = level.Load()?.Get<FSoftObjectPath>("WorldAsset").AssetPathName.Text;
            if (uWorld != null) ProcessWorld(uWorld, loadedLevels);
        }
    }

    public void ProcessStreamingGrid(FStructFallback grid, TreeNode<string> loadedLevels)
    {
        if (grid.TryGetValue(out FStructFallback[] gridLevels, "GridLevels"))
        {
            foreach (var level in gridLevels)
            {
                if (level.TryGetValue<FStructFallback[]>(out var levelCells, "LayerCells"))
                {
                    foreach (var levelCell in levelCells)
                    {
                        IPropertyHolder?[] gridCells = levelCell.GetOrDefault<IPropertyHolder?[]>("GridCells", Array.Empty<IPropertyHolder>()); 
                        if (gridCells.All(x => x == null)) // in 4.26 this is an array of FallBacks and newer versions it's an array of UObjects 
                            gridCells = levelCell.GetOrDefault<UObject?[]>("GridCells", Array.Empty<UObject>());
                        //
                        // if (levelCell.TryGetValue<IPropertyHolder?[]>(out var gridCells, "GridCells"))
                        // {
                            foreach (var gridCell in gridCells)
                            {
                                if (gridCell == null) continue;
                                if (gridCell.TryGetValue<UObject>(out var levelStreaming, "LevelStreaming") &&
                                    levelStreaming.TryGetValue(out FSoftObjectPath worldAsset, "WorldAsset"))
                                {
                                    var text = worldAsset.ToString();
                                    if (text.SubstringAfterLast("/").StartsWith("HLOD"))
                                        continue;
                                    ProcessWorld(text, loadedLevels);
                                }
                            }
                        // }
                    }
                }
            }
        }
    }

    private static readonly Type[] ExportableExports = { typeof(ALandscapeProxy), typeof(UWorld), typeof(AWorldSettings) };

    private void ProcessActor(FPackageIndex actor, TreeNode<string> loadedLevels)
    {
        if (actor.IsNull) return;
        var resolved = actor.ResolvedObject;
        if (resolved == null) return;
        if (resolved.Package is Package pkg)
        {
            if (!ProviderUtils.IsExportTypeCompatible(pkg, pkg.ExportMap[resolved.ExportIndex], ExportableExports))
            {
                return;
            }
        }
        else if (resolved.Package is IoPackage io)
        {
            if (!ProviderUtils.IsExportTypeCompatible(io, io.ExportMap[resolved.ExportIndex], ExportableExports))
            {
                return;
            }
        }

        var obj = actor.Load();
        ProcessActor(obj, loadedLevels);
    }

    private void ProcessActor(UObject? actor, TreeNode<string> loadedLevels)
    {
        if (actor == null) return;
#if DEBUG
        // if (actor.Name != "Lobby_Landscape_UAID_06354C4EF56AB19F01_1669640275") return;
#endif
        if (actor is ALandscapeProxy landscape && landscape.LandscapeComponents.Length != 0)
        {
            var guid = landscape.LandscapeGuid;
            if (!LandscapeComps.ContainsKey(guid))
            {
                LandscapeComps[guid] = new List<FPackageIndex>();
            }

            if (!LandscapeActors.ContainsKey(guid))
            {
                LandscapeActors[guid] = new List<ALandscapeProxy>();
            }

            LandscapeActors[guid].Add(landscape);

            var components = landscape.LandscapeComponents;
            LandscapeComps[guid].AddRange(components);
        }

        if (actor.TryGetValue(out UObject partition, "WorldPartition")
            && partition.TryGetValue(out UObject runtimeHash, "RuntimeHash")
            && runtimeHash.TryGetValue(out FStructFallback[] streamingGrids, "StreamingGrids"))
        {
            if (!bScanSubLevels) return;
            foreach (var grid in streamingGrids)
            {
                // var gridName = grid.GetOrDefault("GridName", new FName(i.ToString()));
                // // if (!gridName.Text.StartsWith("MainGrid"))
                // //     continue;
                ProcessStreamingGrid(grid, loadedLevels);
            }
        }
    }

    public bool Save()
    {
        if (LandscapeComps.Count == 0)
        {
            Log.Information("No landscapes found. Press any key to exit.");
            return false;
        }

        Log.Information("Exporting Heightmaps...");

        var exporterOptions = new ExporterOptions() { ExportMaterials = false , MeshFormat = EMeshFormat.UEFormat, CompressionFormat = EFileCompressionFormat.ZSTD };
        var dir = new DirectoryInfo(ExportDirectory);
        dir.Create();
        foreach (var (key, comps) in LandscapeComps)
        {
            var landscape = LandscapeActors[key][0];
            landscape.Name = $"Landscape_{key.ToString()}";

            Log.Information("Exporting Landscape: {0}", landscape.Name);
            // ls.MinimumLevel = LogEventLevel.Error;
            var loadedComps = Utils.LoadPackageIndexWithProgress<ULandscapeComponent>(comps.ToArray(), "Loading Landscape Components");
            // ls.MinimumLevel = LogEventLevel.Information;

            Log.Information("Converting Landscape: {0}", landscape.Name);
            var exporter = new LandscapeExporter(landscape, loadedComps!, exporterOptions, ELandscapeExportFlags.ExportHeightmap | ELandscapeExportFlags.ExportWeightmap);
            Log.Information("Writing Landscape to disk");
            exporter.TryWriteToDir(dir, out var _, out var _2);
        }

        Log.Information("Successfully exported {0} heightmaps. Press any key to exit (CLI).", LandscapeComps.Count);
        return true;
    }
}