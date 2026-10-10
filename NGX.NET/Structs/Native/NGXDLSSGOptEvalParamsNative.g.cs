#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 592)]
internal unsafe struct NGXDLSSGOptEvalParamsNative : IDisposable
{
    [FieldOffset(0)]
    public uint MultiFrameCount;

    [FieldOffset(4)]
    public uint MultiFrameIndex;

    [FieldOffset(8)]
    public Matrix4x4 CameraViewToClip;

    [FieldOffset(72)]
    public Matrix4x4 ClipToCameraView;

    [FieldOffset(136)]
    public Matrix4x4 ClipToLensClip;

    [FieldOffset(200)]
    public Matrix4x4 ClipToPrevClip;

    [FieldOffset(264)]
    public Matrix4x4 PrevClipToClip;

    [FieldOffset(328)]
    public Vector2 JitterOffset;

    [FieldOffset(336)]
    public Vector2 MvecScale;

    [FieldOffset(344)]
    public Vector2 CameraPinholeOffset;

    [FieldOffset(352)]
    public Vector3 CameraPos;

    [FieldOffset(364)]
    public Vector3 CameraUp;

    [FieldOffset(376)]
    public Vector3 CameraRight;

    [FieldOffset(388)]
    public Vector3 CameraFwd;

    [FieldOffset(400)]
    public float CameraNear;

    [FieldOffset(404)]
    public float CameraFar;

    [FieldOffset(408)]
    public float CameraFOV;

    [FieldOffset(412)]
    public float CameraAspectRatio;

    [FieldOffset(416)]
    public Bool8 ColorBuffersHDR;

    [FieldOffset(417)]
    public Bool8 DepthInverted;

    [FieldOffset(418)]
    public Bool8 CameraMotionIncluded;

    [FieldOffset(419)]
    public Bool8 Reset;

    [FieldOffset(420)]
    public Bool8 AutomodeOverrideReset;

    [FieldOffset(421)]
    public Bool8 NotRenderingGameFrames;

    [FieldOffset(422)]
    public Bool8 OrthoProjection;

    [FieldOffset(424)]
    public float MotionVectorsInvalidValue;

    [FieldOffset(428)]
    public Bool8 MotionVectorsDilated;

    [FieldOffset(429)]
    public Bool8 MenuDetectionEnabled;

    [FieldOffset(432)]
    public NGXCoordinatesNative MvecsSubrectBase;

    [FieldOffset(440)]
    public NGXDimensionsNative MvecsSubrectSize;

    [FieldOffset(448)]
    public NGXCoordinatesNative DepthSubrectBase;

    [FieldOffset(456)]
    public NGXDimensionsNative DepthSubrectSize;

    [FieldOffset(464)]
    public NGXCoordinatesNative HudLessSubrectBase;

    [FieldOffset(472)]
    public NGXDimensionsNative HudLessSubrectSize;

    [FieldOffset(480)]
    public NGXCoordinatesNative UiSubrectBase;

    [FieldOffset(488)]
    public NGXDimensionsNative UiSubrectSize;

    [FieldOffset(496)]
    public NGXCoordinatesNative UiAlphaSubrectBase;

    [FieldOffset(504)]
    public NGXDimensionsNative UiAlphaSubrectSize;

    [FieldOffset(512)]
    public NGXCoordinatesNative BidirectionalDistFieldSubrectBase;

    [FieldOffset(520)]
    public NGXDimensionsNative BidirectionalDistFieldSubrectSize;

    [FieldOffset(528)]
    public NGXPrecisionInfoNative BidirectionalDistFieldPrecisionInfo;

    [FieldOffset(540)]
    public float MinRelativeLinearDepthObjectSeparation;

    [FieldOffset(544)]
    public NGXCoordinatesNative BackbufferSubrectBase;

    [FieldOffset(552)]
    public NGXDimensionsNative BackbufferSubrectSize;

    [FieldOffset(560)]
    public NGXCoordinatesNative OutputInterpSubrectBase;

    [FieldOffset(568)]
    public NGXDimensionsNative OutputInterpSubrectSize;

    [FieldOffset(576)]
    public NGXCoordinatesNative OutputRealSubrectBase;

    [FieldOffset(584)]
    public NGXDimensionsNative OutputRealSubrectSize;

