using Showcase.Helpers;
using Showcase.Models;

namespace Showcase.Passes;

internal sealed class GeometryPass(RHI context, RenderResources resources) : Pass(context, resources)
{
    public override void Record(in PassArgs args)
    {
        if (Context.RayQuerySupported)
        {
            Context.UpdateRayTracingScene();
        }
        else
        {
            Context.DrawShadow();
        }

        Context.Transition(Resources.Image(args.Slot, ImageSlot.Shadow), ImageUse.ShaderRead);
        Context.DrawScene();

        foreach (ImageSlot slot in RenderLayout.GeometryOutputs)
        {
            Context.Transition(Resources.Image(args.Slot, slot), ImageUse.ShaderRead);
        }
    }
}
