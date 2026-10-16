namespace NGX.NET;

public static unsafe partial class Ngx
{
    public const int DLSSDebugOverlayValueUnset = -1;
    public const uint VersionAPI = 0x0000015u;

    public static ReadOnlySpan<byte> DLSSGParameterAsyncCreateEnabled => "DLSSG.AsyncCreateEnabled\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterAutomodeOverrideReset => "DLSSG.AutomodeOverrideReset\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBackbuffer => "DLSSG.Backbuffer\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBackbufferFormat => "DLSSG.BackbufferFormat\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBackbufferFrameID => "DLSSG.BackbufferFrameID\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBackbufferSubrectBaseX => "DLSSG.BackbufferSubrectBaseX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBackbufferSubrectBaseY => "DLSSG.BackbufferSubrectBaseY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBackbufferSubrectHeight => "DLSSG.BackbufferSubrectHeight\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBackbufferSubrectWidth => "DLSSG.BackbufferSubrectWidth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBidirectionalDistortionField => "DLSSG.BidirectionalDistortionField\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBidirectionalDistortionFieldSubrectBaseX => "DLSSG.BidirectionalDistortionFieldSubrectBaseX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBidirectionalDistortionFieldSubrectBaseY => "DLSSG.BidirectionalDistortionFieldSubrectBaseY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBidirectionalDistortionFieldSubrectHeight => "DLSSG.BidirectionalDistortionFieldSubrectHeight\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBidirectionalDistortionFieldSubrectWidth => "DLSSG.BidirectionalDistortionFieldSubrectWidth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBidirectionalDistortionFieldLowPrecisionBias => "DLSSG.BidirectionalDistortionFieldLowPrecision.Bias\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBidirectionalDistortionFieldLowPrecisionIsLowPrecision => "DLSSG.BidirectionalDistortionFieldLowPrecision.IsLowPrecision\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterBidirectionalDistortionFieldLowPrecisionScale => "DLSSG.BidirectionalDistortionFieldLowPrecision.Scale\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraAspectRatio => "DLSSG.CameraAspectRatio\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraFOV => "DLSSG.CameraFOV\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraFar => "DLSSG.CameraFar\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraFwdX => "DLSSG.CameraFwdX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraFwdY => "DLSSG.CameraFwdY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraFwdZ => "DLSSG.CameraFwdZ\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraMotionIncluded => "DLSSG.CameraMotionIncluded\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraNear => "DLSSG.CameraNear\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraPinholeOffsetX => "DLSSG.CameraPinholeOffsetX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraPinholeOffsetY => "DLSSG.CameraPinholeOffsetY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraPosX => "DLSSG.CameraPosX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraPosY => "DLSSG.CameraPosY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraPosZ => "DLSSG.CameraPosZ\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraRightX => "DLSSG.CameraRightX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraRightY => "DLSSG.CameraRightY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraRightZ => "DLSSG.CameraRightZ\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraUpX => "DLSSG.CameraUpX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraUpY => "DLSSG.CameraUpY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraUpZ => "DLSSG.CameraUpZ\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterCameraViewToClip => "DLSSG.CameraViewToClip\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterClipToCameraView => "DLSSG.ClipToCameraView\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterClipToLensClip => "DLSSG.ClipToLensClip\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterClipToPrevClip => "DLSSG.ClipToPrevClip\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterColorBuffersHDR => "DLSSG.ColorBuffersHDR\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterDepth => "DLSSG.Depth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterDepthInverted => "DLSSG.DepthInverted\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterDepthSubrectBaseX => "DLSSG.DepthSubrectBaseX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterDepthSubrectBaseY => "DLSSG.DepthSubrectBaseY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterDepthSubrectHeight => "DLSSG.DepthSubrectHeight\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterDepthSubrectWidth => "DLSSG.DepthSubrectWidth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterDynamicResolution => "DLSSG.DynamicResolution\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterEstimateVRAMCallback => "DLSSG.EstimateVRAMCallback\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterEvalFlags => "DLSSG.EvalFlags\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterFullscreenMode => "DLSSG.FullscreenMode\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterHUDLess => "DLSSG.HUDLess\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterHUDLessSubrectBaseX => "DLSSG.HUDLessSubrectBaseX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterHUDLessSubrectBaseY => "DLSSG.HUDLessSubrectBaseY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterHUDLessSubrectHeight => "DLSSG.HUDLessSubrectHeight\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterHUDLessSubrectWidth => "DLSSG.HUDLessSubrectWidth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterHeight => "DLSSG.Height\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterInputBackbufferSubrectBaseX => "DLSSG.InputBackbufferSubrectBaseX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterInputBackbufferSubrectBaseY => "DLSSG.InputBackbufferSubrectBaseY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterInputBackbufferSubrectHeight => "DLSSG.InputBackbufferSubrectHeight\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterInputBackbufferSubrectWidth => "DLSSG.InputBackbufferSubrectWidth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterInternalHeight => "DLSSG.InternalHeight\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterInternalWidth => "DLSSG.InternalWidth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterInvertXAxis => "DLSSG.InvertXAxis\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterInvertYAxis => "DLSSG.InvertYAxis\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterJitterOffsetX => "DLSSG.JitterOffsetX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterJitterOffsetY => "DLSSG.JitterOffsetY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterLinearizedDepthNearFarPartition => "DLSSG.LinearizedDepth_NearFarPartition\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterLinearizedDepthScale => "DLSSG.LinearizedDepth_Scale\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterLiteCandidateBlendingEnabled => "DLSSG.LiteCandidateBlendingEnabled\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMVecs => "DLSSG.MVecs\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMVecsSubrectBaseX => "DLSSG.MVecsSubrectBaseX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMVecsSubrectBaseY => "DLSSG.MVecsSubrectBaseY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMVecsSubrectHeight => "DLSSG.MVecsSubrectHeight\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMVecsSubrectWidth => "DLSSG.MVecsSubrectWidth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMenuDetectionEnabled => "DLSSG.MenuDetectionEnabled\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMinRelativeLinearDepthObjectSeparation => "DLSSG.MinRelativeLinearDepthObjectSeparation\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMultiFrameCount => "DLSSG.MultiFrameCount\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMultiFrameCountMax => "DLSSG.MultiFrameCountMax\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMultiFrameIndex => "DLSSG.MultiFrameIndex\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMvecDilated => "DLSSG.MvecDilated\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMvecInvalidValue => "DLSSG.MvecInvalidValue\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMvecJittered => "DLSSG.MvecJittered\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMvecScaleX => "DLSSG.MvecScaleX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterMvecScaleY => "DLSSG.MvecScaleY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterNotRenderingGameFrames => "DLSSG.NotRenderingGameFrames\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOrthoProjection => "DLSSG.OrthoProjection\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOutputDisableInterpolation => "DLSSG.OutputDisableInterpolation\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOutputInterpolated => "DLSSG.OutputInterpolated\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOutputInterpolatedSubrectBaseX => "DLSSG.OutputInterpolatedSubrectBaseX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOutputInterpolatedSubrectBaseY => "DLSSG.OutputInterpolatedSubrectBaseY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOutputInterpolatedSubrectHeight => "DLSSG.OutputInterpolatedSubrectHeight\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOutputInterpolatedSubrectWidth => "DLSSG.OutputInterpolatedSubrectWidth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOutputReal => "DLSSG.OutputReal\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOutputRealSubrectBaseX => "DLSSG.OutputRealSubrectBaseX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOutputRealSubrectBaseY => "DLSSG.OutputRealSubrectBaseY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOutputRealSubrectHeight => "DLSSG.OutputRealSubrectHeight\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterOutputRealSubrectWidth => "DLSSG.OutputRealSubrectWidth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterPrevClipToClip => "DLSSG.PrevClipToClip\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterReset => "DLSSG.Reset\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterResourceAlwaysProvidedFlags => "DLSSG.ResourceAlwaysProvidedFlags\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterResourceNeverProvidedFlags => "DLSSG.ResourceNeverProvidedFlags\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterTargetFrameRate => "DLSSG.TargetFrameRate\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUI => "DLSSG.UI\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUIAlpha => "DLSSG.UIAlpha\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUIAlphaSubrectBaseX => "DLSSG.UIAlphaSubrectBaseX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUIAlphaSubrectBaseY => "DLSSG.UIAlphaSubrectBaseY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUIAlphaSubrectHeight => "DLSSG.UIAlphaSubrectHeight\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUIAlphaSubrectWidth => "DLSSG.UIAlphaSubrectWidth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUISubrectBaseX => "DLSSG.UISubrectBaseX\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUISubrectBaseY => "DLSSG.UISubrectBaseY\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUISubrectHeight => "DLSSG.UISubrectHeight\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUISubrectWidth => "DLSSG.UISubrectWidth\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUserDebugText => "DLSSG.UserDebugText\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterUserInterfaceRecompositionEnabled => "DLSSG.UserInterfaceRecompositionEnabled\0"u8;

