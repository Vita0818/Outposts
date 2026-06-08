using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Kikaria
{
    public static class KikariaTheme
    {
        public static readonly Color SkyLight = Color.FromArgb(0xFF, 0x63, 0xBA, 0xF5);
        public static readonly Color SkyDark = Color.FromArgb(0xFF, 0x4D, 0xB8, 0xF5);
        public static SolidColorBrush Sky => GetAdaptiveBrush(SkyLight, SkyDark);

        public static readonly Color CyanLight = Color.FromArgb(0xFF, 0x92, 0xE0, 0xE8);
        public static readonly Color CyanDark = Color.FromArgb(0xFF, 0x52, 0xCC, 0xCF);
        public static SolidColorBrush Cyan => GetAdaptiveBrush(CyanLight, CyanDark);

        public static readonly Color MistLight = Color.FromArgb(0xFF, 0xE8, 0xF7, 0xFC);
        public static readonly Color MistDark = Color.FromArgb(0xFF, 0x14, 0x2A, 0x38);
        public static SolidColorBrush Mist => GetAdaptiveBrush(MistLight, MistDark);

        public static readonly Color BlueGrayLight = Color.FromArgb(0xFF, 0x9E, 0xB8, 0xCC);
        public static readonly Color BlueGrayDark = Color.FromArgb(0xFF, 0x7A, 0x9C, 0xB8);
        public static SolidColorBrush BlueGray => GetAdaptiveBrush(BlueGrayLight, BlueGrayDark);

        public static readonly Color MasteredGreenLight = Color.FromArgb(0xFF, 0x5C, 0xC2, 0x8A);
        public static readonly Color MasteredGreenDark = Color.FromArgb(0xFF, 0x52, 0xD1, 0x99);
        public static SolidColorBrush MasteredGreen => GetAdaptiveBrush(MasteredGreenLight, MasteredGreenDark);

        public static readonly Color MasteredDeepGreenLight = Color.FromArgb(0xFF, 0x1E, 0x78, 0x4D);
        public static readonly Color MasteredDeepGreenDark = Color.FromArgb(0xFF, 0x94, 0xEF, 0xBC);
        public static SolidColorBrush MasteredDeepGreen => GetAdaptiveBrush(MasteredDeepGreenLight, MasteredDeepGreenDark);

        public static readonly Color MasteredCompletedGreenLight = Color.FromArgb(0xFF, 0xCA, 0xED, 0xD6);
        public static readonly Color MasteredCompletedGreenDark = Color.FromArgb(0xFF, 0x2E, 0x61, 0x4D);
        public static SolidColorBrush MasteredCompletedGreen => GetAdaptiveBrush(MasteredCompletedGreenLight, MasteredCompletedGreenDark);

        public static readonly Color NextAmberLight = Color.FromArgb(0xFF, 0x8A, 0x7D, 0xBF);
        public static readonly Color NextAmberDark = Color.FromArgb(0xFF, 0x8C, 0x75, 0xD1);
        public static SolidColorBrush NextAmber => GetAdaptiveBrush(NextAmberLight, NextAmberDark);

        public static readonly Color RemoveCoralLight = Color.FromArgb(0xFF, 0xDC, 0x52, 0x4C);
        public static readonly Color RemoveCoralDark = Color.FromArgb(0xFF, 0xFA, 0x6B, 0x6B);
        public static SolidColorBrush RemoveCoral => GetAdaptiveBrush(RemoveCoralLight, RemoveCoralDark);

        public static readonly Color DeepTextLight = Color.FromArgb(0xFF, 0x21, 0x40, 0x54);
        public static readonly Color DeepTextDark = Color.FromArgb(0xFF, 0xE6, 0xF4, 0xFF);
        public static SolidColorBrush DeepText => GetAdaptiveBrush(DeepTextLight, DeepTextDark);

        public static readonly Color SoftTextLight = Color.FromArgb(0xFF, 0x6B, 0x8A, 0x9E);
        public static readonly Color SoftTextDark = Color.FromArgb(0xFF, 0xA8, 0xC5, 0xDB);
        public static SolidColorBrush SoftText => GetAdaptiveBrush(SoftTextLight, SoftTextDark);

        public static readonly Color TertiaryTextLight = Color.FromArgb(0xFF, 0x94, 0xAD, 0xC2);
        public static readonly Color TertiaryTextDark = Color.FromArgb(0xFF, 0x6E, 0x8C, 0xA8);
        public static SolidColorBrush TertiaryText => GetAdaptiveBrush(TertiaryTextLight, TertiaryTextDark);

        public static readonly Color GlassSurfaceLight = Colors.White;
        public static readonly Color GlassSurfaceDark = Color.FromArgb(0xFF, 0x0F, 0x21, 0x2E);
        public static SolidColorBrush GlassSurface => GetAdaptiveBrush(GlassSurfaceLight, GlassSurfaceDark);

        public static readonly Color GlassStrokeAccentLight = Color.FromArgb(0xFF, 0x92, 0xE0, 0xE8);
        public static readonly Color GlassStrokeAccentDark = Color.FromArgb(0xFF, 0x6B, 0xD6, 0xEC);
        public static SolidColorBrush GlassStrokeAccent => GetAdaptiveBrush(GlassStrokeAccentLight, GlassStrokeAccentDark);

        public static readonly Color ShadowLight = Color.FromArgb(0xFF, 0x63, 0xBA, 0xF5);
        public static readonly Color ShadowDark = Color.FromArgb(0xFF, 0x00, 0x05, 0x0D);
        public static SolidColorBrush Shadow => GetAdaptiveBrush(ShadowLight, ShadowDark);

        public static readonly LinearGradientBrush PageGradient
        {
            get
            {
                var brush = new LinearGradientBrush();
                brush.StartPoint = new Windows.Foundation.Point(0, 0);
                brush.EndPoint = new Windows.Foundation.Point(1, 1);
                
                if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                {
                    brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0xED, 0xFA, 0xFF), Offset = 0 });
                    brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0xDB, 0xF5, 0xFA), Offset = 0.5 });
                    brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0xF5, 0xFA, 0xFF), Offset = 1 });
                }
                else
                {
                    brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0x05, 0x12, 0x1C), Offset = 0 });
                    brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0x0A, 0x26, 0x33), Offset = 0.5 });
                    brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0x02, 0x0A, 0x14), Offset = 1 });
                }

                return brush;
            }
        }

        public static readonly LinearGradientBrush ActionGradient
        {
            get
            {
                var brush = new LinearGradientBrush();
                brush.StartPoint = new Windows.Foundation.Point(0, 0);
                brush.EndPoint = new Windows.Foundation.Point(1, 1);
                
                if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                {
                    brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0x59, 0xB7, 0xF7), Offset = 0 });
                    brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0x80, 0xDE, 0xE2), Offset = 1 });
                }
                else
                {
                    brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0x14, 0x70, 0xB2), Offset = 0 });
                    brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0xFF, 0x0F, 0x9E, 0xA8), Offset = 1 });
                }

                return brush;
            }
        }

        private static SolidColorBrush GetAdaptiveBrush(Color light, Color dark)
        {
            return Application.Current.RequestedTheme == ApplicationTheme.Light 
                ? new SolidColorBrush(light) 
                : new SolidColorBrush(dark);
        }
    }
}