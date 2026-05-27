# IOS_HOME_SOURCE_CONTRACT — Kikaria iPhone Compact Home

## 1. Structure (ContentView.swift lines 1585–1649)

Single ScrollView containing ONE VStack(spacing: 0):

```
ScrollView(.vertical, showsIndicators: false)
  VStack(spacing: 0)           ← ALL content in one group
    HStack: Kikaria + Avatar   ← .padding(.top, 14)
    Spacer(minLength: 32)
    StartReviewButton          ← Hero
    Spacer(minLength: 30)
    VStack(spacing: 12)
      TodayOverviewHomeProgressButton
      HomeDashboardGridCard
    .padding(.bottom, 12)
  .padding(.horizontal, metrics.horizontalPadding)
  .frame(maxWidth: metrics.homeMaxWidth)
  .frame(maxWidth: .infinity, minHeight: metrics.height, alignment: .center)
```

## 2. Critical centering mechanism

`.frame(maxWidth: .infinity, minHeight: metrics.height, alignment: .center)`

The VStack has minHeight = screen height. `alignment: .center` centers the content
vertically. SwiftUI `Spacer(minLength:)` expands to fill available space when the
VStack is taller than its intrinsic content. This means the spacers between Header,
Bubble, and Cards ALL expand proportionally — the entire content group is centered
in the viewport.

When content exceeds viewport height, ScrollView activates and Spacer(minLength:)
collapses to its minimum.

## 3. Component roles

| Component | iOS role | iOS baseline dimensions (compact phone, scale=1) |
|-----------|----------|---------------------------------------------------|
| Header (Kikaria + Avatar) | Content flow, NOT fixed bar | 39pt title, 44pt avatar, padding(.top, 14) |
| StartReviewButton | VISUAL HERO | Center circle 190pt, frame 272×260, arrow 70pt |
| Spacer(minLength: 32) | Between Header and Bubble | 32pt MIN, expands with centering |
| Spacer(minLength: 30) | Between Bubble and Cards | 30pt MIN, expands with centering |
| TodayOverviewHomeProgressButton | SECONDARY info | padding 20×20, corner 25, fillOpacity 0.42, date 23pt, days 13pt, progress 25pt |
| HomeDashboardGridCard | TERTIARY info | corner 28, fillOpacity 0.40, metric minHeight 82, name 16pt, "当前预设" 12pt |
| Bottom padding | Breathing room | 12pt |

## 4. What iOS does NOT have

- NO topFraction / heroBand / infoBand zone partitioning
- NO Header fixed to screen top
- NO Bubble forced into upper third of screen
- NO cards excessively compressed
- NO BoxWithConstraints-based manual space allocation

## 5. Why iOS layout looks natural

The centering mechanism naturally distributes extra space so the Bubble is at or
near the visual center. On taller phones, spacers expand and the Bubble stays at
a comfortable eye-level position. On shorter phones, spacers shrink to minimum
and scrolling takes over. No manual calculations needed.

## 6. Scale factors (compact phone)

- homeScale = 1
- headerScale = 1
- cardScale = 1 (via isPadPortrait check)
- homeMaxWidth = .infinity
- horizontalPadding = 24 (width >= 360) or 20 (width < 360)
