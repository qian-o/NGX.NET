using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Showcase.Helpers;

// FG presentation deadlines need sub-millisecond waits without changing the
// process-wide timer resolution or adding a fixed spin/sleep allowance.
internal sealed partial class PresentationTimer : WaitHandle
{
    private const uint CreateWaitableTimerHighResolution = 0x00000002;
    private const uint TimerModifyState = 0x00000002;
    private const uint Synchronize = 0x00100000;

    public PresentationTimer()
    {
        nint handle = CreateWaitableTimerExW(0, 0, CreateWaitableTimerHighResolution, TimerModifyState | Synchronize);

        if (handle == 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        SafeWaitHandle = new(handle, true);
    }

    public void WaitUntil(long deadline, TimeProvider time)
    {
        long now;

        while ((now = time.GetTimestamp()) < deadline)
        {
            long ticks = time.GetElapsedTime(now, deadline).Ticks;

            if (ticks == 0)
            {
                return;
            }

            // Win32 relative due times are negative, in 100-nanosecond units.
            long dueTime = -ticks;

            if (SetWaitableTimerEx(SafeWaitHandle, in dueTime, 0, 0, 0, 0, 0) == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            WaitOne();
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint CreateWaitableTimerExW(nint attributes, nint name, uint flags, uint desiredAccess);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int SetWaitableTimerEx(
        SafeWaitHandle timer,
        in long dueTime,
        int period,
        nint callback,
        nint argument,
        nint wakeContext,
        uint tolerableDelay);
}
