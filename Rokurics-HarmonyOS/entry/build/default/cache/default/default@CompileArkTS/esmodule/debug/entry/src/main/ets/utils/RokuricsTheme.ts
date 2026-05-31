/**
 * Theme system - mirrors RokuricsColors.swift + RokuricsTypography.swift + glass modifiers
 */
// ── Color utility: converts #RRGGBB base + AA alpha suffix to HarmonyOS #AARRGGBB ──
export function colorAlpha(baseHex: string, alphaHex: string): string {
    return '#' + alphaHex + baseHex.substring(1);
}
// ── Colors (dark mode — mirrors RokuricsColors.swift adaptive dark values) ──
export class RokuricsColors {
    static readonly aqua = '#57D6D1';
    static readonly mint = '#52BD94';
    static readonly mistGreen = '#0F2B26';
    static readonly softTeal = '#85CCCC';
    static readonly skyCyan = '#4DB3EB';
    static readonly paleAqua = '#266B61';
    static readonly coral = '#F5757A';
    static readonly deepText = '#E6FAF7';
    static readonly softText = '#A8D1D1';
    static readonly tertiaryText = '#759EA1';
    static readonly glassSurface = '#0D2424';
    static readonly glassStroke = '#8ADBD1';
    static readonly glassStrokeAccent = '#61D4C2';
    static readonly shadowColor = '#000808';
    static readonly pageBackground = '#051414';
    static readonly actionStart = '#128080';
    static readonly actionEnd = '#2BAB82';
    static readonly recordingAccentStart = '#D14257';
    static readonly recordingAccentEnd = '#E0706B';
    // ── Page gradient color stops (mirrors pageGradient dark in RokuricsColors.swift) ──
    static readonly pageGradientStart = '#051414';
    static readonly pageGradientMid = '#0A2B29';
    static readonly pageGradientEnd = '#030D12';
    // ── Action/orb gradient stops (mirrors actionGradient dark in RokuricsColors.swift) ──
    static readonly actionGradientStart = '#128080';
    static readonly actionGradientEnd = '#2BAB82';
}
// ── Typography ──
export interface Font {
    size: number;
    weight: number;
    family?: string;
}
export enum FontWeight {
    Regular = 400,
    Medium = 500,
    SemiBold = 600,
    Bold = 700
}
export class RokuricsTypography {
    static appTitle(size: number = 39): Font {
        return { size: size, weight: FontWeight.Medium, family: 'serif' };
    }
    static pageTitle(size: number = 30): Font {
        return { size: size, weight: FontWeight.Bold };
    }
    static sectionTitle(size: number = 17): Font {
        return { size: size, weight: FontWeight.SemiBold };
    }
    static body(size: number = 15): Font {
        return { size: size, weight: FontWeight.Regular };
    }
    static caption(size: number = 12): Font {
        return { size: size, weight: FontWeight.Medium };
    }
    static largeNumber(size: number = 42): Font {
        return { size: size, weight: FontWeight.Bold };
    }
    static button(size: number = 17): Font {
        return { size: size, weight: FontWeight.SemiBold };
    }
}
// ── Dark-mode glass opacity scaling (mirrors Apple's colorScheme adaptation) ──
// Apple reduces glass opacity by ~0.78× in dark mode across all glass modifiers.
// These constants are the light-mode values. For dark mode, multiply by 0.78.
// Callers: use the exported constants directly; they already include dark-mode scaling.
let DARK_MODE_SCALE = 0.78;
// Utility to adapt a hex alpha (last 2 chars) for dark mode
export function darkModeGlassOpacity(lightHexAlpha: string, isDark: boolean): string {
    if (!isDark)
        return lightHexAlpha;
    const alpha = parseInt(lightHexAlpha, 16);
    const scaled = Math.max(4, Math.floor(alpha * DARK_MODE_SCALE));
    return scaled.toString(16).padStart(2, '0').toUpperCase();
}
// Pre-computed glass opacity strings (hex alpha for appending to colors).
// Dark mode defaults (scaled from light 0.78× fill, 0.82× stroke, per Apple RokuricsGlassStyle):
export const glassFillOpacity = '5C'; // min(0.66*0.78, 0.36) → ~36% fill
export const glassStrokeHighOpacity = '38'; // min(0.27*0.82, 0.34) → ~22% stroke
export const glassStrokeMidOpacity = '0E'; // min(0.07*0.82, 0.34) → ~5% stroke
export const glassAccentOpacity = '0A'; // ~4% accent
// Reusable glass card backing (mirrors rokuricsLiquidGlassCard dark mode) ──
export function GlassCardContent(child: WrappedBuilder<[
]>, parent = null): void {
    const __child__ = child;
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender, child = __child__) => {
        Column.create();
        Column.borderRadius(20);
        Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity));
        Column.shadow({
            color: colorAlpha(RokuricsColors.shadowColor, '18'),
            radius: 18,
            offsetY: 10
        });
        Column.border({
            width: 1,
            color: {
                colors: [
                    [0xFFFFFF, 0.22],
                    [RokuricsColors.glassStroke, 0.18],
                    [RokuricsColors.glassStrokeAccent, 0.24]
                ],
                direction: GradientDirection.RightBottom
            },
            radius: 20
        } as BorderOptions);
    }, Column);
    child.builder.bind(this)();
    Column.pop();
}
// ── Glass circle (mirrors rokuricsGlassCircle dark mode) ──
export function GlassCircle(size: number, child: WrappedBuilder<[
]>, parent = null): void {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create();
        Stack.width(size);
        Stack.height(size);
        Stack.borderRadius(size / 2);
        Stack.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '5C'));
        Stack.shadow({
            color: colorAlpha(RokuricsColors.shadowColor, '12'),
            radius: 14,
            offsetY: 7
        });
        Stack.border({
            width: 1,
            color: {
                colors: [
                    [0xFFFFFF, 0.22],
                    [RokuricsColors.glassStroke, 0.14],
                    [RokuricsColors.aqua, 0.24]
                ],
                direction: GradientDirection.RightBottom
            },
            radius: size / 2
        } as BorderOptions);
    }, Stack);
    child.builder.bind(this)();
    Stack.pop();
}
// ── Status pill (mirrors RokuricsStatusPill) ──
export function StatusPill(text: string, color: string, parent = null): void {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Row.create({ space: 6 });
        Row.padding({ left: 10, right: 10, top: 5, bottom: 5 });
        Row.borderRadius(12);
        Row.backgroundColor(colorAlpha(color, '18'));
        Row.border({ width: 1, color: colorAlpha(color, '30'), radius: 12 });
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.width(8);
        Circle.height(8);
        Circle.fill(color);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Text.create(text);
        Text.fontSize(11);
        Text.fontWeight(FontWeight.Medium);
        Text.fontColor(color);
        Text.maxLines(1);
    }, Text);
    Text.pop();
    Row.pop();
}
// ── Icon circle button (mirrors RokuricsIconCircleButton dark mode) ──
export function IconCircleButton(symbol: string, size: number, tint: string, onClick: () => void, parent = null): void {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Button.createWithChild();
        Button.width(size);
        Button.height(size);
        Button.borderRadius(size / 2);
        Button.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '5C'));
        Button.shadow({
            color: colorAlpha(RokuricsColors.shadowColor, '10'),
            radius: 12,
            offsetY: 6
        });
        Button.border({
            width: 1,
            color: {
                colors: [
                    [0xFFFFFF, 0.22],
                    [RokuricsColors.glassStroke, 0.14],
                    [RokuricsColors.aqua, 0.24]
                ],
                direction: GradientDirection.RightBottom
            },
            radius: size / 2
        } as BorderOptions);
        Button.onClick(onClick);
    }, Button);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Text.create(symbol);
        Text.fontSize(size * 0.42);
        Text.fontWeight(FontWeight.SemiBold);
        Text.fontColor(tint);
    }, Text);
    Text.pop();
    Button.pop();
}
// ── Waveform icon (mirrors recording leading icon) ──
export function WaveformIcon(size: number, parent = null): void {
    const __size__ = size;
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender, size = __size__) => {
        Stack.create();
        Stack.width(size);
        Stack.height(size);
        Stack.borderRadius(size / 2);
        Stack.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '4A'));
        Stack.border({
            width: 1,
            color: {
                colors: [
                    [0xFFFFFF, 0.20],
                    [RokuricsColors.glassStroke, 0.12],
                    [RokuricsColors.aqua, 0.20]
                ],
                direction: GradientDirection.RightBottom
            },
            radius: size / 2
        } as BorderOptions);
        Stack.shadow({
            color: colorAlpha(RokuricsColors.shadowColor, '08'),
            radius: 9,
            offsetY: 4
        });
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender, size = __size__) => {
        Row.create({ space: size * 0.1 });
        Row.justifyContent(FlexAlign.Center);
        Row.alignItems(VerticalAlign.Center);
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender, size = __size__) => {
        Rect.create();
        Rect.width(size * 0.12);
        Rect.height(size * 0.18);
        Rect.radius(size * 0.06);
        Rect.fill(RokuricsColors.aqua);
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender, size = __size__) => {
        Rect.create();
        Rect.width(size * 0.12);
        Rect.height(size * 0.40);
        Rect.radius(size * 0.06);
        Rect.fill(RokuricsColors.aqua);
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender, size = __size__) => {
        Rect.create();
        Rect.width(size * 0.12);
        Rect.height(size * 0.28);
        Rect.radius(size * 0.06);
        Rect.fill(RokuricsColors.aqua);
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender, size = __size__) => {
        Rect.create();
        Rect.width(size * 0.12);
        Rect.height(size * 0.56);
        Rect.radius(size * 0.06);
        Rect.fill(RokuricsColors.aqua);
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender, size = __size__) => {
        Rect.create();
        Rect.width(size * 0.12);
        Rect.height(size * 0.34);
        Rect.radius(size * 0.06);
        Rect.fill(RokuricsColors.aqua);
    }, Rect);
    Row.pop();
    Stack.pop();
}
// ── Segment button (for provider kind toggle, etc.) ──
export function SegmentedButton(labels: string[], selectedIndex: number, onSelect: (index: number) => void, parent = null): void {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Row.create({ space: 0 });
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        ForEach.create();
        const forEachItemGenFunction = (_item, index: number) => {
            const label = _item;
            (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
                Button.createWithChild();
                Button.padding({ left: 14, right: 14, top: 7, bottom: 7 });
                Button.backgroundColor(index === selectedIndex ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'));
                Button.borderRadius(index === 0 ?
                    { topLeft: 8, bottomLeft: 8 } :
                    index === labels.length - 1 ?
                        { topRight: 8, bottomRight: 8 } : 0);
                Button.onClick(() => onSelect(index));
            }, Button);
            (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
                Text.create(label);
                Text.fontSize(13);
                Text.fontColor(index === selectedIndex ? Color.White : RokuricsColors.softText);
            }, Text);
            Text.pop();
            Button.pop();
        };
        (parent ? parent : this).forEachUpdateFunction(elmtId, labels, forEachItemGenFunction, undefined, true, false);
    }, ForEach);
    ForEach.pop();
    Row.pop();
}
// ── Adaptive layout utilities (mirrors RokuricsAdaptiveLayout.swift) ──
export enum WidthCategory {
    COMPACT = "compact",
    REGULAR_PAD = "regularPad",
    WIDE_PAD = "widePad"
}
export function classifyWidth(width: number): WidthCategory {
    if (width < 600)
        return WidthCategory.COMPACT;
    if (width < 900)
        return WidthCategory.REGULAR_PAD;
    return WidthCategory.WIDE_PAD;
}
export function isPadWidth(width: number): boolean {
    return classifyWidth(width) !== WidthCategory.COMPACT;
}
export function horizontalPaddingForWidth(width: number): number {
    const cat = classifyWidth(width);
    if (cat === WidthCategory.COMPACT)
        return width < 360 ? 20 : 24;
    if (cat === WidthCategory.REGULAR_PAD)
        return 32;
    return 40;
}
export function homeMaxWidthForWidth(width: number): number {
    const cat = classifyWidth(width);
    if (cat === WidthCategory.COMPACT)
        return width;
    if (cat === WidthCategory.REGULAR_PAD)
        return 680;
    return 760;
}
