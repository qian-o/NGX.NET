using System.Runtime.InteropServices;
using Showcase.Models;

namespace Showcase.DirectX12;

// NVIDIA/nvapi 70d337db9186e968eab622f7e786de7e437faf3d,
// nvapi.h and nvapi_interface.h. Uses the installed driver, not an NGX feature.
internal sealed unsafe class NVLowLatency : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SleepMode
    {
        public uint Version;
        public byte LowLatency;
        public byte Boost;
        public uint MinimumInterval;
        public byte UseMarkers;
        public byte UseMinQueueTime;
        public fixed byte Reserved[30];
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MarkerParameters
    {
        public uint Version;
        public ulong Frame;
        public LatencyMarker Marker;
        public ulong Reserved0;
        public fixed byte Reserved[56];
    }

    private readonly nint module;
    private readonly nint device;
    private readonly delegate* unmanaged[Cdecl]<nint, SleepMode*, int> setMode;
    private readonly delegate* unmanaged[Cdecl]<nint, int> sleep;
    private readonly delegate* unmanaged[Cdecl]<nint, MarkerParameters*, int> setMarker;
    private readonly delegate* unmanaged[Cdecl]<int> unload;
    private readonly object sync = new();

    public bool Available { get; }

    public NVLowLatency(nint nativeDevice)
    {
        device = nativeDevice;

        if (!NativeLibrary.TryLoad("nvapi64.dll", typeof(NVLowLatency).Assembly, DllImportSearchPath.System32, out module))
        {
            Console.WriteLine("Reflex: NVIDIA NVAPI driver library is unavailable.");
            return;
        }

        delegate* unmanaged[Cdecl]<uint, nint> query = (delegate* unmanaged[Cdecl]<uint, nint>)NativeLibrary.GetExport(module, "nvapi_QueryInterface");
        delegate* unmanaged[Cdecl]<int> initialize = (delegate* unmanaged[Cdecl]<int>)query(0x0150e828);
        unload = (delegate* unmanaged[Cdecl]<int>)query(0xd22bdd7e);
        setMode = (delegate* unmanaged[Cdecl]<nint, SleepMode*, int>)query(0xac1ca9e0);
        sleep = (delegate* unmanaged[Cdecl]<nint, int>)query(0x852cd1d2);
        setMarker = (delegate* unmanaged[Cdecl]<nint, MarkerParameters*, int>)query(0xd9984c05);

        if (initialize == null || setMode == null || sleep == null || setMarker == null || initialize() != 0)
        {
            Console.WriteLine("Reflex: required NVAPI entry points could not be initialized.");
            return;
        }

        SleepMode mode = new() { Version = (uint)sizeof(SleepMode) | (1u << 16), LowLatency = 1, UseMarkers = 1 };
        int result = setMode(device, &mode);
        Available = result == 0;
        Console.WriteLine($"Reflex (NVAPI): {(Available ? "Available" : $"status {result}")}");
    }

    public void Sleep()
    {
        if (Available)
        {
            lock (sync)
            {
                Check(sleep(device), "NvAPI_D3D_Sleep");
            }
        }
    }

    public void Marker(LatencyMarker marker, ulong frame)
    {
        if (Available)
        {
            MarkerParameters parameters = new() { Version = (uint)sizeof(MarkerParameters) | (1u << 16), Frame = frame, Marker = marker };
            lock (sync)
            {
                Check(setMarker(device, &parameters), "NvAPI_D3D_SetLatencyMarker");
            }
        }
    }

    private static void Check(int result, string operation)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{operation}: {result}");
        }
    }

    public void Dispose()
    {
        if (Available)
        {
            SleepMode mode = new() { Version = (uint)sizeof(SleepMode) | (1u << 16) };
            _ = setMode(device, &mode);
        }

        if (module != 0)
        {
            if (unload != null)
            {
                _ = unload();
            }

            NativeLibrary.Free(module);
        }
    }
}
