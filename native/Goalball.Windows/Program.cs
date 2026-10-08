using System.Globalization;
namespace Goalball.Windows;
internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("es-ES");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("es-ES");
        if (args.Contains("--verificar")) return Diagnostics.Run();
        ApplicationConfiguration.Initialize(); Application.Run(new GameWindow()); return 0;
    }
}
