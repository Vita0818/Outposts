if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface TodayOverviewPage_Params {
}
interface TodayOverviewPageContent_Params {
    presetName?: string;
    masteredCount?: number;
    totalCount?: number;
    dailyGoal?: number;
    todayMastered?: number;
    todayHint?: number;
    todayReview?: number;
    countdownDays?: string;
    recentActivity?: StudyActivityRecord[];
    contentPadH?: number;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { StudyActivityType } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import type { StudyActivityRecord } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, pageHorizontalPadding, ipadPortraitOverviewTopInset } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function TodayOverviewPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new TodayOverviewPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/TodayOverviewPage.ets", line: 15, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "TodayOverviewPageContent" });
    }
}
export class TodayOverviewPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__presetName = new ObservedPropertySimplePU('', this, "presetName");
        this.__masteredCount = new ObservedPropertySimplePU(0, this, "masteredCount");
        this.__totalCount = new ObservedPropertySimplePU(0, this, "totalCount");
        this.__dailyGoal = new ObservedPropertySimplePU(0, this, "dailyGoal");
        this.__todayMastered = new ObservedPropertySimplePU(0, this, "todayMastered");
        this.__todayHint = new ObservedPropertySimplePU(0, this, "todayHint");
        this.__todayReview = new ObservedPropertySimplePU(0, this, "todayReview");
        this.__countdownDays = new ObservedPropertySimplePU('--', this, "countdownDays");
        this.__recentActivity = new ObservedPropertyObjectPU([], this, "recentActivity");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: TodayOverviewPageContent_Params) {
        if (params.presetName !== undefined) {
            this.presetName = params.presetName;
        }
        if (params.masteredCount !== undefined) {
            this.masteredCount = params.masteredCount;
        }
        if (params.totalCount !== undefined) {
            this.totalCount = params.totalCount;
        }
        if (params.dailyGoal !== undefined) {
            this.dailyGoal = params.dailyGoal;
        }
        if (params.todayMastered !== undefined) {
            this.todayMastered = params.todayMastered;
        }
        if (params.todayHint !== undefined) {
            this.todayHint = params.todayHint;
        }
        if (params.todayReview !== undefined) {
            this.todayReview = params.todayReview;
        }
        if (params.countdownDays !== undefined) {
            this.countdownDays = params.countdownDays;
        }
        if (params.recentActivity !== undefined) {
            this.recentActivity = params.recentActivity;
        }
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
    }
    updateStateVars(params: TodayOverviewPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__presetName.purgeDependencyOnElmtId(rmElmtId);
        this.__masteredCount.purgeDependencyOnElmtId(rmElmtId);
        this.__totalCount.purgeDependencyOnElmtId(rmElmtId);
        this.__dailyGoal.purgeDependencyOnElmtId(rmElmtId);
        this.__todayMastered.purgeDependencyOnElmtId(rmElmtId);
        this.__todayHint.purgeDependencyOnElmtId(rmElmtId);
        this.__todayReview.purgeDependencyOnElmtId(rmElmtId);
        this.__countdownDays.purgeDependencyOnElmtId(rmElmtId);
        this.__recentActivity.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__presetName.aboutToBeDeleted();
        this.__masteredCount.aboutToBeDeleted();
        this.__totalCount.aboutToBeDeleted();
        this.__dailyGoal.aboutToBeDeleted();
        this.__todayMastered.aboutToBeDeleted();
        this.__todayHint.aboutToBeDeleted();
        this.__todayReview.aboutToBeDeleted();
        this.__countdownDays.aboutToBeDeleted();
        this.__recentActivity.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __presetName: ObservedPropertySimplePU<string>;
    get presetName() {
        return this.__presetName.get();
    }
    set presetName(newValue: string) {
        this.__presetName.set(newValue);
    }
    private __masteredCount: ObservedPropertySimplePU<number>;
    get masteredCount() {
        return this.__masteredCount.get();
    }
    set masteredCount(newValue: number) {
        this.__masteredCount.set(newValue);
    }
    private __totalCount: ObservedPropertySimplePU<number>;
    get totalCount() {
        return this.__totalCount.get();
    }
    set totalCount(newValue: number) {
        this.__totalCount.set(newValue);
    }
    private __dailyGoal: ObservedPropertySimplePU<number>;
    get dailyGoal() {
        return this.__dailyGoal.get();
    }
    set dailyGoal(newValue: number) {
        this.__dailyGoal.set(newValue);
    }
    private __todayMastered: ObservedPropertySimplePU<number>;
    get todayMastered() {
        return this.__todayMastered.get();
    }
    set todayMastered(newValue: number) {
        this.__todayMastered.set(newValue);
    }
    private __todayHint: ObservedPropertySimplePU<number>;
    get todayHint() {
        return this.__todayHint.get();
    }
    set todayHint(newValue: number) {
        this.__todayHint.set(newValue);
    }
    private __todayReview: ObservedPropertySimplePU<number>;
    get todayReview() {
        return this.__todayReview.get();
    }
    set todayReview(newValue: number) {
        this.__todayReview.set(newValue);
    }
    private __countdownDays: ObservedPropertySimplePU<string>;
    get countdownDays() {
        return this.__countdownDays.get();
    }
    set countdownDays(newValue: string) {
        this.__countdownDays.set(newValue);
    }
    private __recentActivity: ObservedPropertyObjectPU<StudyActivityRecord[]>;
    get recentActivity() {
        return this.__recentActivity.get();
    }
    set recentActivity(newValue: StudyActivityRecord[]) {
        this.__recentActivity.set(newValue);
    }
    private __contentPadH: ObservedPropertySimplePU<number>;
    get contentPadH() {
        return this.__contentPadH.get();
    }
    set contentPadH(newValue: number) {
        this.__contentPadH.set(newValue);
    }
    aboutToAppear(): void {
        this.contentPadH = pageHorizontalPadding();
        this.refreshState();
    }
    refreshState(): void {
        this.presetName = appState.currentPreset.name;
        this.masteredCount = appState.masteredCount;
        this.totalCount = appState.totalCount;
        this.dailyGoal = appState.dailyGoal;
        this.todayMastered = appState.todayMarkedMasteredCount;
        this.todayHint = appState.todayViewedHintCount;
        this.todayReview = appState.todayReviewedAnswerCount;
        const days = appState.countdownDayCount;
        this.countdownDays = days !== null ? `${days} 天` : '--';
        const records = appState.getCurrentPresetActivityRecords();
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        this.recentActivity = records
            .filter(r => r.date >= today.getTime())
            .sort((a, b) => b.date - a.date)
            .slice(0, 20);
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            NavDestination.create(() => {
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Scroll.create();
                    Scroll.width('100%');
                    Scroll.height('100%');
                    Scroll.backgroundColor(KikariaColors.PAGE_BG);
                    Scroll.scrollBar(BarState.Off);
                }, Scroll);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 18 });
                    Column.width('100%');
                    Column.constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' });
                    Column.padding({ left: this.contentPadH, right: this.contentPadH });
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.width('100%');
                    Row.padding({ left: 20, right: 20, top: 12 + ipadPortraitOverviewTopInset(), bottom: 12 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithChild();
                    Button.backgroundColor(Color.Transparent);
                    Button.onClick(() => { navPathStack.pop(); });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('← 返回');
                    Text.fontSize(17);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Button.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('今日概览');
                    Text.fontSize(17);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.DEEP_TEXT);
                }, Text);
                Text.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Main progress card
                    Column.create({ space: 12 });
                    // Main progress card
                    Column.width('100%');
                    // Main progress card
                    Column.padding(24);
                    // Main progress card
                    Column.borderRadius(22);
                    // Main progress card
                    Column.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                    // Main progress card
                    Column.backdropBlur(18);
                    // Main progress card
                    Column.shadow({ radius: 14, color: KikariaColors.SHADOW_COLOR, offsetY: 6 });
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(`${this.todayMastered}/${this.dailyGoal}`);
                    Text.fontSize(48);
                    Text.fontWeight(FontWeight.Bold);
                    Text.fontColor(KikariaColors.DEEP_TEXT);
                    Text.fontFamily('serif');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('今日掌握');
                    Text.fontSize(15);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.width('100%');
                    Row.height(6);
                    Row.borderRadius(3);
                    Row.backgroundColor(KikariaColors.MIST);
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.width(`${Math.min(100, this.todayMastered / Math.max(1, this.dailyGoal) * 100)}%`);
                    Row.height(6);
                    Row.borderRadius(3);
                    Row.linearGradient({
                        angle: 90,
                        colors: [[KikariaColors.MASTERED_GRADIENT_START, 0], [KikariaColors.MASTERED_GRADIENT_END, 1]]
                    });
                }, Row);
                Row.pop();
                Row.pop();
                // Main progress card
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Stats grid
                    Row.create({ space: 12 });
                    // Stats grid
                    Row.width('100%');
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 6 });
                    Column.layoutWeight(1);
                    Column.padding(16);
                    Column.borderRadius(18);
                    Column.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                    Column.backdropBlur(12);
                    Column.shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 });
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(`${this.masteredCount}`);
                    Text.fontSize(28);
                    Text.fontWeight(FontWeight.Bold);
                    Text.fontColor(KikariaColors.MASTERED_GREEN);
                    Text.fontFamily('serif');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('总掌握');
                    Text.fontSize(12);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 6 });
                    Column.layoutWeight(1);
                    Column.padding(16);
                    Column.borderRadius(18);
                    Column.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                    Column.backdropBlur(12);
                    Column.shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 });
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(`${this.todayHint}`);
                    Text.fontSize(28);
                    Text.fontWeight(FontWeight.Bold);
                    Text.fontColor(KikariaColors.SKY);
                    Text.fontFamily('serif');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('查看提示');
                    Text.fontSize(12);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 6 });
                    Column.layoutWeight(1);
                    Column.padding(16);
                    Column.borderRadius(18);
                    Column.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                    Column.backdropBlur(12);
                    Column.shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 });
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(`${this.todayReview}`);
                    Text.fontSize(28);
                    Text.fontWeight(FontWeight.Bold);
                    Text.fontColor(KikariaColors.NEXT_AMBER);
                    Text.fontFamily('serif');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('查看答案');
                    Text.fontSize(12);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Column.pop();
                // Stats grid
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Countdown card
                    Column.create({ space: 8 });
                    // Countdown card
                    Column.width('100%');
                    // Countdown card
                    Column.padding(20);
                    // Countdown card
                    Column.borderRadius(18);
                    // Countdown card
                    Column.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                    // Countdown card
                    Column.backdropBlur(12);
                    // Countdown card
                    Column.shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 });
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('倒计时');
                    Text.fontSize(15);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.DEEP_TEXT);
                    Text.width('100%');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create({ space: 16 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(this.countdownDays);
                    Text.fontSize(28);
                    Text.fontWeight(FontWeight.Bold);
                    Text.fontColor(KikariaColors.DEEP_TEXT);
                    Text.fontFamily('serif');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(`总知识点: ${this.totalCount}`);
                    Text.fontSize(13);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Row.pop();
                // Countdown card
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    // Today's activity
                    if (this.recentActivity.length > 0) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 8 });
                                Column.width('100%');
                                Column.padding(20);
                                Column.borderRadius(18);
                                Column.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                                Column.backdropBlur(12);
                                Column.shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 });
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('今日活动');
                                Text.fontSize(15);
                                Text.fontWeight(FontWeight.Medium);
                                Text.fontColor(KikariaColors.DEEP_TEXT);
                                Text.width('100%');
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                ForEach.create();
                                const forEachItemGenFunction = _item => {
                                    const record = _item;
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Row.create({ space: 12 });
                                        Row.width('100%');
                                        Row.padding({ top: 10, bottom: 10 });
                                        Row.border({ width: { bottom: 1 }, color: KikariaColors.MIST });
                                    }, Row);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(this.activityIcon(record.type));
                                        Text.fontSize(16);
                                    }, Text);
                                    Text.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(record.pointTitle);
                                        Text.fontSize(14);
                                        Text.fontColor(KikariaColors.DEEP_TEXT);
                                        Text.layoutWeight(1);
                                        Text.maxLines(1);
                                        Text.textOverflow({ overflow: TextOverflow.Ellipsis });
                                    }, Text);
                                    Text.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(this.activityLabel(record.type));
                                        Text.fontSize(12);
                                        Text.fontColor(KikariaColors.SOFT_TEXT);
                                    }, Text);
                                    Text.pop();
                                    Row.pop();
                                };
                                this.forEachUpdateFunction(elmtId, this.recentActivity, forEachItemGenFunction);
                            }, ForEach);
                            ForEach.pop();
                            Column.pop();
                        });
                    }
                    else {
                        this.ifElseBranchUpdateFunction(1, () => {
                        });
                    }
                }, If);
                If.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.width('100%');
                    Row.padding({ top: 8 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithLabel('查看历史记录 →');
                    Button.fontSize(15);
                    Button.fontWeight(FontWeight.Medium);
                    Button.fontColor(KikariaColors.SKY);
                    Button.backgroundColor(Color.Transparent);
                    Button.onClick(() => {
                        navPathStack.pushPathByName('ReviewHistoryPage', undefined);
                    });
                }, Button);
                Button.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.height(40);
                }, Row);
                Row.pop();
                Column.pop();
                Scroll.pop();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/TodayOverviewPage" });
            NavDestination.hideTitleBar(true);
            NavDestination.width('100%');
            NavDestination.height('100%');
        }, NavDestination);
        NavDestination.pop();
    }
    activityIcon(type: StudyActivityType): string {
        switch (type) {
            case StudyActivityType.MARKED_MASTERED: return '✓';
            case StudyActivityType.REMOVED_MASTERED: return '↩';
            case StudyActivityType.ADDED_REINFORCEMENT: return '★';
            case StudyActivityType.REMOVED_REINFORCEMENT: return '☆';
            case StudyActivityType.VIEWED_HINT: return '💡';
            case StudyActivityType.REVIEWED_ANSWER: return '📖';
        }
    }
    activityLabel(type: StudyActivityType): string {
        switch (type) {
            case StudyActivityType.MARKED_MASTERED: return '已掌握';
            case StudyActivityType.REMOVED_MASTERED: return '取消掌握';
            case StudyActivityType.ADDED_REINFORCEMENT: return '已标重点';
            case StudyActivityType.REMOVED_REINFORCEMENT: return '取消重点';
            case StudyActivityType.VIEWED_HINT: return '查看提示';
            case StudyActivityType.REVIEWED_ANSWER: return '复习';
        }
    }
    rerender() {
        this.updateDirtyElements();
    }
}
class TodayOverviewPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: TodayOverviewPage_Params) {
    }
    updateStateVars(params: TodayOverviewPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/TodayOverviewPage", isUserCreateStack: false });
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new TodayOverviewPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/TodayOverviewPage.ets", line: 294, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "TodayOverviewPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "TodayOverviewPage";
    }
}
registerNamedRoute(() => new TodayOverviewPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/TodayOverviewPage", pageFullPath: "entry/src/main/ets/pages/TodayOverviewPage", integratedHsp: "false", moduleType: "followWithHap" });
