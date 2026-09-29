namespace Showcase.Models;

// Values shared by the official NVAPI and VK_NV_low_latency2 marker enums.
internal enum LatencyMarker
{
    SimulationStart = 0,
    SimulationEnd = 1,
    RenderSubmitStart = 2,
    RenderSubmitEnd = 3,
    PresentStart = 4,
    PresentEnd = 5,
    InputSample = 6,
    TriggerFlash = 7,
    PCLatencyPing = 8,
    OutOfBandRenderSubmitStart = 9,
    OutOfBandRenderSubmitEnd = 10,
    OutOfBandPresentStart = 11,
    OutOfBandPresentEnd = 12
}
