using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
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
    }
}