#nullable enable

namespace NGX.NET;

public readonly struct Extensions(string[] instanceExtensions, string[] deviceExtensions)
{
    public string[] InstanceExtensions { get; } = instanceExtensions;

    public string[] DeviceExtensions { get; } = deviceExtensions;
}
