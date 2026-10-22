using SkiaSharp;

namespace Gallean.Client.Shaders.Tests;

public sealed class ShaderLibraryTests
{
    [Test]
    public void ShaderKeysTest()
    {
        Assert.That(ShaderKeys.All, Is.EqualTo([
            new ShaderKey("AnimatedTextSegment", "AnimatedTextSegment.sksl"),
            new ShaderKey("AnimatedTextSmoke", "AnimatedTextSmoke.sksl"),
            new ShaderKey("BattleBackground", "BattleBackground.sksl"),
            new ShaderKey("BattleFluid", "BattleFluid.sksl"),
            new ShaderKey("BattleRain", "BattleRain.sksl"),
            new ShaderKey("BattleShadow", "BattleShadow.sksl"),
            new ShaderKey("BattleUnit", "BattleUnit.sksl"),
            new ShaderKey("BattleWorldTransition", "BattleWorldTransition.sksl"),
            new ShaderKey("GalleanLogo", "GalleanLogo.sksl"),
            new ShaderKey("MainMenuBackground", "MainMenuBackground.sksl"),
            new ShaderKey("PlateLight", "PlateLight.sksl"),
            new ShaderKey("Water", "Water.sksl"),
            new ShaderKey("WorldBattleTransition", "WorldBattleTransition.sksl"),
            new ShaderKey("WorldHighlight", "WorldHighlight.sksl"),
            new ShaderKey("WorldReach", "WorldReach.sksl")
        ]));
    }

    [Test]
    public void CompileTest()
    {
        foreach (ShaderKey key in ShaderKeys.All)
        {
            Assert.DoesNotThrow(() => new ShaderProgram(key));
        }
    }

    [Test]
    public void MissingResourceTest()
    {
        Assert.Throws<KeyNotFoundException>(() => new ShaderProgram(new ShaderKey("Missing", "Missing.sksl")));
    }

    [Test]
    public void EditorConcurrencyTest()
    {
        WaterShaderProgram program = new();

        using (WaterShaderProgram.Editor editor = program.Edit())
        {
            editor.SetTime(1.0f);
        }

        Task task = Task.Run(() =>
        {
            using WaterShaderProgram.Editor editor = program.Edit();

            editor.SetTime(2.0f);
        });

        Assert.That(task.Wait(TimeSpan.FromSeconds(5)), Is.True);
    }

    [Test]
    public void UniformsTest()
    {
        WaterShaderProgram program = new();

        using WaterShaderProgram.Editor editor = program.Edit();

        editor.SetShore(SKShader.CreateColor(SKColors.Gray));
        editor.SetGrain(SKShader.CreateColor(SKColors.Gray));
        editor.SetAlbedo(SKShader.CreateColor(SKColors.Gray));
        editor.SetHalfTile(new SKPoint(32, 16));
        editor.SetFieldScale(4);
        editor.SetShoreRange(6);
        editor.SetGrainScale(4.0f);
        editor.SetOctaveScales(new SKColorF(4.1f, 9.3f, 19.5f, 37.0f));
        editor.SetDeepColor(new SKColorF(0.08f, 0.27f, 0.38f, 1.0f));
        editor.SetShallowColor(new SKColorF(0.18f, 0.58f, 0.61f, 1.0f));
        editor.SetSkyColor(new SKColorF(0.70f, 0.85f, 0.93f, 1.0f));
        editor.SetFoamColor(new SKColorF(0.93f, 0.98f, 0.99f, 1.0f));
        editor.SetSunColor(new SKColorF(0.69f, 0.93f, 0.99f, 1.0f));
        editor.SetTime(1.5f);
        editor.SetFineDetail(1.0f);
        editor.SetOctaveHeightWeights(new SKColorF(0.5f, 0.28f, 0.15f, 0.07f));
        editor.SetOctaveSlopeWeights(new SKColorF(0.5f, 0.28f, 0.15f, 0.07f));

        using SKShader shader = editor.Build();
    }

    [Test]
    public void BattleUnitUniformsTest()
    {
        BattleUnitShaderProgram program = new();

        using BattleUnitShaderProgram.Editor editor = program.Edit();

        editor.SetSource(SKShader.CreateColor(SKColors.Gray));
        editor.SetNormals(SKShader.CreateColor(SKColors.Gray));
        editor.SetAmbient(SKShader.CreateColor(SKColors.Gray));
        editor.SetReflection(SKShader.CreateColor(SKColors.Gray));
        editor.SetRigLightPositions([
            new SKColorF(900.0f, -260.0f, 820.0f, 1400.0f),
            new SKColorF(100.0f, 100.0f, 600.0f, 1200.0f),
            new SKColorF(60.0f, 340.0f, 140.0f, 420.0f),
            new SKColorF(575.0f, 380.0f, 110.0f, 360.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f)
        ]);
        editor.SetRigLightRadiances([
            new SKColorF(0.91f, 1.27f, 2.6f, 0.0f),
            new SKColorF(0.12f, 0.24f, 0.7f, 0.0f),
            new SKColorF(1.4f, 0.21f, 0.01f, 0.0f),
            new SKColorF(3.0f, 1.57f, 0.47f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f)
        ]);
        editor.SetSunDirection(new SKPoint3(0.011f, -0.998f, 0.065f));
        editor.SetSunRadiance(new SKPoint3(2.0f, 1.75f, 1.24f));
        editor.SetSkyRadiance(new SKPoint3(0.44f, 0.39f, 0.29f));
        editor.SetAmbientSize(new SKPoint(64, 32));
        editor.SetReflectionSize(new SKPoint(256, 128));

        using SKShader shader = editor.Build();
    }

