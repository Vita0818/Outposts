# Home Layout Diagnosis

## Screenshot Info
- Device: emulator-5554 (Pixel 8, 1080x2400)
- Before screenshot: home-before.png
- Before UI hierarchy: home-before.xml

## Android Hierarchy Key Nodes (from home-before.xml)

| Node | Bounds | Notes |
|------|--------|-------|
| Kikaria title | [63,464][459,585] | Top-left header, 39sp |
| Profile avatar (K) | [896,462][1022,588] | Top-right, 44dp |
| Start bubble (→) | [287,705][794,1212] | 190dp circle in 220dp box, centered horizontally |
| Progress card | [63,1326][1017,1577] | "May 24th", "-- Days Left", "0/20" |
| Scope card | [63,1609][379,1860] | "范围" 2 |
| Reinforcement card | [382,1609][698,1860] | "重点集锦" 0 |
| Mastered card | [701,1609][1017,1860] | "已掌握" 0 |
| Preset row | [63,1863][1017,2010] | "大学英语 Band 4" 当前预设 |
| ScrollView | [0,427][1080,2042] | Wraps all content |
| Page background | [0,0][1080,2400] | Full screen |

Screen: 1080x2400, visible content area ~1615px (427 to 2042)

## Current Layout Problems

1. **Box(Center) wrapping scrollable Column is wrong**: The Box with contentAlignment=Center tries to center the scrollable Column as a whole, but since the Column fills most of the screen, this has no visible effect. It's not equivalent to iOS `.frame(minHeight: metrics.height, alignment: .center)`.

2. **Content starts at y=464 with large gap above**: The ScrollView starts at y=427, but the first content (Kikaria title) is at y=464. There's 37px of empty space inside the scroll area before any content, plus the system status bar area (y=0..427).

3. **Vertical distribution issues**: The spacing between elements (header→bubble 32dp, bubble→progress 30dp, progress→dashboard 12dp, bottom 12dp) matches iOS, but the overall vertical centering is missing. Content sits at the top of the screen rather than being vertically centered when shorter than screen.

4. **Content doesn't opt into maxWidth**: iOS uses `.frame(maxWidth: metrics.homeMaxWidth)` (720pt for iPhone) to constrain content width on wide screens. Android uses full width with 24dp horizontal padding, which is close enough for this screen width (1080 - 48 = 1032).

## iOS Home Structure (ContentView.swift:1585-1648)

File: /Users/vita/Vitemis/Vela/Kikaria/Kikaria/ContentView.swift
Lines: 1585-1648 (iPhone compact-width branch, inside `else` of `if metrics.homeUsesTwoColumnLayout ... else if metrics.isPadPortrait ... else`)

Layout tree:
```
ScrollView(.vertical)                                       // line 1580
  └── VStack(spacing: 0)                                    // line 1586
        ├── HStack { Text("Kikaria") + Spacer + Avatar }    // line 1587-1605
        │   .padding(.top, 14)                              // line 1606
        ├── Spacer(minLength: 32)                           // line 1608
        ├── StartReviewButton                               // line 1610-1619
        ├── Spacer(minLength: 30)                           // line 1621
        └── VStack(spacing: 12)                             // line 1623
              ├── TodayOverviewHomeProgressButton           // line 1624-1633
              └── HomeDashboardGridCard                     // line 1635-1643
            .padding(.bottom, 12)                           // line 1644
      .padding(.horizontal, metrics.horizontalPadding)      // line 1646
      .frame(maxWidth: metrics.homeMaxWidth)                // line 1647
      .frame(maxWidth: .infinity, minHeight: metrics.height, alignment: .center)  // line 1648
```

Key layout pattern (line 1648):
```swift
.frame(maxWidth: .infinity, minHeight: metrics.height, alignment: .center)
```
This sets the VStack's min height to the screen height and centers content within it.

Excluded iPad/Mac branches: `homeLandscapeContent` (line 1378), `padPortraitHomeContent` (line 1303).

## Fix Plan

1. Remove the outer `Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center)` wrapper.
2. Use `BoxWithConstraints` to read available height.
3. Apply `.defaultMinSize(minHeight = maxHeight)` to the scrollable Column so it's at least screen-height tall.
4. Set `verticalArrangement = Arrangement.Center` on the Column to vertically center content when shorter than screen.
5. Keep all existing spacers, header, bubble, cards as-is.
6. This directly replicates iOS pattern: ScrollView + minHeight + center alignment.

Why this helps:
- When content is shorter than screen (typical case), items are vertically centered.
- When content overflows (many cards), the scroll works naturally.
- No change to horizontal layout, spacers, or individual components.
