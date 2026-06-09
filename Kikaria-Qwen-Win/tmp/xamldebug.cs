using System;
using System.Reflection;

class Program
{
    static int Main(string[] args)
    {
        try
        {
            var asm = Assembly.LoadFrom(@"C:\Users\Vita\.nuget\packages\microsoft.windowsappsdk\1.7.250606001\tools\net472\XamlCompiler.exe");
            var entryPoint = asm.EntryPoint;
            if (entryPoint == null)
            {
                Console.Error.WriteLine("No entry point found");
                return 1;
            }
            Console.WriteLine($"Entry point: {entryPoint.DeclaringType.FullName}.{entryPoint.Name}");
            var parameters = entryPoint.GetParameters();
            object[] invokeArgs;
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string[]))
            {
                invokeArgs = new object[] { new string[] { args[0], args[1] } };
            }
            else
            {
                invokeArgs = new object[] { args[0], args[1] };
            }
            var result = entryPoint.Invoke(null, invokeArgs);
            Console.WriteLine($"Returned: {result}");
            return result is int i ? i : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Exception: {ex}");
            if (ex.InnerException != null)
                Console.Error.WriteLine($"Inner: {ex.InnerException}");
            return 1;
        }
    }
}