    [Test]
    public void BattleFluidUniformsTest()
    {
        BattleFluidShaderProgram program = new();

        using BattleFluidShaderProgram.Editor editor = program.Edit();

        editor.SetThickness(SKShader.CreateColor(SKColors.Gray));
        editor.SetGain(3.4f);
        editor.SetExtinction(1.7f);
        editor.SetPowder(3.0f);
        editor.SetPowderFloor(0.35f);
        editor.SetBodyRamp(1.1f);
        editor.SetPeakRamp(0.7f);
        editor.SetPeakOffset(0.8f);
        editor.SetDeepColor(new SKColorF(0.03f, 0.21f, 0.22f, 1.0f));
        editor.SetBodyColor(new SKColorF(0.13f, 0.72f, 0.50f, 1.0f));
        editor.SetPeakColor(new SKColorF(0.50f, 1.00f, 0.80f, 1.0f));

        using SKShader shader = editor.Build();
    }

    [Test]
    public void BattleRainUniformsTest()
    {
        BattleRainShaderProgram program = new();

        using BattleRainShaderProgram.Editor editor = program.Edit();

        editor.SetNormals(SKShader.CreateColor(SKColors.Blue));
        editor.SetHeightField(SKShader.CreateColor(SKColors.Black));
        editor.SetRigLightPositions([
            new SKColorF(900.0f, -260.0f, 820.0f, 1400.0f),
            new SKColorF(100.0f, 100.0f, 600.0f, 1200.0f),
            new SKColorF(60.0f, 340.0f, 140.0f, 420.0f),
            new SKColorF(575.0f, 380.0f, 110.0f, 360.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f)
        ]);
        editor.SetRigLightRadiances([
            new SKColorF(0.91f, 1.27f, 2.6f, 0.0f),
            new SKColorF(0.12f, 0.24f, 0.7f, 0.0f),
            new SKColorF(1.4f, 0.21f, 0.01f, 0.0f),
            new SKColorF(3.0f, 1.57f, 0.47f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f),
            new SKColorF(0.0f, 0.0f, 0.0f, 0.0f)
        ]);
        editor.SetSkyRadiance(new SKPoint3(0.191f, 0.169f, 0.126f));
        editor.SetGroundNormal(new SKPoint3(0.0f, -0.812f, 0.584f));
        editor.SetSceneToGround(new SKColorF(0.0053f, -0.0074f, 0.0044f, 0.0092f));
        editor.SetGroundOffset(new SKPoint(0.0f, 0.0f));
        editor.SetDepthScale(830.0f);
        editor.SetBattlefieldCenter(new SKPoint(475.0f, 350.3f));
        editor.SetPerspectiveScalePerPixel(0.0003f);
        editor.SetTime(3.5f);
        editor.SetNearDepth(0.5f);
        editor.SetFarDepth(1.0f);
        editor.SetStreakRadiance(new SKPoint3(0.579f, 0.700f, 0.832f));
        editor.SetSplashRadiance(new SKPoint3(0.736f, 0.832f, 0.914f));
        editor.SetHazeRadiance(new SKPoint3(0.190f, 0.268f, 0.375f));
        editor.SetStreakSpacing(new SKPoint(22.0f, 140.0f));
        editor.SetStreakFall(900.0f);
        editor.SetStreakSlant(0.22f);
        editor.SetStreakLength(62.0f);
        editor.SetStreakWidth(1.1f);
        editor.SetStreakChance(0.7f);
        editor.SetStreakOpacity(0.55f);
        editor.SetDepthSpread(2.2f);
        editor.SetDepthFade(0.35f);
        editor.SetSplashSpacing(0.3f);
        editor.SetSplashRate(1.4f);
        editor.SetSplashRadius(0.45f);
        editor.SetSplashWidth(0.09f);
        editor.SetSplashChance(0.35f);
        editor.SetSplashOpacity(0.7f);
        editor.SetGroundFacing(0.35f);
        editor.SetHaze(0.18f);

        using SKShader shader = editor.Build();
    }

    [Test]
    public void UnsetUniformsTest()
    {
        BattleFluidShaderProgram program = new();

        using SKShader thickness = SKShader.CreateColor(new SKColor(128, 128, 128, 255));
        using SKShader heightField = SKShader.CreateColor(SKColors.Black);
        using BattleFluidShaderProgram.Editor editor = program.Edit();

        editor.SetThickness(thickness);
        editor.SetHeightField(heightField);
        editor.SetGain(3.0f);
        editor.SetExtinction(1.7f);
        editor.SetPowder(3.0f);
        editor.SetPowderFloor(0.35f);
        editor.SetBodyRamp(1.1f);
        editor.SetPeakRamp(0.7f);
        editor.SetPeakOffset(0.8f);
        editor.SetDeepColor(new SKColorF(0.03f, 0.21f, 0.22f, 1.0f));
        editor.SetBodyColor(new SKColorF(0.13f, 0.72f, 0.5f, 1.0f));
        editor.SetPeakColor(new SKColorF(0.5f, 1.0f, 0.8f, 1.0f));
        editor.SetScatterColor(new SKColorF(0.75f, 0.84f, 0.8f, 1.0f));

        using SKShader shader = editor.Build();
        using SKSurface surface = SKSurface.Create(new SKImageInfo(8, 8, SKColorType.Rgba8888, SKAlphaType.Premul));
        using SKPaint paint = new() { Shader = shader };

        surface.Canvas.DrawRect(SKRect.Create(8, 8), paint);

        using SKPixmap pixmap = surface.PeekPixels();
        byte[] pixel = pixmap.GetPixelSpan().Slice(((4 * 8) + 4) * 4, 4).ToArray();

        Assert.That(pixel, Is.EqualTo(new byte[] { 52, 185, 135, 235 }));
    }
}
