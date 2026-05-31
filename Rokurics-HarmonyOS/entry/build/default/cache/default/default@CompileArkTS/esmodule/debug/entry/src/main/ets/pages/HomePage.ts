if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface HomePage_Params {
    recordingManager?: RecordingManager;
    state?: RecordingState;
    isActive?: boolean;
    breathePhase?: number;
    orbScale?: number;
    headerScale?: number;
    isMacPaired?: boolean;
    connectionStatus?: string;
}
import display from "@ohos:display";
import { getSharedRecordingManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/RecordingManager";
import type { RecordingManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/RecordingManager";
import { RecordingState } from "@bundle:com.vita0818.rokurics/entry/ets/models/RecordingModels";
import { formatClock } from "@bundle:com.vita0818.rokurics/entry/ets/utils/FormatHelpers";
import { colorAlpha, RokuricsColors, FontWeight, glassFillOpacity, glassStrokeHighOpacity, glassStrokeMidOpacity } from "@bundle:com.vita0818.rokurics/entry/ets/utils/RokuricsTheme";
import { PersonIcon, BooksIcon, ChatIcon, ConnectionIcon } from "@bundle:com.vita0818.rokurics/entry/ets/utils/CustomIcons";
import { hapticLight } from "@bundle:com.vita0818.rokurics/entry/ets/utils/HapticFeedback";
class HomePage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.recordingManager = getSharedRecordingManager(getContext(this));
        this.__state = new ObservedPropertySimplePU(RecordingState.IDLE, this, "state");
        this.__isActive = new ObservedPropertySimplePU(false, this, "isActive");
        this.__breathePhase = new ObservedPropertySimplePU(0, this, "breathePhase");
        this.__orbScale = new ObservedPropertySimplePU(0.84, this, "orbScale");
        this.__headerScale = new ObservedPropertySimplePU(1, this, "headerScale");
        this.__isMacPaired = new ObservedPropertySimplePU(false, this, "isMacPaired");
        this.__connectionStatus = new ObservedPropertySimplePU('offline', this, "connectionStatus");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: HomePage_Params) {
        if (params.recordingManager !== undefined) {
            this.recordingManager = params.recordingManager;
        }
        if (params.state !== undefined) {
            this.state = params.state;
        }
        if (params.isActive !== undefined) {
            this.isActive = params.isActive;
        }
        if (params.breathePhase !== undefined) {
            this.breathePhase = params.breathePhase;
        }
        if (params.orbScale !== undefined) {
            this.orbScale = params.orbScale;
        }
        if (params.headerScale !== undefined) {
            this.headerScale = params.headerScale;
        }
        if (params.isMacPaired !== undefined) {
            this.isMacPaired = params.isMacPaired;
        }
        if (params.connectionStatus !== undefined) {
            this.connectionStatus = params.connectionStatus;
        }
    }
    updateStateVars(params: HomePage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__state.purgeDependencyOnElmtId(rmElmtId);
        this.__isActive.purgeDependencyOnElmtId(rmElmtId);
        this.__breathePhase.purgeDependencyOnElmtId(rmElmtId);
        this.__orbScale.purgeDependencyOnElmtId(rmElmtId);
        this.__headerScale.purgeDependencyOnElmtId(rmElmtId);
        this.__isMacPaired.purgeDependencyOnElmtId(rmElmtId);
        this.__connectionStatus.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__state.aboutToBeDeleted();
        this.__isActive.aboutToBeDeleted();
        this.__breathePhase.aboutToBeDeleted();
        this.__orbScale.aboutToBeDeleted();
        this.__headerScale.aboutToBeDeleted();
        this.__isMacPaired.aboutToBeDeleted();
        this.__connectionStatus.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private recordingManager: RecordingManager;
    private __state: ObservedPropertySimplePU<RecordingState>;
    get state() {
        return this.__state.get();
    }
    set state(newValue: RecordingState) {
        this.__state.set(newValue);
    }
    private __isActive: ObservedPropertySimplePU<boolean>;
    get isActive() {
        return this.__isActive.get();
    }
    set isActive(newValue: boolean) {
        this.__isActive.set(newValue);
    }
    private __breathePhase: ObservedPropertySimplePU<number>;
    get breathePhase() {
        return this.__breathePhase.get();
    }
    set breathePhase(newValue: number) {
        this.__breathePhase.set(newValue);
    }
    private __orbScale: ObservedPropertySimplePU<number>;
    get orbScale() {
        return this.__orbScale.get();
    }
    set orbScale(newValue: number) {
        this.__orbScale.set(newValue);
    }
    private __headerScale: ObservedPropertySimplePU<number>;
    get headerScale() {
        return this.__headerScale.get();
    }
    set headerScale(newValue: number) {
        this.__headerScale.set(newValue);
    }
    private __isMacPaired: ObservedPropertySimplePU<boolean>;
    get isMacPaired() {
        return this.__isMacPaired.get();
    }
    set isMacPaired(newValue: boolean) {
        this.__isMacPaired.set(newValue);
    }
    private __connectionStatus: ObservedPropertySimplePU<string>;
    get connectionStatus() {
        return this.__connectionStatus.get();
    }
    set connectionStatus(newValue: string) {
        this.__connectionStatus.set(newValue);
    }
    aboutToAppear(): void {
        this.recordingManager.onStateChange((s: RecordingState) => {
            this.state = s;
            this.isActive = this.isActiveState(s);
        });
        this.state = this.recordingManager.state;
        this.isActive = this.isActiveState(this.state);
        this.recordingManager.reloadRecordings();
        this.calibrateScale();
        this.startBreathing();
    }
    private calibrateScale(): void {
        const w = display.getDefaultDisplaySync().width;
        const h = display.getDefaultDisplaySync().height;
        if (w < 360 || h < 760) {
            this.orbScale = 0.78;
            this.headerScale = 0.92;
        }
        else if (h < 820) {
            this.orbScale = 0.88;
            this.headerScale = 0.96;
        }
        else {
            this.orbScale = 1;
            this.headerScale = 1;
        }
    }
    private startBreathing(): void {
        const update = () => {
            const t = Date.now() / 1000;
            this.breathePhase = Math.sin(t * Math.PI / 2.4) * 0.5 + 0.5;
            setTimeout(update, 50);
        };
        update();
    }
    private isActiveState(s: RecordingState): boolean {
        return s === RecordingState.REQUESTING_PERMISSION ||
            s === RecordingState.CONFIGURING_SESSION ||
            s === RecordingState.RECORDING ||
            s === RecordingState.PAUSED ||
            s === RecordingState.STOPPING ||
            s === RecordingState.SAVING;
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Stack.create();
            Stack.width('100%');
            Stack.height('100%');
            Stack.linearGradient({
                direction: GradientDirection.RightBottom,
                colors: [
                    [RokuricsColors.pageGradientStart, 1.0],
                    [RokuricsColors.pageGradientMid, 1.0],
                    [RokuricsColors.pageGradientEnd, 1.0]
                ]
            });
        }, Stack);
        // Ambient background bubbles
        this.AmbientBackground.bind(this)();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.height('100%');
            Column.backgroundColor(Color.Transparent);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Header
            Row.create();
            // Header
            Row.width('100%');
            // Header
            Row.padding({
                left: 24 * this.headerScale,
                right: 24 * this.headerScale,
                top: 18 * this.headerScale + 42
            });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('Rokurics');
            Text.fontSize(39 * this.headerScale);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(RokuricsColors.deepText);
            Text.fontFamily('serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Profile avatar button (glass circle)
            Button.createWithChild();
            // Profile avatar button (glass circle)
            Button.width(46 * this.headerScale);
            // Profile avatar button (glass circle)
            Button.height(46 * this.headerScale);
            // Profile avatar button (glass circle)
            Button.borderRadius(23 * this.headerScale);
            // Profile avatar button (glass circle)
            Button.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '5C'));
            // Profile avatar button (glass circle)
            Button.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '14'),
                radius: 12,
                offsetY: 6
            });
            // Profile avatar button (glass circle)
            Button.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, 0.28],
                        [RokuricsColors.glassStroke, 0.12],
                        [RokuricsColors.aqua, 0.24]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 23 * this.headerScale
            } as BorderOptions);
            // Profile avatar button (glass circle)
            Button.onClick(() => {
                this.getUIContext().getRouter().pushUrl({ url: 'pages/SettingsPage' });
            });
        }, Button);
        PersonIcon.bind(this)(20 * this.headerScale, RokuricsColors.aqua);
        // Profile avatar button (glass circle)
        Button.pop();
        // Header
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Recording Orb
            Button.createWithChild();
            // Recording Orb
            Button.width(272 * this.orbScale);
            // Recording Orb
            Button.height(286 * this.orbScale);
            // Recording Orb
            Button.backgroundColor(Color.Transparent);
            // Recording Orb
            Button.onClick(() => {
                hapticLight();
                this.getUIContext().getRouter().pushUrl({ url: 'pages/RecordingSessionPage' });
            });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Stack.create();
            Stack.width(272 * this.orbScale);
            Stack.height(286 * this.orbScale);
            Stack.scale({
                x: (this.isActive ? 1.018 : 0.992) + this.breathePhase * 0.008,
                y: (this.isActive ? 1.018 : 0.992) + this.breathePhase * 0.008
            });
        }, Stack);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Orbiting ambient satellites (animated)
            Stack.create();
            // Orbiting ambient satellites (animated)
            Stack.width(272 * this.orbScale);
            // Orbiting ambient satellites (animated)
            Stack.height(286 * this.orbScale);
        }, Stack);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Satellite 1: mint → paleAqua, opacity 0.42
            Circle.create();
            // Satellite 1: mint → paleAqua, opacity 0.42
            Circle.width(88 * this.orbScale);
            // Satellite 1: mint → paleAqua, opacity 0.42
            Circle.height(88 * this.orbScale);
            // Satellite 1: mint → paleAqua, opacity 0.42
            Circle.linearGradient({
                direction: GradientDirection.RightBottom,
                colors: [
                    [RokuricsColors.mint, 0.42],
                    [RokuricsColors.paleAqua, 0.42]
                ]
            });
            // Satellite 1: mint → paleAqua, opacity 0.42
            Circle.stroke('#FFFFFF');
            // Satellite 1: mint → paleAqua, opacity 0.42
            Circle.strokeWidth(1);
            // Satellite 1: mint → paleAqua, opacity 0.42
            Circle.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '08'),
                radius: 14,
                offsetY: 8
            });
            // Satellite 1: mint → paleAqua, opacity 0.42
            Circle.position({
                x: (94 - this.breathePhase * 6) * this.orbScale + '%',
                y: (28 + this.breathePhase * 4) * this.orbScale + '%'
            });
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Satellite 2: skyCyan → mistGreen, opacity 0.32
            Circle.create();
            // Satellite 2: skyCyan → mistGreen, opacity 0.32
            Circle.width(76 * this.orbScale);
            // Satellite 2: skyCyan → mistGreen, opacity 0.32
            Circle.height(76 * this.orbScale);
            // Satellite 2: skyCyan → mistGreen, opacity 0.32
            Circle.linearGradient({
                direction: GradientDirection.RightBottom,
                colors: [
                    [RokuricsColors.skyCyan, 0.32],
                    [RokuricsColors.mistGreen, 0.32]
                ]
            });
            // Satellite 2: skyCyan → mistGreen, opacity 0.32
            Circle.stroke('#FFFFFF');
            // Satellite 2: skyCyan → mistGreen, opacity 0.32
            Circle.strokeWidth(1);
            // Satellite 2: skyCyan → mistGreen, opacity 0.32
            Circle.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '08'),
                radius: 14,
                offsetY: 8
            });
            // Satellite 2: skyCyan → mistGreen, opacity 0.32
            Circle.position({
                x: (60 + this.breathePhase * 5) * this.orbScale + '%',
                y: (32 - this.breathePhase * 3) * this.orbScale + '%'
            });
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Satellite 3: aqua → paleAqua, opacity 0.30
            Circle.create();
            // Satellite 3: aqua → paleAqua, opacity 0.30
            Circle.width(74 * this.orbScale);
            // Satellite 3: aqua → paleAqua, opacity 0.30
            Circle.height(74 * this.orbScale);
            // Satellite 3: aqua → paleAqua, opacity 0.30
            Circle.linearGradient({
                direction: GradientDirection.RightBottom,
                colors: [
                    [RokuricsColors.aqua, 0.30],
                    [RokuricsColors.paleAqua, 0.30]
                ]
            });
            // Satellite 3: aqua → paleAqua, opacity 0.30
            Circle.stroke('#FFFFFF');
            // Satellite 3: aqua → paleAqua, opacity 0.30
            Circle.strokeWidth(1);
            // Satellite 3: aqua → paleAqua, opacity 0.30
            Circle.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '08'),
                radius: 14,
                offsetY: 8
            });
            // Satellite 3: aqua → paleAqua, opacity 0.30
            Circle.position({
                x: (50 - this.breathePhase * 4) * this.orbScale + '%',
                y: (68 + this.breathePhase * 3) * this.orbScale + '%'
            });
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Satellite 4: mistGreen → mint, opacity 0.34
            Circle.create();
            // Satellite 4: mistGreen → mint, opacity 0.34
            Circle.width(68 * this.orbScale);
            // Satellite 4: mistGreen → mint, opacity 0.34
            Circle.height(68 * this.orbScale);
            // Satellite 4: mistGreen → mint, opacity 0.34
            Circle.linearGradient({
                direction: GradientDirection.RightBottom,
                colors: [
                    [RokuricsColors.mistGreen, 0.34],
                    [RokuricsColors.mint, 0.34]
                ]
            });
            // Satellite 4: mistGreen → mint, opacity 0.34
            Circle.stroke('#FFFFFF');
            // Satellite 4: mistGreen → mint, opacity 0.34
            Circle.strokeWidth(1);
            // Satellite 4: mistGreen → mint, opacity 0.34
            Circle.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '08'),
                radius: 14,
                offsetY: 8
            });
            // Satellite 4: mistGreen → mint, opacity 0.34
            Circle.position({
                x: (28 + this.breathePhase * 5) * this.orbScale + '%',
                y: (62 - this.breathePhase * 3) * this.orbScale + '%'
            });
        }, Circle);
        // Orbiting ambient satellites (animated)
        Stack.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Sound ripple (animated breathing)
            Circle.create();
            // Sound ripple (animated breathing)
            Circle.width(238 * this.orbScale);
            // Sound ripple (animated breathing)
            Circle.height(238 * this.orbScale);
            // Sound ripple (animated breathing)
            Circle.stroke(colorAlpha(RokuricsColors.aqua, '08'));
            // Sound ripple (animated breathing)
            Circle.strokeWidth(1.4);
            // Sound ripple (animated breathing)
            Circle.fill(Color.Transparent);
            // Sound ripple (animated breathing)
            Circle.opacity(0.07 + this.breathePhase * 0.05);
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Circle.create();
            Circle.width(202 * this.orbScale);
            Circle.height(202 * this.orbScale);
            Circle.stroke(colorAlpha(RokuricsColors.aqua, '10'));
            Circle.strokeWidth(1.4);
            Circle.fill(Color.Transparent);
            Circle.opacity(0.10 + this.breathePhase * 0.05);
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Recording red ripple
            if (this.state === RecordingState.RECORDING) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Circle.create();
                        Circle.width(222 * this.orbScale);
                        Circle.height(222 * this.orbScale);
                        Circle.stroke(colorAlpha(RokuricsColors.coral, '20'));
                        Circle.strokeWidth(1.4);
                        Circle.fill(Color.Transparent);
                        Circle.opacity(0.16 + this.breathePhase * 0.08);
                    }, Circle);
                });
            }
            else if (this.state === RecordingState.PAUSED) {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Circle.create();
                        Circle.width(222 * this.orbScale);
                        Circle.height(222 * this.orbScale);
                        Circle.stroke(colorAlpha(RokuricsColors.softTeal, '15'));
                        Circle.strokeWidth(1.4);
                        Circle.fill(Color.Transparent);
                        Circle.opacity(0.11 + this.breathePhase * 0.06);
                    }, Circle);
                });
            }
            // Main orb with gradient fill
            else {
                this.ifElseBranchUpdateFunction(2, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Main orb with gradient fill
            Circle.create();
            // Main orb with gradient fill
            Circle.width(190 * this.orbScale);
            // Main orb with gradient fill
            Circle.height(190 * this.orbScale);
            // Main orb with gradient fill
            Circle.linearGradient({
                direction: GradientDirection.RightBottom,
                colors: [
                    [RokuricsColors.actionGradientStart, 1.0],
                    [RokuricsColors.actionGradientEnd, 1.0]
                ]
            });
            // Main orb with gradient fill
            Circle.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '24'),
                radius: 30 * this.orbScale,
                offsetY: 18 * this.orbScale
            });
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Center content
            if (this.isActive) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.getCenterText());
                        Text.fontSize(34 * this.orbScale);
                        Text.fontWeight(FontWeight.Bold);
                        Text.fontColor('#FFFFFF');
                        Text.fontFamily('serif');
                        Text.opacity(this.isPausedBlinking() ? (0.35 + this.breathePhase * 0.65) : 0.97);
                    }, Text);
                    Text.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Stack.create();
                        Stack.opacity(0.97);
                    }, Stack);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Rect.create({ width: 56 * this.orbScale, height: 8 * this.orbScale });
                        Rect.radius(4);
                        Rect.fill(Color.White);
                    }, Rect);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Rect.create({ width: 8 * this.orbScale, height: 56 * this.orbScale });
                        Rect.radius(4);
                        Rect.fill(Color.White);
                    }, Rect);
                    Stack.pop();
                });
            }
        }, If);
        If.pop();
        Stack.pop();
        // Recording Orb
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Navigation Card with glass styling
            Row.create();
            // Navigation Card with glass styling
            Row.width('88%');
            // Navigation Card with glass styling
            Row.height(104);
            // Navigation Card with glass styling
            Row.borderRadius(30);
            // Navigation Card with glass styling
            Row.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'));
            // Navigation Card with glass styling
            Row.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, 0.24],
                        [RokuricsColors.glassStroke, 0.14],
                        [RokuricsColors.glassStrokeAccent, 0.24]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 30
            } as BorderOptions);
            // Navigation Card with glass styling
            Row.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '12'),
                radius: 20,
                offsetY: 11
            });
            // Navigation Card with glass styling
            Row.margin({ bottom: 16 * this.headerScale });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Study Library
            Button.createWithChild();
            // Study Library
            Button.layoutWeight(1);
            // Study Library
            Button.height(104);
            // Study Library
            Button.backgroundColor(Color.Transparent);
            // Study Library
            Button.onClick(() => {
                this.getUIContext().getRouter().pushUrl({ url: 'pages/RecordingLibraryPage' });
            });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
            Column.width('100%');
            Column.height(104);
            Column.justifyContent(FlexAlign.Center);
        }, Column);
        BooksIcon.bind(this)(27, RokuricsColors.aqua);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('学习库');
            Text.fontSize(13);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        Column.pop();
        // Study Library
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Divider
            Rect.create();
            // Divider
            Rect.width(1);
            // Divider
            Rect.height(54);
            // Divider
            Rect.fill(colorAlpha(RokuricsColors.softText, '14'));
        }, Rect);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // AI Chat
            Button.createWithChild();
            // AI Chat
            Button.layoutWeight(1);
            // AI Chat
            Button.height(104);
            // AI Chat
            Button.backgroundColor(Color.Transparent);
            // AI Chat
            Button.onClick(() => {
                this.getUIContext().getRouter().pushUrl({ url: 'pages/AIChatPage' });
            });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
            Column.width('100%');
            Column.height(104);
            Column.justifyContent(FlexAlign.Center);
        }, Column);
        ChatIcon.bind(this)(27, RokuricsColors.mint);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('AI 对话');
            Text.fontSize(13);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        Column.pop();
        // AI Chat
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Divider
            Rect.create();
            // Divider
            Rect.width(1);
            // Divider
            Rect.height(54);
            // Divider
            Rect.fill(colorAlpha(RokuricsColors.softText, '14'));
        }, Rect);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Mac Connection
            Button.createWithChild();
            // Mac Connection
            Button.layoutWeight(1);
            // Mac Connection
            Button.height(104);
            // Mac Connection
            Button.backgroundColor(Color.Transparent);
            // Mac Connection
            Button.onClick(() => {
                this.getUIContext().getRouter().pushUrl({ url: 'pages/MacConnectionPage' });
            });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
            Column.width('100%');
            Column.height(104);
            Column.justifyContent(FlexAlign.Center);
        }, Column);
        ConnectionIcon.bind(this)(27, RokuricsColors.softTeal, this.isMacPaired);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('Mac 连接');
            Text.fontSize(13);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        Column.pop();
        // Mac Connection
        Button.pop();
        // Navigation Card with glass styling
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Device connection preview card (compact)
            if (this.isMacPaired) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create();
                        Column.margin({ bottom: 34 * this.headerScale });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create({ space: 8 });
                        Row.padding({ left: 14, right: 14, top: 8, bottom: 8 });
                        Row.borderRadius(14);
                        Row.backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity));
                        Row.border({
                            width: 1,
                            color: {
                                colors: [
                                    [0xFFFFFF, glassStrokeHighOpacity],
                                    [RokuricsColors.glassStroke, glassStrokeMidOpacity]
                                ],
                                direction: GradientDirection.RightBottom
                            },
                            radius: 14
                        } as BorderOptions);
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Circle.create();
                        Circle.width(8);
                        Circle.height(8);
                        Circle.fill(RokuricsColors.mint);
                    }, Circle);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('Mac 已连接 · 8787');
                        Text.fontSize(12);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    Row.pop();
                    Column.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Blank.create();
                        Blank.height(18 * this.headerScale);
                    }, Blank);
                    Blank.pop();
                });
            }
        }, If);
        If.pop();
        Column.pop();
        Stack.pop();
    }
    AmbientBackground(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Stack.create();
            Stack.width('100%');
            Stack.height('100%');
        }, Stack);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Bubble 1: paleAqua → mint, opacity 0.30
            Circle.create();
            // Bubble 1: paleAqua → mint, opacity 0.30
            Circle.width(150);
            // Bubble 1: paleAqua → mint, opacity 0.30
            Circle.height(150);
            // Bubble 1: paleAqua → mint, opacity 0.30
            Circle.linearGradient({
                direction: GradientDirection.RightBottom,
                colors: [
                    [RokuricsColors.paleAqua, 0.30],
                    [RokuricsColors.mint, 0.30]
                ]
            });
            // Bubble 1: paleAqua → mint, opacity 0.30
            Circle.stroke('#FFFFFF');
            // Bubble 1: paleAqua → mint, opacity 0.30
            Circle.strokeWidth(1);
            // Bubble 1: paleAqua → mint, opacity 0.30
            Circle.opacity(0.30);
            // Bubble 1: paleAqua → mint, opacity 0.30
            Circle.position({ x: -30, y: '-10%' });
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Bubble 2: skyCyan → mistGreen, opacity 0.22
            Circle.create();
            // Bubble 2: skyCyan → mistGreen, opacity 0.22
            Circle.width(190);
            // Bubble 2: skyCyan → mistGreen, opacity 0.22
            Circle.height(190);
            // Bubble 2: skyCyan → mistGreen, opacity 0.22
            Circle.linearGradient({
                direction: GradientDirection.RightBottom,
                colors: [
                    [RokuricsColors.skyCyan, 0.22],
                    [RokuricsColors.mistGreen, 0.22]
                ]
            });
            // Bubble 2: skyCyan → mistGreen, opacity 0.22
            Circle.stroke('#FFFFFF');
            // Bubble 2: skyCyan → mistGreen, opacity 0.22
            Circle.strokeWidth(1);
            // Bubble 2: skyCyan → mistGreen, opacity 0.22
            Circle.opacity(0.22);
            // Bubble 2: skyCyan → mistGreen, opacity 0.22
            Circle.position({ x: '85%', y: '15%' });
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Bubble 3: mint → aqua, opacity 0.18
            Circle.create();
            // Bubble 3: mint → aqua, opacity 0.18
            Circle.width(170);
            // Bubble 3: mint → aqua, opacity 0.18
            Circle.height(170);
            // Bubble 3: mint → aqua, opacity 0.18
            Circle.linearGradient({
                direction: GradientDirection.RightBottom,
                colors: [
                    [RokuricsColors.mint, 0.18],
                    [RokuricsColors.aqua, 0.18]
                ]
            });
            // Bubble 3: mint → aqua, opacity 0.18
            Circle.stroke('#FFFFFF');
            // Bubble 3: mint → aqua, opacity 0.18
            Circle.strokeWidth(1);
            // Bubble 3: mint → aqua, opacity 0.18
            Circle.opacity(0.18);
            // Bubble 3: mint → aqua, opacity 0.18
            Circle.position({ x: '75%', y: '70%' });
        }, Circle);
        Stack.pop();
    }
    private getCenterText(): string {
        switch (this.state) {
            case RecordingState.RECORDING:
            case RecordingState.PAUSED:
                return formatClock(this.recordingManager.elapsedSeconds);
            default:
                return '...';
        }
    }
    private isPausedBlinking(): boolean {
        return this.state === RecordingState.PAUSED;
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "HomePage";
    }
}
registerNamedRoute(() => new HomePage(undefined, {}), "", { bundleName: "com.vita0818.rokurics", moduleName: "entry", pagePath: "pages/HomePage", pageFullPath: "entry/src/main/ets/pages/HomePage", integratedHsp: "false", moduleType: "followWithHap" });
