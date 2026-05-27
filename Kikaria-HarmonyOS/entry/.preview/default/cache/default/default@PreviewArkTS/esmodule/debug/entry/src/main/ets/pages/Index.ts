if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface Index_Params {
    isDark?: boolean;
    contentPadH?: number;
    presetName?: string;
    masteredCount?: number;
    reinforcedCount?: number;
    totalCount?: number;
    allTags?: string[];
    selectedTags?: Set<string>;
    dailyGoal?: number;
    todayMastered?: number;
    countdownDays?: string;
    homeDateTitle?: string;
    homeProgressText?: string;
    bubbleScale?: number;
    displayName?: string;
    useTwoColumn?: boolean;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { ReviewMode, ordinalSuffix } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, isPadPortrait, isTwoColumnCapable, pageHorizontalPadding, homeLandscapeAvailableWidth, homeLandscapeColumnSpacing, homeLandscapeLeftWidth, homeLandscapeRightWidth, homeLandscapeBubbleScale, padPortraitHomeTitleSize, padPortraitHomeAvatarSize, padPortraitHomeTopPadding, padPortraitHomeBubbleSize, padPortraitHomeBubbleFontSize } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
import { ScopeSelectionPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/ScopeSelectionPage";
import { TodayOverviewPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/TodayOverviewPage";
import { ReviewHistoryPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/ReviewHistoryPage";
import { MarkdownFormatGuideBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/MarkdownFormatGuidePage";
import { ReviewPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/ReviewPage";
import { SettingsPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/SettingsPage";
import { ReinforcementPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/ReinforcementPage";
import { MasteredPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/MasteredPage";
import { OnboardingPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/OnboardingPage";
import { PresetSelectionPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/PresetSelectionPage";
import { EditProfilePageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/EditProfilePage";
import { EditKnowledgePointPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/EditKnowledgePointPage";
import { EditPresetPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/EditPresetPage";
import { NewPresetPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/NewPresetPage";
import { InitialProfileSetupPageBuilder } from "@bundle:com.vita0818.kikaria/entry/ets/pages/InitialProfileSetupPage";
class Index extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__isDark = this.createStorageLink('kikaria_isDarkMode', false, "isDark");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.__presetName = new ObservedPropertySimplePU('', this, "presetName");
        this.__masteredCount = new ObservedPropertySimplePU(0, this, "masteredCount");
        this.__reinforcedCount = new ObservedPropertySimplePU(0, this, "reinforcedCount");
        this.__totalCount = new ObservedPropertySimplePU(0, this, "totalCount");
        this.__allTags = new ObservedPropertyObjectPU([], this, "allTags");
        this.__selectedTags = new ObservedPropertyObjectPU(new Set(), this, "selectedTags");
        this.__dailyGoal = new ObservedPropertySimplePU(20, this, "dailyGoal");
        this.__todayMastered = new ObservedPropertySimplePU(0, this, "todayMastered");
        this.__countdownDays = new ObservedPropertySimplePU('-- Days Left', this, "countdownDays");
        this.__homeDateTitle = new ObservedPropertySimplePU('', this, "homeDateTitle");
        this.__homeProgressText = new ObservedPropertySimplePU('', this, "homeProgressText");
        this.__bubbleScale = new ObservedPropertySimplePU(1.0, this, "bubbleScale");
        this.__displayName = new ObservedPropertySimplePU('', this, "displayName");
        this.__useTwoColumn = new ObservedPropertySimplePU(false, this, "useTwoColumn");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: Index_Params) {
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
        if (params.presetName !== undefined) {
            this.presetName = params.presetName;
        }
        if (params.masteredCount !== undefined) {
            this.masteredCount = params.masteredCount;
        }
        if (params.reinforcedCount !== undefined) {
            this.reinforcedCount = params.reinforcedCount;
        }
        if (params.totalCount !== undefined) {
            this.totalCount = params.totalCount;
        }
        if (params.allTags !== undefined) {
            this.allTags = params.allTags;
        }
        if (params.selectedTags !== undefined) {
            this.selectedTags = params.selectedTags;
        }
        if (params.dailyGoal !== undefined) {
            this.dailyGoal = params.dailyGoal;
        }
        if (params.todayMastered !== undefined) {
            this.todayMastered = params.todayMastered;
        }
        if (params.countdownDays !== undefined) {
            this.countdownDays = params.countdownDays;
        }
        if (params.homeDateTitle !== undefined) {
            this.homeDateTitle = params.homeDateTitle;
        }
        if (params.homeProgressText !== undefined) {
            this.homeProgressText = params.homeProgressText;
        }
        if (params.bubbleScale !== undefined) {
            this.bubbleScale = params.bubbleScale;
        }
        if (params.displayName !== undefined) {
            this.displayName = params.displayName;
        }
        if (params.useTwoColumn !== undefined) {
            this.useTwoColumn = params.useTwoColumn;
        }
    }
    updateStateVars(params: Index_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__isDark.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
        this.__presetName.purgeDependencyOnElmtId(rmElmtId);
        this.__masteredCount.purgeDependencyOnElmtId(rmElmtId);
        this.__reinforcedCount.purgeDependencyOnElmtId(rmElmtId);
        this.__totalCount.purgeDependencyOnElmtId(rmElmtId);
        this.__allTags.purgeDependencyOnElmtId(rmElmtId);
        this.__selectedTags.purgeDependencyOnElmtId(rmElmtId);
        this.__dailyGoal.purgeDependencyOnElmtId(rmElmtId);
        this.__todayMastered.purgeDependencyOnElmtId(rmElmtId);
        this.__countdownDays.purgeDependencyOnElmtId(rmElmtId);
        this.__homeDateTitle.purgeDependencyOnElmtId(rmElmtId);
        this.__homeProgressText.purgeDependencyOnElmtId(rmElmtId);
        this.__bubbleScale.purgeDependencyOnElmtId(rmElmtId);
        this.__displayName.purgeDependencyOnElmtId(rmElmtId);
        this.__useTwoColumn.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__isDark.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        this.__presetName.aboutToBeDeleted();
        this.__masteredCount.aboutToBeDeleted();
        this.__reinforcedCount.aboutToBeDeleted();
        this.__totalCount.aboutToBeDeleted();
        this.__allTags.aboutToBeDeleted();
        this.__selectedTags.aboutToBeDeleted();
        this.__dailyGoal.aboutToBeDeleted();
        this.__todayMastered.aboutToBeDeleted();
        this.__countdownDays.aboutToBeDeleted();
        this.__homeDateTitle.aboutToBeDeleted();
        this.__homeProgressText.aboutToBeDeleted();
        this.__bubbleScale.aboutToBeDeleted();
        this.__displayName.aboutToBeDeleted();
        this.__useTwoColumn.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __isDark: ObservedPropertyAbstractPU<boolean>;
    get isDark() {
        return this.__isDark.get();
    }
    set isDark(newValue: boolean) {
        this.__isDark.set(newValue);
    }
    private __contentPadH: ObservedPropertySimplePU<number>;
    get contentPadH() {
        return this.__contentPadH.get();
    }
    set contentPadH(newValue: number) {
        this.__contentPadH.set(newValue);
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
    private __reinforcedCount: ObservedPropertySimplePU<number>;
    get reinforcedCount() {
        return this.__reinforcedCount.get();
    }
    set reinforcedCount(newValue: number) {
        this.__reinforcedCount.set(newValue);
    }
    private __totalCount: ObservedPropertySimplePU<number>;
    get totalCount() {
        return this.__totalCount.get();
    }
    set totalCount(newValue: number) {
        this.__totalCount.set(newValue);
    }
    private __allTags: ObservedPropertyObjectPU<string[]>;
    get allTags() {
        return this.__allTags.get();
    }
    set allTags(newValue: string[]) {
        this.__allTags.set(newValue);
    }
    private __selectedTags: ObservedPropertyObjectPU<Set<string>>;
    get selectedTags() {
        return this.__selectedTags.get();
    }
    set selectedTags(newValue: Set<string>) {
        this.__selectedTags.set(newValue);
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
    private __countdownDays: ObservedPropertySimplePU<string>;
    get countdownDays() {
        return this.__countdownDays.get();
    }
    set countdownDays(newValue: string) {
        this.__countdownDays.set(newValue);
    }
    private __homeDateTitle: ObservedPropertySimplePU<string>;
    get homeDateTitle() {
        return this.__homeDateTitle.get();
    }
    set homeDateTitle(newValue: string) {
        this.__homeDateTitle.set(newValue);
    }
    private __homeProgressText: ObservedPropertySimplePU<string>;
    get homeProgressText() {
        return this.__homeProgressText.get();
    }
    set homeProgressText(newValue: string) {
        this.__homeProgressText.set(newValue);
    }
    private __bubbleScale: ObservedPropertySimplePU<number>;
    get bubbleScale() {
        return this.__bubbleScale.get();
    }
    set bubbleScale(newValue: number) {
        this.__bubbleScale.set(newValue);
    }
    private __displayName: ObservedPropertySimplePU<string>;
    get displayName() {
        return this.__displayName.get();
    }
    set displayName(newValue: string) {
        this.__displayName.set(newValue);
    }
    private __useTwoColumn: ObservedPropertySimplePU<boolean>;
    get useTwoColumn() {
        return this.__useTwoColumn.get();
    }
    set useTwoColumn(newValue: boolean) {
        this.__useTwoColumn.set(newValue);
    }
    aboutToAppear(): void {
        this.contentPadH = pageHorizontalPadding();
        this.useTwoColumn = isTwoColumnCapable();
        this.refreshState();
        this.bubbleScale = 0.94;
        if (!appState.hasCompletedProfileSetup) {
            navPathStack.pushPathByName('InitialProfileSetupPage', undefined);
        }
        else if (!appState.hasCompletedOnboarding) {
            navPathStack.pushPathByName('OnboardingPage', undefined);
        }
    }
    onPageShow(): void {
        this.refreshState();
    }
    refreshState(): void {
        this.presetName = appState.currentPreset.name;
        this.masteredCount = appState.masteredCount;
        this.reinforcedCount = appState.reinforcedCount;
        this.totalCount = appState.totalCount;
        this.allTags = appState.allTags;
        this.selectedTags = new Set(appState.selectedTags);
        this.dailyGoal = appState.dailyGoal;
        this.todayMastered = appState.todayMarkedMasteredCount;
        this.displayName = appState.userProfile.displayName;
        const days = appState.countdownDayCount;
        this.countdownDays = days !== null ? `${days} Days Left` : '-- Days Left';
        const now = new Date();
        const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        const day = now.getDate();
        this.homeDateTitle = `${monthNames[now.getMonth()]} ${day}${ordinalSuffix(day)}`;
        this.homeProgressText = `${this.todayMastered}/${this.dailyGoal}`;
    }
    navDestinationRouter(name: string, param: Object, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (name === 'ScopeSelectionPage') {
                this.ifElseBranchUpdateFunction(0, () => {
                    ScopeSelectionPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'TodayOverviewPage') {
                this.ifElseBranchUpdateFunction(1, () => {
                    TodayOverviewPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'ReviewHistoryPage') {
                this.ifElseBranchUpdateFunction(2, () => {
                    ReviewHistoryPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'MarkdownFormatGuide') {
                this.ifElseBranchUpdateFunction(3, () => {
                    MarkdownFormatGuideBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'ReviewPage') {
                this.ifElseBranchUpdateFunction(4, () => {
                    ReviewPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'SettingsPage') {
                this.ifElseBranchUpdateFunction(5, () => {
                    SettingsPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'ReinforcementPage') {
                this.ifElseBranchUpdateFunction(6, () => {
                    ReinforcementPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'MasteredPage') {
                this.ifElseBranchUpdateFunction(7, () => {
                    MasteredPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'OnboardingPage') {
                this.ifElseBranchUpdateFunction(8, () => {
                    OnboardingPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'PresetSelectionPage') {
                this.ifElseBranchUpdateFunction(9, () => {
                    PresetSelectionPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'EditProfilePage') {
                this.ifElseBranchUpdateFunction(10, () => {
                    EditProfilePageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'EditKnowledgePointPage') {
                this.ifElseBranchUpdateFunction(11, () => {
                    EditKnowledgePointPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'EditPresetPage') {
                this.ifElseBranchUpdateFunction(12, () => {
                    EditPresetPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'NewPresetPage') {
                this.ifElseBranchUpdateFunction(13, () => {
                    NewPresetPageBuilder.bind(this)(name, param);
                });
            }
            else if (name === 'InitialProfileSetupPage') {
                this.ifElseBranchUpdateFunction(14, () => {
                    InitialProfileSetupPageBuilder.bind(this)(name, param);
                });
            }
            else /* ── Header bar (shared across layouts) ── */ {
                this.ifElseBranchUpdateFunction(15, () => {
                });
            }
        }, If);
        If.pop();
    }
    /* ── Header bar (shared across layouts) ── */
    headerBar(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.debugLine("entry/src/main/ets/pages/Index.ets(132:5)", "entry");
            Row.width('100%');
            Row.padding({ top: 14 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('Kikaria');
            Text.debugLine("entry/src/main/ets/pages/Index.ets(133:7)", "entry");
            Text.fontSize(39);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.fontFamily('serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
            Blank.debugLine("entry/src/main/ets/pages/Index.ets(139:7)", "entry");
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.debugLine("entry/src/main/ets/pages/Index.ets(141:7)", "entry");
            Row.width(44);
            Row.height(44);
            Row.borderRadius(22);
            Row.backgroundColor(KikariaColors.SKY);
            Row.justifyContent(FlexAlign.Center);
            Row.onClick(() => {
                navPathStack.pushPathByName('SettingsPage', undefined);
            });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.displayName.charAt(0).toUpperCase());
            Text.debugLine("entry/src/main/ets/pages/Index.ets(142:9)", "entry");
            Text.fontSize(18);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor('#FFFFFF');
        }, Text);
        Text.pop();
        Row.pop();
        Row.pop();
    }
    /* ── Review bubble (animated, gradient) ── */
    reviewBubble(scale: number, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 10 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(163:5)", "entry");
            globalThis.Context.animation({
                duration: 5400,
                curve: Curve.EaseInOut,
                iterations: -1,
                playMode: PlayMode.Alternate
            });
            Column.width(200);
            Column.height(200);
            Column.borderRadius(100);
            Column.justifyContent(FlexAlign.Center);
            Column.scale({ x: scale, y: scale });
            Column.linearGradient({
                angle: 135,
                colors: [[KikariaColors.SKY, 0], [KikariaColors.CYAN, 1]]
            });
            Column.shadow({
                radius: 28,
                color: `${KikariaColors.SKY}48`,
                offsetY: 18
            });
            globalThis.Context.animation(null);
            Column.onClick(() => {
                this.startNormalReview();
            });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('开始背诵');
            Text.debugLine("entry/src/main/ets/pages/Index.ets(164:7)", "entry");
            Text.fontSize(32);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor('#FFFFFF');
            Text.fontFamily('serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('→');
            Text.debugLine("entry/src/main/ets/pages/Index.ets(170:7)", "entry");
            Text.fontSize(64);
            Text.fontWeight(FontWeight.Lighter);
            Text.fontColor('#FFFFFF');
            Text.opacity(0.9);
        }, Text);
        Text.pop();
        Column.pop();
    }
    /* ── Progress card ── */
    progressCard(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 14 });
            Row.debugLine("entry/src/main/ets/pages/Index.ets(204:5)", "entry");
            Row.width('100%');
            Row.padding(20);
            Row.borderRadius(25);
            Row.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
            Row.backdropBlur(20);
            Row.shadow({ radius: 17, color: KikariaColors.SHADOW_COLOR, offsetY: 9 });
            Row.onClick(() => {
                navPathStack.pushPathByName('TodayOverviewPage', undefined);
            });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 5 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(205:7)", "entry");
            Column.alignItems(HorizontalAlign.Start);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.homeDateTitle);
            Text.debugLine("entry/src/main/ets/pages/Index.ets(206:9)", "entry");
            Text.fontSize(23);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.fontFamily('serif');
            Text.maxLines(1);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.countdownDays);
            Text.debugLine("entry/src/main/ets/pages/Index.ets(213:9)", "entry");
            Text.fontSize(13);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(KikariaColors.SOFT_TEXT);
            Text.maxLines(1);
        }, Text);
        Text.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
            Blank.debugLine("entry/src/main/ets/pages/Index.ets(221:7)", "entry");
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.homeProgressText);
            Text.debugLine("entry/src/main/ets/pages/Index.ets(223:7)", "entry");
            Text.fontSize(25);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.MASTERED_DEEP_GREEN);
            Text.fontFamily('serif');
        }, Text);
        Text.pop();
        Row.pop();
    }
    /* ── Dashboard grid (Scope / Reinforcement / Mastered) ── */
    dashboardGrid(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 0 });
            Row.debugLine("entry/src/main/ets/pages/Index.ets(243:5)", "entry");
            Row.width('100%');
            Row.borderRadius(25);
            Row.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
            Row.backdropBlur(20);
            Row.shadow({ radius: 17, color: KikariaColors.SHADOW_COLOR, offsetY: 9 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 6 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(244:7)", "entry");
            Column.layoutWeight(1);
            Column.padding(16);
            Column.alignItems(HorizontalAlign.Center);
            Column.onClick(() => {
                navPathStack.pushPathByName('ScopeSelectionPage', undefined);
            });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(appState.selectedScopeCountText);
            Text.debugLine("entry/src/main/ets/pages/Index.ets(245:9)", "entry");
            Text.fontSize(26);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.SKY);
            Text.fontFamily('serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('范围');
            Text.debugLine("entry/src/main/ets/pages/Index.ets(251:9)", "entry");
            Text.fontSize(13);
            Text.fontColor(KikariaColors.SOFT_TEXT);
        }, Text);
        Text.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.debugLine("entry/src/main/ets/pages/Index.ets(262:7)", "entry");
            Row.width(1);
            Row.height(40);
            Row.backgroundColor(`${KikariaColors.BLUE_GRAY_LIGHT}1E`);
        }, Row);
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 6 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(267:7)", "entry");
            Column.layoutWeight(1);
            Column.padding(16);
            Column.alignItems(HorizontalAlign.Center);
            Column.onClick(() => {
                navPathStack.pushPathByName('ReinforcementPage', undefined);
            });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(`${this.reinforcedCount}`);
            Text.debugLine("entry/src/main/ets/pages/Index.ets(268:9)", "entry");
            Text.fontSize(26);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.NEXT_AMBER);
            Text.fontFamily('serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('重点集锦');
            Text.debugLine("entry/src/main/ets/pages/Index.ets(274:9)", "entry");
            Text.fontSize(13);
            Text.fontColor(KikariaColors.SOFT_TEXT);
        }, Text);
        Text.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.debugLine("entry/src/main/ets/pages/Index.ets(285:7)", "entry");
            Row.width(1);
            Row.height(40);
            Row.backgroundColor(`${KikariaColors.BLUE_GRAY_LIGHT}1E`);
        }, Row);
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 6 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(290:7)", "entry");
            Column.layoutWeight(1);
            Column.padding(16);
            Column.alignItems(HorizontalAlign.Center);
            Column.onClick(() => {
                navPathStack.pushPathByName('MasteredPage', undefined);
            });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(`${this.masteredCount}`);
            Text.debugLine("entry/src/main/ets/pages/Index.ets(291:9)", "entry");
            Text.fontSize(26);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.MASTERED_GREEN);
            Text.fontFamily('serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('已掌握');
            Text.debugLine("entry/src/main/ets/pages/Index.ets(297:9)", "entry");
            Text.fontSize(13);
            Text.fontColor(KikariaColors.SOFT_TEXT);
        }, Text);
        Text.pop();
        Column.pop();
        Row.pop();
    }
    /* ── Preset selector card ── */
    presetSelectorCard(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 8 });
            Row.debugLine("entry/src/main/ets/pages/Index.ets(318:5)", "entry");
            Row.width('100%');
            Row.padding(18);
            Row.borderRadius(25);
            Row.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
            Row.backdropBlur(20);
            Row.shadow({ radius: 17, color: KikariaColors.SHADOW_COLOR, offsetY: 9 });
            Row.onClick(() => {
                navPathStack.pushPathByName('PresetSelectionPage', undefined);
            });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.presetName);
            Text.debugLine("entry/src/main/ets/pages/Index.ets(319:7)", "entry");
            Text.fontSize(16);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.maxLines(1);
            Text.layoutWeight(1);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(`${this.totalCount} 条`);
            Text.debugLine("entry/src/main/ets/pages/Index.ets(326:7)", "entry");
            Text.fontSize(13);
            Text.fontColor(KikariaColors.SOFT_TEXT);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('›');
            Text.debugLine("entry/src/main/ets/pages/Index.ets(330:7)", "entry");
            Text.fontSize(16);
            Text.fontColor(KikariaColors.TERTIARY_TEXT);
        }, Text);
        Text.pop();
        Row.pop();
    }
    /* ── Tag filter section ── */
    tagFilters(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 10 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(348:5)", "entry");
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('按标签筛选');
            Text.debugLine("entry/src/main/ets/pages/Index.ets(349:7)", "entry");
            Text.fontSize(15);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.allTags.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Flex.create({ wrap: FlexWrap.Wrap, justifyContent: FlexAlign.Start });
                        Flex.debugLine("entry/src/main/ets/pages/Index.ets(356:9)", "entry");
                        Flex.width('100%');
                    }, Flex);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        ForEach.create();
                        const forEachItemGenFunction = _item => {
                            const tag = _item;
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(tag);
                                Text.debugLine("entry/src/main/ets/pages/Index.ets(358:13)", "entry");
                                Text.fontSize(12);
                                Text.fontWeight(FontWeight.Medium);
                                Text.fontColor(this.selectedTags.has(tag) ? '#FFFFFF' : KikariaColors.SOFT_TEXT);
                                Text.backgroundColor(this.selectedTags.has(tag) ? KikariaColors.SKY : KikariaColors.MIST);
                                Text.borderRadius(12);
                                Text.padding({ left: 12, right: 12, top: 6, bottom: 6 });
                                Text.margin({ right: 8, bottom: 8 });
                                Text.onClick(() => {
                                    appState.toggleTag(tag);
                                    this.selectedTags = new Set(appState.selectedTags);
                                    this.refreshState();
                                });
                            }, Text);
                            Text.pop();
                        };
                        this.forEachUpdateFunction(elmtId, this.allTags.slice(0, 12), forEachItemGenFunction);
                    }, ForEach);
                    ForEach.pop();
                    Flex.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        Column.pop();
    }
    /* ── Reinforcement / Mastered quick-review rows ── */
    quickReviewRows(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 10 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(382:5)", "entry");
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.reinforcedCount > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create();
                        Row.debugLine("entry/src/main/ets/pages/Index.ets(384:9)", "entry");
                        Row.width('100%');
                        Row.height(44);
                        Row.justifyContent(FlexAlign.Center);
                        Row.borderRadius(14);
                        Row.backgroundColor(`${KikariaColors.NEXT_AMBER}14`);
                        Row.border({ width: 1, color: `${KikariaColors.NEXT_AMBER}33` });
                        Row.onClick(() => {
                            this.startReinforcementReview();
                        });
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(`★ 复习重点 (${this.reinforcedCount})`);
                        Text.debugLine("entry/src/main/ets/pages/Index.ets(385:11)", "entry");
                        Text.fontSize(15);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(KikariaColors.NEXT_AMBER);
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
            if (this.masteredCount > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create();
                        Row.debugLine("entry/src/main/ets/pages/Index.ets(402:9)", "entry");
                        Row.width('100%');
                        Row.height(44);
                        Row.justifyContent(FlexAlign.Center);
                        Row.borderRadius(14);
                        Row.backgroundColor(`${KikariaColors.MASTERED_GREEN}14`);
                        Row.border({ width: 1, color: `${KikariaColors.MASTERED_GREEN}33` });
                        Row.onClick(() => {
                            this.startMasteredReview();
                        });
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(`✓ 浏览已掌握 (${this.masteredCount})`);
                        Text.debugLine("entry/src/main/ets/pages/Index.ets(403:11)", "entry");
                        Text.fontSize(15);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(KikariaColors.MASTERED_GREEN);
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
        Column.pop();
    }
    /* ── Left column (compact layout) ── */
    compactLeftColumn(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 18 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(425:5)", "entry");
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Bubble centered
            Column.create();
            Column.debugLine("entry/src/main/ets/pages/Index.ets(427:7)", "entry");
            // Bubble centered
            Column.width('100%');
            // Bubble centered
            Column.alignItems(HorizontalAlign.Center);
            // Bubble centered
            Column.padding({ top: 16, bottom: 16 });
        }, Column);
        this.reviewBubble.bind(this)(this.bubbleScale);
        // Bubble centered
        Column.pop();
        this.progressCard.bind(this)();
        this.dashboardGrid.bind(this)();
        this.presetSelectorCard.bind(this)();
        this.tagFilters.bind(this)();
        this.quickReviewRows.bind(this)();
        Column.pop();
    }
    /* ── Left column (landscape layout) ── */
    landscapeLeftColumn(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 18 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(445:5)", "entry");
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.debugLine("entry/src/main/ets/pages/Index.ets(446:7)", "entry");
            Column.width('100%');
            Column.alignItems(HorizontalAlign.Center);
            Column.padding({ top: 12, bottom: 12 });
        }, Column);
        this.reviewBubble.bind(this)(this.bubbleScale * homeLandscapeBubbleScale());
        Column.pop();
        this.progressCard.bind(this)();
        this.dashboardGrid.bind(this)();
        Column.pop();
    }
    /* ── Right column (landscape layout) ── */
    landscapeRightColumn(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 18 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(461:5)", "entry");
        }, Column);
        this.presetSelectorCard.bind(this)();
        this.tagFilters.bind(this)();
        this.quickReviewRows.bind(this)();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.debugLine("entry/src/main/ets/pages/Index.ets(465:7)", "entry");
            Row.height(20);
        }, Row);
        Row.pop();
        Column.pop();
    }
    /* ── Two-column landscape layout ── */
    twoColumnLayout(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.debugLine("entry/src/main/ets/pages/Index.ets(472:5)", "entry");
        }, Column);
        this.headerBar.bind(this)();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: homeLandscapeColumnSpacing() });
            Row.debugLine("entry/src/main/ets/pages/Index.ets(475:7)", "entry");
            Row.width('100%');
            Row.constraintSize({ maxWidth: homeLandscapeAvailableWidth() });
            Row.justifyContent(FlexAlign.Center);
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.debugLine("entry/src/main/ets/pages/Index.ets(476:9)", "entry");
            Column.width(homeLandscapeLeftWidth());
        }, Column);
        this.landscapeLeftColumn.bind(this)();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.debugLine("entry/src/main/ets/pages/Index.ets(481:9)", "entry");
            Column.width(homeLandscapeRightWidth());
            Column.alignItems(HorizontalAlign.Start);
        }, Column);
        this.landscapeRightColumn.bind(this)();
        Column.pop();
        Row.pop();
        Column.pop();
    }
    /* ── iPad portrait layout ── */
    padPortraitLayout(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 0 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(496:5)", "entry");
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.debugLine("entry/src/main/ets/pages/Index.ets(497:7)", "entry");
            Row.width('100%');
            Row.padding({ top: padPortraitHomeTopPadding() });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('Kikaria');
            Text.debugLine("entry/src/main/ets/pages/Index.ets(498:9)", "entry");
            Text.fontSize(padPortraitHomeTitleSize());
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.fontFamily('serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
            Blank.debugLine("entry/src/main/ets/pages/Index.ets(504:9)", "entry");
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.debugLine("entry/src/main/ets/pages/Index.ets(506:9)", "entry");
            Row.width(padPortraitHomeAvatarSize());
            Row.height(padPortraitHomeAvatarSize());
            Row.borderRadius(padPortraitHomeAvatarSize() / 2);
            Row.backgroundColor(KikariaColors.SKY);
            Row.justifyContent(FlexAlign.Center);
            Row.onClick(() => { navPathStack.pushPathByName('SettingsPage', undefined); });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.displayName.charAt(0).toUpperCase());
            Text.debugLine("entry/src/main/ets/pages/Index.ets(507:11)", "entry");
            Text.fontSize(Math.round(padPortraitHomeAvatarSize() * 0.35));
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor('#FFFFFF');
        }, Text);
        Text.pop();
        Row.pop();
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.debugLine("entry/src/main/ets/pages/Index.ets(522:7)", "entry");
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.debugLine("entry/src/main/ets/pages/Index.ets(523:9)", "entry");
            Column.width('100%');
            Column.layoutWeight(1);
            Column.alignItems(HorizontalAlign.Center);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
            Blank.debugLine("entry/src/main/ets/pages/Index.ets(524:11)", "entry");
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 10 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(525:11)", "entry");
            globalThis.Context.animation({ duration: 5400, curve: Curve.EaseInOut, iterations: -1, playMode: PlayMode.Alternate });
            Column.width(padPortraitHomeBubbleSize());
            Column.height(padPortraitHomeBubbleSize());
            Column.borderRadius(padPortraitHomeBubbleSize() / 2);
            Column.justifyContent(FlexAlign.Center);
            Column.scale({ x: this.bubbleScale, y: this.bubbleScale });
            Column.linearGradient({
                angle: 135,
                colors: [[KikariaColors.SKY, 0], [KikariaColors.CYAN, 1]]
            });
            Column.shadow({ radius: 28, color: `${KikariaColors.SKY}48`, offsetY: 18 });
            globalThis.Context.animation(null);
            Column.onClick(() => { this.startNormalReview(); });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('开始背诵');
            Text.debugLine("entry/src/main/ets/pages/Index.ets(526:13)", "entry");
            Text.fontSize(padPortraitHomeBubbleFontSize());
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor('#FFFFFF');
            Text.fontFamily('serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('→');
            Text.debugLine("entry/src/main/ets/pages/Index.ets(532:13)", "entry");
            Text.fontSize(72);
            Text.fontWeight(FontWeight.Lighter);
            Text.fontColor('#FFFFFF');
            Text.opacity(0.9);
        }, Text);
        Text.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
            Blank.debugLine("entry/src/main/ets/pages/Index.ets(550:11)", "entry");
        }, Blank);
        Blank.pop();
        Column.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 18 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(558:7)", "entry");
            Column.width('100%');
        }, Column);
        this.progressCard.bind(this)();
        this.dashboardGrid.bind(this)();
        this.presetSelectorCard.bind(this)();
        this.tagFilters.bind(this)();
        this.quickReviewRows.bind(this)();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.debugLine("entry/src/main/ets/pages/Index.ets(564:9)", "entry");
            Row.height(40);
        }, Row);
        Row.pop();
        Column.pop();
        Column.pop();
    }
    /* ── Single-column layout (compact / portrait tablet) ── */
    singleColumnLayout(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 18 });
            Column.debugLine("entry/src/main/ets/pages/Index.ets(573:5)", "entry");
            Column.constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' });
        }, Column);
        this.headerBar.bind(this)();
        this.compactLeftColumn.bind(this)();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.debugLine("entry/src/main/ets/pages/Index.ets(576:7)", "entry");
            Row.height(40);
        }, Row);
        Row.pop();
        Column.pop();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/Index", isUserCreateStack: false });
            Navigation.debugLine("entry/src/main/ets/pages/Index.ets(582:5)", "entry");
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
            Navigation.navDestination({ builder: this.navDestinationRouter.bind(this) });
        }, Navigation);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Scroll.create();
            Scroll.debugLine("entry/src/main/ets/pages/Index.ets(583:7)", "entry");
            Scroll.width('100%');
            Scroll.height('100%');
            Scroll.backgroundColor(KikariaColors.PAGE_BG);
            Scroll.scrollBar(BarState.Off);
            Scroll.align(Alignment.TopStart);
        }, Scroll);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.debugLine("entry/src/main/ets/pages/Index.ets(584:9)", "entry");
            Column.width('100%');
            Column.padding({ left: this.contentPadH, right: this.contentPadH });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.useTwoColumn) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.twoColumnLayout.bind(this)();
                });
            }
            else if (isPadPortrait()) {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.padPortraitLayout.bind(this)();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(2, () => {
                    this.singleColumnLayout.bind(this)();
                });
            }
        }, If);
        If.pop();
        Column.pop();
        Scroll.pop();
        Navigation.pop();
    }
    startNormalReview(): void {
        appState.startReview(ReviewMode.NORMAL);
        navPathStack.pushPathByName('ReviewPage', undefined);
    }
    startReinforcementReview(): void {
        appState.startReview(ReviewMode.REINFORCEMENT);
        navPathStack.pushPathByName('ReviewPage', undefined);
    }
    startMasteredReview(): void {
        appState.startReview(ReviewMode.MASTERED);
        navPathStack.pushPathByName('ReviewPage', undefined);
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "Index";
    }
}
registerNamedRoute(() => new Index(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/Index", pageFullPath: "entry/src/main/ets/pages/Index", integratedHsp: "false", moduleType: "followWithHap" });
