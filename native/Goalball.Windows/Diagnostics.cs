using System.Runtime.InteropServices;
using Goalball.Core;
using NAudio.Wave;
namespace Goalball.Windows;
internal static class Diagnostics
{
    public static int Run()
    {
        try
        {
            var handle = NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "nvdaControllerClient.dll"));
            foreach (var export in new[] { "nvdaController_testIfRunning", "nvdaController_speakText", "nvdaController_brailleMessage", "nvdaController_cancelSpeech" }) NativeLibrary.GetExport(handle, export);
            var test = Marshal.GetDelegateForFunctionPointer<TestRunning>(NativeLibrary.GetExport(handle, "nvdaController_testIfRunning"));
            int result = test(); NativeLibrary.Free(handle);
            var report = new List<string> { "PASS: DLL oficial de NVDA cargada y funciones presentes.", $"NVDA testIfRunning = {result}. Cero significa NVDA abierto; no es necesario en el servidor de compilación." };
            foreach (var name in new[] { "bell", "throw", "save", "dive", "goal", "whistle" })
            {
                using var reader = new WaveFileReader(Path.Combine(AppContext.BaseDirectory, "sonidos", name + ".wav"));
                if (reader.WaveFormat.SampleRate != 44100 || reader.Length == 0) throw new InvalidDataException(name);
                report.Add($"PASS: {name}.wav, {reader.WaveFormat.SampleRate} Hz.");
            }
            var match = new Match(); match.Start(); if (match.Phase != Phase.Play) throw new Exception("Inicio");
            report.Add("PASS: motor inicia partido.");
            File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "verificacion.txt"), report); return 0;
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "verificacion.txt"), ex.ToString()); return 1; }
    }
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate int TestRunning();
}
