namespace NGX.NET;

internal static unsafe class NativeLifetime
{
    private static readonly Lock gate = new();
    private static readonly Dictionary<(NGXGraphicsAPI Api, nint Device), List<NativeScope>> initialization = [];
    private static readonly Dictionary<(nint Parameters, string Slot), List<(NGXGraphicsAPI Api, NativeScope Scope)>> parameters = [];
    private static readonly Dictionary<(nint Context, nint Stream), (nint Pointer, NativeScope Scope)> cudaDevices = [];

    internal static void Retain(NGXGraphicsAPI api, nint device, NativeScope scope, NGXResult result)
    {
        if (result is not NGXResult.Success)
        {
            scope.Dispose();

            return;
        }

        using Lock.Scope _ = gate.EnterScope();

        if (!initialization.TryGetValue((api, device), out List<NativeScope>? scopes))
        {
            scopes = [];
            initialization.Add((api, device), scopes);
        }

        scopes.Add(scope);
    }

    internal static void Retain(NGXGraphicsAPI api, NGXParameter parameter, string slot, NativeScope scope, NGXResult result)
    {
        using Lock.Scope _ = gate.EnterScope();

        if (!parameters.TryGetValue((parameter.Value, slot), out List<(NGXGraphicsAPI Api, NativeScope Scope)>? scopes))
        {
            scopes = [];
            parameters.Add((parameter.Value, slot), scopes);
        }

        if (result is NGXResult.Success)
        {
            foreach ((NGXGraphicsAPI _, NativeScope previous) in scopes)
            {
                previous.Dispose();
            }
            scopes.Clear();
        }

        scopes.Add((api, scope));
    }

    internal static void Release(NGXParameter parameter)
    {
        using Lock.Scope _ = gate.EnterScope();

        foreach ((nint Parameters, string Slot) key in parameters.Keys.Where(key => key.Parameters == parameter.Value).ToArray())
        {
            List<(NGXGraphicsAPI Api, NativeScope Scope)> scopes = parameters[key];
            parameters.Remove(key);

            for (int i = scopes.Count - 1; i >= 0; i--)
            {
                scopes[i].Scope.Dispose();
            }
        }
    }

    internal static void Release(NGXGraphicsAPI api, nint device)
    {
        using Lock.Scope _ = gate.EnterScope();

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
            List<(NGXGraphicsAPI Api, NativeScope Scope)> scopes = parameters[key];
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

        foreach ((nint _, NativeScope scope) in cudaDevices.Values)
        {
            scope.Dispose();
        }
        cudaDevices.Clear();
    }

    internal static NGXCUDADeviceNative* GetCudaDevice(NGXCUDADevice device)
    {
        using Lock.Scope _ = gate.EnterScope();

        if (cudaDevices.TryGetValue((device.CudaContext, device.CudaStream), out (nint Pointer, NativeScope Scope) retained))
        {
            return (NGXCUDADeviceNative*)retained.Pointer;
        }

        NativeScope scope = new();
        NGXCUDADeviceNative* pointer = scope.Alloc(new NGXCUDADeviceNative(device));
        cudaDevices.Add((device.CudaContext, device.CudaStream), ((nint)pointer, scope));

        return pointer;
    }
}
