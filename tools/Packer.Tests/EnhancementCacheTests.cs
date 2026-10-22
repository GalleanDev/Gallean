using Gallean.Tests.Common;

namespace Packer.Tests;

public sealed class EnhancementCacheTests
{
    private static List<string> GetCachedFileNames(string cacheDirectory)
    {
        List<string> names = [];

        foreach (string path in Directory.EnumerateFiles(cacheDirectory))
        {
            names.Add(Path.GetFileName(path));
        }

        return names;
    }

    private static string FormatImage(EnhancementCache.Image image)
    {
        return $"{image.PixelWidth}x{image.PixelHeight} {string.Join(",", image.Pixels)}";
    }

    private static void WritePixel(EnhancementCache cache, EnhancementCache.Key key, byte value)
    {
        cache.Write(key, 1, 1, new byte[] { value, value, value, 255 });
    }

    [Test]
    public void DeviceTest()
    {
        using Sandbox sb = Sandbox.Create();

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cpuCache = new(cacheDirectory, UmbraDevice.Cpu);
        EnhancementCache gpuCache = new(cacheDirectory, UmbraDevice.Gpu);
        UmbraSpriteHash hash = new(Convert.FromHexString("8fa1c2"));
        EnhancementCache.Key key = new([EnhancementType.Upscale], UpscalerType.Vsr, hash, null, null);

        WritePixel(cpuCache, key, 10);

        Assert.That(cpuCache.Contains(key), Is.True);
        Assert.That(gpuCache.Contains(key), Is.False);

        WritePixel(gpuCache, key, 20);

        Assert.That(FormatImage(cpuCache.Read(key)), Is.EqualTo("1x1 10,10,10,255"));
        Assert.That(FormatImage(gpuCache.Read(key)), Is.EqualTo("1x1 20,20,20,255"));
        Assert.That(GetCachedFileNames(cacheDirectory), Is.EquivalentTo([
            "cpu.8fa1c2.none.upscale.vsr.none.png",
            "gpu.8fa1c2.none.upscale.vsr.none.png"
        ]));
    }

    [Test]
    public void LayerTest()
    {
        using Sandbox sb = Sandbox.Create();

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash hash = new(Convert.FromHexString("8fa1c2"));
        EnhancementCache.Key sprite = new([EnhancementType.Upscale], UpscalerType.Vsr, hash, null, null);
        EnhancementCache.Key normal = new([EnhancementType.Upscale], UpscalerType.Vsr, hash, AssetSpriteLayerType.Normal, null);

        WritePixel(cache, normal, 10);

        Assert.That(cache.Contains(normal), Is.True);
        Assert.That(cache.Contains(sprite), Is.False);
        Assert.That(GetCachedFileNames(cacheDirectory), Is.EqualTo([
            "cpu.8fa1c2.normal.upscale.vsr.none.png"
        ]));
    }

    [Test]
    public void TransformTest()
    {
        using Sandbox sb = Sandbox.Create();

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash hash = new(Convert.FromHexString("8fa1c2"));
        EnhancementCache.Key upscale = new([EnhancementType.Upscale], UpscalerType.Vsr, hash, null, null);
        EnhancementCache.Key refine = new([EnhancementType.Upscale, EnhancementType.Refine], UpscalerType.Vsr, hash, null, null);

        WritePixel(cache, upscale, 10);
        WritePixel(cache, refine, 20);

        Assert.That(FormatImage(cache.Read(upscale)), Is.EqualTo("1x1 10,10,10,255"));
        Assert.That(FormatImage(cache.Read(refine)), Is.EqualTo("1x1 20,20,20,255"));
    }

    [Test]
    public void EnhancementsTest()
    {
        using Sandbox sb = Sandbox.Create();

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash hash = new(Convert.FromHexString("8fa1c2"));

        WritePixel(cache, new EnhancementCache.Key([EnhancementType.Refine], null, hash, null, null), 10);
        WritePixel(cache, new EnhancementCache.Key([EnhancementType.Upscale, EnhancementType.Alpha], UpscalerType.Vsr, hash, null, null), 20);
        WritePixel(
            cache,
            new EnhancementCache.Key(
                [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha, EnhancementType.Surface],
                UpscalerType.Vsr,
                hash,
                AssetSpriteLayerType.Normal,
                null),
            30);

        Assert.That(GetCachedFileNames(cacheDirectory), Is.EquivalentTo([
            "cpu.8fa1c2.none.refine.none.none.png",
            "cpu.8fa1c2.none.upscale+alpha.vsr.none.png",
            "cpu.8fa1c2.normal.upscale+refine+alpha+surface.vsr.none.png"
        ]));
    }

