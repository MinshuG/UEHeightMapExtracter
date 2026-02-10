using System.ComponentModel;
using System.Reflection;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace HeightMapExtractor;

public static class Utils
{
    static bool NoConsole = false;
    public static void ProgressBar(string message, int progress, int tot)
    {
        if (NoConsole) return;
        try
        {
            Console.CursorLeft = 0;
        }
        catch (Exception e)
        {
            NoConsole = true;
            return; // missing console handle
        }
        
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
    
    public static T?[] LoadPackageIndexWithProgress<T>(FPackageIndex[] packageIndexes, string message) where T : UObject
    {
        var progress = 0;
        var total = packageIndexes.Length;
        var result = new T?[total];
        
        for (var i = 0; i < packageIndexes.Length; i++)
        {
            Utils.ProgressBar(message, i + 1, total);
            var obj = packageIndexes[i].Load<T>();
            result[i] = obj;
        }
        Console.WriteLine();
        return result;
    }
    
    
    public static void RegisterAssembly()
    {
        // ObjectTypeRegistry.RegisterEngine(typeof(AFortWorldSettings).Assembly);
    }
    
    // Source - https://stackoverflow.com/a/10986749
    // Posted by Irish
    // Retrieved 2026-02-10, License - CC BY-SA 3.0
    public static string DescriptionAttr<T>(this T source)
    {
        FieldInfo fi = source.GetType().GetField(source.ToString());

        DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
            typeof(DescriptionAttribute), false);

        if (attributes != null && attributes.Length > 0) return attributes[0].Description;
        else return source.ToString();
    }
}