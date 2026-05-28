if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface RecordingSessionPage_Params {
    recordingManager?: RecordingManager;
    state?: RecordingState;
    elapsedSeconds?: number;
    statusText?: string;
    suggestedTitle?: string;
    pendingTitle?: string;
    filingType?: string;
    filingSubject?: string;
    filingChapter?: string;
    filingTopic?: string;
    showFilingPicker?: boolean;
    isPausedBlinking?: boolean;
    pulsePhase?: number;
}
import { getSharedRecordingManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/RecordingManager";
import type { RecordingManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/RecordingManager";
import { RecordingState, StudyFilingPath } from "@bundle:com.vita0818.rokurics/entry/ets/models/RecordingModels";
import { formatClock, formatDuration } from "@bundle:com.vita0818.rokurics/entry/ets/utils/FormatHelpers";
import { colorAlpha, RokuricsColors, FontWeight } from "@bundle:com.vita0818.rokurics/entry/ets/utils/RokuricsTheme";
import { PlayIcon, PauseIcon } from "@bundle:com.vita0818.rokurics/entry/ets/utils/CustomIcons";
const FILING_OPTIONS: Record<string, string[]> = {
    'type': ['课堂录音', '自学笔记', '会议记录', '访谈采访', '灵感记录', '其他'],
    'subject': ['数学', '物理', '化学', '生物', '计算机', '英语', '历史', '文学', '哲学', '艺术'],
    'chapter': [],
    'topic': []
};
class RecordingSessionPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.recordingManager = getSharedRecordingManager();
        this.__state = new ObservedPropertySimplePU(RecordingState.IDLE, this, "state");
        this.__elapsedSeconds = new ObservedPropertySimplePU(0, this, "elapsedSeconds");
        this.__statusText = new ObservedPropertySimplePU('准备中', this, "statusText");
        this.__suggestedTitle = new ObservedPropertySimplePU('', this, "suggestedTitle");
        this.__pendingTitle = new ObservedPropertySimplePU('', this, "pendingTitle");
        this.__filingType = new ObservedPropertySimplePU('', this, "filingType");
        this.__filingSubject = new ObservedPropertySimplePU('', this, "filingSubject");
        this.__filingChapter = new ObservedPropertySimplePU('', this, "filingChapter");
        this.__filingTopic = new ObservedPropertySimplePU('', this, "filingTopic");
        this.__showFilingPicker = new ObservedPropertySimplePU(false, this, "showFilingPicker");
        this.__isPausedBlinking = new ObservedPropertySimplePU(false, this, "isPausedBlinking");
        this.__pulsePhase = new ObservedPropertySimplePU(0, this, "pulsePhase");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: RecordingSessionPage_Params) {
        if (params.recordingManager !== undefined) {
            this.recordingManager = params.recordingManager;
        }
        if (params.state !== undefined) {
            this.state = params.state;
        }
        if (params.elapsedSeconds !== undefined) {
            this.elapsedSeconds = params.elapsedSeconds;
        }
        if (params.statusText !== undefined) {
            this.statusText = params.statusText;
        }
        if (params.suggestedTitle !== undefined) {
            this.suggestedTitle = params.suggestedTitle;
        }
        if (params.pendingTitle !== undefined) {
            this.pendingTitle = params.pendingTitle;
        }
        if (params.filingType !== undefined) {
            this.filingType = params.filingType;
        }
        if (params.filingSubject !== undefined) {
            this.filingSubject = params.filingSubject;
        }
        if (params.filingChapter !== undefined) {
            this.filingChapter = params.filingChapter;
        }
        if (params.filingTopic !== undefined) {
            this.filingTopic = params.filingTopic;
        }
        if (params.showFilingPicker !== undefined) {
            this.showFilingPicker = params.showFilingPicker;
        }
        if (params.isPausedBlinking !== undefined) {
            this.isPausedBlinking = params.isPausedBlinking;
        }
        if (params.pulsePhase !== undefined) {
            this.pulsePhase = params.pulsePhase;
        }
    }
    updateStateVars(params: RecordingSessionPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__state.purgeDependencyOnElmtId(rmElmtId);
        this.__elapsedSeconds.purgeDependencyOnElmtId(rmElmtId);
        this.__statusText.purgeDependencyOnElmtId(rmElmtId);
        this.__suggestedTitle.purgeDependencyOnElmtId(rmElmtId);
        this.__pendingTitle.purgeDependencyOnElmtId(rmElmtId);
        this.__filingType.purgeDependencyOnElmtId(rmElmtId);
        this.__filingSubject.purgeDependencyOnElmtId(rmElmtId);
        this.__filingChapter.purgeDependencyOnElmtId(rmElmtId);
        this.__filingTopic.purgeDependencyOnElmtId(rmElmtId);
        this.__showFilingPicker.purgeDependencyOnElmtId(rmElmtId);
        this.__isPausedBlinking.purgeDependencyOnElmtId(rmElmtId);
        this.__pulsePhase.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__state.aboutToBeDeleted();
        this.__elapsedSeconds.aboutToBeDeleted();
        this.__statusText.aboutToBeDeleted();
        this.__suggestedTitle.aboutToBeDeleted();
        this.__pendingTitle.aboutToBeDeleted();
        this.__filingType.aboutToBeDeleted();
        this.__filingSubject.aboutToBeDeleted();
        this.__filingChapter.aboutToBeDeleted();
        this.__filingTopic.aboutToBeDeleted();
        this.__showFilingPicker.aboutToBeDeleted();
        this.__isPausedBlinking.aboutToBeDeleted();
        this.__pulsePhase.aboutToBeDeleted();
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
    private __elapsedSeconds: ObservedPropertySimplePU<number>;
    get elapsedSeconds() {
        return this.__elapsedSeconds.get();
    }
    set elapsedSeconds(newValue: number) {
        this.__elapsedSeconds.set(newValue);
    }
    private __statusText: ObservedPropertySimplePU<string>;
    get statusText() {
        return this.__statusText.get();
    }
    set statusText(newValue: string) {
        this.__statusText.set(newValue);
    }
    private __suggestedTitle: ObservedPropertySimplePU<string>;
    get suggestedTitle() {
        return this.__suggestedTitle.get();
    }
    set suggestedTitle(newValue: string) {
        this.__suggestedTitle.set(newValue);
    }
    private __pendingTitle: ObservedPropertySimplePU<string>;
    get pendingTitle() {
        return this.__pendingTitle.get();
    }
    set pendingTitle(newValue: string) {
        this.__pendingTitle.set(newValue);
    }
    private __filingType: ObservedPropertySimplePU<string>;
    get filingType() {
        return this.__filingType.get();
    }
    set filingType(newValue: string) {
        this.__filingType.set(newValue);
    }
    private __filingSubject: ObservedPropertySimplePU<string>;
    get filingSubject() {
        return this.__filingSubject.get();
    }
    set filingSubject(newValue: string) {
        this.__filingSubject.set(newValue);
    }
    private __filingChapter: ObservedPropertySimplePU<string>;
    get filingChapter() {
        return this.__filingChapter.get();
    }
    set filingChapter(newValue: string) {
        this.__filingChapter.set(newValue);
    }
    private __filingTopic: ObservedPropertySimplePU<string>;
    get filingTopic() {
        return this.__filingTopic.get();
    }
    set filingTopic(newValue: string) {
        this.__filingTopic.set(newValue);
    }
    private __showFilingPicker: ObservedPropertySimplePU<boolean>;
    get showFilingPicker() {
        return this.__showFilingPicker.get();
    }
    set showFilingPicker(newValue: boolean) {
        this.__showFilingPicker.set(newValue);
    }
    private __isPausedBlinking: ObservedPropertySimplePU<boolean>;
    get isPausedBlinking() {
        return this.__isPausedBlinking.get();
    }
    set isPausedBlinking(newValue: boolean) {
        this.__isPausedBlinking.set(newValue);
    }
    private __pulsePhase: ObservedPropertySimplePU<number>;
    get pulsePhase() {
        return this.__pulsePhase.get();
    }
    set pulsePhase(newValue: number) {
        this.__pulsePhase.set(newValue);
    }
    aboutToAppear(): void {
        this.recordingManager.onStateChange((s: RecordingState) => {
            this.state = s;
            this.elapsedSeconds = this.recordingManager.elapsedSeconds;
            this.statusText = this.recordingManager.statusMessage;
            if (s === RecordingState.PAUSED) {
                this.isPausedBlinking = true;
            }
            else {
                this.isPausedBlinking = false;
            }
        });
        this.state = this.recordingManager.state;
        this.elapsedSeconds = this.recordingManager.elapsedSeconds;
        this.statusText = this.recordingManager.statusMessage;
        this.suggestedTitle = this.recordingManager.suggestedTitle;
        this.pendingTitle = this.recordingManager.pendingTitle ?? '';
        // Auto-start recording if idle
        if (this.state === RecordingState.IDLE) {
            this.recordingManager.startRecording();
        }
        // Simple pulse for recording indicator
        const updatePulse = () => {
            const t = Date.now() / 1000;
            this.pulsePhase = Math.sin(t * Math.PI) * 0.5 + 0.5;
            if (this.state === RecordingState.RECORDING || this.state === RecordingState.PAUSED) {
                setTimeout(updatePulse, 50);
            }
        };
        updatePulse();
    }
    private getFilingPath(): StudyFilingPath {
        return new StudyFilingPath(this.filingType || undefined, this.filingSubject || undefined, this.filingChapter || undefined, this.filingTopic || undefined);
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Stack.create();
            Stack.width('100%');
            Stack.height('100%');
            Stack.backgroundColor(RokuricsColors.pageBackground);
        }, Stack);
        // Ambient background
        this.SessionBackground.bind(this)();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.height('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Header
            Row.create();
            // Header
            Row.width('100%');
            // Header
            Row.padding({ left: 16, right: 16, top: 56 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Glass circle back button
            Button.createWithChild();
            // Glass circle back button
            Button.width(44);
            // Glass circle back button
            Button.height(44);
            // Glass circle back button
            Button.borderRadius(22);
            // Glass circle back button
            Button.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'));
            // Glass circle back button
            Button.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '10'),
                radius: 12,
                offsetY: 6
            });
            // Glass circle back button
            Button.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, 0.44],
                        [0xEFFAF8, 0.14],
                        [0x59C7C2, 0.12]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 22
            } as BorderOptions);
            // Glass circle back button
            Button.onClick(() => this.getUIContext().getRouter().back());
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('←');
            Text.fontSize(18);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        // Glass circle back button
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Stop / Save button
            if (this.state === RecordingState.RECORDING || this.state === RecordingState.PAUSED) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 20, right: 20, top: 10, bottom: 10 });
                        Button.borderRadius(20);
                        Button.backgroundColor(RokuricsColors.coral);
                        Button.shadow({
                            color: colorAlpha(RokuricsColors.coral, '30'),
                            radius: 12,
                            offsetY: 4
                        });
                        Button.onClick(() => {
                            this.recordingManager.stopRecording();
                        });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('停止');
                        Text.fontSize(16);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(Color.White);
                    }, Text);
                    Text.pop();
                    Button.pop();
                });
            }
            else if (this.state === RecordingState.FILING) {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 20, right: 20, top: 10, bottom: 10 });
                        Button.borderRadius(20);
                        Button.backgroundColor(RokuricsColors.aqua);
                        Button.shadow({
                            color: colorAlpha(RokuricsColors.aqua, '30'),
                            radius: 12,
                            offsetY: 4
                        });
                        Button.onClick(() => {
                            const filing = this.getFilingPath();
                            this.recordingManager.finalizeRecording(this.pendingTitle || this.suggestedTitle, filing.isEmpty ? null : filing);
                            this.getUIContext().getRouter().back();
                        });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('保存');
                        Text.fontSize(16);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(Color.White);
                    }, Text);
                    Text.pop();
                    Button.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(2, () => {
                });
            }
        }, If);
        If.pop();
        // Header
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Timer card with glass styling (mirrors Apple's liquid glass card)
            Column.create({ space: 16 });
            // Timer card with glass styling (mirrors Apple's liquid glass card)
            Column.width('85%');
            // Timer card with glass styling (mirrors Apple's liquid glass card)
            Column.padding({ top: 36, bottom: 36, left: 18, right: 18 });
            // Timer card with glass styling (mirrors Apple's liquid glass card)
            Column.borderRadius(34);
            // Timer card with glass styling (mirrors Apple's liquid glass card)
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '5C'));
            // Timer card with glass styling (mirrors Apple's liquid glass card)
            Column.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '12'),
                radius: 24,
                offsetY: 14
            });
            // Timer card with glass styling (mirrors Apple's liquid glass card)
            Column.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, 0.42],
                        [0xEFFAF8, 0.18],
                        [0x91E8D6, 0.14]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 34
            } as BorderOptions);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(formatClock(this.elapsedSeconds));
            Text.fontSize(78);
            Text.fontWeight(FontWeight.Bold);
            Text.fontFamily('serif');
            Text.fontColor(this.getTimerColor());
            Text.maxLines(1);
            Text.opacity(this.isPausedBlinking ? (0.4 + this.pulsePhase * 0.6) : 1.0);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.statusText);
            Text.fontSize(15);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Recording indicator dot
            if (this.state === RecordingState.RECORDING) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create({ space: 6 });
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Circle.create();
                        Circle.width(8);
                        Circle.height(8);
                        Circle.fill(RokuricsColors.coral);
                        Circle.opacity(0.7 + this.pulsePhase * 0.3);
                    }, Circle);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('录制中');
                        Text.fontSize(12);
                        Text.fontColor(RokuricsColors.coral);
                    }, Text);
                    Text.pop();
                    Row.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.state === RecordingState.FAILED) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.recordingManager.lastError ?? '录音失败');
                        Text.fontSize(13);
                        Text.fontColor(RokuricsColors.coral);
                        Text.textAlign(TextAlign.Center);
                        Text.maxLines(2);
                        Text.padding({ left: 16, right: 16 });
                    }, Text);
                    Text.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        // Timer card with glass styling (mirrors Apple's liquid glass card)
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Control buttons (glass cards)
            if (this.state === RecordingState.RECORDING || this.state === RecordingState.PAUSED) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create({ space: 12 });
                        Row.width('100%');
                        Row.padding({ left: 22, right: 22, bottom: 36 });
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Pause/Resume button
                        Button.createWithChild();
                        // Pause/Resume button
                        Button.layoutWeight(1);
                        // Pause/Resume button
                        Button.height(76);
                        // Pause/Resume button
                        Button.borderRadius(24);
                        // Pause/Resume button
                        Button.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '61'));
                        // Pause/Resume button
                        Button.shadow({
                            color: colorAlpha(RokuricsColors.shadowColor, '08'),
                            radius: 12,
                            offsetY: 6
                        });
                        // Pause/Resume button
                        Button.border({
                            width: 1,
                            color: {
                                colors: [
                                    [0xFFFFFF, 0.34],
                                    [0xEFFAF8, 0.14],
                                    [0x91E8D6, 0.10]
                                ],
                                direction: GradientDirection.RightBottom
                            },
                            radius: 24
                        } as BorderOptions);
                        // Pause/Resume button
                        Button.onClick(() => this.recordingManager.toggleRecording());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 8 });
                        Column.foregroundColor(RokuricsColors.softTeal);
                        Column.width('100%');
                        Column.height(76);
                        Column.justifyContent(FlexAlign.Center);
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.state === RecordingState.PAUSED) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                PlayIcon.bind(this)(22, RokuricsColors.softTeal);
                            });
                        }
                        else {
                            this.ifElseBranchUpdateFunction(1, () => {
                                PauseIcon.bind(this)(22, RokuricsColors.softTeal);
                            });
                        }
                    }, If);
                    If.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.state === RecordingState.PAUSED ? '继续' : '暂停');
                        Text.fontSize(13);
                        Text.fontWeight(FontWeight.SemiBold);
                    }, Text);
                    Text.pop();
                    Column.pop();
                    // Pause/Resume button
                    Button.pop();
                    Row.pop();
                });
            }
            // Pause/Resume single button for compact view
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Pause/Resume single button for compact view
            if (this.state === RecordingState.RECORDING || this.state === RecordingState.PAUSED) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.width(72);
                        Button.height(72);
                        Button.borderRadius(36);
                        Button.backgroundColor(this.state === RecordingState.RECORDING ? RokuricsColors.coral : RokuricsColors.aqua);
                        Button.shadow({
                            color: colorAlpha((this.state === RecordingState.RECORDING ? RokuricsColors.coral : RokuricsColors.aqua), '40'),
                            radius: 18,
                            offsetY: 6
                        });
                        Button.onClick(() => this.recordingManager.toggleRecording());
                        Button.margin({ bottom: 60 });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.state === RecordingState.RECORDING) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                PauseIcon.bind(this)(36, '#FFFFFF');
                            });
                        }
                        else {
                            this.ifElseBranchUpdateFunction(1, () => {
                                PlayIcon.bind(this)(36, '#FFFFFF');
                            });
                        }
                    }, If);
                    If.pop();
                    Button.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Filing UI overlay
            if (this.state === RecordingState.FILING) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.FilingOverlay.bind(this)();
                });
            }
            // Saved state
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Saved state
            if (this.state === RecordingState.SAVED) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 12 });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('已保存');
                        Text.fontSize(24);
                        Text.fontWeight(FontWeight.Bold);
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(`时长：${formatDuration(this.elapsedSeconds)}`);
                        Text.fontSize(14);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.margin({ top: 20 });
                        Button.onClick(() => {
                            this.getUIContext().getRouter().back();
                        });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('返回首页');
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    Column.pop();
                });
            }
            // Failed state
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Failed state
            if (this.state === RecordingState.FAILED) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 12 });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('录音失败');
                        Text.fontSize(18);
                        Text.fontColor(RokuricsColors.coral);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.recordingManager.lastError ?? '');
                        Text.fontSize(13);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.margin({ top: 16 });
                        Button.onClick(() => this.getUIContext().getRouter().back());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('返回');
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    Column.pop();
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
    FilingOverlay(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Stack.create();
            Stack.width('100%');
            Stack.height('100%');
            Stack.zIndex(10);
            Stack.position({ x: 0, y: 0 });
        }, Stack);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Semi-transparent backdrop
            Column.create();
            // Semi-transparent backdrop
            Column.width('100%');
            // Semi-transparent backdrop
            Column.height('100%');
            // Semi-transparent backdrop
            Column.backgroundColor('#00000028');
            // Semi-transparent backdrop
            Column.onClick(() => { });
        }, Column);
        // Semi-transparent backdrop
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Filing card overlay
            Scroll.create();
            // Filing card overlay
            Scroll.width('100%');
            // Filing card overlay
            Scroll.height('100%');
            // Filing card overlay
            Scroll.padding(24);
            // Filing card overlay
            Scroll.constraintSize({ maxWidth: 420 });
        }, Scroll);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 16 });
            Column.width('100%');
            Column.padding(22);
            Column.borderRadius(30);
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, 'F5'));
            Column.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '1C'),
                radius: 26,
                offsetY: 14
            });
            Column.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, 0.50],
                        [0xEFFAF8, 0.20],
                        [0x91E8D6, 0.16]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 30
            } as BorderOptions);
            Column.constraintSize({ maxWidth: 380 });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Header
            Text.create('录音归档');
            // Header
            Text.fontSize(30);
            // Header
            Text.fontWeight(FontWeight.Bold);
            // Header
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        // Header
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('选择门类→课程→章节→主题，或直接保存');
            Text.fontSize(14);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Title input
            Column.create({ space: 8 });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('录音标题');
            Text.fontSize(13);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            TextInput.create({
                text: this.pendingTitle || this.suggestedTitle,
                placeholder: '输入录音标题'
            });
            TextInput.fontSize(20);
            TextInput.fontWeight(FontWeight.SemiBold);
            TextInput.fontColor(RokuricsColors.deepText);
            TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '80'));
            TextInput.borderRadius(12);
            TextInput.padding(16);
            TextInput.width('100%');
            TextInput.onChange((value: string) => {
                this.pendingTitle = value;
                this.recordingManager.updatePendingTitle(value);
            });
        }, TextInput);
        // Title input
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Filing level buttons
            Row.create({ space: 8 });
        }, Row);
        this.LevelButton.bind(this)('门类', this.filingType, true);
        this.LevelButton.bind(this)('课程', this.filingSubject, this.filingType.length > 0);
        this.LevelButton.bind(this)('章节', this.filingChapter, this.filingSubject.length > 0);
        this.LevelButton.bind(this)('主题', this.filingTopic, this.filingChapter.length > 0);
        // Filing level buttons
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Show options for active level
            if (this.filingSubject.length > 0 && this.filingChapter.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('主题 (自由输入)');
                        Text.fontSize(12);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: this.filingTopic, placeholder: '输入主题名' });
                        TextInput.fontSize(14);
                        TextInput.fontColor(RokuricsColors.deepText);
                        TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'));
                        TextInput.borderRadius(8);
                        TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
                        TextInput.onChange((value: string) => { this.filingTopic = value; });
                    }, TextInput);
                });
            }
            else if (this.filingType.length > 0 && this.filingSubject.length > 0) {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('章节 (自由输入)');
                        Text.fontSize(12);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: this.filingChapter, placeholder: '输入章节名' });
                        TextInput.fontSize(14);
                        TextInput.fontColor(RokuricsColors.deepText);
                        TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'));
                        TextInput.borderRadius(8);
                        TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
                        TextInput.onChange((value: string) => { this.filingChapter = value; });
                    }, TextInput);
                });
            }
            else if (this.filingType.length > 0) {
                this.ifElseBranchUpdateFunction(2, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Subject options
                        Row.create({ space: 6 });
                        // Subject options
                        Row.flexShrink(0);
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        ForEach.create();
                        const forEachItemGenFunction = _item => {
                            const option = _item;
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(option);
                                Text.fontSize(12);
                                Text.fontWeight(FontWeight.Medium);
                                Text.fontColor(this.filingSubject === option ? Color.White : RokuricsColors.softText);
                                Text.padding({ left: 10, right: 10, top: 6, bottom: 6 });
                                Text.borderRadius(12);
                                Text.backgroundColor(this.filingSubject === option ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'));
                                Text.onClick(() => {
                                    if (this.filingSubject === option) {
                                        this.filingSubject = '';
                                    }
                                    else {
                                        this.filingSubject = option;
                                    }
                                });
                            }, Text);
                            Text.pop();
                        };
                        this.forEachUpdateFunction(elmtId, FILING_OPTIONS['subject'], forEachItemGenFunction);
                    }, ForEach);
                    ForEach.pop();
                    // Subject options
                    Row.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(3, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Type options
                        Row.create({ space: 6 });
                        // Type options
                        Row.flexShrink(0);
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        ForEach.create();
                        const forEachItemGenFunction = _item => {
                            const option = _item;
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(option);
                                Text.fontSize(12);
                                Text.fontWeight(FontWeight.Medium);
                                Text.fontColor(this.filingType === option ? Color.White : RokuricsColors.softText);
                                Text.padding({ left: 10, right: 10, top: 6, bottom: 6 });
                                Text.borderRadius(12);
                                Text.backgroundColor(this.filingType === option ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'));
                                Text.onClick(() => {
                                    if (this.filingType === option) {
                                        this.filingType = '';
                                    }
                                    else {
                                        this.filingType = option;
                                    }
                                });
                            }, Text);
                            Text.pop();
                        };
                        this.forEachUpdateFunction(elmtId, FILING_OPTIONS['type'], forEachItemGenFunction);
                    }, ForEach);
                    ForEach.pop();
                    // Type options
                    Row.pop();
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Filing summary
            if (this.filingType || this.filingSubject || this.filingChapter || this.filingTopic) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(`归档路径：${this.getFilingPath().displaySummary}`);
                        Text.fontSize(12);
                        Text.fontColor(RokuricsColors.aqua);
                        Text.margin({ top: 4 });
                    }, Text);
                    Text.pop();
                });
            }
            // Action buttons
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Action buttons
            Row.create({ space: 12 });
            // Action buttons
            Row.width('100%');
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.layoutWeight(1);
            Button.height(48);
            Button.borderRadius(24);
            Button.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '4D'));
            Button.border({ width: 1, color: colorAlpha(RokuricsColors.softText, '20'), radius: 24 });
            Button.onClick(() => {
                this.recordingManager.finalizeRecordingDirectSave();
                this.getUIContext().getRouter().back();
            });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('直接保存');
            Text.fontSize(15);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.layoutWeight(1);
            Button.height(48);
            Button.borderRadius(24);
            Button.backgroundColor((this.filingType || this.filingSubject || this.filingChapter || this.filingTopic) ?
                RokuricsColors.aqua : RokuricsColors.tertiaryText);
            Button.enabled(!!(this.filingType || this.filingSubject || this.filingChapter || this.filingTopic));
            Button.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '16'),
                radius: 14,
                offsetY: 8
            });
            Button.onClick(() => {
                const filing = this.getFilingPath();
                this.recordingManager.finalizeRecording(this.pendingTitle || this.suggestedTitle, filing.isEmpty ? null : filing);
                this.getUIContext().getRouter().back();
            });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('保存归档');
            Text.fontSize(15);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(Color.White);
        }, Text);
        Text.pop();
        Button.pop();
        // Action buttons
        Row.pop();
        Column.pop();
        // Filing card overlay
        Scroll.pop();
        Stack.pop();
    }
    LevelButton(label: string, value: string, enabled: boolean, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 3 });
            Column.padding({ left: 8, right: 8, top: 6, bottom: 6 });
            Column.constraintSize({ minWidth: 64 });
            Column.borderRadius(13);
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '56'));
            Column.border({
                width: 1,
                color: enabled ? colorAlpha(RokuricsColors.aqua, '46') : colorAlpha(RokuricsColors.softText, '18'),
                radius: 13
            });
            Column.opacity(enabled ? 1 : 0.46);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(label);
            Text.fontSize(9);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(RokuricsColors.tertiaryText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(value || '—');
            Text.fontSize(11);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(enabled ? RokuricsColors.deepText : RokuricsColors.tertiaryText);
            Text.maxLines(1);
            Text.textOverflow({ overflow: TextOverflow.Ellipsis });
            Text.constraintSize({ maxWidth: 56 });
        }, Text);
        Text.pop();
        Column.pop();
    }
    SessionBackground(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Stack.create();
            Stack.width('100%');
            Stack.height('100%');
        }, Stack);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Circle.create();
            Circle.width(220);
            Circle.height(220);
            Circle.fill(colorAlpha(RokuricsColors.mint, '20'));
            Circle.position({ x: -50, y: -150 });
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Circle.create();
            Circle.width(260);
            Circle.height(260);
            Circle.fill(colorAlpha(RokuricsColors.skyCyan, '16'));
            Circle.position({ x: '85%', y: '75%' });
        }, Circle);
        Stack.pop();
    }
    private getTimerColor(): string {
        switch (this.state) {
            case RecordingState.RECORDING:
                return RokuricsColors.coral;
            case RecordingState.PAUSED:
                return RokuricsColors.softTeal;
            default:
                return RokuricsColors.deepText;
        }
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "RecordingSessionPage";
    }
}
registerNamedRoute(() => new RecordingSessionPage(undefined, {}), "", { bundleName: "com.vita0818.rokurics", moduleName: "entry", pagePath: "pages/RecordingSessionPage", pageFullPath: "entry/src/main/ets/pages/RecordingSessionPage", integratedHsp: "false", moduleType: "followWithHap" });