    public static ReadOnlySpan<byte> DLSSGParameterWidth => "DLSSG.Width\0"u8;

    public static ReadOnlySpan<byte> EParameterAlbedo => [0x23, 0x21, 0x00];

    public static ReadOnlySpan<byte> EParameterBlendFactor => [0x23, 0x26, 0x00];

    public static ReadOnlySpan<byte> EParameterBufferAllocCallback => [0x23, 0x32, 0x00];

    public static ReadOnlySpan<byte> EParameterColor => [0x23, 0x1E, 0x00];

    public static ReadOnlySpan<byte> EParameterColorFormat => [0x23, 0x1F, 0x00];

    public static ReadOnlySpan<byte> EParameterColorSizeInBytes => [0x23, 0x20, 0x00];

    public static ReadOnlySpan<byte> EParameterCreationNodeMask => [0x23, 0x35, 0x00];

    public static ReadOnlySpan<byte> EParameterDLSSMode => [0x23, 0x41, 0x00];

    public static ReadOnlySpan<byte> EParameterDLSSOptimalSettingsCallback => [0x23, 0x3E, 0x00];

    public static ReadOnlySpan<byte> EParameterDeepDVCAvailable => [0x23, 0x46, 0x00];

    public static ReadOnlySpan<byte> EParameterDeepResolveAvailable => [0x23, 0x42, 0x00];

    public static ReadOnlySpan<byte> EParameterDeprecated43 => [0x23, 0x43, 0x00];

    public static ReadOnlySpan<byte> EParameterDepth => [0x23, 0x3D, 0x00];

    public static ReadOnlySpan<byte> EParameterEvaluationNode => [0x23, 0x17, 0x00];

    public static ReadOnlySpan<byte> EParameterFormat => [0x23, 0x2F, 0x00];

    public static ReadOnlySpan<byte> EParameterGraphicsAPI => [0x23, 0x47, 0x00];

    public static ReadOnlySpan<byte> EParameterHeight => [0x23, 0x11, 0x00];

    public static ReadOnlySpan<byte> EParameterHintUseFireflySwatter => [0x23, 0x3A, 0x00];

    public static ReadOnlySpan<byte> EParameterImageSignalProcessingAvailable => [0x23, 0x09, 0x00];

    public static ReadOnlySpan<byte> EParameterImageSuperResolutionAvailable => [0x23, 0x03, 0x00];

    public static ReadOnlySpan<byte> EParameterImageSuperResolutionScaleFactor21 => [0x23, 0x0A, 0x00];

    public static ReadOnlySpan<byte> EParameterImageSuperResolutionScaleFactor31 => [0x23, 0x0B, 0x00];

    public static ReadOnlySpan<byte> EParameterImageSuperResolutionScaleFactor32 => [0x23, 0x0C, 0x00];

    public static ReadOnlySpan<byte> EParameterImageSuperResolutionScaleFactor43 => [0x23, 0x0D, 0x00];

    public static ReadOnlySpan<byte> EParameterInPaintingAvailable => [0x23, 0x02, 0x00];

    public static ReadOnlySpan<byte> EParameterInput1 => [0x23, 0x18, 0x00];

    public static ReadOnlySpan<byte> EParameterInput1Format => [0x23, 0x19, 0x00];

    public static ReadOnlySpan<byte> EParameterInput1SizeInBytes => [0x23, 0x1A, 0x00];

    public static ReadOnlySpan<byte> EParameterInput2 => [0x23, 0x1B, 0x00];

    public static ReadOnlySpan<byte> EParameterInput2Format => [0x23, 0x1C, 0x00];

    public static ReadOnlySpan<byte> EParameterInput2SizeInBytes => [0x23, 0x1D, 0x00];

