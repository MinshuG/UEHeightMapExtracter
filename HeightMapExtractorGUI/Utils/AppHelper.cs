using System;
using Avalonia.Controls;
using Serilog.Core;

namespace HeightMapExtractorGUI.Models;

public static class AppHelper
{
    public static Window MainWindow;
    public static TopLevel TopLevel => TopLevel.GetTopLevel(MainWindow) ?? throw new NullReferenceException("TopLevel is null");
    public static LoggingLevelSwitch LoggingLevelSwitch = new();
}