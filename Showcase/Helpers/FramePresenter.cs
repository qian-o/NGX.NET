using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Showcase.Models;

namespace Showcase.Helpers;

// Presentation runs on one worker. Per-slot completion prevents
// rendering from overwriting either retained real frames or generated frames.
internal sealed class FramePresenter(
    Action<int> waitRendering,
    Func<GpuImage, bool> present,
    Action waitPresentation) : IDisposable
{
    private sealed record Batch(int Slot, GpuImage Real, GpuImage? Generated, TimeSpan Interval, TaskCompletionSource Completion);

    private readonly BlockingCollection<Batch> queue = new(RenderLayout.FramesInFlight);
    private readonly Task[] slots = Enumerable.Repeat(Task.CompletedTask, RenderLayout.FramesInFlight).ToArray();
    private Thread? thread;
    private ExceptionDispatchInfo? failure;
    private int presented;
    private int recreate;

    public bool NeedsRecreation => Interlocked.Exchange(ref recreate, 0) != 0;

    public uint ReadPresentedCount() => (uint)Interlocked.Exchange(ref presented, 0);

    public void WaitSlot(int slot)
    {
        slots[slot].GetAwaiter().GetResult();
        failure?.Throw();
    }

    public void Enqueue(int slot, GpuImage real, GpuImage? generated, TimeSpan interval)
    {
        failure?.Throw();
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        slots[slot] = completion.Task;
        thread ??= Start();
        queue.Add(new(slot, real, generated, interval, completion));
    }

    private Thread Start()
    {
        Thread worker = new(Run)
        {
            Name = "NGX presentation",
            IsBackground = true
        };

        worker.Start();

        return worker;
    }

    private void Run()
    {
        foreach (Batch batch in queue.GetConsumingEnumerable())
        {
            try
            {
                failure?.Throw();
                waitRendering(batch.Slot);

                try
                {
                    bool presentReal = true;

                    if (batch.Generated is not null)
                    {
                        // Copy/Present time belongs inside the half-frame interval.
                        long deadline = Stopwatch.GetTimestamp() + (long)(batch.Interval.TotalSeconds * Stopwatch.Frequency / 2);
                        presentReal = Present(batch.Generated);

                        if (presentReal)
                        {
                            WaitUntil(deadline);
                        }
                    }

                    if (presentReal)
                    {
                        Present(batch.Real);
                    }
                }
                finally
                {
                    // Present can return before its GPU copy finishes. Retain the
                    // frame slot until every copy has finished reading its images.
                    waitPresentation();
                }

                batch.Completion.SetResult();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
                batch.Completion.SetException(exception);
            }
        }
    }

    private bool Present(GpuImage image)
    {
        bool success = present(image);

        if (success)
        {
            Interlocked.Increment(ref presented);
        }
        else
        {
            Interlocked.Exchange(ref recreate, 1);
        }

        return success;
    }

    private static void WaitUntil(long deadline)
    {
        long now;

        while ((now = Stopwatch.GetTimestamp()) < deadline)
        {
            double milliseconds = Stopwatch.GetElapsedTime(now, deadline).TotalMilliseconds;

            if (milliseconds >= 1)
            {
                Thread.Sleep((int)Math.Min(milliseconds, int.MaxValue));
            }
            else
            {
                Thread.Yield();
            }
        }
    }

    public void Drain() => Task.WhenAll(slots).GetAwaiter().GetResult();

    public void Dispose()
    {
        queue.CompleteAdding();
        thread?.Join();
        queue.Dispose();
    }
}
