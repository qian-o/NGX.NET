using System.Diagnostics;

namespace Showcase.Helpers;

internal sealed class FrameStatistics
{
    public double? PresentedFps { get; private set; }

    private long startedAt;
    private ulong presentedFrames;
    private bool presentationKnown = true;

    public void Reset(long timestamp)
    {
        StartInterval(timestamp);
        PresentedFps = null;
    }

    public void RecordFrame(long timestamp, uint? presented)
    {
        if (presented is uint count)
        {
            presentedFrames += count;
        }
        else
        {
            presentationKnown = false;
        }

        double seconds = Stopwatch.GetElapsedTime(startedAt, timestamp).TotalSeconds;

        if (seconds < 0.5)
        {
            return;
        }

        // Count successful presentation calls, including zero-frame samples.
        // The configured FG multiplier is never used to estimate FPS.
        PresentedFps = presentationKnown ? presentedFrames / seconds : null;
        StartInterval(timestamp);
    }

    private void StartInterval(long timestamp)
    {
        startedAt = timestamp;
        presentedFrames = 0;
        presentationKnown = true;
    }
}
