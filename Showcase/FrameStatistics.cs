using System.Diagnostics;

namespace Showcase;

internal sealed class FrameStatistics
{
    public double? RenderFps { get; private set; }
    public double? PresentedFps { get; private set; }
    public double CpuMilliseconds { get; private set; }
    private long startedAt;
    private uint renderedFrames;
    private ulong presentedFrames;
    private double cpuTotal;
    private bool presentationKnown = true;

    public void Reset(long timestamp)
    {
        StartInterval(timestamp);
        RenderFps = null;
        PresentedFps = null;
        CpuMilliseconds = 0;
    }

    public bool RecordFrame(long timestamp, uint? presented, double cpuMilliseconds)
    {
        renderedFrames++;
        cpuTotal += cpuMilliseconds;
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
            return false;
        }

        RenderFps = renderedFrames / seconds;
        // Sum SDK-reported presentation counts, including dropped/zero-frame
        // samples. The configured FG multiplier is never used to estimate FPS.
        PresentedFps = presentationKnown ? presentedFrames / seconds : null;
        CpuMilliseconds = cpuTotal / renderedFrames;
        StartInterval(timestamp);
        return true;
    }

    private void StartInterval(long timestamp)
    {
        startedAt = timestamp;
        renderedFrames = 0;
        presentedFrames = 0;
        cpuTotal = 0;
        presentationKnown = true;
    }
}
