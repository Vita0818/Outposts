/**
 * Custom drawn icons replacing emoji placeholders
 * Uses standalone @Builder functions for cross-page reuse
 */
import { colorAlpha,  RokuricsColors } from './RokuricsTheme'

// ── Back arrow (polygon-drawn, consistent across devices) ──
@Builder
export function BackIcon(size: number, color: string) {
  Stack() {
    Polygon()
      .points([
        [size * 0.85, size * 0.15],
        [size * 0.35, size * 0.46],
        [size * 0.85, size * 0.77]
      ])
      .fill(Color.Transparent)
      .stroke(color)
      .strokeWidth(size * 0.10)
      .strokeLineCap(LineCapStyle.Round)
      .strokeLineJoin(LineJoinStyle.Round)
      Rect()
        .width(size * 0.55)
        .height(size * 0.10)
        .radius(size * 0.05)
        .fill(color)
        .position({ x: size * 0.05, y: size * 0.47 })
        .translate({ x: 0, y: 0 })
  }
  .width(size)
  .height(size)
}

// ── Close / X ──
@Builder
export function CloseIcon(size: number, color: string) {
  Text('✕')
    .fontSize(size * 0.88)
    .fontWeight(600)
    .fontColor(color)
}

// ── Play triangle ──
@Builder
export function PlayIcon(size: number, color: string) {
  Polygon()
    .points([
      [0, 0],
      [size, size * 0.5],
      [0, size]
    ] as [number, number][])
    .fill(color)
    .width(size * 0.7)
    .height(size)
}

// ── Pause (two bars) ──
@Builder
export function PauseIcon(size: number, color: string) {
  Row({ space: size * 0.18 }) {
    Rect()
      .width(size * 0.22)
      .height(size * 0.6)
      .radius(size * 0.05)
      .fill(color)
    Rect()
      .width(size * 0.22)
      .height(size * 0.6)
      .radius(size * 0.05)
      .fill(color)
  }
  .width(size)
  .height(size)
  .justifyContent(FlexAlign.Center)
  .alignItems(VerticalAlign.Center)
}

// ── Books (two rectangles + spine) ──
@Builder
export function BooksIcon(size: number, color: string) {
  Stack({ alignContent: Alignment.Bottom }) {
    Rect()
      .width(size * 0.18)
      .height(size * 0.48)
      .radius(size * 0.03)
      .fill(color)
      .rotate({ angle: -8 })
      .position({ x: size * 0.08, y: size * 0.05 })
    Rect()
      .width(size * 0.18)
      .height(size * 0.48)
      .radius(size * 0.03)
      .fill(colorAlpha(color, 'CC'))
      .rotate({ angle: 8 })
      .position({ x: size * 0.40, y: size * 0.05 })
    Rect()
      .width(size * 0.52)
      .height(size * 0.06)
      .radius(size * 0.03)
      .fill(color)
      .position({ x: size * 0.06, y: size * 0.56 })
  }
  .width(size)
  .height(size)
}

// ── Chat bubble ──
@Builder
export function ChatIcon(size: number, color: string) {
  Stack({ alignContent: Alignment.TopStart }) {
    Rect()
      .width(size * 0.56)
      .height(size * 0.42)
      .radius(size * 0.10)
      .fill(color)
      .position({ x: 0, y: 0 })
    Polygon()
      .points([
        [size * 0.14, size * 0.42],
        [size * 0.24, size * 0.42],
        [size * 0.08, size * 0.62]
      ] as [number, number][])
      .fill(color)
  }
  .width(size * 0.6)
  .height(size * 0.7)
}

// ── Trash can ──
@Builder
export function TrashIcon(size: number, color: string) {
  Stack({ alignContent: Alignment.Top }) {
    // Lid handle
    Rect()
      .width(size * 0.2)
      .height(size * 0.06)
      .radius(size * 0.02)
      .fill(color)
      .position({ x: size * 0.24, y: 0 })

    // Lid
    Rect()
      .width(size * 0.52)
      .height(size * 0.08)
      .radius(size * 0.02)
      .fill(color)
      .position({ x: size * 0.08, y: size * 0.08 })

    // Body (simple rounded rect)
    Rect()
      .width(size * 0.44)
      .height(size * 0.44)
      .radius(size * 0.05)
      .fill(color)
      .position({ x: size * 0.12, y: size * 0.16 })

    // Inner lines
    Rect().width(size * 0.03).height(size * 0.22)
      .radius(size * 0.01).fill('#33FFFFFF')
      .position({ x: size * 0.20, y: size * 0.26 })
    Rect().width(size * 0.03).height(size * 0.28)
      .radius(size * 0.01).fill('#33FFFFFF')
      .position({ x: size * 0.30, y: size * 0.22 })
    Rect().width(size * 0.03).height(size * 0.18)
      .radius(size * 0.01).fill('#33FFFFFF')
      .position({ x: size * 0.40, y: size * 0.28 })
  }
  .width(size * 0.65)
  .height(size * 0.7)
}

