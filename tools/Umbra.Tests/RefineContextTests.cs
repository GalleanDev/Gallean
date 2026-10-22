using TorchSharp;

namespace Umbra.Tests;

public sealed class RefineContextTests
{
    [Test]
    public void PromptTest()
    {
        torch.Tensor empty = torch.zeros(1, 3, 16);
        torch.Tensor corpse = torch.ones(1, 3, 16);
        RefinePrompt corpsePrompt = new(new RefinePromptId("battle-corpse"), []);

        using RefineContext contexts = new(
            new Dictionary<string, torch.Tensor> { ["empty"] = empty, ["battle-corpse"] = corpse },
            new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt> { [new UmbraSequenceId("battle-corpse/large/0")] = corpsePrompt }));

        RefinePrompt facePrompt = contexts.GetPrompt(new UmbraSequenceId("unit-face/g000uu0001"));

        Assert.That(contexts.GetPrompt(new UmbraSequenceId("battle-corpse/large/0")), Is.SameAs(corpsePrompt));
        Assert.That(facePrompt.Id, Is.EqualTo(new RefinePromptId("empty")));
        Assert.That(facePrompt.Regions, Is.Empty);
        Assert.That(contexts.GetPromptContext(corpsePrompt.Id), Is.SameAs(corpse));
        Assert.That(contexts.GetPromptContext(facePrompt.Id), Is.SameAs(empty));
    }
}