    public static ReadOnlySpan<byte> EParameterIsDevSnippetBranch => [0x23, 0x45, 0x00];

    public static ReadOnlySpan<byte> EParameterMvOffsetX => [0x23, 0x38, 0x00];

    public static ReadOnlySpan<byte> EParameterMvOffsetY => [0x23, 0x39, 0x00];

    public static ReadOnlySpan<byte> EParameterMvScaleX => [0x23, 0x2C, 0x00];

    public static ReadOnlySpan<byte> EParameterMvScaleY => [0x23, 0x2D, 0x00];

    public static ReadOnlySpan<byte> EParameterModel => [0x23, 0x2E, 0x00];

    public static ReadOnlySpan<byte> EParameterMotionVectors => [0x23, 0x27, 0x00];

    public static ReadOnlySpan<byte> EParameterNumFrames => [0x23, 0x0E, 0x00];

    public static ReadOnlySpan<byte> EParameterOptLevel => [0x23, 0x44, 0x00];

    public static ReadOnlySpan<byte> EParameterOutHeight => [0x23, 0x13, 0x00];

    public static ReadOnlySpan<byte> EParameterOutWidth => [0x23, 0x12, 0x00];

    public static ReadOnlySpan<byte> EParameterOutput => [0x23, 0x22, 0x00];

    public static ReadOnlySpan<byte> EParameterOutputFormat => [0x23, 0x23, 0x00];

    public static ReadOnlySpan<byte> EParameterOutputSizeInBytes => [0x23, 0x24, 0x00];

    public static ReadOnlySpan<byte> EParameterPerfQualityValue => [0x23, 0x3F, 0x00];

    public static ReadOnlySpan<byte> EParameterPreviousOutput => [0x23, 0x37, 0x00];

    public static ReadOnlySpan<byte> EParameterRTXValue => [0x23, 0x40, 0x00];

    public static ReadOnlySpan<byte> EParameterRectH => [0x23, 0x2B, 0x00];

    public static ReadOnlySpan<byte> EParameterRectW => [0x23, 0x2A, 0x00];

    public static ReadOnlySpan<byte> EParameterRectX => [0x23, 0x28, 0x00];

    public static ReadOnlySpan<byte> EParameterRectY => [0x23, 0x29, 0x00];

    public static ReadOnlySpan<byte> EParameterReserved00 => [0x23, 0x00, 0x00];

    public static ReadOnlySpan<byte> EParameterReserved06 => [0x23, 0x06, 0x00];

    public static ReadOnlySpan<byte> EParameterReserved07 => [0x23, 0x07, 0x00];

    public static ReadOnlySpan<byte> EParameterReserved08 => [0x23, 0x08, 0x00];

    public static ReadOnlySpan<byte> EParameterReserved48 => [0x23, 0x48, 0x00];

    public static ReadOnlySpan<byte> EParameterReserved49 => [0x23, 0x49, 0x00];

    public static ReadOnlySpan<byte> EParameterReset => [0x23, 0x25, 0x00];

    public static ReadOnlySpan<byte> EParameterResourceAllocCallback => [0x23, 0x31, 0x00];

    public static ReadOnlySpan<byte> EParameterResourceReleaseCallback => [0x23, 0x34, 0x00];

    public static ReadOnlySpan<byte> EParameterResourceHeight => [0x23, 0x3C, 0x00];

    public static ReadOnlySpan<byte> EParameterResourceWidth => [0x23, 0x3B, 0x00];

    public static ReadOnlySpan<byte> EParameterScale => [0x23, 0x0F, 0x00];

    public static ReadOnlySpan<byte> EParameterScratch => [0x23, 0x15, 0x00];

    public static ReadOnlySpan<byte> EParameterScratchSizeInBytes => [0x23, 0x16, 0x00];

    public static ReadOnlySpan<byte> EParameterSharpness => [0x23, 0x14, 0x00];

    public static ReadOnlySpan<byte> EParameterSizeInBytes => [0x23, 0x30, 0x00];

    public static ReadOnlySpan<byte> EParameterSlowMotionAvailable => [0x23, 0x04, 0x00];

    public static ReadOnlySpan<byte> EParameterSuperSamplingAvailable => [0x23, 0x01, 0x00];

    public static ReadOnlySpan<byte> EParameterTex2DAllocCallback => [0x23, 0x33, 0x00];

    public static ReadOnlySpan<byte> EParameterVideoSuperResolutionAvailable => [0x23, 0x05, 0x00];

    public static ReadOnlySpan<byte> EParameterVisibilityNodeMask => [0x23, 0x36, 0x00];

    public static ReadOnlySpan<byte> EParameterWidth => [0x23, 0x10, 0x00];

    public static ReadOnlySpan<byte> ParameterAlbedo => "Albedo\0"u8;

    public static ReadOnlySpan<byte> ParameterAnimatedTextureMask => "AnimatedTextureMask\0"u8;

    public static ReadOnlySpan<byte> ParameterBlendFactor => "BlendFactor\0"u8;

    public static ReadOnlySpan<byte> ParameterBufferAllocCallback => "BufferAllocCallback\0"u8;

    public static ReadOnlySpan<byte> ParameterColor => "Color\0"u8;

    public static ReadOnlySpan<byte> ParameterColorFormat => "Color.Format\0"u8;

    public static ReadOnlySpan<byte> ParameterColorSizeInBytes => "Color.SizeInBytes\0"u8;

