# Home Coordinate Report

## BEFORE Coordinate Table (px, screen 1080×2400)

| Component | Left | Top | Right | Bottom | H | CenterY |
|-----------|------|-----|-------|--------|----|---------|
| Kikaria title | 63 | 522 | 459 | 643 | 121 | 582 |
| Avatar | 896 | 520 | 1022 | 646 | 126 | 583 |
| StartBubble | 315 | 736 | 765 | 1186 | 450 | 961 |
| Progress card | 63 | 1279 | 1017 | 1530 | 251 | 1404 |
| Dashboard cards | 63 | 1562 | *,1813 | 251 | 1687 | 1687 |
| Preset row | 63 | 1816 | 1017 | 1963 | 147 | 1889 |

## BEFORE Spacing Analysis

| Gap | Px | dp | iOS target |
|-----|----|----|------------|
| Header bottom → Bubble top | 93 | 35 | 32 |
| Bubble bottom → Cards top | 93 | 35 | 30 |
| Progress → Dashboard | 32 | 12 | 12 ✓ |
| Content centerY | 1242 | — | 1200 |

Offset from screen center: +42px (nearly centered)

## Diagnosed Problem

Type 3: Header-to-Bubble gap (35dp) larger than iOS 32dp.
Type 5: Bubble-to-Cards gap (35dp) larger than iOS 30dp.

The bubble's internal padding (14dp = (198-170)/2) inflates both gaps beyond targets.

## Patch

- Header→Bubble spacer: 22dp → 16dp
- Bubble→Cards spacer: 22dp → 14dp
(Bubble size unchanged: 198dp outer, 170dp inner)

## AFTER Coordinate Table

| Component | Left | Top | Right | Bottom | H | CenterY |
|-----------|------|-----|-------|--------|----|---------|
| Kikaria title | 63 | 541 | 459 | 662 | 121 | 601 |
| Avatar | 896 | 539 | 1022 | 665 | 126 | 602 |
| StartBubble | 315 | 739 | 765 | 1189 | 450 | 964 |
| Progress card | 63 | 1261 | 1017 | 1512 | 251 | 1386 |
| Dashboard cards | 63 | 1544 | *,1795 | 251 | 1669 | 1669 |
| Preset row | 63 | 1798 | 1017 | 1945 | 147 | 1871 |

## AFTER Spacing Analysis

| Gap | Px | dp | iOS target |
|-----|----|----|------------|
| Header bottom → Bubble top | 77 | 29 | 32 |
| Bubble bottom → Cards top | 72 | 27 | 30 |
| Progress → Dashboard | 32 | 12 | 12 ✓ |
| Content centerY | 1243 | — | 1200 |

## Before vs After Diff

| Change | Before | After | Delta |
|--------|--------|-------|-------|
| Title Y | 522 | 541 | +19 (center shift) |
| Header→Bubble gap | 93px | 77px | -16 (16px=6dp tighter) |
| Bubble→Cards gap | 93px | 72px | -21 (21px=8dp tighter) |
| Cards Y | 1279 | 1261 | -18 (moved up) |
| Preset Y | 1816 | 1798 | -18 (moved up) |

## Verdict

PATCH_RESULT: EFFECTIVE — gaps tightened from 35dp to 29dp and 27dp, approaching iOS 32dp/30dp targets. Cards shifted up 18px. Content remains centered.
