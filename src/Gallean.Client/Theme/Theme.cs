using Avalonia.Media;

namespace Gallean.Client;

internal static class Theme
{
    public static class Accent
    {
        public static ThemeAccent Default { get; } = new(
            Color.FromRgb(243, 217, 147),
            Color.FromRgb(255, 246, 230),
            Color.FromRgb(212, 185, 124),
            Color.FromRgb(138, 98, 66),
            Color.FromRgb(204, 160, 114),
            Color.FromRgb(92, 62, 44));
    }

    public static class Common
    {
        public static Color WindowBackgroundColor { get; } = Color.FromRgb(26, 24, 29);
        public static Color WindowTranslucentBackgroundColor { get; } = Color.FromArgb(150, 26, 24, 29);
        public static Color PrimaryTextColor { get; } = Color.FromRgb(226, 214, 192);
        public static Color SecondaryTextColor { get; } = Color.FromRgb(152, 141, 122);
        public static Color TextOutlineColor { get; } = Color.FromArgb(200, 8, 6, 6);
        public static Color ModalBackdropColor { get; } = Color.FromArgb(200, 19, 18, 23);
        public static Color PopupBackgroundColor { get; } = Color.FromRgb(41, 40, 45);
        public static Color DividerColor { get; } = Color.FromRgb(54, 52, 58);
    }

    public static class Modal
    {
        public static Color BodyTopColor { get; } = Color.FromRgb(24, 27, 32);
        public static Color BodyBottomColor { get; } = Color.FromRgb(16, 37, 49);
        public static Color AccentBodyBottomColor { get; } = Color.FromRgb(31, 22, 38);
        public static Color BorderOutlineColor { get; } = Color.FromRgb(18, 13, 9);
        public static Color PortraitFrameColor { get; } = Color.FromRgb(108, 105, 114);
        public static Color TitleColor { get; } = Color.FromRgb(240, 234, 222);
        public static Color EffectColor { get; } = Color.FromRgb(150, 190, 226);
    }

    public static class Pager
    {
        public static Color BandColor { get; } = Color.FromArgb(150, 12, 10, 10);
    }

    public static class ScenarioDialog
    {
        public static Color BandColor { get; } = Color.FromArgb(232, 14, 13, 17);
        public static Color BandFadeColor { get; } = Color.FromArgb(0, 14, 13, 17);
        public static Color OrnamentGlowColor { get; } = Color.FromArgb(110, 243, 217, 147);
        public static Color SpeakerColor { get; } = Color.FromRgb(228, 182, 102);
        public static Color TextColor { get; } = Color.FromRgb(240, 230, 210);
        public static Color HistorySpeakerColor { get; } = Color.FromRgb(152, 141, 122);
        public static Color HistoryTextColor { get; } = Color.FromRgb(196, 186, 166);
        public static Color HistoryCharacterColor { get; } = Color.FromRgb(190, 164, 118);
        public static Color HistoryObjectColor { get; } = Color.FromRgb(132, 164, 160);
        public static Color SmokeColor { get; } = Color.FromArgb(160, 200, 196, 206);
    }

    public static class ScenarioText
    {
        public static Color CharacterColor { get; } = Color.FromRgb(232, 192, 120);
        public static Color ObjectColor { get; } = Color.FromRgb(150, 200, 196);
    }

    public static class Tooltip
    {
        private static Color ShadowColor { get; } = Color.FromArgb(190, 8, 7, 10);

