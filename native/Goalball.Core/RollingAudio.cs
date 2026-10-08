namespace Goalball.Core;

// Un flujo continuo de una grabación mono, con panorama y distancia actualizados
// desde la simulación. No reinicia un efecto corto en cada fotograma.
public sealed class RollingAudio(float[] recording)
{
    private double cursor;
    private volatile float pan, gain, rate = 1;
    public volatile bool Active = true;
    public void Position(double x, double y, double speed, double volume)
    {
        pan = (float)Math.Clamp((x - 4.5) / 4.5, -1, 1);
        gain = (float)(Math.Clamp(volume, 0, 1) * (.12 + .5 * (1 - Math.Clamp(Math.Abs(y - 2) / 18, 0, 1))));
        rate = (float)Math.Clamp(.8 + speed / 40, .8, 1.2);
    }
    public int Read(float[] buffer, int offset, int count)
    {
        if (!Active || recording.Length < 2) return 0;
        double angle = (pan + 1) * Math.PI / 4;
        float left = (float)Math.Cos(angle) * gain, right = (float)Math.Sin(angle) * gain;
        int written = 0;
        while (written + 1 < count)
        {
            int first = (int)cursor, next = (first + 1) % recording.Length;
            float sample = recording[first] + (recording[next] - recording[first]) * (float)(cursor - first);
            buffer[offset + written++] = sample * left; buffer[offset + written++] = sample * right;
            cursor = (cursor + rate) % recording.Length;
        }
        return written;
    }
}
