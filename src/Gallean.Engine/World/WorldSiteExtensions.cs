namespace Gallean.Engine;

public static class WorldSiteExtensions
{
    extension(WorldSite site)
    {
        public WorldPosition Entrance => FortificationCalculator.GetEntrance(site.Footprint);
    }
}
