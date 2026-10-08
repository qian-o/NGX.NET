namespace NGX.NET;

internal static unsafe class NgxLifetime
{
    private static readonly Lock gate = new();
    private static readonly Dictionary<(string Backend, nint Device), List<NativeCall>> initialization = [];
    private static readonly Dictionary<(nint Parameters, string Method), List<NativeCall>> parameterData = [];
    private static readonly Dictionary<nint, string> parameterBackends = [];
    private static readonly Dictionary<(nint Context, nint Stream), NativeOwner<NGXCUDADeviceNative>> cudaDevices = [];
    private static readonly HashSet<nint> activeCudaDevices = [];

    internal static void BeginInitialization(string backend, nint device, NativeCall call)
    {
        lock (gate)
        {
            if (!initialization.TryGetValue((backend, device), out List<NativeCall>? entries))
            {
                entries = [];
                initialization.Add((backend, device), entries);
            }
            try { entries.Add(call); }
            catch
            {
                if (entries.Count == 0) initialization.Remove((backend, device));
                throw;
            }
        }
    }

    internal static void EndInitialization(string backend, nint device, bool succeeded, ref NativeCall? call)
    {
        lock (gate)
        {
            if (succeeded)
            {
                call = null;
                return;
            }
            List<NativeCall> entries = initialization[(backend, device)];
            entries.Remove(call!);
            if (entries.Count == 0) initialization.Remove((backend, device));
        }
    }

    internal static void BeginParameters(nint parameters, string method, NativeCall call)
    {
        lock (gate)
        {
            if (!parameterData.TryGetValue((parameters, method), out List<NativeCall>? entries))
            {
                entries = [];
                parameterData.Add((parameters, method), entries);
            }
            try { entries.Add(call); }
            catch
            {
                if (entries.Count == 0) parameterData.Remove((parameters, method));
                throw;
            }
        }
    }

    internal static void EndParameters(nint parameters, string method, bool returned, bool succeeded, ref NativeCall? call)
    {
        lock (gate)
        {
            List<NativeCall> entries = parameterData[(parameters, method)];
            if (!returned)
            {
                entries.Remove(call!);
                if (entries.Count == 0) parameterData.Remove((parameters, method));
                return;
            }
            NativeCall current = call!;
            call = null;
            // Early failures may leave old pointers. DLSSG additionally leaves
            // its optional matrix pointers unchanged when options are omitted.
            // Registration precedes native entry; committing needs no allocation.
            if (succeeded)
            {
                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(entries[i], current)) continue;
                    if (!current.HasFrameGenerationOptions && entries[i].HasFrameGenerationOptions) continue;
                    entries[i].Dispose();
                    entries.RemoveAt(i);
                }
            }
        }
    }

    internal static void RegisterParameters(string backend, nint parameters)
    {
        if (parameters == 0) return;
        lock (gate) parameterBackends[parameters] = backend;
    }

    internal static void PrepareParameters()
    {
        lock (gate) parameterBackends.EnsureCapacity(checked(parameterBackends.Count + 1));
    }

    internal static void ReleaseParameters(nint parameters, bool destroyed = false)
    {
        lock (gate)
        {
            foreach (var key in parameterData.Keys.Where(key => key.Parameters == parameters).ToArray())
            {
                foreach (NativeCall entry in parameterData[key]) entry.Dispose();
                parameterData.Remove(key);
            }
            if (destroyed) parameterBackends.Remove(parameters);
        }
    }

    internal static void Shutdown(string backend, nint device)
    {
        lock (gate)
        {
            foreach (var key in initialization.Keys.Where(key => key.Backend == backend && (device == 0 || key.Device == device)).ToArray())
            {
                foreach (NativeCall entry in initialization[key]) entry.Dispose();
                initialization.Remove(key);
            }
            if (device == 0 || !initialization.Keys.Any(key => key.Backend == backend))
            {
                foreach (nint parameters in parameterBackends.Where(item => item.Value == backend).Select(item => item.Key).ToArray()) ReleaseParameters(parameters, true);
            }
            if (backend == "CUDA")
            {
                foreach (var key in cudaDevices.Keys.Where(key => device == 0 || (nint)cudaDevices[key].Pointer == device).ToArray())
                {
                    activeCudaDevices.Remove((nint)cudaDevices[key].Pointer);
                    cudaDevices[key].Dispose();
                    cudaDevices.Remove(key);
                }
            }
        }
    }

    internal static NGXCUDADeviceNative* CudaDevice(NGXCUDADevice device)
    {
        lock (gate)
        {
            activeCudaDevices.EnsureCapacity(checked(cudaDevices.Count + 1));
            if (!cudaDevices.TryGetValue((device.CudaContext, device.CudaStream), out NativeOwner<NGXCUDADeviceNative>? owner))
            {
                NGXCUDADeviceNative native = new(in device);
                owner = new(ref native);
                try { cudaDevices.Add((device.CudaContext, device.CudaStream), owner); }
                catch { owner.Dispose(); throw; }
            }
            return owner.Pointer;
        }
    }

    internal static void FinishCudaDevice(nint device, bool success)
    {
        if (device == 0) return;
        lock (gate)
        {
            if (success) activeCudaDevices.Add(device);
            if (activeCudaDevices.Contains(device) || initialization.ContainsKey(("CUDA", device))) return;
            foreach (var key in cudaDevices.Keys.Where(key => (nint)cudaDevices[key].Pointer == device).ToArray())
            {
                cudaDevices[key].Dispose();
                cudaDevices.Remove(key);
            }
        }
    }
}
