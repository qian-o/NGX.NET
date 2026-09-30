using Showcase.Models;

namespace Showcase.Passes;

internal abstract class Pass(RHI context, RenderResources resources)
{
    protected RHI Context { get; } = context;

    protected RenderResources Resources { get; } = resources;

    public abstract void Record(in PassArgs args);
}