    public static ReadOnlySpan<byte> ParameterCreationNodeMask => "CreationNodeMask\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDGetStatsCallback => "DLSSDGetStatsCallback\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDOptimalSettingsCallback => "DLSSDOptimalSettingsCallback\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDAlpha => "DLSSD.Alpha\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDAlphaSubrectBaseX => "DLSSD.Alpha.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDAlphaSubrectBaseY => "DLSSD.Alpha.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterDepthOfField => "DLSSD.ColorAfterDepthOfField\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterDepthOfFieldSubrectBaseX => "DLSSD.ColorAfterDepthOfField.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterDepthOfFieldSubrectBaseY => "DLSSD.ColorAfterDepthOfField.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterFog => "DLSSD.ColorAfterFog\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterFogSubrectBaseX => "DLSSD.ColorAfterFog.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterFogSubrectBaseY => "DLSSD.ColorAfterFog.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterParticles => "DLSSD.ColorAfterParticles\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterParticlesSubrectBaseX => "DLSSD.ColorAfterParticles.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterParticlesSubrectBaseY => "DLSSD.ColorAfterParticles.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterScreenSpaceRefraction => "DLSSD.ColorAfterScreenSpaceRefraction\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterScreenSpaceRefractionSubrectBaseX => "DLSSD.ColorAfterScreenSpaceRefraction.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterScreenSpaceRefractionSubrectBaseY => "DLSSD.ColorAfterScreenSpaceRefraction.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterScreenSpaceSubsurfaceScattering => "DLSSD.ColorAfterScreenSpaceSubsurfaceScattering\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterScreenSpaceSubsurfaceScatteringSubrectBaseX => "DLSSD.ColorAfterScreenSpaceSubsurfaceScattering.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterScreenSpaceSubsurfaceScatteringSubrectBaseY => "DLSSD.ColorAfterScreenSpaceSubsurfaceScattering.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterTransparency => "DLSSD.ColorAfterTransparency\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterTransparencySubrectBaseX => "DLSSD.ColorAfterTransparency.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorAfterTransparencySubrectBaseY => "DLSSD.ColorAfterTransparency.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeDepthOfField => "DLSSD.ColorBeforeDepthOfField\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeDepthOfFieldSubrectBaseX => "DLSSD.ColorBeforeDepthOfField.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeDepthOfFieldSubrectBaseY => "DLSSD.ColorBeforeDepthOfField.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeFog => "DLSSD.ColorBeforeFog\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeFogSubrectBaseX => "DLSSD.ColorBeforeFog.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeFogSubrectBaseY => "DLSSD.ColorBeforeFog.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeParticles => "DLSSD.ColorBeforeParticles\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeParticlesSubrectBaseX => "DLSSD.ColorBeforeParticles.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeParticlesSubrectBaseY => "DLSSD.ColorBeforeParticles.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeScreenSpaceRefraction => "DLSSD.ColorBeforeScreenSpaceRefraction\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeScreenSpaceRefractionSubrectBaseX => "DLSSD.ColorBeforeScreenSpaceRefraction.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeScreenSpaceRefractionSubrectBaseY => "DLSSD.ColorBeforeScreenSpaceRefraction.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeScreenSpaceSubsurfaceScattering => "DLSSD.ColorBeforeScreenSpaceSubsurfaceScattering\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeScreenSpaceSubsurfaceScatteringSubrectBaseX => "DLSSD.ColorBeforeScreenSpaceSubsurfaceScattering.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeScreenSpaceSubsurfaceScatteringSubrectBaseY => "DLSSD.ColorBeforeScreenSpaceSubsurfaceScattering.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeTransparency => "DLSSD.ColorBeforeTransparency\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeTransparencySubrectBaseX => "DLSSD.ColorBeforeTransparency.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDColorBeforeTransparencySubrectBaseY => "DLSSD.ColorBeforeTransparency.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDepthOfFieldGuide => "DLSSD.DepthOfFieldGuide\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDepthOfFieldGuideSubrectBaseX => "DLSSD.DepthOfFieldGuide.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDepthOfFieldGuideSubrectBaseY => "DLSSD.DepthOfFieldGuide.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDiffuseHitDistance => "DLSSD.DiffuseHitDistance\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDiffuseHitDistanceSubrectBaseX => "DLSSD.DiffuseHitDistance.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDiffuseHitDistanceSubrectBaseY => "DLSSD.DiffuseHitDistance.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDiffuseRayDirection => "DLSSD.DiffuseRayDirection\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDiffuseRayDirectionHitDistance => "DLSSD.DiffuseRayDirectionHitDistance\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDiffuseRayDirectionHitDistanceSubrectBaseX => "DLSSD.DiffuseRayDirectionHitDistance.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDiffuseRayDirectionHitDistanceSubrectBaseY => "DLSSD.DiffuseRayDirectionHitDistance.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDiffuseRayDirectionSubrectBaseX => "DLSSD.DiffuseRayDirection.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDDiffuseRayDirectionSubrectBaseY => "DLSSD.DiffuseRayDirection.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDOutputAlpha => "DLSSD.OutputAlpha\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDOutputAlphaSubrectBaseX => "DLSSD.OutputAlpha.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDOutputAlphaSubrectBaseY => "DLSSD.OutputAlpha.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDReflectedAlbedo => "DLSSD.ReflectedAlbedo\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDReflectedAlbedoSubrectBaseX => "DLSSD.ReflectedAlbedo.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDReflectedAlbedoSubrectBaseY => "DLSSD.ReflectedAlbedo.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDResponsivityMask => "DLSSD.ResponsivityMask\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDResponsivityMaskSubrectBaseX => "DLSSD.ResponsivityMask.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDResponsivityMaskSubrectBaseY => "DLSSD.ResponsivityMask.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDScreenSpaceRefractionGuide => "DLSSD.ScreenSpaceRefractionGuide\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDScreenSpaceRefractionGuideSubrectBaseX => "DLSSD.ScreenSpaceRefractionGuide.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDScreenSpaceRefractionGuideSubrectBaseY => "DLSSD.ScreenSpaceRefractionGuide.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDScreenSpaceSubsurfaceScatteringGuide => "DLSSD.ScreenSpaceSubsurfaceScatteringGuide\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDScreenSpaceSubsurfaceScatteringGuideSubrectBaseX => "DLSSD.ScreenSpaceSubsurfaceScatteringGuide.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDScreenSpaceSubsurfaceScatteringGuideSubrectBaseY => "DLSSD.ScreenSpaceSubsurfaceScatteringGuide.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDSpecularHitDistance => "DLSSD.SpecularHitDistance\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDSpecularHitDistanceSubrectBaseX => "DLSSD.SpecularHitDistance.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDSpecularHitDistanceSubrectBaseY => "DLSSD.SpecularHitDistance.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDSpecularRayDirection => "DLSSD.SpecularRayDirection\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDSpecularRayDirectionHitDistance => "DLSSD.SpecularRayDirectionHitDistance\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDSpecularRayDirectionHitDistanceSubrectBaseX => "DLSSD.SpecularRayDirectionHitDistance.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDSpecularRayDirectionHitDistanceSubrectBaseY => "DLSSD.SpecularRayDirectionHitDistance.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDSpecularRayDirectionSubrectBaseX => "DLSSD.SpecularRayDirection.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDSpecularRayDirectionSubrectBaseY => "DLSSD.SpecularRayDirection.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSGetStatsCallback => "DLSSGetStatsCallback\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSMode => "DLSSMode\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSOptimalSettingsCallback => "DLSSOptimalSettingsCallback\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSClipToPrevClipMatrix => "ClipToPrevClipMatrix\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSCheckerboardJitterHack => "DLSS.Checkerboard.Jitter.Hack\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDenoiseMode => "DLSS.Denoise.Mode\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDisocclusionMask => "DLSS.DisocclusionMask\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDisocclusionMaskSubrectBaseX => "DLSS.DisocclusionMask.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSDisocclusionMaskSubrectBaseY => "DLSS.DisocclusionMask.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSEnableOutputSubrects => "DLSS.Enable.Output.Subrects\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSExposureScale => "DLSS.Exposure.Scale\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSFeatureCreateFlags => "DLSS.Feature.Create.Flags\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSGetDynamicMaxRenderHeight => "DLSS.Get.Dynamic.Max.Render.Height\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSGetDynamicMaxRenderWidth => "DLSS.Get.Dynamic.Max.Render.Width\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSGetDynamicMinRenderHeight => "DLSS.Get.Dynamic.Min.Render.Height\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSGetDynamicMinRenderWidth => "DLSS.Get.Dynamic.Min.Render.Width\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSHintRenderPresetBalanced => "DLSS.Hint.Render.Preset.Balanced\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSHintRenderPresetDLAA => "DLSS.Hint.Render.Preset.DLAA\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSHintRenderPresetPerformance => "DLSS.Hint.Render.Preset.Performance\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSHintRenderPresetQuality => "DLSS.Hint.Render.Preset.Quality\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSHintRenderPresetUltraPerformance => "DLSS.Hint.Render.Preset.UltraPerformance\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSHintRenderPresetUltraQuality => "DLSS.Hint.Render.Preset.UltraQuality\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInvViewProjectionMatrix => "InvViewProjectionMatrix\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSIndicatorInvertXAxis => "DLSS.Indicator.Invert.X.Axis\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSIndicatorInvertYAxis => "DLSS.Indicator.Invert.Y.Axis\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputBiasCurrentColorMask => "DLSS.Input.Bias.Current.Color.Mask\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputBiasCurrentColorSubrectBaseX => "DLSS.Input.Bias.Current.Color.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputBiasCurrentColorSubrectBaseY => "DLSS.Input.Bias.Current.Color.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputColorSubrectBaseX => "DLSS.Input.Color.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputColorSubrectBaseY => "DLSS.Input.Color.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputDepthSubrectBaseX => "DLSS.Input.Depth.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputDepthSubrectBaseY => "DLSS.Input.Depth.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputDiffuseAlbedoSubrectBaseX => "DLSS.Input.DiffuseAlbedo.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputDiffuseAlbedoSubrectBaseY => "DLSS.Input.DiffuseAlbedo.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputMvSubrectBaseX => "DLSS.Input.MV.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputMvSubrectBaseY => "DLSS.Input.MV.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputNormalsSubrectBaseX => "DLSS.Input.Normals.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputNormalsSubrectBaseY => "DLSS.Input.Normals.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputRoughnessSubrectBaseX => "DLSS.Input.Roughness.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputRoughnessSubrectBaseY => "DLSS.Input.Roughness.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputSpecularAlbedoSubrectBaseX => "DLSS.Input.SpecularAlbedo.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputSpecularAlbedoSubrectBaseY => "DLSS.Input.SpecularAlbedo.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputTranslucencySubrectBaseX => "DLSS.Input.Translucency.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSInputTranslucencySubrectBaseY => "DLSS.Input.Translucency.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSOutputSubrectBaseX => "DLSS.Output.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSOutputSubrectBaseY => "DLSS.Output.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSOverlayDebugLayer => "DLSS.Overlay.Debug.Layer\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSOverlayFullScreen => "DLSS.Overlay.Full.Screen\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSOverlayJitterDebug => "DLSS.Overlay.Jitter.Debug\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSOverlayShowNans => "DLSS.Overlay.Show.Nans\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSPreExposure => "DLSS.Pre.Exposure\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSRenderSubrectDimensionsHeight => "DLSS.Render.Subrect.Dimensions.Height\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSRenderSubrectDimensionsWidth => "DLSS.Render.Subrect.Dimensions.Width\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSResponsivityMask => "DLSS.ResponsivityMask\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSResponsivityMaskSubrectBaseX => "DLSS.ResponsivityMask.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSResponsivityMaskSubrectBaseY => "DLSS.ResponsivityMask.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSRoughnessMode => "DLSS.Roughness.Mode\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSTransparencyLayer => "DLSS.TransparencyLayer\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSTransparencyLayerMvecs => "DLSS.TransparencyLayerMvecs\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSTransparencyLayerMvecsSubrectBaseX => "DLSS.TransparencyLayerMvecs.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSTransparencyLayerMvecsSubrectBaseY => "DLSS.TransparencyLayerMvecs.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSTransparencyLayerOpacity => "DLSS.TransparencyLayerOpacity\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSTransparencyLayerOpacitySubrectBaseX => "DLSS.TransparencyLayerOpacity.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSTransparencyLayerOpacitySubrectBaseY => "DLSS.TransparencyLayerOpacity.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSTransparencyLayerSubrectBaseX => "DLSS.TransparencyLayer.Subrect.Base.X\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSTransparencyLayerSubrectBaseY => "DLSS.TransparencyLayer.Subrect.Base.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSViewToClipMatrix => "ViewToClipMatrix\0"u8;

