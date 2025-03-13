namespace Vereesa.Neon.Helpers;

public static class WellknownPaths
{
    public static string AppData =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Vereesa");
}
