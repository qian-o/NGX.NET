namespace Streamline.NET;

public unsafe partial struct PCLHelper
{
    /// <summary>
    /// Returns the stored native PCL marker.
    /// </summary>
    public readonly PCLMarker Get()
    {
        return marker;
    }
}
