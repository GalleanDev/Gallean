using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class ThemeTests
{
    private static Dictionary<string, Color?> GetThemeBrushTokens()
    {
        Dictionary<string, Color?> tokens = [];

        foreach (Type type in typeof(Theme).GetNestedTypes(BindingFlags.Public))
        {
            string keyPrefix = type.Name == nameof(Theme.Common) ? string.Empty : type.Name;
            AddThemeBrushTokens(tokens, type, keyPrefix);
        }

        return tokens;
    }

    private static void AddThemeBrushTokens(Dictionary<string, Color?> tokens, Type type, string keyPrefix)
    {
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (property.PropertyType != typeof(Color))
            {
                continue;
            }

            if (!property.Name.EndsWith("Color", StringComparison.Ordinal))
            {
                continue;
            }

            string propertyName = property.Name[..^"Color".Length];
            string resourceKey = $"{keyPrefix}{propertyName}Brush";
            object? value = property.GetValue(null);
            Color color = Assert.IsOfType<Color>(value);
            tokens.Add(resourceKey, color);
        }

        foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public))
        {
            AddThemeBrushTokens(tokens, nestedType, $"{keyPrefix}{nestedType.Name}");
        }
    }

    [AvaloniaTest]
    public void ResolveTokensTest()
    {
        Application app = Assert.NotNull(Application.Current);

        Assert.That(app.TryFindResource("FontSize4Xl", null, out object? display), Is.True);
        Assert.That(display, Is.EqualTo(56.0));

        Assert.That(app.TryFindResource("PrimaryTextBrush", null, out object? primaryText), Is.True);
        ISolidColorBrush primaryTextBrush = Assert.IsOfType<ISolidColorBrush>(primaryText);
        Assert.That(primaryTextBrush.Color, Is.EqualTo(Theme.Common.PrimaryTextColor));

        Assert.That(app.TryFindResource("CardBackgroundBrush", null, out object? cardBackground), Is.True);
        ISolidColorBrush cardBackgroundBrush = Assert.IsOfType<ISolidColorBrush>(cardBackground);
        Assert.That(cardBackgroundBrush.Color, Is.EqualTo(Theme.Card.BackgroundColor));

        Assert.That(app.TryFindResource("BorderThicknessLg", null, out object? borderThickness), Is.True);
        Assert.That(borderThickness, Is.EqualTo(new Thickness(4)));
    }

    [AvaloniaTest]
    public void BrushTokensTest()
    {
        Application app = Assert.NotNull(Application.Current);
        Dictionary<string, Color?> expected = GetThemeBrushTokens();
        Dictionary<string, Color?> actual = [];

        foreach (KeyValuePair<string, Color?> token in expected)
        {
            Color? actualColor = null;
            if (app.TryFindResource(token.Key, null, out object? resource))
            {
                if (resource is ISolidColorBrush brush)
                {
                    actualColor = brush.Color;
                }
            }

            actual.Add(token.Key, actualColor);
        }

        Assert.That(actual, Is.EqualTo(expected));
    }

    [AvaloniaTest]
    public void AccentTokensTest()
    {
        Application app = Assert.NotNull(Application.Current);
        app.TryFindResource("AccentBrush", null, out object? accent);
        app.TryFindResource("FocusRingBrush", null, out object? focusRing);
        app.TryFindResource("ModalRuleGradientBrush", null, out object? rule);
        ISolidColorBrush accentBrush = Assert.IsOfType<ISolidColorBrush>(accent);
        ISolidColorBrush focusRingBrush = Assert.IsOfType<ISolidColorBrush>(focusRing);
        IGradientBrush ruleBrush = Assert.IsOfType<IGradientBrush>(rule);

        Assert.That(accentBrush.Color, Is.EqualTo(Color.FromRgb(243, 217, 147)));
        Assert.That(focusRingBrush.Color, Is.EqualTo(Color.FromRgb(243, 217, 147)));
        Assert.That(focusRingBrush.Opacity, Is.EqualTo(0.86));
        Assert.That(ruleBrush.Opacity, Is.EqualTo(0.67));
        Assert.That(ruleBrush.GradientStops.Select(stop => stop.Color), Is.EqualTo([
            Color.FromArgb(0, 138, 98, 66),
            Color.FromRgb(138, 98, 66),
            Color.FromRgb(138, 98, 66),
            Color.FromArgb(0, 138, 98, 66)
        ]));
    }

    [AvaloniaTest]
    public void TextClassTest()
    {
        TextBlock title = new() { Classes = { "display" }, Text = "Gallean" };
        Window window = new() { Content = title };
        window.Show();

        Assert.That(title.FontSize, Is.EqualTo(56.0));
    }

    [AvaloniaTest]
    public void PopupFontFamilyTest()
    {
        TextBlock text = new() { Text = "Gallean" };
        Popup popup = new() { Child = text };
        Window window = new() { Content = popup };
        window.Show();

        popup.IsOpen = true;

        Assert.That(text.FontFamily, Is.EqualTo(new FontFamily("avares://Gallean.Client/Theme/Assets#Alegreya Sans")));
    }

    [AvaloniaTest]
    public void FontTest()
    {
        Assert.That(ThemeFont.Regular.FamilyName, Is.EqualTo("Alegreya Sans"));
        Assert.That(ThemeFont.Regular.FontWeight, Is.EqualTo(400));
        Assert.That(ThemeFont.Bold.FamilyName, Is.EqualTo("Alegreya Sans"));
        Assert.That(ThemeFont.Bold.FontWeight, Is.EqualTo(700));
    }

    [AvaloniaTest]
    public void HeadingFontTest()
    {
        TextBlock title = new() { Classes = { "h3" }, Text = "Gallean" };
        Window window = new() { Content = title };
        window.Show();

        Typeface typeface = new(title.FontFamily, title.FontStyle, title.FontWeight);
        GlyphTypeface glyphTypeface = typeface.GlyphTypeface;

        Assert.That(glyphTypeface.FamilyName, Is.EqualTo("Alegreya"));
        Assert.That(glyphTypeface.Weight, Is.EqualTo(FontWeight.Bold));
    }

    [AvaloniaTest]
    [TestCase(typeof(MainMenuScreenView))]
    [TestCase(typeof(ErrorScreenView))]
    [TestCase(typeof(GameEndedScreenView))]
    [TestCase(typeof(LoadingScreenView))]
    [TestCase(typeof(CampaignSelectScreenView))]
    [TestCase(typeof(ScenarioSelectScreenView))]
    [TestCase(typeof(ScenarioEventPopupView))]
    [TestCase(typeof(SpellbookTabView))]
    [TestCase(typeof(BuildingTabView))]
    [TestCase(typeof(ManagementScreenView))]
    [TestCase(typeof(LeaderPurchaseView))]
    public void ScreenResourceResolutionTest(Type viewType)
    {
        Control view = Assert.IsOfType<Control>(Activator.CreateInstance(viewType));
        Window window = new() { Content = view };

        Assert.DoesNotThrow(window.Show);
    }
}