    public static ReadOnlySpan<byte> ParameterDLSSWorldToViewMatrix => "WorldToViewMatrix\0"u8;

    public static ReadOnlySpan<byte> ParameterDeepResolveAvailable => "DeepResolve.Available\0"u8;

    public static ReadOnlySpan<byte> ParameterDeepResolveFeatureInitResult => "DeepResolve.FeatureInitResult\0"u8;

    public static ReadOnlySpan<byte> ParameterDeepResolveMinDriverVersionMajor => "DeepResolve.MinDriverVersionMajor\0"u8;

    public static ReadOnlySpan<byte> ParameterDeepResolveMinDriverVersionMinor => "DeepResolve.MinDriverVersionMinor\0"u8;

    public static ReadOnlySpan<byte> ParameterDeepResolveNeedsUpdatedDriver => "DeepResolve.NeedsUpdatedDriver\0"u8;

    public static ReadOnlySpan<byte> ParameterDenoise => "Denoise\0"u8;

    public static ReadOnlySpan<byte> ParameterDepth => "Depth\0"u8;

    public static ReadOnlySpan<byte> ParameterDepthHighRes => "DepthHighRes\0"u8;

    public static ReadOnlySpan<byte> ParameterDiffuseAlbedo => "DLSS.Input.DiffuseAlbedo\0"u8;

