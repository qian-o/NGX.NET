namespace NGX.NET;

internal static unsafe class NativeLifetime
{
    private static readonly Lock @lock = new();
    private static readonly Dictionary<(NGXGraphicsAPI Api, nint Device), List<NativeScope>> initialization = [];
    private static readonly Dictionary<(nint Parameters, string Slot), List<SlotEntry>> parameters = [];
    private static readonly Dictionary<(nint Context, nint Stream), CudaDeviceEntry> cudaDevices = [];

    internal static void Retain(NGXGraphicsAPI api, nint device, NativeScope scope, NGXResult result)
    {
        if (result.IsFailure)
        {
            scope.Dispose();

            return;
        }

        using Lock.Scope _ = @lock.EnterScope();

        if (!initialization.TryGetValue((api, device), out List<NativeScope>? scopes))
        {
            scopes = [];
            initialization.Add((api, device), scopes);
        }

        scopes.Add(scope);
    }

    internal static void Retain(NGXGraphicsAPI api, NGXParameter parameter, string slot, NativeScope scope, NGXResult result)
    {
        using Lock.Scope _ = @lock.EnterScope();

        if (!parameters.TryGetValue((parameter.Value, slot), out List<SlotEntry>? scopes))
        {
            scopes = [];
            parameters.Add((parameter.Value, slot), scopes);
        }

        if (result.IsSuccess)
        {
            foreach (SlotEntry entry in scopes)
            {
                entry.Scope.Dispose();
            }
            scopes.Clear();
        }

        scopes.Add(new(api, scope));
    }

    internal static void Release(NGXParameter parameter)
    {
        using Lock.Scope _ = @lock.EnterScope();

        foreach ((nint Parameters, string Slot) key in parameters.Keys.Where(key => key.Parameters == parameter.Value).ToArray())
        {
            List<SlotEntry> scopes = parameters[key];
            parameters.Remove(key);

            for (int i = scopes.Count - 1; i >= 0; i--)
            {
                scopes[i].Scope.Dispose();
            }
        }
    }

    internal static void Release(NGXGraphicsAPI api, nint device)
    {
        using Lock.Scope _ = @lock.EnterScope();

        foreach ((NGXGraphicsAPI Api, nint Device) key in initialization.Keys.Where(key => key.Api == api && (device is 0 || key.Device == device)).ToArray())
        {
            List<NativeScope> scopes = initialization[key];
            initialization.Remove(key);

            for (int i = scopes.Count - 1; i >= 0; i--)
            {
                scopes[i].Dispose();
            }
        }

        if (initialization.Keys.Any(key => key.Api == api))
        {
            return;
        }

        foreach ((nint Parameters, string Slot) key in parameters.Keys.ToArray())
        {
            List<SlotEntry> scopes = parameters[key];
            for (int i = scopes.Count - 1; i >= 0; i--)
            {
                if (scopes[i].Api != api)
                {
                    continue;
                }

                NativeScope scope = scopes[i].Scope;
                scopes.RemoveAt(i);
                scope.Dispose();
            }

            if (scopes.Count is 0)
            {
                parameters.Remove(key);
            }
        }

        if (api is not NGXGraphicsAPI.Cuda)
        {
            return;
        }

        foreach (CudaDeviceEntry entry in cudaDevices.Values)
        {
            entry.Scope.Dispose();
        }
        cudaDevices.Clear();
    }

    internal static NGXCUDADeviceNative* GetCudaDevice(NGXCUDADevice device)
    {
        using Lock.Scope _ = @lock.EnterScope();

        if (cudaDevices.TryGetValue((device.CudaContext, device.CudaStream), out CudaDeviceEntry retained))
        {
            return (NGXCUDADeviceNative*)retained.Pointer;
        }

        NativeScope scope = new();
        NGXCUDADeviceNative* pointer = scope.Alloc(new NGXCUDADeviceNative(device));
        cudaDevices.Add((device.CudaContext, device.CudaStream), new((nint)pointer, scope));

        return pointer;
    }

    private readonly struct SlotEntry(NGXGraphicsAPI api, NativeScope scope)
    {
        public readonly NGXGraphicsAPI Api = api;

        public readonly NativeScope Scope = scope;
    }

    private readonly struct CudaDeviceEntry(nint pointer, NativeScope scope)
    {
        public readonly nint Pointer = pointer;

        public readonly NativeScope Scope = scope;
    }
}
