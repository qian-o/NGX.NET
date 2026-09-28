namespace Streamline.NET;

internal static unsafe partial class SLNative
{
    static SLNative()
    {
        StreamlineLibrary.Register();
    }
}
