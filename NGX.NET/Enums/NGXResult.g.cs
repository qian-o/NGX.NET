#nullable enable

namespace NGX.NET;

public enum NGXResult : uint
{
    Success = 1u,

    Fail = 0xBAD00000u,

    FailFeatureNotSupported = 0xBAD00001u,

    FailPlatformError = 0xBAD00002u,

    FailFeatureAlreadyExists = 0xBAD00003u,

    FailFeatureNotFound = 0xBAD00004u,

    FailInvalidParameter = 0xBAD00005u,

    FailScratchBufferTooSmall = 0xBAD00006u,

    FailNotInitialized = 0xBAD00007u,

    FailUnsupportedInputFormat = 0xBAD00008u,

    FailRwFlagMissing = 0xBAD00009u,

    FailMissingInput = 0xBAD0000Au,

    FailUnableToInitializeFeature = 0xBAD0000Bu,

    FailOutOfDate = 0xBAD0000Cu,

    FailOutOfGpuMemory = 0xBAD0000Du,

    FailUnsupportedFormat = 0xBAD0000Eu,

    FailUnableToWriteToAppDataPath = 0xBAD0000Fu,

    FailUnsupportedParameter = 0xBAD00010u,

    FailDenied = 0xBAD00011u,

    FailNotImplemented = 0xBAD00012u,
}
