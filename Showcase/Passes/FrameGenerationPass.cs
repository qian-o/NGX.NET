using Showcase.Models;

namespace Showcase.Passes;

internal class FrameGenerationPass(RHI context, RenderResources resources) : Pass(context, resources)
{
    public GpuImage? Generated { get; private set; }

    public override void Record(in PassArgs args)
    {
        Generated = null;

        if (!args.FrameGeneration)
        {
            return;
        }

        GpuImage output = Resources.GeneratedFrames[args.Slot];
        Context.Transition(Resources.Image(args.Slot, ImageSlot.Final), ImageUse.ShaderRead);
        Context.Transition(output, ImageUse.Storage);
        bool generated = Context.NGX.Generate(Context.Command, Resources.Frames[args.Slot], output, args.Camera, args.Reset);
        Context.Transition(output, ImageUse.CopySource);
        Generated = generated ? output : null;
    }
}
