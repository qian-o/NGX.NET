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
}
