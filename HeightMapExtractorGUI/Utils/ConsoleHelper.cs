using System;
using System.IO;
using System.Runtime.InteropServices;
using Serilog.Events;

namespace HeightMapExtractorGUI.Models;

public static class ConsoleHelper
{
    static bool _created = false;
    public static void ShowConsole()
    {
        AppHelper.LoggingLevelSwitch.MinimumLevel = LogEventLevel.Debug;
        if (!_created)
        {
            AllocConsole();
            _created = true;
        }

        var console = GetConsoleWindow();
        ShowWindow(console, 5); // 5 = SW_SHOW
        // SW_SHOWNA = 8 ..?
    }
    
    public static void HideConsole()
    {
        AppHelper.LoggingLevelSwitch.MinimumLevel = LogEventLevel.Information;

        if (!_created) return;
        var console = GetConsoleWindow();
        ShowWindow(console, 0); // 0 = SW_HIDE
    }

    // P/Invoke required:
    [DllImport("kernel32")]
    static extern bool AllocConsole();
    [DllImport("kernel32.dll")]
    static extern bool FreeConsole();
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}