// ── Document badge ──
@Builder
export function DocBadgeIcon(size: number, color: string) {
  Stack() {
    Rect()
      .width(size * 0.45)
      .height(size * 0.6)
      .radius(size * 0.05)
      .fill(color)
    // Fold corner
    Polygon()
      .points([
        [size * 0.32, 0],
        [size * 0.45, 0],
        [size * 0.45, size * 0.18],
        [size * 0.32, 0]
      ] as [number, number][])
      .fill('#40FFFFFF')
      .position({ x: 0, y: 0 })
    // Lines
    Rect().width(size * 0.28).height(size * 0.04)
      .radius(size * 0.02).fill('#60FFFFFF')
      .position({ x: size * 0.08, y: size * 0.20 })
    Rect().width(size * 0.20).height(size * 0.04)
      .radius(size * 0.02).fill('#40FFFFFF')
      .position({ x: size * 0.08, y: size * 0.32 })
    Rect().width(size * 0.24).height(size * 0.04)
      .radius(size * 0.02).fill('#40FFFFFF')
      .position({ x: size * 0.08, y: size * 0.44 })
  }
  .width(size)
  .height(size)
}

// ── Note/Clipboard badge ──
@Builder
export function NoteBadgeIcon(size: number, color: string) {
  Stack() {
    // Clip
    Rect()
      .width(size * 0.22)
      .height(size * 0.10)
      .radius(size * 0.03)
      .fill('#50FFFFFF')
      .position({ x: size * 0.16, y: 0 })
    // Board
    Rect()
      .width(size * 0.45)
      .height(size * 0.6)
      .radius(size * 0.05)
      .fill(color)
      .position({ x: size * 0.05, y: size * 0.10 })
    // Lines
    Rect().width(size * 0.24).height(size * 0.04)
      .radius(size * 0.02).fill('#60FFFFFF')
      .position({ x: size * 0.12, y: size * 0.22 })
    Rect().width(size * 0.18).height(size * 0.04)
      .radius(size * 0.02).fill('#40FFFFFF')
      .position({ x: size * 0.12, y: size * 0.34 })
    Rect().width(size * 0.22).height(size * 0.04)
      .radius(size * 0.02).fill('#40FFFFFF')
      .position({ x: size * 0.12, y: size * 0.46 })
  }
  .width(size)
  .height(size * 0.75)
}

// ── Ellipsis (⋯) ──
@Builder
export function EllipsisIcon(size: number, color: string) {
  Row({ space: size * 0.28 }) {
    Circle().width(size * 0.14).height(size * 0.14).fill(color)
    Circle().width(size * 0.14).height(size * 0.14).fill(color)
    Circle().width(size * 0.14).height(size * 0.14).fill(color)
  }
  .width(size)
  .height(size * 0.18)
  .justifyContent(FlexAlign.Center)
}

// ── Settings gear ──
@Builder
export function GearIcon(size: number, color: string) {
  Stack() {
    Circle()
      .width(size * 0.6)
      .height(size * 0.6)
      .stroke(color)
      .strokeWidth(size * 0.07)
      .fill(Color.Transparent)
    Circle()
      .width(size * 0.18)
      .height(size * 0.18)
      .fill(color)
    // Teeth
    Rect().width(size * 0.07).height(size * 0.18)
      .radius(size * 0.03).fill(color)
      .position({ x: size * 0.46, y: size * 0.06 })
    Rect().width(size * 0.07).height(size * 0.18)
      .radius(size * 0.03).fill(color)
      .position({ x: size * 0.46, y: size * 0.76 })
    Rect().width(size * 0.18).height(size * 0.07)
      .radius(size * 0.03).fill(color)
      .position({ x: size * 0.06, y: size * 0.46 })
    Rect().width(size * 0.18).height(size * 0.07)
      .radius(size * 0.03).fill(color)
      .position({ x: size * 0.76, y: size * 0.46 })
  }
  .width(size)
  .height(size)
}

