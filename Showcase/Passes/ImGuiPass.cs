using Showcase.Models;

namespace Showcase.Passes;

internal class ImGuiPass(RHI context, RenderResources resources) : Pass(context, resources)
{
    public override void Record(in PassArgs args)
    {
        Context.DrawUI(args.DrawData);
        Context.Transition(Resources.Image(args.Slot, ImageSlot.UI), ImageUse.ShaderRead);
        Context.Transition(Resources.Image(args.Slot, ImageSlot.Final), ImageUse.Storage);
        Context.Dispatch(ComputePass.Composite, Resources.OutputWidth, Resources.OutputHeight, args.Constants);
    }
}
