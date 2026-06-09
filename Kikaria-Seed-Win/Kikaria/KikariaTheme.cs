using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Kikaria
{
    public class KikariaTheme
    {
        private static KikariaTheme? _instance;
        public static KikariaTheme Instance => _instance ??= new KikariaTheme();

        public Color SkyLight => Color.FromArgb(0xFF, 0x63, 0xBA, 0xF5);
        public Color SkyDark => Color.FromArgb(0xFF, 0x4D, 0xB8, 0xF5);
        public SolidColorBrush Sky => GetAdaptiveBrush(SkyLight, SkyDark);

        public Color CyanLight => Color.FromArgb(0xFF, 0x92, 0xE0, 0xE8);
        public Color CyanDark => Color.FromArgb(0xFF, 0x52, 0xCC, 0xCF);
        public SolidColorBrush Cyan => GetAdaptiveBrush(CyanLight, CyanDark);

        public Color MistLight => Color.FromArgb(0xFF, 0xE8, 0xF7, 0xFC);
        public Color MistDark => Color.FromArgb(0xFF, 0x14, 0x2A, 0x38);
        public SolidColorBrush Mist => GetAdaptiveBrush(MistLight, MistDark);

        public Color BlueGrayLight => Color.FromArgb(0xFF, 0x9E, 0xB8, 0xCC);
        public Color BlueGrayDark => Color.FromArgb(0xFF, 0x7A, 0x9C, 0xB8);
        public SolidColorBrush BlueGray => GetAdaptiveBrush(BlueGrayLight, BlueGrayDark);

        public Color MasteredGreenLight => Color.FromArgb(0xFF, 0x5C, 0xC2, 0x8A);
        public Color MasteredGreenDark => Color.FromArgb(0xFF, 0x52, 0xD1, 0x99);
        public SolidColorBrush MasteredGreen => GetAdaptiveBrush(MasteredGreenLight, MasteredGreenDark);

        public Color MasteredDeepGreenLight => Color.FromArgb(0xFF, 0x1E, 0x78, 0x4D);
        public Color MasteredDeepGreenDark => Color.FromArgb(0xFF, 0x94, 0xEF, 0xBC);
        public SolidColorBrush MasteredDeepGreen => GetAdaptiveBrush(MasteredDeepGreenLight, MasteredDeepGreenDark);

        public Color MasteredCompletedGreenLight => Color.FromArgb(0xFF, 0xCA, 0xED, 0xD6);
        public Color MasteredCompletedGreenDark => Color.FromArgb(0xFF, 0x2E, 0x61, 0x4D);
        public SolidColorBrush MasteredCompletedGreen => GetAdaptiveBrush(MasteredCompletedGreenLight, MasteredCompletedGreenDark);

        public Color NextAmberLight => Color.FromArgb(0xFF, 0x8A, 0x7D, 0xBF);
        public Color NextAmberDark => Color.FromArgb(0xFF, 0x8C, 0x75, 0xD1);
        public SolidColorBrush NextAmber => GetAdaptiveBrush(NextAmberLight, NextAmberDark);

        public Color RemoveCoralLight => Color.FromArgb(0xFF, 0xDC, 0x52, 0x4C);
        public Color RemoveCoralDark => Color.FromArgb(0xFF, 0xFA, 0x6B, 0x6B);
        public SolidColorBrush RemoveCoral => GetAdaptiveBrush(RemoveCoralLight, RemoveCoralDark);

        public Color DeepTextLight => Color.FromArgb(0xFF, 0x21, 0x40, 0x54);
        public Color DeepTextDark => Color.FromArgb(0xFF, 0xE6, 0xF4, 0xFF);
        public SolidColorBrush DeepText => GetAdaptiveBrush(DeepTextLight, DeepTextDark);

        public Color SoftTextLight => Color.FromArgb(0xFF, 0x6B, 0x8A, 0x9E);
        public Color SoftTextDark => Color.FromArgb(0xFF, 0xA8, 0xC5, 0xDB);
        public SolidColorBrush SoftText => GetAdaptiveBrush(SoftTextLight, SoftTextDark);

        public Color TertiaryTextLight => Color.FromArgb(0xFF, 0x94, 0xAD, 0xC2);
        public Color TertiaryTextDark => Color.FromArgb(0xFF, 0x6E, 0x8C, 0xA8);
        public SolidColorBrush TertiaryText => GetAdaptiveBrush(TertiaryTextLight, TertiaryTextDark);

        public Color GlassSurfaceLight => Colors.White;
        public Color GlassSurfaceDark => Color.FromArgb(0xFF, 0x0F, 0x21, 0x2E);
        public SolidColorBrush GlassSurface => GetAdaptiveBrush(GlassSurfaceLight, GlassSurfaceDark);

        public Color GlassStrokeAccentLight => Color.FromArgb(0xFF, 0x92, 0xE0, 0xE8);
        public Color GlassStrokeAccentDark => Color.FromArgb(0xFF, 0x6B, 0xD6, 0xEC);
        public SolidColorBrush GlassStrokeAccent => GetAdaptiveBrush(GlassStrokeAccentLight, GlassStrokeAccentDark);

        public Color ShadowLight => Color.FromArgb(0xFF, 0x63, 0xBA, 0xF5);
        public Color ShadowDark => Color.FromArgb(0xFF, 0x00, 0x05, 0x0D);
        public SolidColorBrush Shadow => GetAdaptiveBrush(ShadowLight, ShadowDark);

        public LinearGradientBrush PageGradient
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

        public LinearGradientBrush ActionGradient
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

        private SolidColorBrush GetAdaptiveBrush(Color light, Color dark)
        {
            return Application.Current.RequestedTheme == ApplicationTheme.Light 
                ? new SolidColorBrush(light) 
                : new SolidColorBrush(dark);
        }
    }
}
