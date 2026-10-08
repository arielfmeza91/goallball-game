using NAudio.Wave;
using NAudio.Wave.SampleProviders;
namespace Goalball.Windows;
public sealed class GameAudio : IDisposable
{
    private readonly WaveOutEvent output = new() { DesiredLatency = 70 };
    private readonly MixingSampleProvider mixer = new(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)) { ReadFully = true };
    private readonly Dictionary<string, float[]> samples = new();
    public float Volume = .7f;
    public GameAudio()
    {
        foreach (string name in new[] { "bell", "throw", "dive", "save", "goal", "whistle" })
        {
            using var reader = new AudioFileReader(Path.Combine(AppContext.BaseDirectory, "sonidos", name + ".wav"));
            var data = new List<float>(); var buffer = new float[4096]; int count;
            while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                for (int i = 0; i < count; i += reader.WaveFormat.Channels)
                { float v = 0; for (int c = 0; c < reader.WaveFormat.Channels; c++) v += buffer[i + c]; data.Add(v / reader.WaveFormat.Channels); }
            samples[name] = data.ToArray();
        }
        output.Init(mixer); output.Play();
    }
    public void Play(string name, double pan = 0, double gain = 1)
    {
        if (samples.TryGetValue(name, out var data)) mixer.AddMixerInput(new PannedSample(data, Math.Clamp(pan, -1, 1), gain * Volume));
    }
    public void StopEffects() => mixer.RemoveAllMixerInputs();
    public void Dispose() { output.Stop(); output.Dispose(); }
    private sealed class PannedSample(float[] samples, double pan, double gain) : ISampleProvider
    {
        private int position;
        private readonly float left = (float)(Math.Cos((pan + 1) * Math.PI / 4) * gain);
        private readonly float right = (float)(Math.Sin((pan + 1) * Math.PI / 4) * gain);
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        public int Read(float[] buffer, int offset, int count)
        {
            int written = 0;
            while (written + 1 < count && position < samples.Length) { float v = samples[position++]; buffer[offset + written++] = v * left; buffer[offset + written++] = v * right; }
            return written;
        }
    }
}
