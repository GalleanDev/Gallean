namespace Gallean.Engine.Contracts;

public sealed record WorldSubrace(
    WorldSubraceId Id,
    WorldPlayerId PlayerId,
    WorldSubraceType Subrace,
    WorldObjectVariant BannerVariant);
