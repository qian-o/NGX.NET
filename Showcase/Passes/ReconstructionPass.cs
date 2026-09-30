using Showcase.Models;

namespace Showcase.Passes;

internal sealed class ReconstructionPass(RHI context, RenderResources resources) : Pass(context, resources)
{
    public override void Record(in PassArgs args)
    {
        if (args.Reconstruction == Reconstruction.Native)
        {
            return;
        }

        GpuImage output = Resources.Image(args.Slot, ImageSlot.Reconstructed);
        Context.Transition(output, ImageUse.Storage);
        Context.NGX.Evaluate(Context.Command, Resources.Frames[args.Slot], args.Camera, args.Reset, args.Delta);
        Context.Transition(output, ImageUse.ShaderRead);
    }
}