        public static BoxShadows Shadow { get; } = new(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 0,
            Blur = 8,
            Spread = 0,
            Color = ShadowColor
        });
    }

    public static class Loading
    {
        public static Color BackdropColor { get; } = Color.FromArgb(72, 8, 7, 10);
    }

    public static class Hud
    {
        public static Color PlateColor { get; } = Color.FromArgb(191, 19, 19, 21);
        public static Color PlateBorderColor { get; } = Color.FromArgb(150, 10, 10, 12);
        public static Color PanelColor { get; } = Color.FromArgb(240, 19, 19, 21);
        public static Color InkColor { get; } = Color.FromRgb(226, 214, 192);
        public static Color DisabledInkColor { get; } = Color.FromRgb(83, 81, 78);
        public static Color HotkeyTextColor { get; } = Color.FromRgb(152, 141, 122);
        public static Color HotkeyPillColor { get; } = Color.FromRgb(35, 36, 35);
        public static Color HotkeyPillBorderColor { get; } = Color.FromRgb(55, 53, 53);
        public static Color PlateShadowColor { get; } = Color.FromArgb(180, 8, 7, 10);

        public static BoxShadows PlateShadow { get; } = new(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 0,
            Blur = 18,
            Spread = 0,
            Color = PlateShadowColor
        });
    }

    public static class Alert
    {
        public static Color DetailsColor { get; } = Color.FromRgb(196, 186, 168);
    }

    public static class AssetPackError
    {
        public static Color GlowColor { get; } = Color.FromArgb(70, 86, 58, 120);
        public static Color LocationColor { get; } = Color.FromArgb(140, 8, 6, 12);
        public static Color StepTitleColor { get; } = Color.FromRgb(255, 255, 255);
    }

    public static class ModalIcon
    {
        public static Color ErrorInkColor { get; } = Color.FromRgb(226, 128, 110);
        public static Color ErrorRingLightColor { get; } = Color.FromRgb(146, 82, 66);
        public static Color ErrorRingDarkColor { get; } = Color.FromRgb(108, 56, 46);
        public static Color InkShadowColor { get; } = Color.FromArgb(200, 8, 7, 10);
    }

    public static class Artwork
    {
        public static Color OverlayColor { get; } = Color.FromArgb(215, 8, 7, 10);
        public static Color OverlayFadeColor { get; } = Color.FromArgb(0, 8, 7, 10);
        public static Color FeatherColor { get; } = Color.FromArgb(255, 0, 0, 0);
        public static Color FeatherFadeColor { get; } = Color.FromArgb(0, 0, 0, 0);
        public static Color PortraitShadowColor { get; } = Color.FromArgb(140, 8, 7, 10);
    }

    public static class Badge
    {
        public static Color BackgroundColor { get; } = Color.FromRgb(26, 25, 28);
    }

    public static class StatRow
    {
        public static Color StripeColor { get; } = Color.FromArgb(90, 51, 50, 58);
        public static Color IncreaseColor { get; } = Color.FromRgb(140, 224, 150);
        public static Color DecreaseColor { get; } = Color.FromRgb(244, 122, 112);
    }

    public static class AttackSource
    {
        public static Color WeaponColor { get; } = Color.FromRgb(206, 208, 216);
        public static Color MindColor { get; } = Color.FromRgb(196, 144, 236);
        public static Color LifeColor { get; } = Color.FromRgb(250, 226, 140);
        public static Color DeathColor { get; } = Color.FromRgb(160, 206, 140);
        public static Color FireColor { get; } = Color.FromRgb(244, 128, 64);
        public static Color WaterColor { get; } = Color.FromRgb(100, 160, 244);
        public static Color EarthColor { get; } = Color.FromRgb(200, 156, 104);
        public static Color AirColor { get; } = Color.FromRgb(140, 226, 236);
    }

    public static class LeaderAbility
    {
        public static Color FaceTopColor { get; } = Color.FromArgb(220, 52, 44, 40);
        public static Color FaceBottomColor { get; } = Color.FromArgb(220, 30, 27, 31);
    }

    public static class MainMenu
    {
        public static class Light
        {
            public static Color WispColor { get; } = Color.FromRgb(204, 242, 140);
            public static Color WispGoldColor { get; } = Color.FromRgb(240, 222, 128);
            public static Color WispGreenColor { get; } = Color.FromRgb(172, 236, 150);
            public static Color WispPaleColor { get; } = Color.FromRgb(226, 246, 200);
            public static Color EmberColor { get; } = Color.FromRgb(255, 140, 46);
        }

        public static Color BackdropColor { get; } = Color.FromRgb(0, 0, 0);
        public static Color VersionTextColor { get; } = Color.FromRgb(226, 214, 192);
        public static Color VersionOutlineColor { get; } = Color.FromArgb(230, 23, 23, 23);
    }

    public static class GameMenu
    {
        public static Color OverlayColor { get; } = Color.FromArgb(120, 8, 7, 10);
        public static Color VignetteColor { get; } = Color.FromArgb(235, 8, 7, 10);
        public static Color VignetteFadeColor { get; } = Color.FromArgb(0, 8, 7, 10);
    }

    public static class Logo
    {
        public static Color AmbientColor { get; } = Color.FromRgb(125, 125, 128);
        public static Color GrazingColor { get; } = Color.FromRgb(128, 166, 204);
        public static Color MoonColor { get; } = Color.FromRgb(170, 195, 255);
        public static Color ShineColor { get; } = Color.FromRgb(255, 240, 215);
        public static Color ShadowColor { get; } = Color.FromArgb(200, 0, 0, 0);
    }

    public static class ScenarioSelect
    {
        public static Color BackdropColor { get; } = Color.FromRgb(0, 0, 0);
    }

    public static class HotseatTurn
    {
        public static Color BackdropColor { get; } = Color.FromRgb(19, 18, 23);
    }

    public static class LeaderUpgrade
    {
        public static Color GlowColor { get; } = Color.FromArgb(150, 243, 217, 147);
        public static Color SmokeColor { get; } = Color.FromArgb(160, 196, 176, 226);
    }

    public static class Briefing
    {
        public static Color EmberColor { get; } = Color.FromRgb(170, 110, 52);
        public static Color SmokeColor { get; } = Color.FromArgb(160, 200, 196, 206);
        public static Color BackdropColor { get; } = Color.FromArgb(150, 0, 0, 0);
        public static Color BackdropFadeColor { get; } = Color.FromArgb(0, 0, 0, 0);
        public static Color ObjectiveShadowColor { get; } = Color.FromArgb(150, 255, 176, 88);
    }

    public static class TabBar
    {
        public static Color BackgroundColor { get; } = Color.FromRgb(25, 21, 18);
        public static Color BackgroundDarkColor { get; } = Color.FromRgb(19, 18, 16);
        public static Color TextColor { get; } = Color.FromRgb(100, 84, 72);
        public static Color HoverTextColor { get; } = Color.FromRgb(160, 138, 116);
        public static Color SelectedTextColor { get; } = Color.FromRgb(234, 226, 206);
        public static Color SelectedCellTopColor { get; } = Color.FromRgb(18, 17, 15);
        public static Color SelectedCellBottomColor { get; } = Color.FromRgb(44, 40, 36);
        public static Color SelectedCellBorderColor { get; } = Color.FromRgb(62, 57, 52);
        public static Color SelectedCellBorderShadowColor { get; } = Color.FromRgb(13, 12, 11);
        public static Color UnderlineColor { get; } = Color.FromRgb(238, 234, 222);
        public static Color UnderlineGlowColor { get; } = Color.FromArgb(150, 255, 244, 222);
        public static Color RuleColor { get; } = Color.FromRgb(176, 136, 86);
        public static Color RuleDarkColor { get; } = Color.FromRgb(118, 88, 58);
        public static Color OrnamentColor { get; } = Color.FromRgb(150, 112, 70);
        public static Color PatternColor { get; } = Color.FromRgb(170, 130, 82);
        public static Color PatternDarkColor { get; } = Color.FromRgb(112, 82, 52);
        public static Color HotkeyFaceColor { get; } = Color.FromRgb(242, 241, 238);
        public static Color HotkeyHoverFaceColor { get; } = Color.FromRgb(255, 236, 196);
        public static Color HotkeyTextColor { get; } = Color.FromRgb(31, 30, 29);
        public static Color OrnamentShadowColor { get; } = Color.FromArgb(200, 0, 0, 0);
    }

    public static class Panel
    {
        public static Color TranslucentBackgroundColor { get; } = Color.FromArgb(240, 34, 32, 38);
        public static Color BorderColor { get; } = Color.FromRgb(54, 52, 58);
        public static Color BodyTopColor { get; } = Color.FromRgb(24, 27, 32);
        public static Color BodyBottomColor { get; } = Color.FromRgb(21, 33, 40);
    }

    public static class Card
    {
        public static Color BackgroundColor { get; } = Color.FromArgb(235, 30, 27, 24);
        public static Color BackgroundDarkColor { get; } = Color.FromArgb(235, 9, 8, 8);
        public static Color HoverBackgroundColor { get; } = Color.FromArgb(235, 48, 43, 38);
        public static Color HoverBackgroundDarkColor { get; } = Color.FromArgb(235, 20, 18, 17);
        public static Color SelectedFaceTopColor { get; } = Color.FromRgb(98, 89, 78);
        public static Color SelectedFaceBottomColor { get; } = Color.FromRgb(58, 52, 45);
        public static Color SelectedBorderLightColor { get; } = Color.FromRgb(164, 152, 134);
        public static Color SelectedBorderDarkColor { get; } = Color.FromRgb(30, 26, 23);
    }

    public static class List
    {
        public static Color BackgroundColor { get; } = Color.FromArgb(70, 0, 0, 0);
    }

    public static class DropZone
    {
        public static Color BackgroundColor { get; } = Color.FromRgb(30, 27, 26);
        public static Color HoverBackgroundColor { get; } = Color.FromRgb(58, 46, 32);
        public static Color ShadowColor { get; } = Color.FromArgb(200, 0, 0, 0);
        public static Color BackdropColor { get; } = Color.FromArgb(150, 8, 8, 10);
    }

    public static class Frame
    {
        public static Color BackgroundColor { get; } = Color.FromRgb(26, 25, 28);
        public static Color BorderColor { get; } = Color.FromRgb(54, 52, 58);
    }

    public static class Slot
    {
        public static Color BackgroundColor { get; } = Color.FromRgb(26, 25, 28);
    }

    public static class UnitPortrait
    {
        public static Color FrameEdgeColor { get; } = Color.FromRgb(18, 12, 7);
        public static Color FrameLightColor { get; } = Color.FromRgb(246, 214, 146);
        public static Color FrameColor { get; } = Color.FromRgb(178, 134, 72);
        public static Color FrameDarkColor { get; } = Color.FromRgb(104, 70, 34);
        public static Color FrameBevelLightColor { get; } = Color.FromArgb(200, 255, 236, 186);
        public static Color FrameBevelDarkColor { get; } = Color.FromArgb(160, 40, 24, 10);
        public static Color FrameGlossColor { get; } = Color.FromArgb(90, 255, 244, 220);
        public static Color FrameGlossFadeColor { get; } = Color.FromArgb(0, 255, 244, 220);
        public static Color SelectedFrameLightColor { get; } = Color.FromRgb(255, 253, 247);
        public static Color SelectedFrameColor { get; } = Color.FromRgb(228, 224, 216);
        public static Color SelectedFrameDarkColor { get; } = Color.FromRgb(148, 142, 134);
        public static Color EmptyFrameLightColor { get; } = Color.FromRgb(66, 60, 62);
        public static Color EmptyFrameColor { get; } = Color.FromRgb(46, 43, 48);
        public static Color EmptyFrameDarkColor { get; } = Color.FromRgb(31, 29, 34);
        public static Color BackgroundColor { get; } = Color.FromRgb(26, 25, 28);
        public static Color EmptyBackgroundColor { get; } = Color.FromRgb(18, 17, 20);
        public static Color ShadowColor { get; } = Color.FromArgb(210, 0, 0, 0);
        public static Color InnerShadowColor { get; } = Color.FromArgb(220, 0, 0, 0);
        public static Color GlowColor { get; } = Color.FromArgb(200, 243, 217, 147);
        public static Color GlowHaloColor { get; } = Color.FromArgb(105, 243, 217, 147);
        public static Color HighlightOverlayColor { get; } = Color.FromArgb(36, 243, 217, 147);
        public static Color LostHitpointsEdgeColor { get; } = Color.FromArgb(230, 255, 122, 98);
        public static Color LostHitpointsTopColor { get; } = Color.FromArgb(120, 190, 34, 32);
        public static Color LostHitpointsBottomColor { get; } = Color.FromArgb(175, 86, 6, 12);
        public static Color DeadOverlayTopColor { get; } = Color.FromArgb(70, 10, 11, 14);
        public static Color DeadOverlayBottomColor { get; } = Color.FromArgb(200, 6, 6, 9);
        public static Color FootOverlayFadeColor { get; } = Color.FromArgb(0, 6, 6, 8);
        public static Color FootOverlayColor { get; } = Color.FromArgb(215, 6, 6, 8);
        public static Color OverlayMaskTopColor { get; } = Color.FromArgb(190, 0, 0, 0);
        public static Color OverlayMaskBottomColor { get; } = Color.FromArgb(255, 0, 0, 0);
        public static Color OverlayGlossEdgeColor { get; } = Color.FromArgb(90, 255, 255, 255);
        public static Color OverlayGlossTopColor { get; } = Color.FromArgb(30, 255, 255, 255);
        public static Color OverlayGlossMiddleColor { get; } = Color.FromArgb(0, 0, 0, 0);
        public static Color OverlayGlossBottomColor { get; } = Color.FromArgb(110, 0, 0, 0);
        public static Color HitpointsHealthyColor { get; } = Color.FromRgb(246, 242, 232);
        public static Color HitpointsWoundedColor { get; } = Color.FromRgb(248, 206, 82);
        public static Color HitpointsCriticalColor { get; } = Color.FromRgb(255, 96, 78);
        public static Color HitpointsOutlineColor { get; } = Color.FromArgb(200, 8, 6, 6);
        public static Color HitpointsShadowColor { get; } = Color.FromArgb(140, 0, 0, 0);

        public static BoxShadows Shadow { get; } = new(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 1,
            Blur = 3,
            Spread = 1,
            Color = ShadowColor
        });

        public static BoxShadows InnerShadow { get; } = new(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 0,
            Blur = 8,
            Spread = 2,
            Color = InnerShadowColor,
            IsInset = true
        });

        public static BoxShadows Glow { get; } = new(
            new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 0,
                Blur = 6,
                Spread = 2,
                Color = GlowColor
            },
            [
                new BoxShadow
                {
                    OffsetX = 0,
                    OffsetY = 0,
                    Blur = 18,
                    Spread = 2,
                    Color = GlowHaloColor
                }
            ]);
    }

    public static class UnitName
    {
        public static Color ShadowColor { get; } = Color.FromArgb(140, 0, 0, 0);
    }

    public static class Bar
    {
        public static Color EdgeColor { get; } = Color.FromArgb(240, 8, 4, 4);
        public static Color BevelColor { get; } = Color.FromArgb(54, 255, 236, 222);
        public static Color RelightTopColor { get; } = Color.FromArgb(22, 255, 240, 236);
        public static Color RelightUpperColor { get; } = Color.FromArgb(46, 255, 240, 236);
        public static Color RelightLowerColor { get; } = Color.FromArgb(56, 0, 0, 0);
        public static Color RelightBottomColor { get; } = Color.FromArgb(24, 0, 0, 0);
        public static Color OverlayColor { get; } = Color.FromArgb(150, 0, 0, 0);
        public static Color OverlayFadeColor { get; } = Color.FromArgb(0, 0, 0, 0);
        public static Color FillOverlayColor { get; } = Color.FromArgb(120, 0, 0, 0);
        public static Color FillOverlayFadeColor { get; } = Color.FromArgb(0, 0, 0, 0);
        public static Color TextColor { get; } = Color.FromRgb(250, 246, 240);
        public static Color TextOutlineColor { get; } = Color.FromArgb(230, 10, 5, 5);
        public static Color HitpointsFillColor { get; } = Color.FromRgb(184, 34, 58);
        public static Color HitpointsTrackColor { get; } = Color.FromRgb(56, 20, 14);
        public static Color ExperienceFillColor { get; } = Color.FromRgb(52, 118, 206);
        public static Color ExperienceTrackColor { get; } = Color.FromRgb(16, 28, 48);
        public static Color ExperienceGainFillColor { get; } = Color.FromRgb(132, 196, 255);
        public static Color MaximumExperienceFillColor { get; } = Color.FromRgb(166, 120, 62);
        public static Color MaximumExperienceTrackColor { get; } = Color.FromRgb(44, 30, 16);
        public static Color PendingExperienceFillColor { get; } = Color.FromRgb(220, 168, 60);
        public static Color PendingExperienceTrackColor { get; } = Color.FromRgb(50, 36, 12);
    }

    public static class OfferStatus
    {
        public static Color UnaffordableResourceColor { get; } = Color.FromRgb(232, 112, 92);
        public static Color ReasonColor { get; } = Color.FromRgb(226, 164, 118);
    }

    public static class Spell
    {
        public static Color IconLightBackgroundColor { get; } = Color.FromRgb(220, 206, 187);
        public static Color IconDarkBackgroundColor { get; } = Color.FromRgb(30, 28, 34);
        public static Color SelectedBorderColor { get; } = Color.FromRgb(255, 246, 230);
        public static Color TileHoverColor { get; } = Color.FromArgb(28, 243, 217, 147);
        public static Color TileSelectedColor { get; } = Color.FromArgb(46, 243, 217, 147);
        public static Color LockedOverlayColor { get; } = Color.FromArgb(200, 22, 21, 26);
        public static Color ResearchOverlayColor { get; } = Color.FromArgb(150, 22, 21, 26);
        public static Color ResearchGlowColor { get; } = Color.FromRgb(255, 226, 160);
        public static Color LockedInkColor { get; } = Color.FromRgb(163, 116, 110);
    }

    public static class Field
    {
        public static Color FaceTopColor { get; } = Color.FromRgb(20, 15, 10);
        public static Color FaceMiddleColor { get; } = Color.FromRgb(30, 23, 16);
        public static Color FaceBottomColor { get; } = Color.FromRgb(44, 34, 24);
        public static Color HoverFaceTopColor { get; } = Color.FromRgb(32, 24, 16);
        public static Color HoverFaceMiddleColor { get; } = Color.FromRgb(46, 36, 23);
        public static Color HoverFaceBottomColor { get; } = Color.FromRgb(64, 50, 32);
        public static Color TextColor { get; } = Color.FromRgb(239, 228, 207);
        public static Color DisabledTextColor { get; } = Color.FromRgb(94, 86, 74);
        public static Color TextShadowColor { get; } = Color.FromArgb(230, 0, 0, 0);
    }

    public static class Pill
    {
        public static Color OutlineColor { get; } = Color.FromRgb(24, 14, 4);
        public static Color InnerShadowColor { get; } = Color.FromArgb(220, 0, 0, 0);
        public static Color FaceTopColor { get; } = Color.FromRgb(27, 39, 48);
        public static Color FaceMiddleColor { get; } = Color.FromRgb(34, 48, 57);
        public static Color FaceBottomColor { get; } = Color.FromRgb(25, 35, 44);
        public static Color HoverFaceTopColor { get; } = Color.FromRgb(38, 54, 64);
        public static Color HoverFaceMiddleColor { get; } = Color.FromRgb(46, 64, 75);
        public static Color HoverFaceBottomColor { get; } = Color.FromRgb(34, 48, 58);
        public static Color TextColor { get; } = Color.FromRgb(235, 232, 225);
        public static Color DisabledTextColor { get; } = Color.FromRgb(118, 126, 128);
        public static Color DisabledFaceTopColor { get; } = Color.FromRgb(22, 28, 32);
        public static Color DisabledFaceMiddleColor { get; } = Color.FromRgb(26, 33, 38);
        public static Color DisabledFaceBottomColor { get; } = Color.FromRgb(20, 25, 29);
        public static Color TextShadowColor { get; } = Color.FromArgb(230, 0, 0, 0);
        public static Color PrimaryFaceTopColor { get; } = Color.FromRgb(8, 44, 62);
        public static Color PrimaryFaceMiddleColor { get; } = Color.FromRgb(12, 58, 80);
        public static Color PrimaryFaceBottomColor { get; } = Color.FromRgb(6, 38, 54);
        public static Color PrimaryHoverFaceTopColor { get; } = Color.FromRgb(14, 60, 82);
        public static Color PrimaryHoverFaceMiddleColor { get; } = Color.FromRgb(20, 78, 104);
        public static Color PrimaryHoverFaceBottomColor { get; } = Color.FromRgb(10, 52, 72);
        public static Color PrimaryDisabledFaceTopColor { get; } = Color.FromRgb(23, 32, 37);
        public static Color PrimaryDisabledFaceMiddleColor { get; } = Color.FromRgb(31, 43, 48);
        public static Color PrimaryDisabledFaceBottomColor { get; } = Color.FromRgb(19, 28, 32);
        public static Color DangerFaceTopColor { get; } = Color.FromRgb(58, 18, 14);
        public static Color DangerFaceMiddleColor { get; } = Color.FromRgb(76, 24, 18);
        public static Color DangerFaceBottomColor { get; } = Color.FromRgb(50, 14, 11);
        public static Color DangerHoverFaceTopColor { get; } = Color.FromRgb(80, 26, 20);
        public static Color DangerHoverFaceMiddleColor { get; } = Color.FromRgb(104, 34, 26);
        public static Color DangerHoverFaceBottomColor { get; } = Color.FromRgb(68, 20, 15);
        public static Color DangerDisabledFaceTopColor { get; } = Color.FromRgb(32, 22, 21);
        public static Color DangerDisabledFaceMiddleColor { get; } = Color.FromRgb(43, 29, 28);
        public static Color DangerDisabledFaceBottomColor { get; } = Color.FromRgb(27, 18, 17);
        public static Color SuccessFaceTopColor { get; } = Color.FromRgb(16, 46, 22);
        public static Color SuccessFaceMiddleColor { get; } = Color.FromRgb(22, 62, 30);
        public static Color SuccessFaceBottomColor { get; } = Color.FromRgb(13, 40, 19);
        public static Color SuccessHoverFaceTopColor { get; } = Color.FromRgb(24, 64, 31);
        public static Color SuccessHoverFaceMiddleColor { get; } = Color.FromRgb(32, 84, 42);
        public static Color SuccessHoverFaceBottomColor { get; } = Color.FromRgb(19, 56, 27);
        public static Color SuccessDisabledFaceTopColor { get; } = Color.FromRgb(25, 32, 26);
        public static Color SuccessDisabledFaceMiddleColor { get; } = Color.FromRgb(33, 43, 35);
        public static Color SuccessDisabledFaceBottomColor { get; } = Color.FromRgb(21, 28, 22);
    }

    public static class Plate
    {
        public static Color OuterShadowColor { get; } = Color.FromArgb(176, 0, 0, 0);
        public static Color InnerShadowColor { get; } = Color.FromArgb(192, 0, 0, 0);
        public static Color TextShadowColor { get; } = Color.FromArgb(230, 0, 0, 0);
        public static Color GlossColor { get; } = Color.FromArgb(10, 255, 255, 255);
        public static Color GlossFadeColor { get; } = Color.FromArgb(0, 255, 255, 255);
        public static Color RoundGlossColor { get; } = Color.FromArgb(5, 255, 255, 255);
        public static Color FaceTopColor { get; } = Color.FromArgb(236, 26, 20, 14);
        public static Color FaceMiddleColor { get; } = Color.FromArgb(236, 44, 34, 24);
        public static Color FaceBottomColor { get; } = Color.FromArgb(236, 20, 15, 10);
        public static Color HoverFaceTopColor { get; } = Color.FromArgb(236, 40, 31, 20);
        public static Color HoverFaceMiddleColor { get; } = Color.FromArgb(236, 64, 50, 32);
        public static Color HoverFaceBottomColor { get; } = Color.FromArgb(236, 32, 24, 16);
        public static Color TextColor { get; } = Color.FromRgb(239, 228, 207);
        public static Color DisabledTextColor { get; } = Color.FromRgb(94, 86, 74);
        public static Color PrimaryFaceTopColor { get; } = Color.FromArgb(232, 0, 22, 34);
        public static Color PrimaryFaceMiddleColor { get; } = Color.FromArgb(232, 4, 38, 52);
        public static Color PrimaryFaceBottomColor { get; } = Color.FromArgb(232, 2, 19, 25);
        public static Color PrimaryHoverFaceTopColor { get; } = Color.FromArgb(232, 0, 36, 54);
        public static Color PrimaryHoverFaceMiddleColor { get; } = Color.FromArgb(232, 8, 60, 80);
        public static Color PrimaryHoverFaceBottomColor { get; } = Color.FromArgb(232, 4, 31, 40);
        public static Color PrimaryDisabledTextColor { get; } = Color.FromRgb(74, 104, 114);
        public static Color PriceOverlayColor { get; } = Color.FromArgb(80, 0, 0, 0);
        public static Color PriceDividerColor { get; } = Color.FromArgb(200, 138, 98, 66);
        public static Color KeyLightColor { get; } = Color.FromRgb(255, 170, 90);
        public static Color FillLightColor { get; } = Color.FromRgb(140, 170, 255);
        public static Color PointerLightColor { get; } = Color.FromRgb(255, 230, 190);
    }

    public static class Slider
    {
        public static Color TrackTopColor { get; } = Color.FromRgb(12, 9, 7);
        public static Color TrackBottomColor { get; } = Color.FromRgb(30, 23, 16);
        public static Color DisabledFillColor { get; } = Color.FromRgb(94, 86, 74);
    }

    public static class ScrollBar
    {
        public static Color TrackColor { get; } = Color.FromRgb(16, 12, 9);
        public static Color TrackBorderColor { get; } = Color.FromRgb(60, 42, 30);
        public static Color ThumbGlossColor { get; } = Color.FromArgb(70, 255, 236, 204);
    }

    public static class BuildingGraph
    {
        public static Color FighterColor { get; } = Color.FromRgb(214, 86, 72);
        public static Color RangerColor { get; } = Color.FromRgb(112, 184, 96);
        public static Color MageColor { get; } = Color.FromRgb(124, 134, 232);
        public static Color SpecialColor { get; } = Color.FromRgb(86, 194, 190);
        public static Color CommonColor { get; } = Color.FromRgb(176, 190, 208);
        public static Color LevelRingColor { get; } = Color.FromArgb(64, 243, 217, 147);
        public static Color CommonLightColor { get; } = Color.FromRgb(228, 236, 246);
        public static Color CommonDarkColor { get; } = Color.FromRgb(104, 116, 134);
        public static Color HubLightColor { get; } = Color.FromArgb(205, 240, 218, 160);
        public static Color HubColor { get; } = Color.FromArgb(205, 204, 162, 94);
        public static Color HubDarkColor { get; } = Color.FromArgb(205, 150, 108, 56);
        public static Color HubShadowColor { get; } = Color.FromArgb(150, 24, 14, 6);
        public static Color HubBevelColor { get; } = Color.FromArgb(170, 255, 242, 204);
        public static Color HighlightRingColor { get; } = Color.FromRgb(255, 246, 230);
        public static Color HighlightGlowColor { get; } = Color.FromArgb(185, 255, 246, 230);
        public static Color SceneOverlayColor { get; } = Color.FromArgb(190, 10, 9, 13);
        public static Color NodeBackgroundColor { get; } = Color.FromRgb(30, 29, 33);
        public static Color BlockedOverlayColor { get; } = Color.FromArgb(140, 22, 21, 26);
        public static Color BlockedInkColor { get; } = Color.FromRgb(163, 116, 110);
        private static Color NodeShadowColor { get; } = Color.FromArgb(170, 6, 5, 8);

        public static BoxShadows HighlightGlow { get; } = new(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 0,
            Blur = 14,
            Spread = 1,
            Color = HighlightGlowColor
        });

        public static BoxShadows NodeShadow { get; } = new(new BoxShadow
        {
            OffsetX = 0,
            OffsetY = 2,
            Blur = 8,
            Spread = 0,
            Color = NodeShadowColor
        });
    }

    public static class CapitalScene
    {
        public static Color EdgeMaskColor { get; } = Color.FromArgb(255, 255, 255, 255);
        public static Color EdgeMaskFadeColor { get; } = Color.FromArgb(0, 255, 255, 255);
        public static Color PanelBodyTopColor { get; } = Color.FromArgb(214, 24, 27, 32);
        public static Color PanelBodyBottomColor { get; } = Color.FromArgb(214, 21, 33, 40);
        public static Color FootOverlayColor { get; } = Color.FromArgb(170, 6, 5, 8);
        public static Color FootOverlayFadeColor { get; } = Color.FromArgb(0, 6, 5, 8);
    }

    public static class BattleLog
    {
        public static Color FadeTopColor { get; } = Color.FromArgb(110, 0, 0, 0);
        public static Color FadeBottomColor { get; } = Color.FromArgb(255, 0, 0, 0);
    }

    public static class Battle
    {
        public static class GroundMarker
        {
            public static Color OverlayColor { get; } = Color.FromRgb(216, 214, 222);
            public static Color CurrentColor { get; } = Color.FromRgb(243, 217, 147);
            public static Color EnemyTargetColor { get; } = Color.FromRgb(212, 71, 71);
            public static Color AllyTargetColor { get; } = Color.FromRgb(116, 176, 255);
            public static Color AllyReachableColor { get; } = Color.FromArgb(128, 116, 176, 255);
            public static Color SummonTargetColor { get; } = Color.FromRgb(140, 224, 150);
            public static Color SummonReachableColor { get; } = Color.FromArgb(128, 140, 224, 150);
        }

        public static class GridMarker
        {
            public static Color EnemyTargetColor { get; } = Color.FromRgb(212, 71, 71);
            public static Color EnemyReachableColor { get; } = Color.FromRgb(172, 44, 42);
            public static Color AllyTargetColor { get; } = Color.FromRgb(116, 176, 255);
            public static Color AllyReachableColor { get; } = Color.FromRgb(62, 116, 196);
            public static Color SummonTargetColor { get; } = Color.FromRgb(140, 224, 150);
            public static Color SummonReachableColor { get; } = Color.FromArgb(128, 140, 224, 150);
        }

        public static class Highlight
        {
            public static Color CurrentColor { get; } = Color.FromArgb(110, 243, 217, 147);
            public static Color EnemyTargetColor { get; } = Color.FromArgb(150, 232, 36, 36);
            public static Color AllyTargetColor { get; } = Color.FromArgb(150, 96, 164, 255);
            public static Color AllyReachableColor { get; } = Color.FromArgb(60, 96, 164, 255);
            public static Color SummonTargetColor { get; } = Color.FromArgb(150, 120, 224, 132);
            public static Color SummonReachableColor { get; } = Color.FromArgb(60, 120, 224, 132);
        }

        public static class StatusEffect
        {
            public static Color FaceColor { get; } = Color.FromRgb(26, 25, 28);
            public static Color CounterTextColor { get; } = Color.FromRgb(240, 226, 200);
            public static Color ParalyzeOverlayColor { get; } = Color.FromArgb(90, 226, 230, 238);
            public static Color PetrifyOverlayColor { get; } = Color.FromArgb(160, 118, 120, 128);
            public static Color PoisonOverlayColor { get; } = Color.FromArgb(100, 92, 196, 64);
            public static Color FrostbiteOverlayColor { get; } = Color.FromArgb(105, 120, 196, 248);
            public static Color BlisterOverlayColor { get; } = Color.FromArgb(100, 244, 120, 48);
            public static Color DefendColor { get; } = Color.FromRgb(120, 158, 196);
            public static Color ParalyzeColor { get; } = Color.FromRgb(236, 238, 244);
            public static Color PetrifyColor { get; } = Color.FromRgb(150, 150, 156);
            public static Color PoisonColor { get; } = Color.FromRgb(120, 200, 90);
            public static Color FrostbiteColor { get; } = Color.FromRgb(130, 200, 240);
            public static Color BlisterColor { get; } = Color.FromRgb(240, 140, 70);
            public static Color RetreatColor { get; } = Color.FromRgb(210, 210, 214);
            public static Color FortificationColor { get; } = Color.FromRgb(90, 130, 170);
            public static Color IncreaseDamageColor { get; } = Color.FromRgb(230, 170, 74);
            public static Color ReduceDamageColor { get; } = Color.FromRgb(220, 80, 70);
            public static Color ReduceInitiativeColor { get; } = Color.FromRgb(170, 120, 220);
            public static Color ReduceLevelColor { get; } = Color.FromRgb(150, 130, 170);
            public static Color ShatterArmorColor { get; } = Color.FromRgb(200, 120, 60);
            public static Color CureColor { get; } = Color.FromRgb(110, 210, 130);
            public static Color TransformColor { get; } = Color.FromRgb(214, 110, 184);
            public static Color BoostModifierColor { get; } = Color.FromRgb(243, 217, 147);
            public static Color WeakenModifierColor { get; } = Color.FromRgb(196, 92, 120);
        }

        public static class Feedback
        {
            public static Color DamageOverlayColor { get; } = Color.FromArgb(80, 224, 74, 66);
            public static Color DamageTextColor { get; } = Color.FromRgb(244, 122, 112);
            public static Color MissOverlayColor { get; } = Color.FromArgb(72, 236, 198, 74);
            public static Color MissTextColor { get; } = Color.FromRgb(250, 224, 96);
            public static Color WardOverlayColor { get; } = Color.FromArgb(80, 90, 150, 230);
            public static Color WardTextColor { get; } = Color.FromRgb(142, 190, 246);
            public static Color HealOverlayColor { get; } = Color.FromArgb(80, 86, 200, 96);
            public static Color HealTextColor { get; } = Color.FromRgb(140, 224, 150);
            public static Color EffectOverlayColor { get; } = Color.FromArgb(80, 160, 110, 220);
            public static Color EffectTextColor { get; } = Color.FromRgb(198, 150, 236);
            public static Color ActionOverlayColor { get; } = Color.FromArgb(72, 120, 132, 150);
            public static Color ActionTextColor { get; } = Color.FromRgb(200, 208, 220);
        }

        public static class ActionBar
        {
            public static Color SuggestedEmberCoreColor { get; } = Color.FromRgb(232, 122, 44);
            public static Color SuggestedEmberColor { get; } = Color.FromRgb(176, 70, 24);
            public static Color SuggestedEmberFadeColor { get; } = Color.FromArgb(0, 120, 42, 16);
            public static Color SuggestedEmberMaskColor { get; } = Color.FromArgb(255, 255, 255, 255);
            public static Color SuggestedEmberMaskFadeColor { get; } = Color.FromArgb(0, 255, 255, 255);
            public static Color SuggestedHeatColor { get; } = Color.FromRgb(255, 178, 102);
            public static Color SuggestedHeatFadeColor { get; } = Color.FromArgb(0, 255, 178, 102);
            public static Color SuggestedInkColor { get; } = Color.FromRgb(255, 240, 214);
        }

        public static class Fluid
        {
            public static Color FogDeepColor { get; } = Color.FromRgb(62, 68, 76);
            public static Color FogBodyColor { get; } = Color.FromRgb(168, 176, 184);
            public static Color FogPeakColor { get; } = Color.FromRgb(232, 236, 240);
            public static Color FogScatterColor { get; } = Color.FromRgb(210, 214, 218);
            public static Color MiasmaDeepColor { get; } = Color.FromRgb(35, 61, 66);
            public static Color MiasmaBodyColor { get; } = Color.FromRgb(101, 180, 156);
            public static Color MiasmaPeakColor { get; } = Color.FromRgb(180, 246, 222);
            public static Color MiasmaScatterColor { get; } = Color.FromRgb(200, 215, 212);
            public static Color DustDeepColor { get; } = Color.FromRgb(72, 64, 58);
            public static Color DustBodyColor { get; } = Color.FromRgb(170, 160, 148);
            public static Color DustPeakColor { get; } = Color.FromRgb(224, 216, 204);
            public static Color DustScatterColor { get; } = Color.FromRgb(212, 204, 192);
            public static Color LavaDeepColor { get; } = Color.FromRgb(120, 10, 0);
            public static Color LavaBodyColor { get; } = Color.FromRgb(255, 90, 0);
            public static Color LavaPeakColor { get; } = Color.FromRgb(255, 220, 120);
            public static Color LavaScatterColor { get; } = Color.FromRgb(0, 0, 0);
            public static Color VaporDeepColor { get; } = Color.FromRgb(80, 70, 68);
            public static Color VaporBodyColor { get; } = Color.FromRgb(176, 166, 160);
            public static Color VaporPeakColor { get; } = Color.FromRgb(240, 232, 226);
            public static Color VaporScatterColor { get; } = Color.FromRgb(236, 200, 170);
            public static Color WaterDeepColor { get; } = Color.FromRgb(0, 70, 80);
            public static Color WaterBodyColor { get; } = Color.FromRgb(40, 210, 210);
            public static Color WaterPeakColor { get; } = Color.FromRgb(190, 255, 250);
            public static Color WaterScatterColor { get; } = Color.FromRgb(0, 0, 0);
        }

        public static class Rain
        {
            public static Color StreakColor { get; } = Color.FromRgb(198, 216, 235);
            public static Color SplashColor { get; } = Color.FromRgb(222, 234, 244);
            public static Color HazeColor { get; } = Color.FromRgb(120, 140, 162);
        }

        public static class Light
        {
            public static Color SunDawnColor { get; } = Color.FromRgb(255, 226, 178);
            public static Color SunMiddayColor { get; } = Color.FromRgb(255, 243, 215);
            public static Color SunEveningColor { get; } = Color.FromRgb(255, 176, 96);
            public static Color MoonColor { get; } = Color.FromRgb(158, 184, 255);
            public static Color SkyColor { get; } = Color.FromRgb(115, 158, 255);
            public static Color SkyDuskColor { get; } = Color.FromRgb(120, 110, 255);
            public static Color LanternColor { get; } = Color.FromRgb(255, 219, 80);
            public static Color WindowColor { get; } = Color.FromRgb(255, 220, 128);
            public static Color LavaColor { get; } = Color.FromRgb(255, 124, 0);
            public static Color GlowingWaterColor { get; } = Color.FromRgb(178, 255, 201);
            public static Color GhostCrystalColor { get; } = Color.FromRgb(133, 255, 209);
            public static Color StormColor { get; } = Color.FromRgb(204, 218, 255);
        }
    }

    public static class World
    {
        public static class Highlight
        {
            public static Color SelectedColor { get; } = Color.FromRgb(116, 176, 255);
            public static Color ExhaustedColor { get; } = Color.FromRgb(168, 170, 176);
            public static Color HostileColor { get; } = Color.FromRgb(232, 64, 52);
        }

        public static class Reach
        {
            public static Color FillColor { get; } = Color.FromArgb(40, 116, 176, 255);
            public static Color FeatherColor { get; } = Color.FromArgb(18, 140, 196, 255);
            public static Color EdgeColor { get; } = Color.FromArgb(150, 170, 214, 255);
            public static Color OutlineColor { get; } = Color.FromArgb(110, 24, 40, 68);
        }

        public static class Path
        {
            public static Color ValidInnerColor { get; } = Color.FromRgb(255, 238, 176);
            public static Color ValidOuterColor { get; } = Color.FromRgb(222, 172, 74);
            public static Color UnreachedInnerColor { get; } = Color.FromRgb(236, 232, 222);
            public static Color UnreachedOuterColor { get; } = Color.FromRgb(156, 150, 138);
            public static Color AttackInnerColor { get; } = Color.FromRgb(255, 182, 156);
            public static Color AttackOuterColor { get; } = Color.FromRgb(206, 60, 46);
            public static Color OutlineColor { get; } = Color.FromArgb(210, 30, 21, 12);
            public static Color MarkerFillColor { get; } = Color.FromRgb(56, 40, 24);
        }

        public static class Objective
        {
            public static Color RingColor { get; } = Color.FromArgb(215, 10, 181, 228);
            public static Color RuneColor { get; } = Color.FromArgb(170, 2, 161, 205);
            public static Color ShadowColor { get; } = Color.FromArgb(90, 0, 0, 0);
        }

        public static class Tile
        {
            public static Color HiddenColor { get; } = Color.FromArgb(255, 0, 0, 0);
        }

        public static class Water
        {
            public static Color DeepColor { get; } = Color.FromRgb(20, 70, 97);
            public static Color ShallowColor { get; } = Color.FromRgb(47, 148, 155);
            public static Color SkyColor { get; } = Color.FromRgb(178, 216, 238);
            public static Color FoamColor { get; } = Color.FromRgb(236, 250, 252);
            public static Color SunColor { get; } = Color.FromRgb(176, 238, 252);
        }

        public static class Race
        {
            public static Color HumanColor { get; } = Color.FromRgb(54, 112, 218);
            public static Color UndeadColor { get; } = Color.FromRgb(108, 88, 144);
            public static Color HereticColor { get; } = Color.FromRgb(176, 58, 48);
            public static Color DwarfColor { get; } = Color.FromRgb(188, 126, 42);
            public static Color NeutralColor { get; } = Color.FromRgb(150, 150, 150);
            public static Color ElfColor { get; } = Color.FromRgb(40, 150, 96);
        }

        public static class StackBar
        {
            public static Color SelectedGlowColor { get; } = Color.FromArgb(230, 96, 196, 236);
            public static Color TextOutlineColor { get; } = Color.FromArgb(230, 6, 6, 8);
            public static Color MovesColor { get; } = Color.FromRgb(236, 232, 222);
            public static Color NoMovesColor { get; } = Color.FromRgb(214, 150, 96);
        }

        public static Color BackdropColor { get; } = Color.FromRgb(18, 17, 21);
        public static Color HoverCursorInnerColor { get; } = Color.FromArgb(250, 255, 252, 190);
        public static Color HoverCursorOuterColor { get; } = Color.FromArgb(240, 222, 212, 72);
        public static Color SpellCursorInnerColor { get; } = Color.FromArgb(230, 222, 206, 255);
        public static Color SpellCursorOuterColor { get; } = Color.FromArgb(220, 132, 98, 214);
        public static Color CursorHaloColor { get; } = Color.FromArgb(120, 0, 0, 0);
    }

    public static class Medallion
    {
        public static Color GoldColor { get; } = Color.FromRgb(122, 84, 46);
        public static Color GoldLightColor { get; } = Color.FromRgb(200, 156, 92);
        public static Color GoldDarkColor { get; } = Color.FromRgb(48, 30, 14);
        public static Color StoneColor { get; } = Color.FromRgb(40, 40, 39);
        public static Color ChannelColor { get; } = Color.FromRgb(24, 24, 23);
        public static Color ShineColor { get; } = Color.FromRgb(255, 244, 214);
        public static Color SlotColor { get; } = Color.FromRgb(26, 16, 10);
        public static Color BorderBackdropColor { get; } = Color.FromRgb(22, 12, 7);
        public static Color InsetColor { get; } = Color.FromRgb(10, 8, 6);
        public static Color HaloColor { get; } = Color.FromArgb(110, 0, 0, 0);
        public static Color FaceColor { get; } = Color.FromRgb(2, 3, 5);
        public static Color FaceEdgeColor { get; } = Color.FromRgb(3, 22, 28);
        public static Color FaceGlowColor { get; } = Color.FromRgb(40, 98, 104);
        public static Color FaceHoverEdgeColor { get; } = Color.FromRgb(6, 34, 42);
        public static Color FaceHoverGlowColor { get; } = Color.FromRgb(70, 140, 150);
        public static Color FaceLitColor { get; } = Color.FromRgb(20, 110, 140);
        public static Color FaceLitEdgeColor { get; } = Color.FromRgb(60, 180, 205);
        public static Color FaceLitGlowColor { get; } = Color.FromRgb(170, 236, 246);
        public static Color FaceLitHoverColor { get; } = Color.FromRgb(30, 130, 160);
        public static Color FaceLitHoverEdgeColor { get; } = Color.FromRgb(90, 200, 224);
        public static Color FaceLitHoverGlowColor { get; } = Color.FromRgb(210, 248, 252);
        public static Color InkColor { get; } = Color.FromRgb(238, 240, 242);
    }

    public static class Minimap
    {
        public static Color ViewportColor { get; } = Color.FromRgb(243, 217, 147);
        public static Color MarkerOutlineColor { get; } = Color.FromArgb(230, 12, 10, 8);
        public static Color CapitalInsetColor { get; } = Color.FromRgb(240, 214, 150);
        public static Color PortraitHoverOverlayColor { get; } = Color.FromArgb(48, 255, 236, 200);
        public static Color DirectionTextColor { get; } = Color.FromRgb(34, 20, 8);
    }

    internal static class DevTools
    {
        public static Color HitboxFillColor { get; } = Color.FromArgb(40, 255, 40, 220);
        public static Color HitboxStrokeColor { get; } = Color.FromArgb(220, 255, 40, 220);
        public static Color LightStrokeColor { get; } = Color.FromArgb(230, 255, 255, 255);
        public static Color LightRangeColor { get; } = Color.FromArgb(90, 255, 255, 255);
        public static Color LabelColor { get; } = Color.FromRgb(255, 255, 255);
    }
}
