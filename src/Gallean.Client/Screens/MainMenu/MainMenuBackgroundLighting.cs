using Avalonia;

namespace Gallean.Client;

internal sealed record MainMenuBackgroundLighting(double DepthScale, IReadOnlyList<MainMenuBackgroundLight> Lights)
{
    public static MainMenuBackgroundLighting Create()
    {
        return new MainMenuBackgroundLighting(
            400.0,
            [
                new MainMenuBackgroundLight(new Point(110.0, 395.0), new Vector(22.0, 8.0), TimeSpan.FromSeconds(13.0), 0.0, 6.0, 24.0, 1.5, Theme.MainMenu.Light.WispColor, 3.0, 0.4, 0.9),
                new MainMenuBackgroundLight(new Point(185.0, 360.0), new Vector(26.0, 9.0), TimeSpan.FromSeconds(17.0), 1.1, 4.6, 14.0, 0.9, Theme.MainMenu.Light.WispPaleColor, 2.1, 0.4, 1.1),
                new MainMenuBackgroundLight(new Point(255.0, 420.0), new Vector(24.0, 10.0), TimeSpan.FromSeconds(11.0), 2.3, 6.8, 31.0, 1.95, Theme.MainMenu.Light.WispGoldColor, 3.6, 0.45, 1.4),
                new MainMenuBackgroundLight(new Point(150.0, 470.0), new Vector(30.0, 10.0), TimeSpan.FromSeconds(19.0), 3.4, 5.4, 19.0, 1.2, Theme.MainMenu.Light.WispColor, 2.6, 0.4, 0.7),
                new MainMenuBackgroundLight(new Point(335.0, 455.0), new Vector(28.0, 10.0), TimeSpan.FromSeconds(15.0), 4.6, 7.8, 41.0, 2.55, Theme.MainMenu.Light.WispGoldColor, 4.3, 0.4, 1.0),
                new MainMenuBackgroundLight(new Point(430.0, 425.0), new Vector(22.0, 8.0), TimeSpan.FromSeconds(12.0), 5.5, 4.6, 14.0, 0.9, Theme.MainMenu.Light.WispGreenColor, 2.1, 0.45, 1.3),
                new MainMenuBackgroundLight(new Point(210.0, 505.0), new Vector(26.0, 9.0), TimeSpan.FromSeconds(14.0), 0.7, 6.0, 24.0, 1.5, Theme.MainMenu.Light.WispPaleColor, 3.0, 0.4, 0.8),
                new MainMenuBackgroundLight(new Point(280.0, 545.0), new Vector(32.0, 10.0), TimeSpan.FromSeconds(21.0), 1.9, 6.8, 31.0, 1.95, Theme.MainMenu.Light.WispColor, 3.6, 0.35, 0.6),
                new MainMenuBackgroundLight(new Point(350.0, 580.0), new Vector(26.0, 8.0), TimeSpan.FromSeconds(16.0), 3.0, 5.4, 19.0, 1.2, Theme.MainMenu.Light.WispGreenColor, 2.6, 0.4, 1.2),
                new MainMenuBackgroundLight(new Point(465.0, 520.0), new Vector(24.0, 10.0), TimeSpan.FromSeconds(13.0), 4.1, 4.6, 14.0, 0.9, Theme.MainMenu.Light.WispGoldColor, 2.1, 0.45, 0.9),
                new MainMenuBackgroundLight(new Point(405.0, 555.0), new Vector(28.0, 9.0), TimeSpan.FromSeconds(18.0), 5.2, 7.8, 41.0, 2.55, Theme.MainMenu.Light.WispColor, 4.3, 0.4, 1.1),
                new MainMenuBackgroundLight(new Point(520.0, 470.0), new Vector(20.0, 8.0), TimeSpan.FromSeconds(10.0), 0.4, 5.4, 19.0, 1.2, Theme.MainMenu.Light.WispPaleColor, 2.6, 0.5, 1.5),
                new MainMenuBackgroundLight(new Point(300.0, 395.0), new Vector(22.0, 8.0), TimeSpan.FromSeconds(12.0), 2.8, 6.0, 24.0, 1.5, Theme.MainMenu.Light.WispGreenColor, 3.0, 0.45, 1.0),
                new MainMenuBackgroundLight(new Point(380.0, 500.0), new Vector(26.0, 9.0), TimeSpan.FromSeconds(16.0), 3.9, 4.6, 14.0, 0.9, Theme.MainMenu.Light.WispColor, 2.1, 0.4, 1.3),
                new MainMenuBackgroundLight(new Point(490.0, 580.0), new Vector(24.0, 8.0), TimeSpan.FromSeconds(14.0), 1.5, 6.8, 31.0, 1.95, Theme.MainMenu.Light.WispGoldColor, 3.6, 0.4, 0.8),
                new MainMenuBackgroundLight(new Point(585.0, 512.0), new Vector(4.0, 2.0), TimeSpan.FromSeconds(4.8), 0.0, 10.0, 60.0, 3.5, Theme.MainMenu.Light.EmberColor, 5.5, 0.45, 2.2)
            ]);
    }
}
