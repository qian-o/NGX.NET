#nullable enable

namespace NGX.NET;

public enum NGXFeature : int
{
    Reserved0 = 0,

    SuperSampling = 1,

    InPainting = 2,

    ImageSuperResolution = 3,

    SlowMotion = 4,

    VideoSuperResolution = 5,

    Reserved1 = 6,

    Reserved2 = 7,

    Reserved3 = 8,

    ImageSignalProcessing = 9,

    DeepResolve = 10,

    FrameGeneration = 11,

    DeepDvc = 12,

    RayReconstruction = 13,

    Reserved14 = 14,

    Reserved15 = 15,

    Reserved16 = 16,

    Reserved17 = 17,

    Reserved18 = 18,

    Count = 19,

    ReservedSdk = 32764,

    ReservedCore = 32765,

    ReservedUnknown = 32766,
}
