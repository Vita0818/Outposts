namespace Flotis.Interop;

public static class FileLocations
{
    public static string AppDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Flotis");
}
