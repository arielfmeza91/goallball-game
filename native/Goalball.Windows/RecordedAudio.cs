using NAudio.Wave;
using NAudio.Wave.SampleProviders;
namespace Goalball.Windows;
internal static class RecordedAudio
{
    public static int Prepare(string directory)
    {
        try
        {
            string output = Path.Combine(AppContext.BaseDirectory, "sonidos"); Directory.CreateDirectory(output);
            Convert(Path.Combine(directory, "jingle.mp3"), Path.Combine(output, "bell.wav"), 6, false);
            Convert(Path.Combine(directory, "rolling.mp3"), Path.Combine(output, "rolling.wav"), 6, true);
            File.Copy(Path.Combine(directory, "GRABACIONES-CREDITOS.txt"), Path.Combine(AppContext.BaseDirectory, "GRABACIONES-CREDITOS.txt"), true);
            return 0;
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "audio-error.txt"), ex.ToString()); return 1; }
    }
    private static void Convert(string source, string destination, double seconds, bool removeImpact)
    {
        using var reader = new AudioFileReader(source);
        ISampleProvider samples = reader;
        if (samples.WaveFormat.Channels == 1) samples = new MonoToStereoSampleProvider(samples);
        if (samples.WaveFormat.Channels != 2) throw new InvalidDataException("La grabación debe ser mono o estéreo.");
        if (samples.WaveFormat.SampleRate != 44100) samples = new WdlResamplingSampleProvider(samples, 44100);
        int max = (int)(seconds * 44100) * 2;
        float[] recording = new float[max]; int count = 0, received;
        while (count < max && (received = samples.Read(recording, count, max - count)) > 0) count += received;
        if (removeImpact && reader.TotalTime.TotalSeconds < seconds) count -= Math.Min(count / 4, (int)(.3 * 44100) * 2);
        count -= count % 2;
        if (count < 4410) throw new InvalidDataException("Grabación demasiado corta.");
        float peak = 0; for (int i = 0; i < count; i++) peak = Math.Max(peak, Math.Abs(recording[i]));
        if (peak < .0001f) throw new InvalidDataException("Grabación sin sonido.");
        // Reduce los chasquidos al repetir el flujo y conserva margen para la mezcla.
        int fade = Math.Min(882, count / 10);
        byte[] pcm = new byte[count * 2];
        for (int i = 0; i < count; i++)
        {
            float envelope = Math.Min(1, Math.Min(i / (float)fade, (count - 1 - i) / (float)fade));
            short value = (short)Math.Clamp(recording[i] / peak * .75f * envelope * 32767, -32767, 32767);
            pcm[i * 2] = (byte)(value & 255); pcm[i * 2 + 1] = (byte)(value >> 8);
        }
        using var writer = new WaveFileWriter(destination, new WaveFormat(44100, 16, 2)); writer.Write(pcm, 0, pcm.Length);
    }
}
