using Showcase.Helpers;
using Showcase.Models;

namespace Showcase.Passes;

internal class ExposurePass(RHI context, RenderResources resources) : Pass(context, resources)
{
    public override void Record(in PassArgs args)
    {
        FrameConstants constants = args.Constants;
        constants.Parameters.Z = args.Reconstruction is not Reconstruction.Native ? 1 : 0;
        GpuImage luminance = Resources.Image(args.Slot, ImageSlot.Luminance);
        GpuImage filtered = Resources.Image(args.Slot, ImageSlot.FilteredLuminance);
        GpuImage exposure = Resources.Image(args.Slot, ImageSlot.Exposure);
        Context.Transition(luminance, ImageUse.Storage);

        // Each luminance tile is reduced by one 8x8 workgroup.
        Context.Dispatch(ComputePass.PrepareLuminance, luminance.Width * 8, luminance.Height * 8, constants);
        Context.Transition(luminance, ImageUse.ShaderRead);
        Context.Transition(filtered, ImageUse.Storage);
        Context.Dispatch(ComputePass.FilterLuminance, filtered.Width, filtered.Height, constants);
        Context.Transition(filtered, ImageUse.ShaderRead);

        // Read the preceding submitted frame, including when slots wrap around.
        int previous = (args.Slot + RenderLayout.FramesInFlight - 1) % RenderLayout.FramesInFlight;
        Context.Transition(Resources.Image(previous, ImageSlot.Exposure), ImageUse.ShaderRead);
        Context.Transition(exposure, ImageUse.Storage);
        Context.Dispatch(ComputePass.MeterExposure, 1, 1, constants);
        Context.Transition(exposure, ImageUse.ShaderRead);
    }
}
