using System.Diagnostics;

namespace Showcase.Helpers;

internal class FrameStatistics
{
    private long startedAt;
    private ulong presentedFrames;

    public double? PresentedFps { get; private set; }

    public void Reset()
    {
        StartInterval(Stopwatch.GetTimestamp());
        PresentedFps = null;
    }

    public void RecordFrame(uint presented)
    {
        long timestamp = Stopwatch.GetTimestamp();
        presentedFrames += presented;
        double seconds = Stopwatch.GetElapsedTime(startedAt, timestamp).TotalSeconds;
        if (seconds < 0.5)
        {
            return;
        }

        // Count successful presentation calls, including zero-frame samples.
        // The configured FG multiplier is never used to estimate FPS.
        PresentedFps = presentedFrames / seconds;
        StartInterval(timestamp);
    }

    private void StartInterval(long timestamp)
    {
        startedAt = timestamp;
        presentedFrames = 0;
    }
}