    public NGXDLSSGOptEvalParamsNative(in NGXDLSSGOptEvalParams value)
    {
        try
        {
            MultiFrameCount = value.MultiFrameCount;
            MultiFrameIndex = value.MultiFrameIndex;
            CameraViewToClip = value.CameraViewToClip;
            ClipToCameraView = value.ClipToCameraView;
            ClipToLensClip = value.ClipToLensClip;
            ClipToPrevClip = value.ClipToPrevClip;
            PrevClipToClip = value.PrevClipToClip;
            JitterOffset = value.JitterOffset;
            MvecScale = value.MvecScale;
            CameraPinholeOffset = value.CameraPinholeOffset;
            CameraPos = value.CameraPos;
            CameraUp = value.CameraUp;
            CameraRight = value.CameraRight;
            CameraFwd = value.CameraFwd;
            CameraNear = value.CameraNear;
            CameraFar = value.CameraFar;
            CameraFOV = value.CameraFOV;
            CameraAspectRatio = value.CameraAspectRatio;
            ColorBuffersHDR = value.ColorBuffersHDR;
            DepthInverted = value.DepthInverted;
            CameraMotionIncluded = value.CameraMotionIncluded;
            Reset = value.Reset;
            AutomodeOverrideReset = value.AutomodeOverrideReset;
            NotRenderingGameFrames = value.NotRenderingGameFrames;
            OrthoProjection = value.OrthoProjection;
            MotionVectorsInvalidValue = value.MotionVectorsInvalidValue;
            MotionVectorsDilated = value.MotionVectorsDilated;
            MenuDetectionEnabled = value.MenuDetectionEnabled;
            MvecsSubrectBase = new(in value.MvecsSubrectBase);
            MvecsSubrectSize = new(in value.MvecsSubrectSize);
            DepthSubrectBase = new(in value.DepthSubrectBase);
            DepthSubrectSize = new(in value.DepthSubrectSize);
            HudLessSubrectBase = new(in value.HudLessSubrectBase);
            HudLessSubrectSize = new(in value.HudLessSubrectSize);
            UiSubrectBase = new(in value.UiSubrectBase);
            UiSubrectSize = new(in value.UiSubrectSize);
            UiAlphaSubrectBase = new(in value.UiAlphaSubrectBase);
            UiAlphaSubrectSize = new(in value.UiAlphaSubrectSize);
            BidirectionalDistFieldSubrectBase = new(in value.BidirectionalDistFieldSubrectBase);
            BidirectionalDistFieldSubrectSize = new(in value.BidirectionalDistFieldSubrectSize);
            BidirectionalDistFieldPrecisionInfo = new(in value.BidirectionalDistFieldPrecisionInfo);
            MinRelativeLinearDepthObjectSeparation = value.MinRelativeLinearDepthObjectSeparation;
            BackbufferSubrectBase = new(in value.BackbufferSubrectBase);
            BackbufferSubrectSize = new(in value.BackbufferSubrectSize);
            OutputInterpSubrectBase = new(in value.OutputInterpSubrectBase);
            OutputInterpSubrectSize = new(in value.OutputInterpSubrectSize);
            OutputRealSubrectBase = new(in value.OutputRealSubrectBase);
            OutputRealSubrectSize = new(in value.OutputRealSubrectSize);
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        OutputRealSubrectSize.Dispose();
        OutputRealSubrectBase.Dispose();
        OutputInterpSubrectSize.Dispose();
        OutputInterpSubrectBase.Dispose();
        BackbufferSubrectSize.Dispose();
        BackbufferSubrectBase.Dispose();
        BidirectionalDistFieldPrecisionInfo.Dispose();
        BidirectionalDistFieldSubrectSize.Dispose();
        BidirectionalDistFieldSubrectBase.Dispose();
        UiAlphaSubrectSize.Dispose();
        UiAlphaSubrectBase.Dispose();
        UiSubrectSize.Dispose();
        UiSubrectBase.Dispose();
        HudLessSubrectSize.Dispose();
        HudLessSubrectBase.Dispose();
        DepthSubrectSize.Dispose();
        DepthSubrectBase.Dispose();
        MvecsSubrectSize.Dispose();
        MvecsSubrectBase.Dispose();
        this = default;
    }
}