    [Test]
    public void MissingTest()
    {
        using Sandbox sb = Sandbox.Create();

        EnhancementCache cache = new(sb.CreateDirectory("cache"), UmbraDevice.Cpu);
        UmbraSpriteHash hash = new(Convert.FromHexString("8fa1c2"));
        EnhancementCache.Key key = new([EnhancementType.Upscale], UpscalerType.Vsr, hash, null, null);

        Assert.That(cache.Contains(key), Is.False);

        FileNotFoundException ex = Assert.Throws<FileNotFoundException>(() => cache.Read(key));

        Assert.That(Path.GetFileName(ex.FileName), Is.EqualTo("cpu.8fa1c2.none.upscale.vsr.none.png"));
    }

    [Test]
    public void FailedWriteTest()
    {
        using Sandbox sb = Sandbox.Create();

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash hash = new(Convert.FromHexString("8fa1c2"));
        EnhancementCache.Key key = new([EnhancementType.Upscale], UpscalerType.Vsr, hash, null, null);

        sb.CreateDirectory(Path.Combine("cache", "cpu.8fa1c2.none.upscale.vsr.none.png"));

        Assert.That(() => WritePixel(cache, key, 10), Throws.Exception);
        Assert.That(cache.Contains(key), Is.False);
        Assert.That(GetCachedFileNames(cacheDirectory), Is.Empty);
    }

    [Test]
    public void UpscalerTest()
    {
        using Sandbox sb = Sandbox.Create();

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash hash = new(Convert.FromHexString("8fa1c2"));
        EnhancementCache.Key vsr = new([EnhancementType.Upscale, EnhancementType.Refine], UpscalerType.Vsr, hash, null, null);
        EnhancementCache.Key mosr = new([EnhancementType.Upscale, EnhancementType.Refine], UpscalerType.Mosr, hash, null, null);
        EnhancementCache.Key none = new([EnhancementType.Refine], null, hash, null, null);

        WritePixel(cache, vsr, 10);
        WritePixel(cache, mosr, 20);
        WritePixel(cache, none, 30);

        Assert.That(FormatImage(cache.Read(vsr)), Is.EqualTo("1x1 10,10,10,255"));
        Assert.That(FormatImage(cache.Read(mosr)), Is.EqualTo("1x1 20,20,20,255"));
        Assert.That(FormatImage(cache.Read(none)), Is.EqualTo("1x1 30,30,30,255"));
        Assert.That(GetCachedFileNames(cacheDirectory), Is.EquivalentTo([
            "cpu.8fa1c2.none.upscale+refine.vsr.none.png",
            "cpu.8fa1c2.none.upscale+refine.mosr.none.png",
            "cpu.8fa1c2.none.refine.none.none.png"
        ]));
    }

    [Test]
    public void PromptTest()
    {
        using Sandbox sb = Sandbox.Create();

        string cacheDirectory = sb.CreateDirectory("cache");
        EnhancementCache cache = new(cacheDirectory, UmbraDevice.Cpu);
        UmbraSpriteHash hash = new(Convert.FromHexString("8fa1c2"));
        EnhancementCache.Key first = new([EnhancementType.Upscale, EnhancementType.Refine], UpscalerType.Vsr, hash, null, new UmbraPromptHash(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef, 0x01, 0x23, 0x45, 0x67 }));
        EnhancementCache.Key second = new([EnhancementType.Upscale, EnhancementType.Refine], UpscalerType.Vsr, hash, null, new UmbraPromptHash(new byte[] { 0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10, 0x76, 0x54, 0x32, 0x10 }));

        WritePixel(cache, first, 10);

        Assert.That(cache.Contains(first), Is.True);
        Assert.That(cache.Contains(second), Is.False);
        Assert.That(GetCachedFileNames(cacheDirectory), Is.EqualTo([
            "cpu.8fa1c2.none.upscale+refine.vsr.0123456789abcdef01234567.png"
        ]));
    }

    [Test]
    public void EqualityTest()
    {
        UmbraSpriteHash hash = new(Convert.FromHexString("8fa1c2"));
        EnhancementCache.Key first = new([EnhancementType.Upscale, EnhancementType.Refine], UpscalerType.Vsr, hash, null, null);
        EnhancementCache.Key second = new([EnhancementType.Upscale, EnhancementType.Refine], UpscalerType.Vsr, hash, null, null);
        EnhancementCache.Key upscale = new([EnhancementType.Upscale], UpscalerType.Vsr, hash, null, null);
        Dictionary<EnhancementCache.Key, int> values = new() { [first] = 10 };

        Assert.That(second, Is.EqualTo(first));
        Assert.That(upscale, Is.Not.EqualTo(first));
        Assert.That(values.ContainsKey(second), Is.True);
        Assert.That(values.ContainsKey(upscale), Is.False);
    }
}
