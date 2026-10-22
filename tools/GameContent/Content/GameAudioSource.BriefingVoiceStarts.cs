namespace GameContent;

internal sealed partial class GameAudioSource
{
    private static class BriefingVoiceStarts
    {
        public static readonly IReadOnlyDictionary<string, TimeSpan> ByName = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase)
        {
            ["BRIEFEL1.WAV"] = TimeSpan.FromMilliseconds(4548.0),
            ["BRIEFEL2.WAV"] = TimeSpan.FromMilliseconds(4648.0),
            ["BRIEFEL3.WAV"] = TimeSpan.FromMilliseconds(4199.0),
            ["BRIEFEL4.WAV"] = TimeSpan.FromMilliseconds(4049.0),
            ["BRIEFEL5.WAV"] = TimeSpan.FromMilliseconds(4249.0),
            ["BRIEFEL6.WAV"] = TimeSpan.FromMilliseconds(4348.0),
            ["BRIEFEL7.WAV"] = TimeSpan.FromMilliseconds(4348.0),
            ["BRIEFEL8.WAV"] = TimeSpan.FromMilliseconds(5048.0),
            ["BRIEFH01.BIK"] = TimeSpan.FromMilliseconds(12745.0),
            ["BRIEFH02.BIK"] = TimeSpan.FromMilliseconds(11595.0),
            ["BRIEFH03.BIK"] = TimeSpan.FromMilliseconds(5298.0),
            ["BRIEFH04.BIK"] = TimeSpan.FromMilliseconds(5448.0),
            ["BRIEFH05.BIK"] = TimeSpan.FromMilliseconds(4748.0),
            ["BRIEFH06.BIK"] = TimeSpan.FromMilliseconds(9946.0),
            ["BRIEFH07.BIK"] = TimeSpan.FromMilliseconds(8047.0),
            ["BRIEFM01.BIK"] = TimeSpan.FromMilliseconds(3249.0),
            ["BRIEFM02.BIK"] = TimeSpan.FromMilliseconds(3599.0),
            ["BRIEFM03.BIK"] = TimeSpan.FromMilliseconds(5198.0),
            ["BRIEFM04.BIK"] = TimeSpan.FromMilliseconds(3699.0),
            ["BRIEFM05.BIK"] = TimeSpan.FromMilliseconds(7347.0),
            ["BRIEFM06.BIK"] = TimeSpan.FromMilliseconds(4648.0),
            ["BRIEFM07.BIK"] = TimeSpan.FromMilliseconds(8996.0),
            ["BRIEFL01.BIK"] = TimeSpan.FromMilliseconds(5298.0),
            ["BRIEFL02.BIK"] = TimeSpan.FromMilliseconds(4348.0),
            ["BRIEFL03.BIK"] = TimeSpan.FromMilliseconds(4748.0),
            ["BRIEFL04.BIK"] = TimeSpan.FromMilliseconds(8048.0),
            ["BRIEFL05.BIK"] = TimeSpan.FromMilliseconds(5098.0),
            ["BRIEFL06.BIK"] = TimeSpan.FromMilliseconds(4398.0),
            ["BRIEFL07.BIK"] = TimeSpan.FromMilliseconds(4898.0),
            ["BRIEFU01.BIK"] = TimeSpan.FromMilliseconds(3999.0),
            ["BRIEFU02.BIK"] = TimeSpan.FromMilliseconds(10846.0),
            ["BRIEFU03.BIK"] = TimeSpan.FromMilliseconds(10096.0),
            ["BRIEFU04.BIK"] = TimeSpan.FromMilliseconds(4099.0),
            ["BRIEFU05.BIK"] = TimeSpan.FromMilliseconds(4998.0),
            ["BRIEFU06.BIK"] = TimeSpan.FromMilliseconds(4998.0),
            ["BRIEFU07.BIK"] = TimeSpan.FromMilliseconds(11968.0)
        };
    }
}
