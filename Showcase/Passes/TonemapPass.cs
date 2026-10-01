using Showcase.Models;

namespace Showcase.Passes;

internal sealed class TonemapPass(RHI context, RenderResources resources) : Pass(context, resources)
{
    public override void Record(in PassArgs args)
    {
        FrameConstants constants = args.Constants;
        constants.Parameters.Z = args.Reconstruction != Reconstruction.Native ? 1 : 0;
        GpuImage display = Resources.Image(args.Slot, ImageSlot.DisplayInput);
        GpuImage hudless = Resources.Image(args.Slot, ImageSlot.Hudless);
        Context.Transition(display, ImageUse.Storage);
        Context.Dispatch(ComputePass.ToneMap, display.Width, display.Height, constants);
        Context.Transition(display, ImageUse.ShaderRead);
        Context.Transition(hudless, ImageUse.Storage);

        // Keep native ray-traced samples intact for the reconstruction comparison.
        ComputePass resolve = args.Reconstruction == Reconstruction.Native && !Context.RayQuerySupported ? ComputePass.NativeResolve : ComputePass.CopyDisplay;
        Context.Dispatch(resolve, Resources.OutputWidth, Resources.OutputHeight, constants);
        Context.Transition(hudless, ImageUse.ShaderRead);
    }
}
