using Avalonia.Headless.NUnit;

namespace Gallean.Client.Tests;

[NonParallelizable]
public sealed class FatalErrorWindowTests
{
    [AvaloniaTest]
    public void ContentTest()
    {
        FatalErrorWindow window = new("Failed to start the game", "Access to the path '/home/player/.local/share/Gallean/Saves' is denied.", "/home/player/.local/share/Gallean/Logs/client-20260101-120000-000Z.log");
        window.Show();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(window.TitleText.Text, Is.EqualTo("Failed to start the game"));
            Assert.That(window.MessageText.Text, Is.EqualTo("Access to the path '/home/player/.local/share/Gallean/Saves' is denied."));
            Assert.That(window.LogLocationText.Text, Is.EqualTo("/home/player/.local/share/Gallean/Logs/client-20260101-120000-000Z.log"));
        }

        window.Close();
    }
}
