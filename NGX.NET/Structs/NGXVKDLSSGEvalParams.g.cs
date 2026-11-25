namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NGXVKDLSSGEvalParams
{
    public NGXResourceVK* Backbuffer;

    public NGXResourceVK* Depth;

    public NGXResourceVK* MVecs;

    public NGXResourceVK* Hudless;

    public NGXResourceVK* UI;

    public NGXResourceVK* UIAlpha;

    public NGXResourceVK* BidirectionalDistortionField;

    public NGXResourceVK* OutputInterpFrame;

    public NGXResourceVK* OutputRealFrame;

    public NGXResourceVK* OutputDisableInterpolation;
}
