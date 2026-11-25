namespace NGX.NET;

[Flags]
public enum NGXDLSSGEvalFlags : int
{
    None = 0,

    UpdateOnlyInsideExtents = 1 << 0,
}
