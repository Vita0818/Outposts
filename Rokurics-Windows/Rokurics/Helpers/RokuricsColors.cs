using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Rokurics.Helpers;

/// <summary>
/// Color palette matching the source Rokurics design system.
/// Mirrors RokuricsColors / MacTheme from source.
/// </summary>
public static class RokuricsColors
{
    // Primary text — Mac deepText: dark=RGB(0.88,0.97,0.96), light=RGB(0.09,0.24,0.27)
    public static Color DeepTextLight => Color.FromArgb(255, 23, 61, 69);
    public static Color DeepTextDark => Color.FromArgb(255, 224, 247, 245);

    // Secondary text — Mac softText: dark=RGB(0.64,0.80,0.80), light=RGB(0.38,0.55,0.57)
    public static Color SoftTextLight => Color.FromArgb(255, 97, 140, 145);
    public static Color SoftTextDark => Color.FromArgb(255, 163, 204, 204);

    // Tertiary text — Mac tertiaryText: dark=RGB(0.42,0.58,0.59), light=RGB(0.56,0.68,0.70)
    public static Color TertiaryTextLight => Color.FromArgb(255, 143, 173, 179);
    public static Color TertiaryTextDark => Color.FromArgb(255, 107, 148, 150);

    // Accent colors — exact match to MacTheme.swift RGB values
    // Mac: aqua = RGB(0.31, 0.74, 0.73)
    public static Color Aqua => Color.FromArgb(255, 79, 190, 186);
    // Mac: mint = RGB(0.58, 0.90, 0.76)
    public static Color Mint => Color.FromArgb(255, 148, 230, 194);
    // Mac: paleCyan = RGB(0.78, 0.95, 0.94)
    public static Color PaleCyan => Color.FromArgb(255, 199, 242, 240);
    // Mac: leaf = RGB(0.36, 0.66, 0.54)
    public static Color Leaf => Color.FromArgb(255, 92, 168, 138);
    // Mac: coral = RGB(0.89, 0.45, 0.43)
    public static Color Coral => Color.FromArgb(255, 227, 115, 110);
    // Mac: amber = RGB(0.88, 0.66, 0.32)
    public static Color Amber => Color.FromArgb(255, 224, 168, 82);
    public static Color SoftTeal => Color.FromArgb(255, 48, 176, 199);

    // Action gradient — matches Mac accentGradient: aqua -> mint, topLeading -> bottomTrailing
    public static Color ActionStart => Aqua;
    public static Color ActionEnd => Mint;

    // Page background — Mac glassSurface: dark=RGB(0.04,0.13,0.13), light=white
    public static Color PageBackgroundLight => Color.FromArgb(255, 255, 255, 255);
    public static Color PageBackgroundDark => Color.FromArgb(255, 10, 33, 33);

    // Card background with teal tint matching Mac glassSurface
    public static Color CardFillLight => Color.FromArgb(200, 255, 255, 255);
    public static Color CardFillDark => Color.FromArgb(200, 15, 40, 40);

    // Glass stroke — Mac: dark=RGB(0.38,0.76,0.73), light=RGB(0.88,1.00,0.96)
    public static Color GlassStrokeLight => Color.FromArgb(255, 224, 255, 245);
    public static Color GlassStrokeDark => Color.FromArgb(255, 97, 194, 186);

    // Shadow — Mac: dark=black, light=RGB(0.28,0.68,0.64)
    public static Color ShadowLight => Color.FromArgb(255, 71, 173, 163);
    public static Color ShadowDark => Color.FromArgb(255, 0, 0, 0);

    // Folder colors matching source StudyFolderColorToken
    public static Color FolderRed => Color.FromArgb(255, 255, 69, 58);
    public static Color FolderOrange => Color.FromArgb(255, 255, 149, 0);
    public static Color FolderYellow => Color.FromArgb(255, 255, 204, 0);
    public static Color FolderGreen => Color.FromArgb(255, 52, 199, 89);
    public static Color FolderMint => Color.FromArgb(255, 102, 212, 207);
    public static Color FolderTeal => Color.FromArgb(255, 48, 176, 199);
    public static Color FolderCyan => Color.FromArgb(255, 79, 190, 186); // = Aqua
    public static Color FolderBlue => Color.FromArgb(255, 0, 122, 255);
    public static Color FolderIndigo => Color.FromArgb(255, 94, 92, 230);
    public static Color FolderPurple => Color.FromArgb(255, 175, 82, 222);
    public static Color FolderGray => Color.FromArgb(255, 142, 142, 147);
    public static Color FolderDefault => Color.FromArgb(255, 0, 122, 255);

    public static Color FolderColorFor(Models.StudyFolderColorToken? token) => token switch
    {
        Models.StudyFolderColorToken.Red => FolderRed,
        Models.StudyFolderColorToken.Orange => FolderOrange,
        Models.StudyFolderColorToken.Yellow => FolderYellow,
        Models.StudyFolderColorToken.Green => FolderGreen,
        Models.StudyFolderColorToken.Mint => FolderMint,
        Models.StudyFolderColorToken.Teal => FolderTeal,
        Models.StudyFolderColorToken.Cyan => FolderCyan,
        Models.StudyFolderColorToken.Blue => FolderBlue,
        Models.StudyFolderColorToken.Indigo => FolderIndigo,
        Models.StudyFolderColorToken.Purple => FolderPurple,
        Models.StudyFolderColorToken.Gray => FolderGray,
        _ => FolderDefault
    };

    public static SolidColorBrush DeepTextBrush(bool isDark) =>
        new(isDark ? DeepTextDark : DeepTextLight);

    public static SolidColorBrush SoftTextBrush(bool isDark) =>
        new(isDark ? SoftTextDark : SoftTextLight);

    public static SolidColorBrush PageBackgroundBrush(bool isDark) =>
        new(isDark ? PageBackgroundDark : PageBackgroundLight);

    public static LinearGradientBrush ActionGradientBrush
    {
        get
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };
            brush.GradientStops.Add(new GradientStop { Color = ActionStart, Offset = 0 });
            brush.GradientStops.Add(new GradientStop { Color = ActionEnd, Offset = 1 });
            return brush;
        }
    }
}
