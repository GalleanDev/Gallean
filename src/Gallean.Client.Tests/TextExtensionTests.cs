using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using Gallean.Configuration;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class TextExtensionTests
{
    [TearDown]
    public void TearDown()
    {
        new TextCatalog([]).MakeCurrent();
    }

    private static IReadOnlyList<string> GetTexts(Control root)
    {
        List<string> texts = [];

        foreach (TextBlock block in root.GetVisualDescendants().OfType<TextBlock>())
        {
            if (block.Text is not null)
            {
                texts.Add(block.Text);
            }
        }

        return texts;
    }

    [AvaloniaTest]
    public void SourceTextTest()
    {
        Window window = new() { Content = new SettingsScreenView() };
        window.Show();

        Assert.That(GetTexts(window), Does.Contain("Windowed mode (F11)"));

        window.Close();
    }

    [AvaloniaTest]
    public void TranslatedTextTest()
    {
        new TextCatalog([new KeyValuePair<string, string>("Windowed mode (F11)", "Fenstermodus (F11)")]).MakeCurrent();

        Window window = new() { Content = new SettingsScreenView() };
        window.Show();

        Assert.That(GetTexts(window), Does.Contain("Fenstermodus (F11)"));

        window.Close();
    }

    [AvaloniaTest]
    public async Task LanguageSwitchTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSettingsStore settingsStore = new(sb.GetPath("config"));

        using GameViewModel gameViewModel = new TestGameViewModelBuilder()
            .WithSaveStore(new GameSaveStore(sb.GetPath("saves"), 10))
            .WithSettingsStore(settingsStore)
            .Build();

        SettingsScreenViewModel settings = new(gameViewModel);
        Window window = new() { Content = new SettingsScreenView { DataContext = settings } };
        window.Show();

        Assert.That(GetTexts(window), Does.Contain("Windowed mode (F11)"));
        Assert.That(GetTexts(window), Does.Contain("Normal"));

        settings.Language = Language.Russian;
        window.UpdateLayout();

        Assert.That(GetTexts(window), Does.Contain("Оконный режим (F11)"));
        Assert.That(GetTexts(window), Does.Contain("Стандарт"));
        Assert.That(GetTexts(window), Does.Not.Contain("Windowed mode (F11)"));
        Assert.That(GetTexts(window), Does.Not.Contain("Normal"));
        Assert.That(settings.UiSize, Is.EqualTo(UiSize.Normal));
        Assert.That(
            gameViewModel.Settings,
            Is.EqualTo(GameSettings.Create(100, 100, false, 100, 5, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal, Language.Russian)));
        Assert.That(await settingsStore.TryLoadAsync(), Is.EqualTo(gameViewModel.Settings));

        window.Close();
    }
}
