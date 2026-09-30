using Showcase.Helpers;
using Showcase.Models;

namespace Showcase.Passes;

internal sealed class LightingPass(RHI context, RenderResources resources) : Pass(context, resources)
{
    public override void Record(in PassArgs args)
    {
        foreach (ImageSlot slot in RenderLayout.LightingOutputs)
        {
            Context.Transition(Resources.Image(args.Slot, slot), ImageUse.Storage);
        }

        if (Context.RayQuerySupported)
        {
            Context.Transition(Resources.LightingSamples, ImageUse.Storage);
            Context.Dispatch(ComputePass.TraceLighting, Resources.InputWidth, Resources.InputHeight, args.Constants, RenderLayout.LightingPaths);
        }

        Context.Transition(Resources.LightingSamples, ImageUse.ShaderRead);
        Context.Dispatch(ComputePass.Lighting, Resources.InputWidth, Resources.InputHeight, args.Constants);

        foreach (ImageSlot slot in RenderLayout.LightingOutputs)
        {
            Context.Transition(Resources.Image(args.Slot, slot), ImageUse.ShaderRead);
        }
    }
}
