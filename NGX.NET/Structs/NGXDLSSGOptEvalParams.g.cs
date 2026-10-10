#nullable enable

namespace NGX.NET;

public struct NGXDLSSGOptEvalParams
{
    public uint MultiFrameCount;

    public uint MultiFrameIndex;

    public Matrix4x4 CameraViewToClip;

    public Matrix4x4 ClipToCameraView;

    public Matrix4x4 ClipToLensClip;

    public Matrix4x4 ClipToPrevClip;

    public Matrix4x4 PrevClipToClip;

    public Vector2 JitterOffset;

    public Vector2 MvecScale;

    public Vector2 CameraPinholeOffset;

    public Vector3 CameraPos;

    public Vector3 CameraUp;

    public Vector3 CameraRight;

    public Vector3 CameraFwd;

    public float CameraNear;

    public float CameraFar;

    public float CameraFOV;

    public float CameraAspectRatio;

    public bool ColorBuffersHDR;

    public bool DepthInverted;

    public bool CameraMotionIncluded;

    public bool Reset;

    public bool AutomodeOverrideReset;

    public bool NotRenderingGameFrames;

    public bool OrthoProjection;

    public float MotionVectorsInvalidValue;

    public bool MotionVectorsDilated;

    public bool MenuDetectionEnabled;

    public NGXCoordinates MvecsSubrectBase;

    public NGXDimensions MvecsSubrectSize;

    public NGXCoordinates DepthSubrectBase;

    public NGXDimensions DepthSubrectSize;

    public NGXCoordinates HudLessSubrectBase;

    public NGXDimensions HudLessSubrectSize;

    public NGXCoordinates UiSubrectBase;

    public NGXDimensions UiSubrectSize;

    public NGXCoordinates UiAlphaSubrectBase;

    public NGXDimensions UiAlphaSubrectSize;

    public NGXCoordinates BidirectionalDistFieldSubrectBase;

    public NGXDimensions BidirectionalDistFieldSubrectSize;

    public NGXPrecisionInfo BidirectionalDistFieldPrecisionInfo;

    public float MinRelativeLinearDepthObjectSeparation;

    public NGXCoordinates BackbufferSubrectBase;

    public NGXDimensions BackbufferSubrectSize;

    public NGXCoordinates OutputInterpSubrectBase;

    public NGXDimensions OutputInterpSubrectSize;

    public NGXCoordinates OutputRealSubrectBase;

    public NGXDimensions OutputRealSubrectSize;

    public NGXDLSSGOptEvalParams()
    {
        this = default;
        MultiFrameCount = 1;
        MultiFrameIndex = 1;
        MinRelativeLinearDepthObjectSeparation = 40.0f;
    }

    internal unsafe NGXDLSSGOptEvalParams(in NGXDLSSGOptEvalParamsNative native)
    {
        MultiFrameCount = native.MultiFrameCount;
        MultiFrameIndex = native.MultiFrameIndex;
        CameraViewToClip = native.CameraViewToClip;
        ClipToCameraView = native.ClipToCameraView;
        ClipToLensClip = native.ClipToLensClip;
        ClipToPrevClip = native.ClipToPrevClip;
        PrevClipToClip = native.PrevClipToClip;
        JitterOffset = native.JitterOffset;
        MvecScale = native.MvecScale;
        CameraPinholeOffset = native.CameraPinholeOffset;
        CameraPos = native.CameraPos;
        CameraUp = native.CameraUp;
        CameraRight = native.CameraRight;
        CameraFwd = native.CameraFwd;
        CameraNear = native.CameraNear;
        CameraFar = native.CameraFar;
        CameraFOV = native.CameraFOV;
        CameraAspectRatio = native.CameraAspectRatio;
        ColorBuffersHDR = native.ColorBuffersHDR;
        DepthInverted = native.DepthInverted;
        CameraMotionIncluded = native.CameraMotionIncluded;
        Reset = native.Reset;
        AutomodeOverrideReset = native.AutomodeOverrideReset;
        NotRenderingGameFrames = native.NotRenderingGameFrames;
        OrthoProjection = native.OrthoProjection;
        MotionVectorsInvalidValue = native.MotionVectorsInvalidValue;
        MotionVectorsDilated = native.MotionVectorsDilated;
        MenuDetectionEnabled = native.MenuDetectionEnabled;
        MvecsSubrectBase = new(in native.MvecsSubrectBase);
        MvecsSubrectSize = new(in native.MvecsSubrectSize);
        DepthSubrectBase = new(in native.DepthSubrectBase);
        DepthSubrectSize = new(in native.DepthSubrectSize);
        HudLessSubrectBase = new(in native.HudLessSubrectBase);
        HudLessSubrectSize = new(in native.HudLessSubrectSize);
        UiSubrectBase = new(in native.UiSubrectBase);
        UiSubrectSize = new(in native.UiSubrectSize);
        UiAlphaSubrectBase = new(in native.UiAlphaSubrectBase);
        UiAlphaSubrectSize = new(in native.UiAlphaSubrectSize);
        BidirectionalDistFieldSubrectBase = new(in native.BidirectionalDistFieldSubrectBase);
        BidirectionalDistFieldSubrectSize = new(in native.BidirectionalDistFieldSubrectSize);
        BidirectionalDistFieldPrecisionInfo = new(in native.BidirectionalDistFieldPrecisionInfo);
        MinRelativeLinearDepthObjectSeparation = native.MinRelativeLinearDepthObjectSeparation;
        BackbufferSubrectBase = new(in native.BackbufferSubrectBase);
        BackbufferSubrectSize = new(in native.BackbufferSubrectSize);
        OutputInterpSubrectBase = new(in native.OutputInterpSubrectBase);
        OutputInterpSubrectSize = new(in native.OutputInterpSubrectSize);
        OutputRealSubrectBase = new(in native.OutputRealSubrectBase);
        OutputRealSubrectSize = new(in native.OutputRealSubrectSize);
    }
}
