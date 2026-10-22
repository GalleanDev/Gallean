using Gallean.Entities;
using Gallean.Tests.Common;

namespace GameContent.Tests;

public sealed class CapitalSceneExtractorTests
{
    [Test]
    public void LoadTest()
    {
        using Sandbox sb = Sandbox.Create();

        string path = sb.WriteTextFile("capital.dat",
        [
            "[HUMAN]",
            "LayerInfoCount=2",
            "LAYERINFO_00=1,,",
            "LAYERINFO_01=0,G000BB0012,",
            "LayerCount=4",
            "LAYER_01=,EMP_MAIN,0,0,0,,0,0,0,538,448,587,540",
            "LAYER_150=,EMP_PLAN1,0,0,0,,0,0,0,0,0,0,0",
            "LAYER_10=G000BB0012,EMP_LIBRARY,0,341,303,,0,0,0,341,303,435,536",
            "LAYER_00=,BG_UNDEAD,0,0,0,,0,0,0,-1,-1,-1,-1"
        ]);

        CapitalSceneExtractor extractor = CapitalSceneExtractor.Load(path);

        IReadOnlyList<(string Race, CapitalSceneLayer Layer)> rows = extractor.GetAllCapitalScenes()
            .SelectMany(pair => pair.Value.Layers.Select(layer => (pair.Key, layer)))
            .ToArray();

        Assert.That(rows, Is.EqualTo(
        [
            ("HUMAN", new CapitalSceneLayer(0, null, "BG_UNDEAD", 0, 0)),
            ("HUMAN", new CapitalSceneLayer(1, null, "EMP_MAIN", 0, 0)),
            ("HUMAN", new CapitalSceneLayer(10, new Db.Id("G000BB0012"), "EMP_LIBRARY", 341, 303)),
            ("HUMAN", new CapitalSceneLayer(150, null, "EMP_PLAN1", 0, 0))
        ]));
        Assert.That(extractor.GetAllCapitalBuildings().ToArray(), Is.EqualTo(
        [
            new KeyValuePair<Db.Id, CapitalSceneLayer>(
                new Db.Id("G000BB0012"),
                new CapitalSceneLayer(10, new Db.Id("G000BB0012"), "EMP_LIBRARY", 341, 303))
        ]));
    }
}
