namespace Marshalling;

internal static class FrameGenerationRetentionChecks
{
    internal static void Run()
    {
        NGXParameter parameters = new(456);
        TrackingScope options = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.D3D12, parameters, "options", options, NGXResult.Success);
        TrackingScope previous = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.D3D12, parameters, "evaluation", previous, NGXResult.Success);

        for (int i = 0; i < 2; i++)
        {
            TrackingScope current = Storage();
            NativeLifetime.Retain(NGXGraphicsAPI.D3D12, parameters, "evaluation", current, NGXResult.Success);
            Assert(previous.IsDisposed && !current.IsDisposed && !options.IsDisposed, "Omitted options preserve the previous matrix scope");
            previous = current;
        }

        TrackingScope replacementOptions = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.D3D12, parameters, "options", replacementOptions, NGXResult.Success);
        Assert(options.IsDisposed && !replacementOptions.IsDisposed && !previous.IsDisposed, "Provided options replace only their own slot");
        NativeLifetime.Release(parameters);
        Assert(replacementOptions.IsDisposed && previous.IsDisposed, "Frame generation pointer cleanup");

        Console.WriteLine("PASS omitted DLSSG options preserve the previous matrix scope through independent slots");
    }
}