// ── Bullet list ──
@Builder
export function BulletListIcon(size: number, color: string) {
  Column({ space: size * 0.16 }) {
    Row({ space: size * 0.14 }) {
      Circle().width(size * 0.14).height(size * 0.14).fill(color)
      Rect().width(size * 0.56).height(size * 0.09).radius(size * 0.04).fill(colorAlpha(color, '40'))
    }
    .width(size * 0.74)
    Row({ space: size * 0.14 }) {
      Circle().width(size * 0.14).height(size * 0.14).fill(color)
      Rect().width(size * 0.44).height(size * 0.09).radius(size * 0.04).fill(colorAlpha(color, '40'))
    }
    .width(size * 0.74)
  }
  .width(size * 0.8)
  .height(size * 0.4)
}

// ── Connection/radio waves ──
@Builder
export function ConnectionIcon(size: number, color: string, active: boolean) {
  Stack() {
    Circle()
      .width(size * 0.7)
      .height(size * 0.7)
      .stroke(active ? color : RokuricsColors.tertiaryText)
      .strokeWidth(size * 0.05)
      .fill(Color.Transparent)
    if (active) {
      Circle()
        .width(size * 0.28)
        .height(size * 0.28)
        .fill(color)
    }
  }
  .width(size)
  .height(size)
}

// ── Stop square ──
@Builder
export function StopIcon(size: number, color: string) {
  Rect()
    .width(size * 0.5)
    .height(size * 0.5)
    .radius(size * 0.07)
    .fill(color)
}

// ── Send arrow ──
@Builder
export function SendIcon(size: number, color: string) {
  Polygon()
    .points([
      [0, size * 0.4],
      [size * 0.65, size * 0.4],
      [size * 0.65, 0],
      [size, size * 0.5],
      [size * 0.65, size],
      [size * 0.65, size * 0.6],
      [0, size * 0.6]
    ] as [number, number][])
    .fill(color)
    .width(size)
    .height(size)
}

// ── Person / avatar silhouette ──
@Builder
export function PersonIcon(size: number, color: string) {
  Stack({ alignContent: Alignment.Top }) {
    // Head
    Circle()
      .width(size * 0.36)
      .height(size * 0.36)
      .fill(color)
      .position({ x: size * 0.32, y: 0 })
    // Body (rounded trapezoid approximated with shapes)
    Circle()
      .width(size * 0.7)
      .height(size * 0.7)
      .fill(color)
      .position({ x: size * 0.15, y: size * 0.34 })
  }
  .width(size)
  .height(size)
}

// ── Edit / pencil ──
@Builder
export function EditIcon(size: number, color: string) {
  Stack() {
    // Pencil body (diagonal line)
    Rect()
      .width(size * 0.08)
      .height(size * 0.7)
      .radius(size * 0.04)
      .fill(color)
      .rotate({ angle: -45 })
    // Pencil tip triangle
    Polygon()
      .points([
        [size * 0.05, size * 0.02],
        [size * 0.26, size * 0.56],
        [size * 0.06, size * 0.56]
      ] as [number, number][])
      .fill(color)
      .rotate({ angle: -45 })
      .position({ x: size * -0.04, y: size * -0.08 })
  }
  .width(size * 0.5)
  .height(size * 0.6)
}

// ── Cloud upload ──
@Builder
export function CloudUploadIcon(size: number, color: string) {
  Stack() {
    // Cloud body
    Row({ space: 0 }) {
      Circle().width(size * 0.2).height(size * 0.2).fill(color)
        .offset({ y: size * -0.04 })
      Rect().width(size * 0.24).height(size * 0.16).fill(color)
      Circle().width(size * 0.24).height(size * 0.24).fill(color)
        .offset({ y: size * -0.02 })
    }
    .width(size * 0.6)
    .height(size * 0.28)

    // Upload arrow
    Polygon()
      .points([
        [size * 0.22, size * 0.32],
        [size * 0.42, size * 0.06],
        [size * 0.62, size * 0.32]
      ] as [number, number][])
      .fill(color)
      .position({ x: 0, y: size * 0.04 })

    Rect()
      .width(size * 0.05)
      .height(size * 0.28)
      .radius(size * 0.02)
      .fill(color)
      .position({ x: size * 0.39, y: size * 0.08 })
  }
  .width(size * 0.7)
  .height(size * 0.7)
}
