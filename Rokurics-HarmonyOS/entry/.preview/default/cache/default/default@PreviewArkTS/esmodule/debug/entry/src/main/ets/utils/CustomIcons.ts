import { colorAlpha, RokuricsColors } from "@bundle:com.vita0818.rokurics/entry/ets/utils/RokuricsTheme";
// ── Back arrow ──
export function BackIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Text.create('←');
        Text.debugLine("entry/src/main/ets/utils/CustomIcons.ets(10:3)", "entry");
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
        Text.debugLine("entry/src/main/ets/utils/CustomIcons.ets(19:3)", "entry");
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
        Polygon.debugLine("entry/src/main/ets/utils/CustomIcons.ets(28:3)", "entry");
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
        Row.debugLine("entry/src/main/ets/utils/CustomIcons.ets(42:3)", "entry");
        Row.width(size);
        Row.height(size);
        Row.justifyContent(FlexAlign.Center);
        Row.alignItems(VerticalAlign.Center);
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(43:5)", "entry");
        Rect.width(size * 0.22);
        Rect.height(size * 0.6);
        Rect.radius(size * 0.05);
        Rect.fill(color);
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(48:5)", "entry");
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
        Stack.debugLine("entry/src/main/ets/utils/CustomIcons.ets(63:3)", "entry");
        Stack.width(size);
        Stack.height(size);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(64:5)", "entry");
        Rect.width(size * 0.18);
        Rect.height(size * 0.48);
        Rect.radius(size * 0.03);
        Rect.fill(color);
        Rect.rotate({ angle: -8 });
        Rect.position({ x: size * 0.08, y: size * 0.05 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(71:5)", "entry");
        Rect.width(size * 0.18);
        Rect.height(size * 0.48);
        Rect.radius(size * 0.03);
        Rect.fill(colorAlpha(color, 'CC'));
        Rect.rotate({ angle: 8 });
        Rect.position({ x: size * 0.40, y: size * 0.05 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(78:5)", "entry");
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
        Stack.debugLine("entry/src/main/ets/utils/CustomIcons.ets(92:3)", "entry");
        Stack.width(size * 0.6);
        Stack.height(size * 0.7);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(93:5)", "entry");
        Rect.width(size * 0.56);
        Rect.height(size * 0.42);
        Rect.radius(size * 0.10);
        Rect.fill(color);
        Rect.position({ x: 0, y: 0 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Polygon.create();
        Polygon.debugLine("entry/src/main/ets/utils/CustomIcons.ets(99:5)", "entry");
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
        Stack.debugLine("entry/src/main/ets/utils/CustomIcons.ets(114:3)", "entry");
        Stack.width(size * 0.65);
        Stack.height(size * 0.7);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Lid handle
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(116:5)", "entry");
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
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(124:5)", "entry");
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
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(132:5)", "entry");
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
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(140:5)", "entry");
        // Inner lines
        Rect.width(size * 0.03);
        // Inner lines
        Rect.height(size * 0.22);
        // Inner lines
        Rect.radius(size * 0.01);
        // Inner lines
        Rect.fill('#33FFFFFF');
        // Inner lines
        Rect.position({ x: size * 0.20, y: size * 0.26 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(143:5)", "entry");
        Rect.width(size * 0.03);
        Rect.height(size * 0.28);
        Rect.radius(size * 0.01);
        Rect.fill('#33FFFFFF');
        Rect.position({ x: size * 0.30, y: size * 0.22 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(146:5)", "entry");
        Rect.width(size * 0.03);
        Rect.height(size * 0.18);
        Rect.radius(size * 0.01);
        Rect.fill('#33FFFFFF');
        Rect.position({ x: size * 0.40, y: size * 0.28 });
    }, Rect);
    Stack.pop();
}
// ── Document badge ──
export function DocBadgeIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create();
        Stack.debugLine("entry/src/main/ets/utils/CustomIcons.ets(157:3)", "entry");
        Stack.width(size);
        Stack.height(size);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(158:5)", "entry");
        Rect.width(size * 0.45);
        Rect.height(size * 0.6);
        Rect.radius(size * 0.05);
        Rect.fill(color);
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Fold corner
        Polygon.create();
        Polygon.debugLine("entry/src/main/ets/utils/CustomIcons.ets(164:5)", "entry");
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
        Polygon.fill('#40FFFFFF');
        // Fold corner
        Polygon.position({ x: 0, y: 0 });
    }, Polygon);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Lines
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(174:5)", "entry");
        // Lines
        Rect.width(size * 0.28);
        // Lines
        Rect.height(size * 0.04);
        // Lines
        Rect.radius(size * 0.02);
        // Lines
        Rect.fill('#60FFFFFF');
        // Lines
        Rect.position({ x: size * 0.08, y: size * 0.20 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(177:5)", "entry");
        Rect.width(size * 0.20);
        Rect.height(size * 0.04);
        Rect.radius(size * 0.02);
        Rect.fill('#40FFFFFF');
        Rect.position({ x: size * 0.08, y: size * 0.32 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(180:5)", "entry");
        Rect.width(size * 0.24);
        Rect.height(size * 0.04);
        Rect.radius(size * 0.02);
        Rect.fill('#40FFFFFF');
        Rect.position({ x: size * 0.08, y: size * 0.44 });
    }, Rect);
    Stack.pop();
}
// ── Note/Clipboard badge ──
export function NoteBadgeIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create();
        Stack.debugLine("entry/src/main/ets/utils/CustomIcons.ets(191:3)", "entry");
        Stack.width(size);
        Stack.height(size * 0.75);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Clip
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(193:5)", "entry");
        // Clip
        Rect.width(size * 0.22);
        // Clip
        Rect.height(size * 0.10);
        // Clip
        Rect.radius(size * 0.03);
        // Clip
        Rect.fill('#50FFFFFF');
        // Clip
        Rect.position({ x: size * 0.16, y: 0 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Board
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(200:5)", "entry");
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
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(207:5)", "entry");
        // Lines
        Rect.width(size * 0.24);
        // Lines
        Rect.height(size * 0.04);
        // Lines
        Rect.radius(size * 0.02);
        // Lines
        Rect.fill('#60FFFFFF');
        // Lines
        Rect.position({ x: size * 0.12, y: size * 0.22 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(210:5)", "entry");
        Rect.width(size * 0.18);
        Rect.height(size * 0.04);
        Rect.radius(size * 0.02);
        Rect.fill('#40FFFFFF');
        Rect.position({ x: size * 0.12, y: size * 0.34 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(213:5)", "entry");
        Rect.width(size * 0.22);
        Rect.height(size * 0.04);
        Rect.radius(size * 0.02);
        Rect.fill('#40FFFFFF');
        Rect.position({ x: size * 0.12, y: size * 0.46 });
    }, Rect);
    Stack.pop();
}
// ── Ellipsis (⋯) ──
export function EllipsisIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Row.create({ space: size * 0.28 });
        Row.debugLine("entry/src/main/ets/utils/CustomIcons.ets(224:3)", "entry");
        Row.width(size);
        Row.height(size * 0.18);
        Row.justifyContent(FlexAlign.Center);
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(225:5)", "entry");
        Circle.width(size * 0.14);
        Circle.height(size * 0.14);
        Circle.fill(color);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(226:5)", "entry");
        Circle.width(size * 0.14);
        Circle.height(size * 0.14);
        Circle.fill(color);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(227:5)", "entry");
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
        Stack.debugLine("entry/src/main/ets/utils/CustomIcons.ets(237:3)", "entry");
        Stack.width(size);
        Stack.height(size);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(238:5)", "entry");
        Circle.width(size * 0.6);
        Circle.height(size * 0.6);
        Circle.stroke(color);
        Circle.strokeWidth(size * 0.07);
        Circle.fill(Color.Transparent);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(244:5)", "entry");
        Circle.width(size * 0.18);
        Circle.height(size * 0.18);
        Circle.fill(color);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Teeth
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(249:5)", "entry");
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
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(252:5)", "entry");
        Rect.width(size * 0.07);
        Rect.height(size * 0.18);
        Rect.radius(size * 0.03);
        Rect.fill(color);
        Rect.position({ x: size * 0.46, y: size * 0.76 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(255:5)", "entry");
        Rect.width(size * 0.18);
        Rect.height(size * 0.07);
        Rect.radius(size * 0.03);
        Rect.fill(color);
        Rect.position({ x: size * 0.06, y: size * 0.46 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(258:5)", "entry");
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
        Column.debugLine("entry/src/main/ets/utils/CustomIcons.ets(269:3)", "entry");
        Column.width(size * 0.8);
        Column.height(size * 0.4);
    }, Column);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Row.create({ space: size * 0.14 });
        Row.debugLine("entry/src/main/ets/utils/CustomIcons.ets(270:5)", "entry");
        Row.width(size * 0.74);
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(271:7)", "entry");
        Circle.width(size * 0.14);
        Circle.height(size * 0.14);
        Circle.fill(color);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(272:7)", "entry");
        Rect.width(size * 0.56);
        Rect.height(size * 0.09);
        Rect.radius(size * 0.04);
        Rect.fill(colorAlpha(color, '40'));
    }, Rect);
    Row.pop();
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Row.create({ space: size * 0.14 });
        Row.debugLine("entry/src/main/ets/utils/CustomIcons.ets(275:5)", "entry");
        Row.width(size * 0.74);
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(276:7)", "entry");
        Circle.width(size * 0.14);
        Circle.height(size * 0.14);
        Circle.fill(color);
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(277:7)", "entry");
        Rect.width(size * 0.44);
        Rect.height(size * 0.09);
        Rect.radius(size * 0.04);
        Rect.fill(colorAlpha(color, '40'));
    }, Rect);
    Row.pop();
    Column.pop();
}
// ── Connection/radio waves ──
export function ConnectionIcon(size: number, color: string, active: boolean, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create();
        Stack.debugLine("entry/src/main/ets/utils/CustomIcons.ets(288:3)", "entry");
        Stack.width(size);
        Stack.height(size);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(289:5)", "entry");
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
                    Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(296:7)", "entry");
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
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(309:3)", "entry");
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
        Polygon.debugLine("entry/src/main/ets/utils/CustomIcons.ets(319:3)", "entry");
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
// ── Person / avatar silhouette ──
export function PersonIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create({ alignContent: Alignment.Top });
        Stack.debugLine("entry/src/main/ets/utils/CustomIcons.ets(337:3)", "entry");
        Stack.width(size);
        Stack.height(size);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Head
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(339:5)", "entry");
        // Head
        Circle.width(size * 0.36);
        // Head
        Circle.height(size * 0.36);
        // Head
        Circle.fill(color);
        // Head
        Circle.position({ x: size * 0.32, y: 0 });
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Body (rounded trapezoid approximated with shapes)
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(345:5)", "entry");
        // Body (rounded trapezoid approximated with shapes)
        Circle.width(size * 0.7);
        // Body (rounded trapezoid approximated with shapes)
        Circle.height(size * 0.7);
        // Body (rounded trapezoid approximated with shapes)
        Circle.fill(color);
        // Body (rounded trapezoid approximated with shapes)
        Circle.position({ x: size * 0.15, y: size * 0.34 });
    }, Circle);
    Stack.pop();
}
// ── Edit / pencil ──
export function EditIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create();
        Stack.debugLine("entry/src/main/ets/utils/CustomIcons.ets(358:3)", "entry");
        Stack.width(size * 0.5);
        Stack.height(size * 0.6);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Pencil body (diagonal line)
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(360:5)", "entry");
        // Pencil body (diagonal line)
        Rect.width(size * 0.08);
        // Pencil body (diagonal line)
        Rect.height(size * 0.7);
        // Pencil body (diagonal line)
        Rect.radius(size * 0.04);
        // Pencil body (diagonal line)
        Rect.fill(color);
        // Pencil body (diagonal line)
        Rect.rotate({ angle: -45 });
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Pencil tip triangle
        Polygon.create();
        Polygon.debugLine("entry/src/main/ets/utils/CustomIcons.ets(367:5)", "entry");
        // Pencil tip triangle
        Polygon.points([
            [size * 0.05, size * 0.02],
            [size * 0.26, size * 0.56],
            [size * 0.06, size * 0.56]
        ] as [
            number,
            number
        ][]);
        // Pencil tip triangle
        Polygon.fill(color);
        // Pencil tip triangle
        Polygon.rotate({ angle: -45 });
        // Pencil tip triangle
        Polygon.position({ x: size * -0.04, y: size * -0.08 });
    }, Polygon);
    Stack.pop();
}
// ── Cloud upload ──
export function CloudUploadIcon(size: number, color: string, parent = null) {
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Stack.create();
        Stack.debugLine("entry/src/main/ets/utils/CustomIcons.ets(384:3)", "entry");
        Stack.width(size * 0.7);
        Stack.height(size * 0.7);
    }, Stack);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        // Cloud body
        Row.create({ space: 0 });
        Row.debugLine("entry/src/main/ets/utils/CustomIcons.ets(386:5)", "entry");
        // Cloud body
        Row.width(size * 0.6);
        // Cloud body
        Row.height(size * 0.28);
    }, Row);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(387:7)", "entry");
        Circle.width(size * 0.2);
        Circle.height(size * 0.2);
        Circle.fill(color);
        Circle.offset({ y: size * -0.04 });
    }, Circle);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Rect.create();
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(389:7)", "entry");
        Rect.width(size * 0.24);
        Rect.height(size * 0.16);
        Rect.fill(color);
    }, Rect);
    (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
        Circle.create();
        Circle.debugLine("entry/src/main/ets/utils/CustomIcons.ets(390:7)", "entry");
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
        Polygon.debugLine("entry/src/main/ets/utils/CustomIcons.ets(397:5)", "entry");
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
        Rect.debugLine("entry/src/main/ets/utils/CustomIcons.ets(406:5)", "entry");
        Rect.width(size * 0.05);
        Rect.height(size * 0.28);
        Rect.radius(size * 0.02);
        Rect.fill(color);
        Rect.position({ x: size * 0.39, y: size * 0.08 });
    }, Rect);
    Stack.pop();
}
