using System.Runtime.InteropServices;
using System.Text;
namespace Goalball.Windows;
public sealed class Nvda : IDisposable
{
    [DllImport("nvdaControllerClient.dll", CallingConvention = CallingConvention.Winapi)] private static extern int nvdaController_testIfRunning();
    [DllImport("nvdaControllerClient.dll", CallingConvention = CallingConvention.Winapi)] private static extern int nvdaController_cancelSpeech();
    [DllImport("nvdaControllerClient.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)] private static extern int nvdaController_speakText(string text);
    [DllImport("nvdaControllerClient.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)] private static extern int nvdaController_brailleMessage(string text);
    [DllImport("user32.dll")] private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetUserObjectInformationW")] private static extern bool GetUserObjectInformation(IntPtr handle, int index, StringBuilder name, uint length, out uint needed);
    [DllImport("user32.dll")] private static extern bool CloseDesktop(IntPtr desktop);
    private readonly Queue<string> queue = new();
    private readonly AutoResetEvent ready = new(false);
    private readonly Thread worker;
    private bool disposed;
    private volatile bool interrupt;
    public volatile bool Running;
    public string Status => Running ? "NVDA conectado" : "NVDA no detectado. Abre NVDA y pulsa F2 para conectar.";
    public Nvda() { worker = new Thread(Loop) { IsBackground = true, Name = "NVDA announcements" }; worker.Start(); }
    public void Speak(string text, bool urgent = false)
    {
        if (string.IsNullOrWhiteSpace(text) || disposed) return;
        lock (queue) { if (urgent) { queue.Clear(); interrupt = true; } if (queue.Count < 8) queue.Enqueue(text); } ready.Set();
    }
    private static bool SafeDesktop()
    {
        // No enviar anuncios cuando Windows muestra la pantalla segura o está bloqueado.
        var d = OpenInputDesktop(0, false, 0x0001); if (d == IntPtr.Zero) return false;
        try { var name = new StringBuilder(256); return GetUserObjectInformation(d, 2, name, 512, out _) && string.Equals(name.ToString(), "Default", StringComparison.OrdinalIgnoreCase); }
        finally { CloseDesktop(d); }
    }
    private void Loop()
    {
        while (!disposed)
        {
            ready.WaitOne(1000); if (disposed) break;
            try
            {
                Running = SafeDesktop() && nvdaController_testIfRunning() == 0;
                if (!Running) { lock (queue) queue.Clear(); continue; }
                if (interrupt) { nvdaController_cancelSpeech(); interrupt = false; }
                while (true)
                {
                    string? text; lock (queue) text = queue.Count > 0 ? queue.Dequeue() : null;
                    if (text == null || !SafeDesktop()) break;
                    nvdaController_speakText(text); nvdaController_brailleMessage(text);
                }
            }
            catch (DllNotFoundException) { Running = false; lock (queue) queue.Clear(); }
            catch (EntryPointNotFoundException) { Running = false; lock (queue) queue.Clear(); }
            catch (BadImageFormatException) { Running = false; lock (queue) queue.Clear(); }
        }
    }
    public void Dispose() { disposed = true; ready.Set(); if (worker.Join(1500)) ready.Dispose(); }
}