    public static ReadOnlySpan<byte> ParameterDiffuseRayDirection => "Diffuse.Ray.Direction\0"u8;

    public static ReadOnlySpan<byte> ParameterExposureTexture => "ExposureTexture\0"u8;

    public static ReadOnlySpan<byte> ParameterFiColor1 => "Color1\0"u8;

    public static ReadOnlySpan<byte> ParameterFiColor2 => "Color2\0"u8;

    public static ReadOnlySpan<byte> ParameterFiDepth1 => "Depth1\0"u8;

    public static ReadOnlySpan<byte> ParameterFiDepth2 => "Depth2\0"u8;

    public static ReadOnlySpan<byte> ParameterFiMode => "FIMode\0"u8;

    public static ReadOnlySpan<byte> ParameterFiMotionVectors1 => "MotionVectors1\0"u8;

    public static ReadOnlySpan<byte> ParameterFiMotionVectors2 => "MotionVectors2\0"u8;

    public static ReadOnlySpan<byte> ParameterFiOfGridSize => "FIOFGridSize\0"u8;

    public static ReadOnlySpan<byte> ParameterFiOfPreset => "FIOFPreset\0"u8;

    public static ReadOnlySpan<byte> ParameterFiOutput1 => "Output1\0"u8;

    public static ReadOnlySpan<byte> ParameterFiOutput2 => "Output2\0"u8;

    public static ReadOnlySpan<byte> ParameterFiOutput3 => "Output3\0"u8;

    public static ReadOnlySpan<byte> ParameterFormat => "Format\0"u8;

    public static ReadOnlySpan<byte> ParameterFrameGenerationAvailable => "FrameGeneration.Available\0"u8;

    public static ReadOnlySpan<byte> ParameterFrameGenerationFeatureInitResult => "FrameGeneration.FeatureInitResult\0"u8;

    public static ReadOnlySpan<byte> ParameterFrameGenerationMinDriverVersionMajor => "FrameGeneration.MinDriverVersionMajor\0"u8;

    public static ReadOnlySpan<byte> ParameterFrameGenerationMinDriverVersionMinor => "FrameGeneration.MinDriverVersionMinor\0"u8;

    public static ReadOnlySpan<byte> ParameterFrameGenerationNeedsUpdatedDriver => "FrameGeneration.NeedsUpdatedDriver\0"u8;

    public static ReadOnlySpan<byte> ParameterFrameInterpolationAvailable => "FrameInterpolation.Available\0"u8;

    public static ReadOnlySpan<byte> ParameterFrameInterpolationFeatureInitResult => "FrameInterpolation.FeatureInitResult\0"u8;

    public static ReadOnlySpan<byte> ParameterFrameInterpolationMinDriverVersionMajor => "FrameInterpolation.MinDriverVersionMajor\0"u8;

    public static ReadOnlySpan<byte> ParameterFrameInterpolationMinDriverVersionMinor => "FrameInterpolation.MinDriverVersionMinor\0"u8;

    public static ReadOnlySpan<byte> ParameterFrameInterpolationNeedsUpdatedDriver => "FrameInterpolation.NeedsUpdatedDriver\0"u8;

    public static ReadOnlySpan<byte> ParameterFrameTimeDeltaInMsec => "FrameTimeDeltaInMsec\0"u8;

    public static ReadOnlySpan<byte> ParameterFreeMemOnReleaseFeature => "FreeMemOnReleaseFeature\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferAlbedo => "GBuffer.Albedo\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferAttrib10 => "GBuffer.Attrib.10\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferAttrib11 => "GBuffer.Attrib.11\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferAttrib12 => "GBuffer.Attrib.12\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferAttrib13 => "GBuffer.Attrib.13\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferAttrib14 => "GBuffer.Attrib.14\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferAttrib15 => "GBuffer.Attrib.15\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferAttrib8 => "GBuffer.Attrib.8\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferAttrib9 => "GBuffer.Attrib.9\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferDiffuseAlbedo => "GBuffer.DiffuseAlbedo\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferDisocclusionMask => "GBuffer.DisocclusionMask\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferEmissive => "GBuffer.Emissive\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferIndirectAlbedo => "GBuffer.IndirectAlbedo\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferMaterialId => "GBuffer.MaterialId\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferMetallic => "GBuffer.Metallic\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferNormals => "GBuffer.Normals\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferRoughness => "GBuffer.Roughness\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferShadingModelId => "GBuffer.ShadingModelId\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferSpecular => "GBuffer.Specular\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferSpecularAlbedo => "GBuffer.SpecularAlbedo\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferSpecularMvec => "GBuffer.SpecularMvec\0"u8;

    public static ReadOnlySpan<byte> ParameterGBufferSubsurface => "GBuffer.Subsurface\0"u8;

    public static ReadOnlySpan<byte> ParameterHeight => "Height\0"u8;

    public static ReadOnlySpan<byte> ParameterHintUseFireflySwatter => "Hint.UseFireflySwatter\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSignalProcessingAvailable => "ImageSignalProcessing.Available\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSignalProcessingFeatureInitResult => "ImageSignalProcessing.FeatureInitResult\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSignalProcessingMinDriverVersionMajor => "ImageSignalProcessing.MinDriverVersionMajor\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSignalProcessingMinDriverVersionMinor => "ImageSignalProcessing.MinDriverVersionMinor\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSignalProcessingNeedsUpdatedDriver => "ImageSignalProcessing.NeedsUpdatedDriver\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSignalProcessingScaleFactor => "ImageSignalProcessing.ScaleFactor\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSuperResolutionAvailable => "ImageSuperResolution.Available\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSuperResolutionFeatureInitResult => "ImageSuperResolution.FeatureInitResult\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSuperResolutionMinDriverVersionMajor => "ImageSuperResolution.MinDriverVersionMajor\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSuperResolutionMinDriverVersionMinor => "ImageSuperResolution.MinDriverVersionMinor\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSuperResolutionNeedsUpdatedDriver => "ImageSuperResolution.NeedsUpdatedDriver\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSuperResolutionScaleFactor21 => "ImageSuperResolution.ScaleFactor.2.1\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSuperResolutionScaleFactor31 => "ImageSuperResolution.ScaleFactor.3.1\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSuperResolutionScaleFactor32 => "ImageSuperResolution.ScaleFactor.3.2\0"u8;

