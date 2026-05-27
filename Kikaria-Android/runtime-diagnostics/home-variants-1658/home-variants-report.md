# Home Layout Candidates — 1658

Three distinct HomeScreen layout strategies, all in one codebase.
Switch via `ActiveHomeLayoutCandidate` in HomeScreen.kt.

## A — IOS_CENTERED

**Design goal**: Faithful to iOS compact Home structure.
Single scrollable content group, vertically centered. No zone splitting.

**Key parameters**:
- Layout: Box(contentAlignment=Center) + verticalScroll
- Bubble: 272dp container, 190dp center, 70sp arrow (iOS exact)
- Cards: iOS baseline (20dp/25dp/0.42 progress, 28dp/0.40 dashboard)
- Spacing: 32dp header→bubble, 30dp bubble→cards, 12dp card gap

**Inherits from**:
- 1655: Centered content group approach
- iOS source: Exact bubble + card dimensions

**Avoids from**:
- 1654/1656: TopFraction zone splitting
- 1655: Overly compressed cards (uses iOS baseline weight)

**Screenshot**: `A_IOS_CENTERED.png`

---

## B — HERO_FORWARD

**Design goal**: Bubble as dominant visual hero, lighter cards, upper layout.
Preserves 1654's best directions done more carefully.

**Key parameters**:
- Layout: BoxWithConstraints + topFraction 0.08
- Bubble: 210dp container, 182dp center, 66sp arrow
- Cards: Light (16dp/22dp/0.38 progress, 22dp/0.36 dashboard)
- Spacing: 34dp header→bubble, 28dp bubble→cards, 10dp card gap

**Inherits from**:
- 1654: Hero-forward direction, lighter cards, viewport-aware layout
- 1656: Reduced topFraction from 0.10, slightly less aggressive bubble

**Avoids from**:
- 1654: 0.10 topFraction (too贴顶)
- 1655: Mechanical iOS centering

**Screenshot**: `B_HERO_FORWARD.png` (note: 33KB — may be blank, suggest re-screenshot)

---

## C — BALANCED (DEFAULT)

**Design goal**: Synthesis between A and B. Header not贴顶, not low.
Bubble as visual centre, cards with balanced weight.

**Key parameters**:
- Layout: BoxWithConstraints + topFraction 0.06
- Bubble: 205dp container, 178dp center, 64sp arrow
- Cards: Balanced (18dp/24dp/0.40 progress, 24dp/0.38 dashboard)
- Spacing: 34dp header→bubble, 30dp bubble→cards, 10dp card gap

**Inherits from**:
- 1656/1657: Balanced synthesis approach
- 1654: Viewport-aware layout
- 1655: Reasonable card weight awareness

**Avoids from**:
- 1654: Too aggressive (topFraction 0.10, too-light cards)
- 1655: Mechanical iOS centering, too-heavy cards
- 1656/1657: TopFraction 0.07 still贴顶 on this device

**Screenshot**: `C_BALANCED.png`

---

## How to choose

1. Look at the three screenshots side by side.
2. Decide which overall page feel you prefer.
3. If you prefer one but want to borrow from another, name the target and the borrowed parameter.
4. Do NOT accept any single candidate as final without visual confirmation.

## How to regenerate a candidate

```kotlin
// In HomeScreen.kt, change:
private val ActiveHomeLayoutCandidate = HomeLayoutCandidate.A_IOS_CENTERED  // or B or C
// Then rebuild, install, screenshot.
```
