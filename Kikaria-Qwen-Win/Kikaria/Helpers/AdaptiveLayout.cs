using System;

namespace Kikaria.Helpers
{
    public enum WidthCategory
    {
        Compact,
        RegularPad,
        WidePad
    }

    public struct Metrics
    {
        // MARK: - Core Dimensions
        public double Width { get; set; }
        public double Height { get; set; }
        public WidthCategory WidthCategory { get; set; }

        // MARK: - Device Classification
        public readonly bool IsPadWidth => WidthCategory == WidthCategory.RegularPad || WidthCategory == WidthCategory.WidePad;
        public readonly bool IsPadPortrait => IsPadWidth && Height >= Width;
        public readonly bool IsPadLandscape => IsPadWidth && Width > Height;

        // MARK: - Layout Capabilities
        public readonly bool IsTwoColumnCapable => Width >= 950 && Width > Height && WidthCategory != WidthCategory.Compact;
        public readonly bool HomeUsesTwoColumnLayout => IsPadLandscape && Width >= 900;
        public readonly bool ReviewUsesTwoColumnLayout => IsPadWidth && Width >= 700;
        public readonly bool StatsUsesTwoColumnLayout => IsPadLandscape && Width >= 900;
        public readonly bool SettingsUsesTwoColumnLayout => IsPadLandscape && Width >= 900;

