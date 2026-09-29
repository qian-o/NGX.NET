using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Showcase.Models;

namespace Showcase.Helpers;

// The presenter owns all swap-chain operations. Per-slot completion prevents
// rendering from overwriting either retained real frames or generated frames.
internal sealed class FramePresenter(Action<int> waitRendering, Func<GpuImage, ulong, bool, bool> present) : IDisposable
{
    private sealed record Batch(int Slot, ulong Frame, GpuImage Real, GpuImage? Generated, TimeSpan Interval, TaskCompletionSource Completion);

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

    public void Enqueue(int slot, ulong frame, GpuImage real, GpuImage? generated, TimeSpan interval)
    {
        failure?.Throw();
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        slots[slot] = completion.Task;
        thread ??= Start();
        queue.Add(new(slot, frame, real, generated, interval, completion));
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

                if (batch.Generated is not null)
                {
                    if (!Present(batch.Generated, batch.Frame, true))
                    {
                        batch.Completion.SetResult();
                        continue;
                    }

                    long deadline = Stopwatch.GetTimestamp() + (long)(batch.Interval.TotalSeconds * Stopwatch.Frequency / 2);

                    while (Stopwatch.GetTimestamp() < deadline)
                    {
                        TimeSpan remaining = Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp(), deadline);

                        if (remaining > TimeSpan.Zero)
                        {
                            Thread.Sleep(remaining);
                        }
                    }
                }

                Present(batch.Real, batch.Frame, false);
                batch.Completion.SetResult();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
                batch.Completion.SetException(exception);
            }
        }
    }

    private bool Present(GpuImage image, ulong frame, bool generated)
    {
        bool success = present(image, frame, generated);

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

    public void Drain() => Task.WhenAll(slots).GetAwaiter().GetResult();

    public void Dispose()
    {
        queue.CompleteAdding();
        thread?.Join();
        queue.Dispose();
    }
}