    public static ReadOnlySpan<byte> ParameterImageSuperResolutionScaleFactor43 => "ImageSuperResolution.ScaleFactor.4.3\0"u8;

    public static ReadOnlySpan<byte> ParameterInPaintingAvailable => "InPainting.Available\0"u8;

    public static ReadOnlySpan<byte> ParameterInPaintingFeatureInitResult => "InPainting.FeatureInitResult\0"u8;

    public static ReadOnlySpan<byte> ParameterInPaintingMinDriverVersionMajor => "InPainting.MinDriverVersionMajor\0"u8;

    public static ReadOnlySpan<byte> ParameterInPaintingMinDriverVersionMinor => "InPainting.MinDriverVersionMinor\0"u8;

    public static ReadOnlySpan<byte> ParameterInPaintingNeedsUpdatedDriver => "InPainting.NeedsUpdatedDriver\0"u8;

    public static ReadOnlySpan<byte> ParameterInput1 => "Input1\0"u8;

    public static ReadOnlySpan<byte> ParameterInput1Format => "Input1.Format\0"u8;

    public static ReadOnlySpan<byte> ParameterInput1SizeInBytes => "Input1.SizeInBytes\0"u8;

    public static ReadOnlySpan<byte> ParameterInput2 => "Input2\0"u8;

    public static ReadOnlySpan<byte> ParameterInput2Format => "Input2.Format\0"u8;

    public static ReadOnlySpan<byte> ParameterInput2SizeInBytes => "Input2.SizeInBytes\0"u8;

    public static ReadOnlySpan<byte> ParameterIsDevSnippetBranch => "Snippet.IsDevBranch\0"u8;

    public static ReadOnlySpan<byte> ParameterIsParticleMask => "IsParticleMask\0"u8;

    public static ReadOnlySpan<byte> ParameterJitterOffsetX => "Jitter.Offset.X\0"u8;

    public static ReadOnlySpan<byte> ParameterJitterOffsetY => "Jitter.Offset.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterMvOffsetX => "MV.Offset.X\0"u8;

    public static ReadOnlySpan<byte> ParameterMvOffsetY => "MV.Offset.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterMvScaleX => "MV.Scale.X\0"u8;

    public static ReadOnlySpan<byte> ParameterMvScaleY => "MV.Scale.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterModel => "Model\0"u8;

    public static ReadOnlySpan<byte> ParameterMotionVectors => "MotionVectors\0"u8;

    public static ReadOnlySpan<byte> ParameterMotionVectors3D => "MotionVectors3D\0"u8;

    public static ReadOnlySpan<byte> ParameterMotionVectorsReflection => "MotionVectorsReflection\0"u8;

    public static ReadOnlySpan<byte> ParameterNumFrames => "NumFrames\0"u8;

    public static ReadOnlySpan<byte> ParameterOptLevel => "Snippet.OptLevel\0"u8;

    public static ReadOnlySpan<byte> ParameterOutHeight => "OutHeight\0"u8;

    public static ReadOnlySpan<byte> ParameterOutRectH => "OutRect.H\0"u8;

    public static ReadOnlySpan<byte> ParameterOutRectW => "OutRect.W\0"u8;

    public static ReadOnlySpan<byte> ParameterOutRectX => "OutRect.X\0"u8;

    public static ReadOnlySpan<byte> ParameterOutRectY => "OutRect.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterOutWidth => "OutWidth\0"u8;

    public static ReadOnlySpan<byte> ParameterOutput => "Output\0"u8;

    public static ReadOnlySpan<byte> ParameterOutputInterpolated => "DLSSG.OutputInterpolated\0"u8;

    public static ReadOnlySpan<byte> ParameterOutputReal => "DLSSG.OutputReal\0"u8;

    public static ReadOnlySpan<byte> ParameterOutputFormat => "Output.Format\0"u8;

    public static ReadOnlySpan<byte> ParameterOutputSizeInBytes => "Output.SizeInBytes\0"u8;

    public static ReadOnlySpan<byte> ParameterPerfQualityValue => "PerfQualityValue\0"u8;

    public static ReadOnlySpan<byte> ParameterPositionViewSpace => "Position.ViewSpace\0"u8;

    public static ReadOnlySpan<byte> ParameterRTXValue => "RTXValue\0"u8;

    public static ReadOnlySpan<byte> ParameterRayReconstructionHintRenderPresetBalanced => "RayReconstruction.Hint.Render.Preset.Balanced\0"u8;

    public static ReadOnlySpan<byte> ParameterRayReconstructionHintRenderPresetDLAA => "RayReconstruction.Hint.Render.Preset.DLAA\0"u8;

    public static ReadOnlySpan<byte> ParameterRayReconstructionHintRenderPresetPerformance => "RayReconstruction.Hint.Render.Preset.Performance\0"u8;

    public static ReadOnlySpan<byte> ParameterRayReconstructionHintRenderPresetQuality => "RayReconstruction.Hint.Render.Preset.Quality\0"u8;

    public static ReadOnlySpan<byte> ParameterRayReconstructionHintRenderPresetUltraPerformance => "RayReconstruction.Hint.Render.Preset.UltraPerformance\0"u8;

    public static ReadOnlySpan<byte> ParameterRayReconstructionHintRenderPresetUltraQuality => "RayReconstruction.Hint.Render.Preset.UltraQuality\0"u8;

    public static ReadOnlySpan<byte> ParameterRayTracingHitDistance => "RayTracingHitDistance\0"u8;

    public static ReadOnlySpan<byte> ParameterRectH => "Rect.H\0"u8;

