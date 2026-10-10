namespace NGX.NET;

[Flags]
public enum NGXDLSSGEvalFlags : int
{
    UpdateOnlyInsideExtents = 1 << 0,

    None = 0
}
