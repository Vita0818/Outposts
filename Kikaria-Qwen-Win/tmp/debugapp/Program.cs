using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.Json;

string toolsDir = @"C:\Users\Vita\.nuget\packages\microsoft.windowsappsdk\1.7.250606001\tools\net472";
string inputJson = @"C:\Users\Vita\Vitemis\Outposts\Kikaria-Qwen-Win\Kikaria\obj\x64\Debug\net8.0-windows10.0.19041.0\input.json";
string outputJson = @"C:\Users\Vita\Vitemis\Outposts\Kikaria-Qwen-Win\Kikaria\obj\x64\Debug\net8.0-windows10.0.19041.0\output.json";

try
{
    Assembly compilerAsm = Assembly.LoadFrom(Path.Combine(toolsDir, "XamlCompiler.exe"));
    
    // Create CompileXaml instance
    Type compileXamlType = compilerAsm.GetType("Microsoft.UI.Xaml.Markup.Compiler.Executable.CompileXaml");
    if (compileXamlType == null)
    {
        foreach (var t in compilerAsm.GetTypes())
            if (t.Name == "CompileXaml")
                compileXamlType = t;
    }
    
    Console.Error.WriteLine($"CompileXaml type: {compileXamlType?.FullName}");
    
    var instance = Activator.CreateInstance(compileXamlType, true);
    
    // Get Log property (ConsoleLogger)
    var logProp = compileXamlType.GetProperty("Log", BindingFlags.NonPublic | BindingFlags.Instance);
    var log = logProp.GetValue(instance);
    Console.Error.WriteLine($"Log type: {log?.GetType().FullName}");
    
    // Call Run method
    var runMethod = compileXamlType.GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
    Console.Error.WriteLine($"Run method: {runMethod}");
    
    int result = (int)runMethod.Invoke(instance, new object[] { new string[] { inputJson, outputJson } });
    Console.Error.WriteLine($"Run returned: {result}");
    
    // Get entries from log
    var entriesProp = log.GetType().GetProperty("Entries");
    var entries = entriesProp.GetValue(log) as IList;
    
    Console.Error.WriteLine($"Log entries count: {entries?.Count}");
    if (entries != null)
    {
        foreach (var entry in entries)
        {
            Console.Error.WriteLine($"Entry: {entry}");
            // Try to get properties
            var entryType = entry.GetType();
            foreach (var prop in entryType.GetProperties())
            {
                var val = prop.GetValue(entry);
                if (val != null && !string.IsNullOrEmpty(val.ToString()))
                    Console.Error.WriteLine($"  {prop.Name}: {val}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FATAL: {ex}");
    if (ex.InnerException != null)
        Console.Error.WriteLine($"INNER: {ex.InnerException}");
}
