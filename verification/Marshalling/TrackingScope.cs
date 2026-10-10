namespace Marshalling;

internal class TrackingScope(Action? destroyed = null) : NativeScope
{
    protected override void Destroy()
    {
        base.Destroy();
        destroyed?.Invoke();
    }
}