        // MARK: - Horizontal Padding
        public readonly double HorizontalPadding
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => Width < 360 ? 20 : 24,
                    WidthCategory.RegularPad => 32,
                    WidthCategory.WidePad => 40,
                    _ => 20
                };
            }
        }

        public readonly double InnerHorizontalPadding
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 16,
                    WidthCategory.RegularPad => 24,
                    WidthCategory.WidePad => 32,
                    _ => 16
                };
            }
        }

        // MARK: - Portrait Max Widths
        public readonly double PortraitMaxCardWidth
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => Width - HorizontalPadding * 2,
                    WidthCategory.RegularPad => 520,
                    WidthCategory.WidePad => 600,
                    _ => Width - HorizontalPadding * 2
                };
            }
        }

        public readonly double PortraitMaxContentWidth
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => Width - HorizontalPadding * 2,
                    WidthCategory.RegularPad => 560,
                    WidthCategory.WidePad => 640,
                    _ => Width - HorizontalPadding * 2
                };
            }
        }

        // MARK: - Landscape Max Widths
        public readonly double LandscapeMaxContentWidth
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => Width - HorizontalPadding * 2,
                    WidthCategory.RegularPad => 700,
                    WidthCategory.WidePad => 900,
                    _ => Width - HorizontalPadding * 2
                };
            }
        }

        public readonly double LandscapeMaxTwoColumnWidth
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.RegularPad => 1000,
                    WidthCategory.WidePad => 1200,
                    _ => Width - HorizontalPadding * 2
                };
            }
        }

        // MARK: - Scale Factors
        public readonly double CardScaleFactor
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 1.0,
                    WidthCategory.RegularPad => 1.05,
                    WidthCategory.WidePad => 1.1,
                    _ => 1.0
                };
            }
        }

        public readonly double TitleScaleFactor
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 1.0,
                    WidthCategory.RegularPad => 1.15,
                    WidthCategory.WidePad => 1.25,
                    _ => 1.0
                };
            }
        }

        public readonly double BodyScaleFactor
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 1.0,
                    WidthCategory.RegularPad => 1.05,
                    WidthCategory.WidePad => 1.1,
                    _ => 1.0
                };
            }
        }

        // MARK: - Portrait Scale Factor
        public readonly double PortraitScaleFactor => IsPadPortrait ? (Width >= 900 ? 1.36 : 1.30) : 1;

        // MARK: - Scale Factors (Apple Source)
        public readonly double HomeScale => IsPadPortrait ? (Width >= 900 ? 1.36 : 1.30) : IsPadWidth ? 1.14 : 1;
        public readonly double HomeHeaderScale => IsPadPortrait ? (Width >= 900 ? 1.20 : 1.16) : IsPadWidth ? 1.14 : 1;
        public readonly double ReviewScale => IsPadPortrait ? (Width >= 900 ? 1.20 : 1.18) : IsPadWidth ? 1.15 : 1;
        public readonly double ReviewButtonScale => IsPadPortrait ? (Width >= 900 ? 1.18 : 1.14) : 1;
        public readonly double CardScale => IsPadPortrait ? (Width >= 900 ? 1.24 : 1.18) : IsPadWidth ? 1.05 : 1;
        public readonly double PresetScale => IsPadPortrait ? (Width >= 900 ? 1.18 : 1.12) : 1;
        public readonly double ScopeScale => IsPadPortrait ? (Width >= 900 ? 1.16 : 1.10) : 1;
        public readonly double OverviewScale => IsPadPortrait ? (Width >= 900 ? 1.18 : 1.12) : 1;
        public readonly double SettingsScale => IsPadPortrait ? (Width >= 900 ? 1.16 : 1.10) : 1;
        public readonly double SettingsRowScale => IsPadPortrait ? (Width >= 900 ? 1.14 : 1.08) : 1;
        public readonly double NewPresetScale => IsPadPortrait ? (Width >= 900 ? 1.16 : 1.10) : 1;
        public readonly double ListCardScale => IsPadPortrait ? (Width >= 900 ? 1.16 : 1.10) : 1;

        // MARK: - Portrait Max Widths
        public readonly double PortraitHomeMaxWidth => Width >= 900 ? 760 : 720;
        public readonly double PortraitMainMaxWidth => Width >= 900 ? 680 : 660;
        public readonly double PortraitFormMaxWidth => Width >= 900 ? 620 : 600;
        public readonly double PortraitReviewMaxWidth => Width >= 900 ? 720 : 700;

        // MARK: - Max Widths
        public readonly double HomeMaxWidth => IsPadPortrait ? PortraitHomeMaxWidth : WidthCategory == WidthCategory.Compact ? double.PositiveInfinity : WidthCategory == WidthCategory.RegularPad ? 700 : 780;
        public readonly double MainMaxWidth => IsPadPortrait ? PortraitMainMaxWidth : WidthCategory == WidthCategory.Compact ? double.PositiveInfinity : WidthCategory == WidthCategory.RegularPad ? 680 : 760;
        public readonly double FormMaxWidth => IsPadPortrait ? PortraitFormMaxWidth : WidthCategory == WidthCategory.Compact ? double.PositiveInfinity : WidthCategory == WidthCategory.RegularPad ? 600 : 640;
        public readonly double ReviewMaxWidth => IsPadPortrait ? PortraitReviewMaxWidth : WidthCategory == WidthCategory.Compact ? double.PositiveInfinity : WidthCategory == WidthCategory.RegularPad ? 760 : 820;

        // MARK: - Landscape Layout Metrics
        public readonly double HomeLandscapeMaxWidth => 1080;
        public readonly double HomeLandscapeColumnSpacing => Math.Clamp((Width - HorizontalPadding * 2) * 0.06, 56, 68);
        public readonly double HomeLandscapeRightWidth => Math.Clamp((Width - HorizontalPadding * 2) * 0.39, 400, 430);
        public readonly double HomeLandscapeLeftWidth => Math.Clamp(Width - HorizontalPadding * 2 - Math.Clamp((Width - HorizontalPadding * 2) * 0.39, 400, 430) - Math.Clamp((Width - HorizontalPadding * 2) * 0.06, 56, 68), 410, 560);
        public readonly double ReviewLandscapeMaxWidth => 1160;
        public readonly double ReviewLandscapeColumnSpacing => Math.Clamp((Width - HorizontalPadding * 2) * 0.055, 48, 64);
        public readonly double ReviewLandscapeRightWidth => Math.Clamp((Width - HorizontalPadding * 2) * 0.32, 340, 380);
        public readonly double ReviewLandscapeLeftWidth => Width - HorizontalPadding * 2 - Math.Clamp((Width - HorizontalPadding * 2) * 0.32, 340, 380) - Math.Clamp((Width - HorizontalPadding * 2) * 0.055, 48, 64);
        public readonly double CollectionLandscapeMaxWidth => 1100;
        public readonly double SettingsLandscapeMaxWidth => 1080;

        // MARK: - Other Metrics
        public readonly double AdaptiveBackButtonSize => 42;
        public readonly double AdaptiveTopBarTrailingWidth => IsPadPortrait ? 64 : 42;
        public readonly double NewPresetInputHeight => IsPadPortrait ? (Width >= 900 ? 62 : 58) : 0;
        public readonly double NewPresetTextEditorHeight => IsPadPortrait ? (Width >= 900 ? 380 : 340) : 260;
        public readonly double ReviewContentVerticalOffset => 8;
        public readonly double ReviewActionBottomPadding => 24;
        public readonly double ScopeGridMinimumWidth => IsPadPortrait ? (Width >= 900 ? 176 : 164) : 132;
        public readonly double ScopeGridSpacing => IsPadPortrait ? 16 : 12;

        // MARK: - Grid Metrics
        public readonly int GridColumns
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 1,
                    WidthCategory.RegularPad => 2,
                    WidthCategory.WidePad => 3,
                    _ => 1
                };
            }
        }

        public readonly double GridSpacing
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 16,
                    WidthCategory.RegularPad => 20,
                    WidthCategory.WidePad => 24,
                    _ => 16
                };
            }
        }

        public readonly double GridItemMinWidth
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 280,
                    WidthCategory.RegularPad => 300,
                    WidthCategory.WidePad => 320,
                    _ => 280
                };
            }
        }

        // MARK: - Spacing Metrics
        public readonly double SectionSpacing
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 24,
                    WidthCategory.RegularPad => 32,
                    WidthCategory.WidePad => 40,
                    _ => 24
                };
            }
        }

        public readonly double ItemSpacing
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 12,
                    WidthCategory.RegularPad => 16,
                    WidthCategory.WidePad => 20,
                    _ => 12
                };
            }
        }

        public readonly double CompactItemSpacing
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 8,
                    WidthCategory.RegularPad => 10,
                    WidthCategory.WidePad => 12,
                    _ => 8
                };
            }
        }

        // MARK: - Sizing Metrics
        public readonly double CardHeight
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 160,
                    WidthCategory.RegularPad => 180,
                    WidthCategory.WidePad => 200,
                    _ => 160
                };
            }
        }

        public readonly double ButtonHeight
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 48,
                    WidthCategory.RegularPad => 52,
                    WidthCategory.WidePad => 56,
                    _ => 48
                };
            }
        }

        public readonly double ButtonCornerRadius
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 14,
                    WidthCategory.RegularPad => 16,
                    WidthCategory.WidePad => 18,
                    _ => 14
                };
            }
        }

        public readonly double CardCornerRadius
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 16,
                    WidthCategory.RegularPad => 20,
                    WidthCategory.WidePad => 24,
                    _ => 16
                };
            }
        }

        public readonly double HeaderHeight
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 56,
                    WidthCategory.RegularPad => 64,
                    WidthCategory.WidePad => 72,
                    _ => 56
                };
            }
        }

        public readonly double BottomBarHeight
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 56,
                    WidthCategory.RegularPad => 60,
                    WidthCategory.WidePad => 64,
                    _ => 56
                };
            }
        }

        public readonly double IconSize
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 24,
                    WidthCategory.RegularPad => 28,
                    WidthCategory.WidePad => 32,
                    _ => 24
                };
            }
        }

        public readonly double SmallIconSize
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 16,
                    WidthCategory.RegularPad => 18,
                    WidthCategory.WidePad => 20,
                    _ => 16
                };
            }
        }

        public readonly double ProgressRingSize
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 60,
                    WidthCategory.RegularPad => 72,
                    WidthCategory.WidePad => 84,
                    _ => 60
                };
            }
        }

        public readonly double AvatarSize
        {
            get
            {
                return WidthCategory switch
                {
                    WidthCategory.Compact => 40,
                    WidthCategory.RegularPad => 48,
                    WidthCategory.WidePad => 56,
                    _ => 40
                };
            }
        }

        // MARK: - Two Column Metrics
        public readonly double TwoColumnPrimaryWidth
        {
            get
            {
                double available = Width - HorizontalPadding * 2 - GridSpacing;
                return Math.Max(300, available * 0.55);
            }
        }

        public readonly double TwoColumnSecondaryWidth
        {
            get
            {
                double available = Width - HorizontalPadding * 2 - GridSpacing;
                return Math.Max(260, available * 0.45);
            }
        }

        // MARK: - Compact Threshold
        public const double CompactWidthThreshold = 600;
        public const double RegularPadWidthThreshold = 900;
    }

    public static class AdaptiveLayout
    {
        public static Metrics MetricsFor(double width, double height)
        {
            WidthCategory category;

            if (width < Metrics.CompactWidthThreshold)
            {
                category = WidthCategory.Compact;
            }
            else if (width < Metrics.RegularPadWidthThreshold)
            {
                category = WidthCategory.RegularPad;
            }
            else
            {
                category = WidthCategory.WidePad;
            }

            return new Metrics
            {
                Width = width,
                Height = height,
                WidthCategory = category
            };
        }
    }
}
