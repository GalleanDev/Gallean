namespace GameContent;

public sealed record CapitalSceneLayer(
    int Order,
    Db.Id? BuildingId,
    string ImageName,
    int X,
    int Y);
