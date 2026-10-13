using System.Diagnostics;
using System.Runtime.ExceptionServices;
using NGX.NET;
using Showcase.Models;

namespace Showcase.Helpers;

// Per-slot completion keeps rendered and generated images alive until presentation finishes.
internal class FramePresenter(Action<int> waitRendering, Func<GpuImage, bool> present, Action waitPresentation) : DisposableObject
{
    private readonly Lock @lock = new();
    private readonly Queue<Batch> queue = new(RenderLayout.FramesInFlight);
    private readonly ManualResetEventSlim[] slots = CreateSlots();
    private readonly AutoResetEvent workAvailable = new(false);

    private Thread? thread;
    private volatile ExceptionDispatchInfo? failure;
    private bool isStopping;
    private int presented;
    private int recreate;

    public bool NeedsRecreation => Interlocked.Exchange(ref recreate, 0) is not 0;

    public uint ReadPresentedCount()
    {
        return (uint)Interlocked.Exchange(ref presented, 0);
    }

    public void WaitSlot(int slot)
    {
        slots[slot].Wait();
        failure?.Throw();
    }

    public void Enqueue(int slot, GpuImage real, GpuImage? generated, TimeSpan interval)
    {
        failure?.Throw();
        thread ??= Start();
        slots[slot].Reset();

        {
            using Lock.Scope _ = @lock.EnterScope();

            queue.Enqueue(new(slot, real, generated, interval));
        }

        workAvailable.Set();
    }

    public void Drain()
    {
        foreach (ManualResetEventSlim slot in slots)
        {
            slot.Wait();
        }

        failure?.Throw();
    }

    protected override void Destroy()
    {
        if (@lock is null)
        {
            return;
        }

        {
            using Lock.Scope _ = @lock.EnterScope();

            isStopping = true;
        }

        workAvailable?.Set();
        thread?.Join();
        workAvailable?.Dispose();

        if (slots is not null)
        {
            for (int i = slots.Length - 1; i >= 0; i--)
            {
                slots[i].Dispose();
            }
        }
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
        while (true)
        {
            Batch batch;
            bool hasBatch;
            {
                using Lock.Scope _ = @lock.EnterScope();

                hasBatch = queue.TryDequeue(out batch);

                if (!hasBatch && isStopping)
                {
                    return;
                }
            }

            if (!hasBatch)
            {
                workAvailable.WaitOne();

                continue;
            }

            try
            {
                if (failure is not null)
                {
                    continue;
                }

                waitRendering(batch.Slot);

                try
                {
                    bool presentReal = true;
                    if (batch.Generated is not null)
                    {
                        // Copy/Present time belongs inside the half-frame interval.
                        long deadline = Stopwatch.GetTimestamp() + (long)(batch.Interval.TotalSeconds * Stopwatch.Frequency / 2.0);
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
                    // Present can return before its GPU copy finishes. Retain the frame slot until every copy has finished reading its images.
                    waitPresentation();
                }
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                slots[batch.Slot].Set();
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

    private static ManualResetEventSlim[] CreateSlots()
    {
        ManualResetEventSlim[] slots = new ManualResetEventSlim[RenderLayout.FramesInFlight];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new(true);
        }

        return slots;
    }

    private static void WaitUntil(long deadline)
    {
        // Use the timestamp deadline directly; timed sleeps can overshoot frame spacing.
        while (Stopwatch.GetTimestamp() < deadline)
        {
            Thread.Yield();
        }
    }

    private readonly struct Batch(int slot, GpuImage real, GpuImage? generated, TimeSpan interval)
    {
        public readonly int Slot = slot;

        public readonly GpuImage Real = real;

        public readonly GpuImage? Generated = generated;

        public readonly TimeSpan Interval = interval;
    }
}
