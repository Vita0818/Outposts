import { RokuricsColors } from "@bundle:com.vita0818.rokurics/entry/ets/utils/RokuricsTheme";
// ── Back arrow ──
export function BackIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Text.create('←');
        Text.fontSize(size);
        Text.fontWeight(600);
        Text.fontColor(color);
    }, Text);
    Text.pop();
}
// ── Close / X ──
export function CloseIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Text.create('✕');
        Text.fontSize(size * 0.88);
        Text.fontWeight(600);
        Text.fontColor(color);
    }, Text);
    Text.pop();
}
// ── Play triangle ──
export function PlayIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Polygon.create();
        Polygon.points([
            [0, 0],
            [size, size * 0.5],
            [0, size]
        ] as [
            number,
            number
        ][]);
        Polygon.fill(color);
        Polygon.width(size * 0.7);
        Polygon.height(size);
    }, Polygon);
}
// ── Pause (two bars) ──
export function PauseIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Row.create({ space: size * 0.18 });
        Row.width(size);
        Row.height(size);
        Row.justifyContent(FlexAlign.Center);
        Row.alignItems(VerticalAlign.Center);
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.22);
        Rect.height(size * 0.6);
        Rect.radius(size * 0.05);
        Rect.fill(color);
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.22);
        Rect.height(size * 0.6);
        Rect.radius(size * 0.05);
        Rect.fill(color);
    }, Rect);
    Row.pop();
}
// ── Books (two rectangles + spine) ──
export function BooksIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create({ alignContent: Alignment.Bottom });
        Stack.width(size);
        Stack.height(size);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.18);
        Rect.height(size * 0.48);
        Rect.radius(size * 0.03);
        Rect.fill(color);
        Rect.rotate({ angle: -8 });
        Rect.position({ x: size * 0.08, y: size * 0.05 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.18);
        Rect.height(size * 0.48);
        Rect.radius(size * 0.03);
        Rect.fill(color + 'CC');
        Rect.rotate({ angle: 8 });
        Rect.position({ x: size * 0.40, y: size * 0.05 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.52);
        Rect.height(size * 0.06);
        Rect.radius(size * 0.03);
        Rect.fill(color);
        Rect.position({ x: size * 0.06, y: size * 0.56 });
    }, Rect);
    Stack.pop();
}
// ── Chat bubble ──
export function ChatIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create({ alignContent: Alignment.TopStart });
        Stack.width(size * 0.6);
        Stack.height(size * 0.7);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.56);
        Rect.height(size * 0.42);
        Rect.radius(size * 0.10);
        Rect.fill(color);
        Rect.position({ x: 0, y: 0 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Polygon.create();
        Polygon.points([
            [size * 0.14, size * 0.42],
            [size * 0.24, size * 0.42],
            [size * 0.08, size * 0.62]
        ] as [
            number,
            number
        ][]);
        Polygon.fill(color);
    }, Polygon);
    Stack.pop();
}
// ── Trash can ──
export function TrashIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create({ alignContent: Alignment.Top });
        Stack.width(size * 0.65);
        Stack.height(size * 0.7);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Lid handle
        Rect.create();
        // Lid handle
        Rect.width(size * 0.2);
        // Lid handle
        Rect.height(size * 0.06);
        // Lid handle
        Rect.radius(size * 0.02);
        // Lid handle
        Rect.fill(color);
        // Lid handle
        Rect.position({ x: size * 0.24, y: 0 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Lid
        Rect.create();
        // Lid
        Rect.width(size * 0.52);
        // Lid
        Rect.height(size * 0.08);
        // Lid
        Rect.radius(size * 0.02);
        // Lid
        Rect.fill(color);
        // Lid
        Rect.position({ x: size * 0.08, y: size * 0.08 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Body (simple rounded rect)
        Rect.create();
        // Body (simple rounded rect)
        Rect.width(size * 0.44);
        // Body (simple rounded rect)
        Rect.height(size * 0.44);
        // Body (simple rounded rect)
        Rect.radius(size * 0.05);
        // Body (simple rounded rect)
        Rect.fill(color);
        // Body (simple rounded rect)
        Rect.position({ x: size * 0.12, y: size * 0.16 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Inner lines
        Rect.create();
        // Inner lines
        Rect.width(size * 0.03);
        // Inner lines
        Rect.height(size * 0.22);
        // Inner lines
        Rect.radius(size * 0.01);
        // Inner lines
        Rect.fill('#FFFFFF33');
        // Inner lines
        Rect.position({ x: size * 0.20, y: size * 0.26 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.03);
        Rect.height(size * 0.28);
        Rect.radius(size * 0.01);
        Rect.fill('#FFFFFF33');
        Rect.position({ x: size * 0.30, y: size * 0.22 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.03);
        Rect.height(size * 0.18);
        Rect.radius(size * 0.01);
        Rect.fill('#FFFFFF33');
        Rect.position({ x: size * 0.40, y: size * 0.28 });
    }, Rect);
    Stack.pop();
}
// ── Document badge ──
export function DocBadgeIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create();
        Stack.width(size);
        Stack.height(size);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.45);
        Rect.height(size * 0.6);
        Rect.radius(size * 0.05);
        Rect.fill(color);
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Fold corner
        Polygon.create();
        // Fold corner
        Polygon.points([
            [size * 0.32, 0],
            [size * 0.45, 0],
            [size * 0.45, size * 0.18],
            [size * 0.32, 0]
        ] as [
            number,
            number
        ][]);
        // Fold corner
        Polygon.fill('#FFFFFF40');
        // Fold corner
        Polygon.position({ x: 0, y: 0 });
    }, Polygon);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Lines
        Rect.create();
        // Lines
        Rect.width(size * 0.28);
        // Lines
        Rect.height(size * 0.04);
        // Lines
        Rect.radius(size * 0.02);
        // Lines
        Rect.fill('#FFFFFF60');
        // Lines
        Rect.position({ x: size * 0.08, y: size * 0.20 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.20);
        Rect.height(size * 0.04);
        Rect.radius(size * 0.02);
        Rect.fill('#FFFFFF40');
        Rect.position({ x: size * 0.08, y: size * 0.32 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.24);
        Rect.height(size * 0.04);
        Rect.radius(size * 0.02);
        Rect.fill('#FFFFFF40');
        Rect.position({ x: size * 0.08, y: size * 0.44 });
    }, Rect);
    Stack.pop();
}
// ── Note/Clipboard badge ──
export function NoteBadgeIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create();
        Stack.width(size);
        Stack.height(size * 0.75);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Clip
        Rect.create();
        // Clip
        Rect.width(size * 0.22);
        // Clip
        Rect.height(size * 0.10);
        // Clip
        Rect.radius(size * 0.03);
        // Clip
        Rect.fill('#FFFFFF50');
        // Clip
        Rect.position({ x: size * 0.16, y: 0 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Board
        Rect.create();
        // Board
        Rect.width(size * 0.45);
        // Board
        Rect.height(size * 0.6);
        // Board
        Rect.radius(size * 0.05);
        // Board
        Rect.fill(color);
        // Board
        Rect.position({ x: size * 0.05, y: size * 0.10 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Lines
        Rect.create();
        // Lines
        Rect.width(size * 0.24);
        // Lines
        Rect.height(size * 0.04);
        // Lines
        Rect.radius(size * 0.02);
        // Lines
        Rect.fill('#FFFFFF60');
        // Lines
        Rect.position({ x: size * 0.12, y: size * 0.22 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.18);
        Rect.height(size * 0.04);
        Rect.radius(size * 0.02);
        Rect.fill('#FFFFFF40');
        Rect.position({ x: size * 0.12, y: size * 0.34 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.22);
        Rect.height(size * 0.04);
        Rect.radius(size * 0.02);
        Rect.fill('#FFFFFF40');
        Rect.position({ x: size * 0.12, y: size * 0.46 });
    }, Rect);
    Stack.pop();
}
// ── Ellipsis (⋯) ──
export function EllipsisIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Row.create({ space: size * 0.28 });
        Row.width(size);
        Row.height(size * 0.18);
        Row.justifyContent(FlexAlign.Center);
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.width(size * 0.14);
        Circle.height(size * 0.14);
        Circle.fill(color);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.width(size * 0.14);
        Circle.height(size * 0.14);
        Circle.fill(color);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.width(size * 0.14);
        Circle.height(size * 0.14);
        Circle.fill(color);
    }, Circle);
    Row.pop();
}
// ── Settings gear ──
export function GearIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create();
        Stack.width(size);
        Stack.height(size);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.width(size * 0.6);
        Circle.height(size * 0.6);
        Circle.stroke(color);
        Circle.strokeWidth(size * 0.07);
        Circle.fill(Color.Transparent);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.width(size * 0.18);
        Circle.height(size * 0.18);
        Circle.fill(color);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Teeth
        Rect.create();
        // Teeth
        Rect.width(size * 0.07);
        // Teeth
        Rect.height(size * 0.18);
        // Teeth
        Rect.radius(size * 0.03);
        // Teeth
        Rect.fill(color);
        // Teeth
        Rect.position({ x: size * 0.46, y: size * 0.06 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.07);
        Rect.height(size * 0.18);
        Rect.radius(size * 0.03);
        Rect.fill(color);
        Rect.position({ x: size * 0.46, y: size * 0.76 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.18);
        Rect.height(size * 0.07);
        Rect.radius(size * 0.03);
        Rect.fill(color);
        Rect.position({ x: size * 0.06, y: size * 0.46 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.18);
        Rect.height(size * 0.07);
        Rect.radius(size * 0.03);
        Rect.fill(color);
        Rect.position({ x: size * 0.76, y: size * 0.46 });
    }, Rect);
    Stack.pop();
}
// ── Bullet list ──
export function BulletListIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Column.create({ space: size * 0.16 });
        Column.width(size * 0.8);
        Column.height(size * 0.4);
    }, Column);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Row.create({ space: size * 0.14 });
        Row.width(size * 0.74);
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.width(size * 0.14);
        Circle.height(size * 0.14);
        Circle.fill(color);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.56);
        Rect.height(size * 0.09);
        Rect.radius(size * 0.04);
        Rect.fill(color + '40');
    }, Rect);
    Row.pop();
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Row.create({ space: size * 0.14 });
        Row.width(size * 0.74);
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.width(size * 0.14);
        Circle.height(size * 0.14);
        Circle.fill(color);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.44);
        Rect.height(size * 0.09);
        Rect.radius(size * 0.04);
        Rect.fill(color + '40');
    }, Rect);
    Row.pop();
    Column.pop();
}
// ── Connection/radio waves ──
export function ConnectionIcon(size: number, color: string, active: boolean, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create();
        Stack.width(size);
        Stack.height(size);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.width(size * 0.7);
        Circle.height(size * 0.7);
        Circle.stroke(active ? color : RokuricsColors.tertiaryText);
        Circle.strokeWidth(size * 0.05);
        Circle.fill(Color.Transparent);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        If.create();
        if (active) {
            (parent ? parent : this).ifElseBranchUpdateFunction(0, () => {
                (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
                    Circle.create();
                    Circle.width(size * 0.28);
                    Circle.height(size * 0.28);
                    Circle.fill(color);
                }, Circle);
            });
        }
        else {
            this.ifElseBranchUpdateFunction(1, () => {
            });
        }
    }, If);
    If.pop();
    Stack.pop();
}
// ── Stop square ──
export function StopIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.5);
        Rect.height(size * 0.5);
        Rect.radius(size * 0.07);
        Rect.fill(color);
    }, Rect);
}
// ── Send arrow ──
export function SendIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Polygon.create();
        Polygon.points([
            [0, size * 0.4],
            [size * 0.65, size * 0.4],
            [size * 0.65, 0],
            [size, size * 0.5],
            [size * 0.65, size],
            [size * 0.65, size * 0.6],
            [0, size * 0.6]
        ] as [
            number,
            number
        ][]);
        Polygon.fill(color);
        Polygon.width(size);
        Polygon.height(size);
    }, Polygon);
}
// ── Cloud upload ──
export function CloudUploadIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create();
        Stack.width(size * 0.7);
        Stack.height(size * 0.7);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Cloud body
        Row.create({ space: 0 });
        // Cloud body
        Row.width(size * 0.6);
        // Cloud body
        Row.height(size * 0.28);
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.width(size * 0.2);
        Circle.height(size * 0.2);
        Circle.fill(color);
        Circle.offset({ y: size * -0.04 });
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.24);
        Rect.height(size * 0.16);
        Rect.fill(color);
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.width(size * 0.24);
        Circle.height(size * 0.24);
        Circle.fill(color);
        Circle.offset({ y: size * -0.02 });
    }, Circle);
    // Cloud body
    Row.pop();
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Upload arrow
        Polygon.create();
        // Upload arrow
        Polygon.points([
            [size * 0.22, size * 0.32],
            [size * 0.42, size * 0.06],
            [size * 0.62, size * 0.32]
        ] as [
            number,
            number
        ][]);
        // Upload arrow
        Polygon.fill(color);
        // Upload arrow
        Polygon.position({ x: 0, y: size * 0.04 });
    }, Polygon);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.width(size * 0.05);
        Rect.height(size * 0.28);
        Rect.radius(size * 0.02);
        Rect.fill(color);
        Rect.position({ x: size * 0.39, y: size * 0.08 });
    }, Rect);
    Stack.pop();
}
