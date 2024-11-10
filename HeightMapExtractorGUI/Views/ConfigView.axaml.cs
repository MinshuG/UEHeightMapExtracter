using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CUE4Parse.UE4.Versions;

namespace HeightMapExtractorGUI.Views;

public partial class ConfigView : UserControl
{
    public ConfigView()
    {
        InitializeComponent();
        Populate();
    }

    private void Populate()
    {
        #region UnrealVersion
        var versions = Enum.GetNames(typeof(EGame)).Distinct().Where(x => !x.EndsWith("_LATEST")).ToArray();
        // push everything that starts to GAME_UE to the top
        var gameSpecificVers = versions.Where(x => x.StartsWith("GAME_UE")).ToArray();
        var otherVers = versions.Where(x => !x.StartsWith("GAME_UE")).ToArray();
        Array.Sort(otherVers);
        versions = gameSpecificVers.Concat(otherVers).ToArray();
        // remove duplicates
        foreach (var version in versions)
        {
            UnrealVersion.Items.Add(version);
        }
        #endregion
        
        # region UE Object Version

        var ue4Versions = Enum.GetNames(typeof(EUnrealEngineObjectUE4Version));
        var ue4VersionsValues = Enum.GetValues<EUnrealEngineObjectUE4Version>();
        for (var i = 0; i < ue4Versions.Length; i++)
        {
            if (ue4Versions[i].Contains("AUTOMATIC_")) continue;
            UE4ObjectVersion.Items.Add($"{(uint) ue4VersionsValues[i]} - {ue4Versions[i]}");
        }

        var ue5Versions = Enum.GetNames(typeof(EUnrealEngineObjectUE5Version));
        var ue5VersionsValues = Enum.GetValues<EUnrealEngineObjectUE5Version>();
        for (var i = 0; i < ue5Versions.Length; i++)
        {
            if (ue4Versions[i].Contains("AUTOMATIC_")) continue;
            UE5ObjectVersion.Items.Add($"{(uint) ue5VersionsValues[i]} - {ue5Versions[i]}");
        }
        #endregion
    }
    
}