namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 592)]
internal unsafe struct NGXDLSSGOptEvalParamsNative(in NGXDLSSGOptEvalParams value)
{
    [FieldOffset(0)]
    public uint MultiFrameCount = value.MultiFrameCount;

    [FieldOffset(4)]
    public uint MultiFrameIndex = value.MultiFrameIndex;

    [FieldOffset(8)]
    public Matrix4x4 CameraViewToClip = value.CameraViewToClip;

    [FieldOffset(72)]
    public Matrix4x4 ClipToCameraView = value.ClipToCameraView;

    [FieldOffset(136)]
    public Matrix4x4 ClipToLensClip = value.ClipToLensClip;

    [FieldOffset(200)]
    public Matrix4x4 ClipToPrevClip = value.ClipToPrevClip;

    [FieldOffset(264)]
    public Matrix4x4 PrevClipToClip = value.PrevClipToClip;

    [FieldOffset(328)]
    public Vector2 JitterOffset = value.JitterOffset;

    [FieldOffset(336)]
    public Vector2 MvecScale = value.MvecScale;

    [FieldOffset(344)]
    public Vector2 CameraPinholeOffset = value.CameraPinholeOffset;

    [FieldOffset(352)]
    public Vector3 CameraPos = value.CameraPos;

    [FieldOffset(364)]
    public Vector3 CameraUp = value.CameraUp;

    [FieldOffset(376)]
    public Vector3 CameraRight = value.CameraRight;

    [FieldOffset(388)]
    public Vector3 CameraFwd = value.CameraFwd;

    [FieldOffset(400)]
    public float CameraNear = value.CameraNear;

    [FieldOffset(404)]
    public float CameraFar = value.CameraFar;

    [FieldOffset(408)]
    public float CameraFOV = value.CameraFOV;

    [FieldOffset(412)]
    public float CameraAspectRatio = value.CameraAspectRatio;

    [FieldOffset(416)]
    public Bool8 ColorBuffersHDR = value.ColorBuffersHDR;

    [FieldOffset(417)]
    public Bool8 DepthInverted = value.DepthInverted;

    [FieldOffset(418)]
    public Bool8 CameraMotionIncluded = value.CameraMotionIncluded;

    [FieldOffset(419)]
    public Bool8 Reset = value.Reset;

    [FieldOffset(420)]
    public Bool8 AutomodeOverrideReset = value.AutomodeOverrideReset;

    [FieldOffset(421)]
    public Bool8 NotRenderingGameFrames = value.NotRenderingGameFrames;

    [FieldOffset(422)]
    public Bool8 OrthoProjection = value.OrthoProjection;

    [FieldOffset(424)]
    public float MotionVectorsInvalidValue = value.MotionVectorsInvalidValue;

    [FieldOffset(428)]
    public Bool8 MotionVectorsDilated = value.MotionVectorsDilated;

    [FieldOffset(429)]
    public Bool8 MenuDetectionEnabled = value.MenuDetectionEnabled;

    [FieldOffset(432)]
    public NGXCoordinatesNative MvecsSubrectBase = new(in value.MvecsSubrectBase);

    [FieldOffset(440)]
    public NGXDimensionsNative MvecsSubrectSize = new(in value.MvecsSubrectSize);

    [FieldOffset(448)]
    public NGXCoordinatesNative DepthSubrectBase = new(in value.DepthSubrectBase);

    [FieldOffset(456)]
    public NGXDimensionsNative DepthSubrectSize = new(in value.DepthSubrectSize);

    [FieldOffset(464)]
    public NGXCoordinatesNative HudLessSubrectBase = new(in value.HudLessSubrectBase);

    [FieldOffset(472)]
    public NGXDimensionsNative HudLessSubrectSize = new(in value.HudLessSubrectSize);

    [FieldOffset(480)]
    public NGXCoordinatesNative UiSubrectBase = new(in value.UiSubrectBase);

    [FieldOffset(488)]
    public NGXDimensionsNative UiSubrectSize = new(in value.UiSubrectSize);

    [FieldOffset(496)]
    public NGXCoordinatesNative UiAlphaSubrectBase = new(in value.UiAlphaSubrectBase);

    [FieldOffset(504)]
    public NGXDimensionsNative UiAlphaSubrectSize = new(in value.UiAlphaSubrectSize);

    [FieldOffset(512)]
    public NGXCoordinatesNative BidirectionalDistFieldSubrectBase = new(in value.BidirectionalDistFieldSubrectBase);

    [FieldOffset(520)]
    public NGXDimensionsNative BidirectionalDistFieldSubrectSize = new(in value.BidirectionalDistFieldSubrectSize);

    [FieldOffset(528)]
    public NGXPrecisionInfoNative BidirectionalDistFieldPrecisionInfo = new(in value.BidirectionalDistFieldPrecisionInfo);

    [FieldOffset(540)]
    public float MinRelativeLinearDepthObjectSeparation = value.MinRelativeLinearDepthObjectSeparation;

    [FieldOffset(544)]
    public NGXCoordinatesNative BackbufferSubrectBase = new(in value.BackbufferSubrectBase);

    [FieldOffset(552)]
    public NGXDimensionsNative BackbufferSubrectSize = new(in value.BackbufferSubrectSize);

    [FieldOffset(560)]
    public NGXCoordinatesNative OutputInterpSubrectBase = new(in value.OutputInterpSubrectBase);

    [FieldOffset(568)]
    public NGXDimensionsNative OutputInterpSubrectSize = new(in value.OutputInterpSubrectSize);

    [FieldOffset(576)]
    public NGXCoordinatesNative OutputRealSubrectBase = new(in value.OutputRealSubrectBase);

    [FieldOffset(584)]
    public NGXDimensionsNative OutputRealSubrectSize = new(in value.OutputRealSubrectSize);
}
