using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Kikaria.Helpers
{
    public enum KikariaThemeColor
    {
        Sky,
        Cyan,
        Mist,
        BlueGray,
        MasteredGreen,
        MasteredDeepGreen,
        MasteredCompletedGreen,
        NextAmber,
        RemoveCoral,
        DeepText,
        SoftText,
        TertiaryText,
        GlassSurface,
        GlassStrokeAccent,
        Shadow,
        BubbleMint,
        BubbleLavender,
        BubbleGreen,
        BubbleWhite
    }

    public static class KikariaTheme
    {
        public static Color SkyLight => Color.FromArgb(255, 99, 186, 245);
        public static Color SkyDark => Color.FromArgb(255, 77, 184, 245);

        public static Color CyanLight => Color.FromArgb(255, 145, 224, 232);
        public static Color CyanDark => Color.FromArgb(255, 82, 204, 209);

        public static Color MistLight => Color.FromArgb(255, 232, 247, 252);
        public static Color MistDark => Color.FromArgb(255, 20, 41, 56);

        public static Color BlueGrayLight => Color.FromArgb(255, 158, 184, 204);
        public static Color BlueGrayDark => Color.FromArgb(255, 122, 156, 184);

        public static Color MasteredGreenLight => Color.FromArgb(255, 92, 194, 138);
        public static Color MasteredGreenDark => Color.FromArgb(255, 82, 209, 153);

        public static Color MasteredDeepGreenLight => Color.FromArgb(255, 31, 120, 77);
        public static Color MasteredDeepGreenDark => Color.FromArgb(255, 148, 240, 189);

        public static Color MasteredCompletedGreenLight => Color.FromArgb(255, 201, 237, 214);
        public static Color MasteredCompletedGreenDark => Color.FromArgb(255, 46, 97, 77);

        public static Color NextAmberLight => Color.FromArgb(255, 138, 125, 191);
        public static Color NextAmberDark => Color.FromArgb(255, 140, 117, 209);

        public static Color RemoveCoralLight => Color.FromArgb(255, 219, 82, 77);
        public static Color RemoveCoralDark => Color.FromArgb(255, 250, 107, 107);

        public static Color DeepTextLight => Color.FromArgb(255, 33, 64, 84);
        public static Color DeepTextDark => Color.FromArgb(255, 230, 245, 255);

        public static Color SoftTextLight => Color.FromArgb(255, 107, 138, 158);
        public static Color SoftTextDark => Color.FromArgb(255, 168, 196, 219);

        public static Color TertiaryTextLight => Color.FromArgb(255, 148, 173, 194);
        public static Color TertiaryTextDark => Color.FromArgb(255, 110, 140, 168);

        public static Color GlassSurfaceLight => Color.FromArgb(255, 255, 255, 255);
        public static Color GlassSurfaceDark => Color.FromArgb(255, 15, 33, 46);

        public static Color GlassStrokeAccentLight => Color.FromArgb(255, 145, 224, 232);
        public static Color GlassStrokeAccentDark => Color.FromArgb(255, 107, 214, 237);

        public static Color ShadowLight => Color.FromArgb(255, 99, 186, 245);
        public static Color ShadowDark => Color.FromArgb(255, 0, 5, 13);

        public static Color BubbleMintLight => Color.FromArgb(255, 186, 242, 230);
        public static Color BubbleMintDark => Color.FromArgb(255, 51, 148, 138);

        public static Color BubbleLavenderLight => Color.FromArgb(255, 191, 199, 255);
        public static Color BubbleLavenderDark => Color.FromArgb(255, 82, 77, 148);

        public static Color BubbleGreenLight => Color.FromArgb(255, 199, 242, 189);
        public static Color BubbleGreenDark => Color.FromArgb(255, 51, 128, 87);

        public static Color BubbleWhiteLight => Color.FromArgb(255, 255, 255, 255);
        public static Color BubbleWhiteDark => Color.FromArgb(255, 38, 59, 84);

        public static Color GetColor(KikariaThemeColor color, bool isDarkMode)
        {
            return color switch
            {
                KikariaThemeColor.Sky => isDarkMode ? SkyDark : SkyLight,
                KikariaThemeColor.Cyan => isDarkMode ? CyanDark : CyanLight,
                KikariaThemeColor.Mist => isDarkMode ? MistDark : MistLight,
                KikariaThemeColor.BlueGray => isDarkMode ? BlueGrayDark : BlueGrayLight,
                KikariaThemeColor.MasteredGreen => isDarkMode ? MasteredGreenDark : MasteredGreenLight,
                KikariaThemeColor.MasteredDeepGreen => isDarkMode ? MasteredDeepGreenDark : MasteredDeepGreenLight,
                KikariaThemeColor.MasteredCompletedGreen => isDarkMode ? MasteredCompletedGreenDark : MasteredCompletedGreenLight,
                KikariaThemeColor.NextAmber => isDarkMode ? NextAmberDark : NextAmberLight,
                KikariaThemeColor.RemoveCoral => isDarkMode ? RemoveCoralDark : RemoveCoralLight,
                KikariaThemeColor.DeepText => isDarkMode ? DeepTextDark : DeepTextLight,
                KikariaThemeColor.SoftText => isDarkMode ? SoftTextDark : SoftTextLight,
                KikariaThemeColor.TertiaryText => isDarkMode ? TertiaryTextDark : TertiaryTextLight,
                KikariaThemeColor.GlassSurface => isDarkMode ? GlassSurfaceDark : GlassSurfaceLight,
                KikariaThemeColor.GlassStrokeAccent => isDarkMode ? GlassStrokeAccentDark : GlassStrokeAccentLight,
                KikariaThemeColor.Shadow => isDarkMode ? ShadowDark : ShadowLight,
                KikariaThemeColor.BubbleMint => isDarkMode ? BubbleMintDark : BubbleMintLight,
                KikariaThemeColor.BubbleLavender => isDarkMode ? BubbleLavenderDark : BubbleLavenderLight,
                KikariaThemeColor.BubbleGreen => isDarkMode ? BubbleGreenDark : BubbleGreenLight,
                KikariaThemeColor.BubbleWhite => isDarkMode ? BubbleWhiteDark : BubbleWhiteLight,
                _ => SkyLight
            };
        }

        public static SolidColorBrush GetBrush(KikariaThemeColor color, bool isDarkMode)
        {
            return new SolidColorBrush(GetColor(color, isDarkMode));
        }

        public static SolidColorBrush SkyLightBrush => new(SkyLight);
        public static SolidColorBrush SkyDarkBrush => new(SkyDark);
        public static SolidColorBrush CyanLightBrush => new(CyanLight);
        public static SolidColorBrush CyanDarkBrush => new(CyanDark);
        public static SolidColorBrush MistLightBrush => new(MistLight);
        public static SolidColorBrush MistDarkBrush => new(MistDark);
        public static SolidColorBrush BlueGrayLightBrush => new(BlueGrayLight);
        public static SolidColorBrush BlueGrayDarkBrush => new(BlueGrayDark);
        public static SolidColorBrush MasteredGreenLightBrush => new(MasteredGreenLight);
        public static SolidColorBrush MasteredGreenDarkBrush => new(MasteredGreenDark);
        public static SolidColorBrush MasteredDeepGreenLightBrush => new(MasteredDeepGreenLight);
        public static SolidColorBrush MasteredDeepGreenDarkBrush => new(MasteredDeepGreenDark);
        public static SolidColorBrush MasteredCompletedGreenLightBrush => new(MasteredCompletedGreenLight);
        public static SolidColorBrush MasteredCompletedGreenDarkBrush => new(MasteredCompletedGreenDark);
        public static SolidColorBrush NextAmberLightBrush => new(NextAmberLight);
        public static SolidColorBrush NextAmberDarkBrush => new(NextAmberDark);
        public static SolidColorBrush RemoveCoralLightBrush => new(RemoveCoralLight);
        public static SolidColorBrush RemoveCoralDarkBrush => new(RemoveCoralDark);
        public static SolidColorBrush DeepTextLightBrush => new(DeepTextLight);
        public static SolidColorBrush DeepTextDarkBrush => new(DeepTextDark);
        public static SolidColorBrush SoftTextLightBrush => new(SoftTextLight);
        public static SolidColorBrush SoftTextDarkBrush => new(SoftTextDark);
        public static SolidColorBrush TertiaryTextLightBrush => new(TertiaryTextLight);
        public static SolidColorBrush TertiaryTextDarkBrush => new(TertiaryTextDark);
        public static SolidColorBrush GlassSurfaceLightBrush => new(GlassSurfaceLight);
        public static SolidColorBrush GlassSurfaceDarkBrush => new(GlassSurfaceDark);
        public static SolidColorBrush GlassStrokeAccentLightBrush => new(GlassStrokeAccentLight);
        public static SolidColorBrush GlassStrokeAccentDarkBrush => new(GlassStrokeAccentDark);
        public static SolidColorBrush ShadowLightBrush => new(ShadowLight);
        public static SolidColorBrush ShadowDarkBrush => new(ShadowDark);
        public static SolidColorBrush BubbleMintLightBrush => new(BubbleMintLight);
        public static SolidColorBrush BubbleMintDarkBrush => new(BubbleMintDark);
        public static SolidColorBrush BubbleLavenderLightBrush => new(BubbleLavenderLight);
        public static SolidColorBrush BubbleLavenderDarkBrush => new(BubbleLavenderDark);
        public static SolidColorBrush BubbleGreenLightBrush => new(BubbleGreenLight);
        public static SolidColorBrush BubbleGreenDarkBrush => new(BubbleGreenDark);
        public static SolidColorBrush BubbleWhiteLightBrush => new(BubbleWhiteLight);
        public static SolidColorBrush BubbleWhiteDarkBrush => new(BubbleWhiteDark);

        public static LinearGradientBrush PageGradient(bool isDarkMode)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };

            if (isDarkMode)
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 5, 18, 28), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 10, 38, 51), Offset = 0.5 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 3, 10, 20), Offset = 1.0 });
            }
            else
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 237, 250, 255), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 219, 245, 250), Offset = 0.5 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 245, 250, 255), Offset = 1.0 });
            }

            return brush;
        }

        public static LinearGradientBrush ActionGradient(bool isDarkMode)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };

            if (isDarkMode)
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 20, 112, 179), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 15, 158, 168), Offset = 1.0 });
            }
            else
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 89, 184, 247), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 128, 222, 227), Offset = 1.0 });
            }

            return brush;
        }

        public static LinearGradientBrush MasteredGradient(bool isDarkMode)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };

            if (isDarkMode)
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 28, 138, 92), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 51, 191, 138), Offset = 1.0 });
            }
            else
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 99, 199, 140), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 173, 232, 194), Offset = 1.0 });
            }

            return brush;
        }

        public static LinearGradientBrush MasteredActionGradient(bool isDarkMode)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };

            if (isDarkMode)
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 23, 122, 84), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 46, 179, 125), Offset = 1.0 });
            }
            else
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 64, 168, 107), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 138, 209, 161), Offset = 1.0 });
            }

            return brush;
        }

        public static LinearGradientBrush NextGradient(bool isDarkMode)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };

            if (isDarkMode)
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 89, 74, 148), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 128, 102, 194), Offset = 1.0 });
            }
            else
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 199, 184, 240), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 148, 135, 204), Offset = 1.0 });
            }

            return brush;
        }

        public static LinearGradientBrush RemoveGradient(bool isDarkMode)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };

            if (isDarkMode)
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 148, 36, 41), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 219, 71, 71), Offset = 1.0 });
            }
            else
            {
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 230, 97, 89), Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(255, 250, 148, 128), Offset = 1.0 });
            }

            return brush;
        }

        public static LinearGradientBrush PageGradientLightBrush => PageGradient(false);
        public static LinearGradientBrush PageGradientDarkBrush => PageGradient(true);
        public static LinearGradientBrush ActionGradientLightBrush => ActionGradient(false);
        public static LinearGradientBrush ActionGradientDarkBrush => ActionGradient(true);
        public static LinearGradientBrush MasteredGradientLightBrush => MasteredGradient(false);
        public static LinearGradientBrush MasteredGradientDarkBrush => MasteredGradient(true);
        public static LinearGradientBrush MasteredActionGradientLightBrush => MasteredActionGradient(false);
        public static LinearGradientBrush MasteredActionGradientDarkBrush => MasteredActionGradient(true);
        public static LinearGradientBrush NextGradientLightBrush => NextGradient(false);
        public static LinearGradientBrush NextGradientDarkBrush => NextGradient(true);
        public static LinearGradientBrush RemoveGradientLightBrush => RemoveGradient(false);
        public static LinearGradientBrush RemoveGradientDarkBrush => RemoveGradient(true);
    }
}
