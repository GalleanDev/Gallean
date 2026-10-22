namespace Gallean.Engine;

public static class WorldFortificationExtensions
{
    extension(WorldFortification fortification)
    {
        public WorldPosition Entrance => FortificationCalculator.GetEntrance(fortification.Footprint);

        public WorldPlayerId? Owner => FortificationCalculator.GetOwner(fortification);
    }
}
