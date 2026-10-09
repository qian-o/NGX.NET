using Showcase.Models;

namespace Showcase.Passes;

internal class ReconstructionPass(RHI context, RenderResources resources) : Pass(context, resources)
{
    public override void Record(in PassArgs args)
    {
        if (args.Reconstruction is Reconstruction.Native)
        {
            return;
        }

        GpuImage output = Resources.Image(args.Slot, ImageSlot.Reconstructed);
        Context.Transition(output, ImageUse.Storage);
        Context.NGX.Evaluate(Context.Command, Resources.Frames[args.Slot], args.Camera, args.Reset, args.Delta);
        Context.Transition(output, ImageUse.ShaderRead);
    }
}
