using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Buffbox;

public class BuffboxClientSystem : ModSystem
{
    private ICoreClientAPI? capi;
    private BuffboxHudRenderer? renderer;

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;
        renderer = new BuffboxHudRenderer(api);
        api.Event.RegisterRenderer(renderer, EnumRenderStage.Ortho, "buffbox:hud");
    }

    public override void Dispose()
    {
        if (capi is not null && renderer is not null)
        {
            capi.Event.UnregisterRenderer(renderer, EnumRenderStage.Ortho);
            renderer.Dispose();
            renderer = null;
        }
    }
}
