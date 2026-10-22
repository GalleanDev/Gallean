using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class TextExtensionTests
{
    [TearDown]
    public void TearDown()
    {
        new TextCatalog([]).Activate();
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
        new TextCatalog([new KeyValuePair<string, string>("Windowed mode (F11)", "Fenstermodus (F11)")]).Activate();

        Window window = new() { Content = new SettingsScreenView() };
        window.Show();

        Assert.That(GetTexts(window), Does.Contain("Fenstermodus (F11)"));

        window.Close();
    }
}
