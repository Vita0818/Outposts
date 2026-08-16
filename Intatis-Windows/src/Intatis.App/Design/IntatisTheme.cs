namespace Intatis.App.Design;

/// <summary>
/// Layout constants ported from the Apple app's IntatisDesign/LayoutProfiles tokens.
/// Colors stay semantic (theme resources) so light/dark follows the system.
/// </summary>
public static class IntatisTheme
{
    // Split layout (NavigationSplitView sidebar ideal 236, min 160).
    public const double SidebarIdealWidth = 236;
    public const double SidebarMinWidth = 160;

    // Chat screen.
    public const double ChatContentMaxWidth = 900;
    public const double MessageMaxWidth = 560;
    public const double PageHeaderFontSize = 30;   // largeTitle: semibold serif
    public const double BrandFontSize = 28;        // brand: semibold serif
    public const double ChatBodyFontSize = 15;     // chat: regular sans
    public const double BodyFontSize = 14;
    public const double HeadlineFontSize = 16;
    public const double CaptionFontSize = 12;
    public const double MetadataFontSize = 10;
    public const double MonospaceFontSize = 13;

    // Radii.
    public const double CardRadius = 22;
    public const double UserBubbleRadius = 16;
    public const double ComposerRadius = 20;
    public const double ArtifactStripRadius = 14;
    public const double InputRadius = 10;
    public const double ProviderRowRadius = 11;
    public const double AgentRowRadius = 12;
    public const double SidebarRowRadius = 10;

    // Composer metrics.
    public const double ComposerControlHeight = 40;
    public const double SelectionMinWidth = 190;
    public const double SelectionMaxWidth = 260;

    // Workspace surfaces.
    public const double CoworkRailActivationWidth = 980;
    public const double CoworkRailWidth = 348;
    public const double CodeThreadMinWidth = 620;
    public const double SettingsMaxWidth = 960;
    public const double SettingsCardMaxWidth = 820;
    public const double ProviderListWidth = 200;

    // Spacing.
    public const double MessageSpacing = 14;
    public const double CardSpacing = 18;
}
