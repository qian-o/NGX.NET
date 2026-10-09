using Hexa.NET.ImGui;
using Showcase.Handlers;

namespace Showcase.Models;

internal readonly struct PassArgs
{
    public required int Slot { get; init; }

    public required FrameConstants Constants { get; init; }

    public required CameraHandler Camera { get; init; }

    public required Reconstruction Reconstruction { get; init; }

    public required bool FrameGeneration { get; init; }

    public required bool Reset { get; init; }

    public required float Delta { get; init; }

    public required ImDrawDataPtr DrawData { get; init; }
}
