namespace Showcase;

// A bounded capture of completed GPU work, independent of FPS and generated frames.
internal sealed class GpuProfile
{
    public const int SampleCount = 30;
    private readonly double[] totals = new double[(int)GpuTimestamp.Count - 1];
    private int samples;
    private int skip;
    public double? LastMilliseconds { get; private set; }

    public void Reset()
    {
        Array.Clear(totals);
        samples = 0;
        // A resize can leave results from the previous configuration in all slots.
        skip = RenderLayout.FramesInFlight;
        LastMilliseconds = null;
    }

    public double[]? Add(ReadOnlySpan<ulong> timestamps, double millisecondsPerTick, ulong mask = ulong.MaxValue)
    {
        if (timestamps.Length != (int)GpuTimestamp.Count || !double.IsFinite(millisecondsPerTick) || millisecondsPerTick <= 0)
        {
            throw new ArgumentException("Invalid GPU timestamp layout or period.");
        }
        Span<double> elapsed = stackalloc double[totals.Length];
        double total = 0;
        for (int i = 0; i < elapsed.Length; i++)
        {
            if (mask == ulong.MaxValue && timestamps[i + 1] < timestamps[i])
            {
                LastMilliseconds = null;
                return null;
            }
            elapsed[i] = ((timestamps[i + 1] - timestamps[i]) & mask) * millisecondsPerTick;
            total += elapsed[i];
        }
        LastMilliseconds = total;
        if (skip > 0)
        {
            skip--;
            return null;
        }
        if (samples == SampleCount)
        {
            return null;
        }
        for (int i = 0; i < totals.Length; i++)
        {
            totals[i] += elapsed[i];
        }
        samples++;
        return samples == SampleCount ? totals.Select(value => value / SampleCount).ToArray() : null;
    }
}
