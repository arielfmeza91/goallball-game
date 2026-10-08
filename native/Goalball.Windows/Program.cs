using System.Globalization;
namespace Goalball.Windows;
internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("es-ES");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("es-ES");
        if (args.Length == 2 && args[0] == "--preparar-audio") return RecordedAudio.Prepare(args[1]);
        if (args.Contains("--verificar")) { ApplicationConfiguration.Initialize(); return Diagnostics.Run(); }
        ApplicationConfiguration.Initialize(); Application.Run(new GameWindow()); return 0;
    }
}