    public static ReadOnlySpan<byte> ParameterRectW => "Rect.W\0"u8;

    public static ReadOnlySpan<byte> ParameterRectX => "Rect.X\0"u8;

    public static ReadOnlySpan<byte> ParameterRectY => "Rect.Y\0"u8;

    public static ReadOnlySpan<byte> ParameterReset => "Reset\0"u8;

    public static ReadOnlySpan<byte> ParameterResourceAllocCallback => "ResourceAllocCallback\0"u8;

    public static ReadOnlySpan<byte> ParameterResourceReleaseCallback => "ResourceReleaseCallback\0"u8;

    public static ReadOnlySpan<byte> ParameterResourceHeight => "ResourceHeight\0"u8;

    public static ReadOnlySpan<byte> ParameterResourceOutHeight => "ResourceOutHeight\0"u8;

    public static ReadOnlySpan<byte> ParameterResourceOutWidth => "ResourceOutWidth\0"u8;

    public static ReadOnlySpan<byte> ParameterResourceWidth => "ResourceWidth\0"u8;

    public static ReadOnlySpan<byte> ParameterScale => "Scale\0"u8;

    public static ReadOnlySpan<byte> ParameterScratch => "Scratch\0"u8;

    public static ReadOnlySpan<byte> ParameterScratchSizeInBytes => "Scratch.SizeInBytes\0"u8;

    public static ReadOnlySpan<byte> ParameterSharpness => "Sharpness\0"u8;

    public static ReadOnlySpan<byte> ParameterSizeInBytes => "SizeInBytes\0"u8;

    public static ReadOnlySpan<byte> ParameterSlowMotionAvailable => "SlowMotion.Available\0"u8;

    public static ReadOnlySpan<byte> ParameterSlowMotionFeatureInitResult => "SlowMotion.FeatureInitResult\0"u8;

    public static ReadOnlySpan<byte> ParameterSlowMotionMinDriverVersionMajor => "SlowMotion.MinDriverVersionMajor\0"u8;

    public static ReadOnlySpan<byte> ParameterSlowMotionMinDriverVersionMinor => "SlowMotion.MinDriverVersionMinor\0"u8;

    public static ReadOnlySpan<byte> ParameterSlowMotionNeedsUpdatedDriver => "SlowMotion.NeedsUpdatedDriver\0"u8;

    public static ReadOnlySpan<byte> ParameterSpecularAlbedo => "DLSS.Input.SpecularAlbedo\0"u8;

    public static ReadOnlySpan<byte> ParameterSuperSamplingDenoisingAvailable => "SuperSamplingDenoising.Available\0"u8;

    public static ReadOnlySpan<byte> ParameterSuperSamplingDenoisingFeatureInitResult => "SuperSamplingDenoising.FeatureInitResult\0"u8;

    public static ReadOnlySpan<byte> ParameterSuperSamplingDenoisingMinDriverVersionMajor => "SuperSamplingDenoising.MinDriverVersionMajor\0"u8;

    public static ReadOnlySpan<byte> ParameterSuperSamplingDenoisingMinDriverVersionMinor => "SuperSamplingDenoising.MinDriverVersionMinor\0"u8;

    public static ReadOnlySpan<byte> ParameterSuperSamplingDenoisingNeedsUpdatedDriver => "SuperSamplingDenoising.NeedsUpdatedDriver\0"u8;

    public static ReadOnlySpan<byte> ParameterSuperSamplingAvailable => "SuperSampling.Available\0"u8;

    public static ReadOnlySpan<byte> ParameterSuperSamplingFeatureInitResult => "SuperSampling.FeatureInitResult\0"u8;

    public static ReadOnlySpan<byte> ParameterSuperSamplingMinDriverVersionMajor => "SuperSampling.MinDriverVersionMajor\0"u8;

    public static ReadOnlySpan<byte> ParameterSuperSamplingMinDriverVersionMinor => "SuperSampling.MinDriverVersionMinor\0"u8;

    public static ReadOnlySpan<byte> ParameterSuperSamplingNeedsUpdatedDriver => "SuperSampling.NeedsUpdatedDriver\0"u8;

    public static ReadOnlySpan<byte> ParameterSuperSamplingScaleFactor => "SuperSampling.ScaleFactor\0"u8;

    public static ReadOnlySpan<byte> ParameterTex2DAllocCallback => "Tex2DAllocCallback\0"u8;

    public static ReadOnlySpan<byte> ParameterTonemapperType => "TonemapperType\0"u8;

    public static ReadOnlySpan<byte> ParameterTransparencyMask => "TransparencyMask\0"u8;

    public static ReadOnlySpan<byte> ParameterUseFoldedNetwork => "DLSS.Use.Folded.Network\0"u8;

    public static ReadOnlySpan<byte> ParameterUseHwDepth => "DLSS.Use.HW.Depth\0"u8;

    public static ReadOnlySpan<byte> ParameterVideoSuperResolutionAvailable => "VideoSuperResolution.Available\0"u8;

    public static ReadOnlySpan<byte> ParameterVideoSuperResolutionFeatureInitResult => "VideoSuperResolution.FeatureInitResult\0"u8;

    public static ReadOnlySpan<byte> ParameterVideoSuperResolutionMinDriverVersionMajor => "VideoSuperResolution.MinDriverVersionMajor\0"u8;

    public static ReadOnlySpan<byte> ParameterVideoSuperResolutionMinDriverVersionMinor => "VideoSuperResolution.MinDriverVersionMinor\0"u8;

    public static ReadOnlySpan<byte> ParameterVideoSuperResolutionNeedsUpdatedDriver => "VideoSuperResolution.NeedsUpdatedDriver\0"u8;

    public static ReadOnlySpan<byte> ParameterViewToClipMatrix => "ViewToClipMatrix\0"u8;

    public static ReadOnlySpan<byte> ParameterVisibilityNodeMask => "VisibilityNodeMask\0"u8;

    public static ReadOnlySpan<byte> ParameterWidth => "Width\0"u8;

    public static bool NVSDK_NGX_FAILED(NGXResult value)
    {
        return ((uint)value & 0xFFF00000u) == (uint)NGXResult.Fail;
    }

    public static bool NVSDK_NGX_SUCCEED(NGXResult value)
    {
        return ((uint)value & 0xFFF00000u) != (uint)NGXResult.Fail;
    }
}
