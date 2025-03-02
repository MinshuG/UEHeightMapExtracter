using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.UObject;
using UE4Config.Parsing;

namespace HeightMapExtractor;

public static class ProviderUtils
{
    // WARNING: This does convert FortniteGame/Plugins/GameFeatures/GameFeatureName/Content/Package into /GameFeatureName/Package
    public static string CompactFilePath(this IFileProvider provider, string path) {
        if (path[0] == '/') {
            return path;
        }

        if (path.StartsWith("Engine/Content")) { // -> /Engine
            return "/Engine" + path["Engine/Content".Length..];
        }

        if (path.StartsWith("Engine/Plugins")) { // -> /Plugins
            return path["Engine".Length..];
        }

        var delim = path.IndexOf("/Content/", StringComparison.Ordinal);
        if (delim == -1) {
            return path;
        }

        // GameName/Content -> /Game
        return "/Game" + path[(delim + "/Content".Length)..];
    }
    
    public static int LoadConsoleVariables(this AbstractFileProvider provider) // src: FModel
    {
        int count = 0;
        foreach (var token in provider.DefaultEngine.Sections.FirstOrDefault(s => s.Name == "ConsoleVariables")?.Tokens ?? new List<IniToken>())
        {
            if (token is not InstructionToken it) continue;
            var boolValue = it.Value.Equals("1");

            switch (it.Key)
            {
                case "r.StaticMesh.KeepMobileMinLODSettingOnDesktop":
                case "r.SkeletalMesh.KeepMobileMinLODSettingOnDesktop":
                    provider.Versions[it.Key[2..]] = boolValue;
                    count++;
                    continue;
            }
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsExportTypeCompatible(Package pkg, FObjectExport export, IEnumerable<Type> types) // For Package
    {
        var obj = StolenConstructObject(pkg.ResolvePackageIndex(export.ClassIndex)?.Object?.Value as UStruct);
        return types.Any(x => x.IsInstanceOfType(obj));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsExportTypeCompatible(IoPackage pkg, FExportMapEntry export, IEnumerable<Type> types) // For IoPackage
    {
        var obj = StolenConstructObject(pkg.ResolveObjectIndex(export.ClassIndex)?.Object?.Value as UStruct);
        return types.Any(x => x.IsInstanceOfType(obj));
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
                obj = scriptClass.ConstructObject(EObjectFlags.RF_NoFlags);
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

    public static T? LoadExportOfType<T>(this IPackage pkg) where T : UObject
    {
        if (pkg is Package package)
        {
            var exports = package.ExportMap;
            for (var i = 0; i < exports.Length; i++)
            {
                var export = exports[i];
                if (IsExportTypeCompatible(package, export, [typeof(T)]))
                {
                    return pkg.ExportsLazy[i].Value as T;
                }
            }
        }
        else if (pkg is IoPackage ioPackage)
        {
            var exports = ioPackage.ExportMap;
            for (var i = 0; i < exports.Length; i++)
            {
                var export = exports[i];
                if (IsExportTypeCompatible(ioPackage, export, [typeof(T)]))
                {
                    return pkg.ExportsLazy[i].Value as T;
                }
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        throw new KeyNotFoundException("No export of type " + typeof(T).Name + " found");
    }
}