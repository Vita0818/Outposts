# Adaptive Layout Report — Kikaria Android Phone Metrics Migration

## 1. Current Window Dimensions

| Configuration | Resolution | Density | dp Width | dp Height | horizontalPadding |
|---|---|---|---|---|---|
| phone-default | 1080×2400 | 420 (2.625x) | 411dp | 914dp | 24dp |
| phone-alt | 1080×1920 | 420 (2.625x) | 411dp | 731dp | 24dp |

## 2. Computed Metrics (phone-default)

```
horizontalPadding       = 24dp   (width >= 360dp)
homeMaxWidth            = Unspecified (compact → .infinity in iOS)
reviewMaxWidth          = Unspecified
formMaxWidth            = Unspecified
homeScale               = 1.0
headerScale             = 1.0
reviewScale             = 1.0
reviewButtonScale       = 1.0
cardScale               = 1.0
reviewActionBottomPadding = 16dp
backButtonSize          = 42dp
backButtonTopPadding    = 12dp
titleTopPadding         = 18dp
pageTopPadding          = 24dp
isCompactPhone          = true
isTallPhone             = true
```

## 3. Key Component Coordinates (phone-default)

| Component | Bounds | Notes |
|---|---|---|
| "Kikaria" title | [63,493][459,614] | 24dp from left, 14dp from safe top; 39sp × headerScale |
| Profile avatar | [896,491][1022,617] | 44dp × headerScale, right-aligned |
| Start bubble | [314,732][766,1184] | 198dp container, centered; 32dp gap from header |
| Progress card | [63,1297][1017,1548] | 30dp below start bubble; 12dp card spacing |
| Dashboard card | [63,1580][1017,1981] | Three metric columns + preset row |
| Back button (overlay) | [55,156][181,282] | 24dp left, 12dp top; 42dp size; z-index overlay |

## 4. Why Coordinates Come From Metrics (Not Hardcoded)

- **horizontalPadding (24dp)**: Computed from `widthDp >= 360`, matching iOS `horizontalPadding(for: width)` line 484-493. Used by all pages via `metrics.horizontalPadding`.
- **header top padding (14dp)**: iOS baseline value from ContentView.swift line 1606 (`padding(.top, 14)`). Not a magic number.
- **Spacer 32dp / 30dp**: iOS baseline values from ContentView.swift lines 1608, 1621 (`Spacer(minLength: 32)`, `Spacer(minLength: 30)`).
- **cardSpacing (12dp)**: iOS baseline from line 1623 (`VStack(spacing: 12)`).
- **backButtonSize (42dp)**: iOS constant from KikariaAdaptiveLayout.swift line 267.
- **reviewActionBottomPadding (16dp)**: iOS compact branch value from line 452.
- **pageTopPadding (24dp)**: iOS `pageTitleTopPadding(defaultValue: 24)` for compact phones.
- **newPresetTextEditorMinHeight (260dp)**: iOS compact value from line 280.

## 5. Layout Changes Between Default and Alt Window

The layout uses `verticalScroll` + `Box(contentAlignment = Center)` on Home, which means:
- At 1080×2400 (tall): Content centers vertically with appropriate whitespace.
- At 1080×1920 (shorter): Content still fits; less vertical whitespace but all elements visible.
- Scroll pages use overlay back button (z-index above content), not content-flow spacers.
- No 56dp/66dp magic spacer workarounds remain.

## 6. Issues Checked

- [x] No overlapping elements
- [x] No truncated content
- [x] No excessive whitespace
- [x] All buttons clickable
- [x] Back button accessible on all sub-pages
- [x] Scroll works on overflow content
- [x] Both window sizes display correctly

## 7. Remaining Manual Review Items

The following require human aesthetic judgment:
1. The exact vertical balance of Home screen content (centered vs slightly top-biased).
2. Review screen action button sizing at edge screen heights.
3. Scope tag grid column count at different widths.
4. Card corner radius consistency across pages.
5. Dark mode glass effect parity with iOS.
