namespace Gallean.Client;

internal sealed record WorldScreenSnapshot(
    WorldRenderRequest RenderRequest,
    WorldRenderContent WorldContent,
    WorldRenderState WorldState);
