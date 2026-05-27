if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface SettingsPage_Params {
}
interface SettingsPageContent_Params {
    isDark?: boolean;
    displayName?: string;
    userHandle?: string;
    presetName?: string;
    dailyGoal?: number;
    dangerPercent?: number;
    notificationsEnabled?: boolean;
    masteredCount?: number;
    totalCount?: number;
    countdownEndDateStr?: string;
    notificationTimeStr?: string;
    showCountdownPicker?: boolean;
    showNotificationPicker?: boolean;
    showDangerPicker?: boolean;
    showGoalPicker?: boolean;
    pickerEndYear?: number;
    pickerEndMonth?: number;
    pickerEndDay?: number;
    pickerHour?: number;
    pickerMinute?: number;
    contentPadH?: number;
    dangerValue?: number;
    useTwoColumn?: boolean;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { SettingsSectionCard, SettingsSectionDivider, SettingsListRow, SettingsToggleRow, SettingsInfoTextRow, SettingsStepperRow } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaComponents";
import { isPadLandscape, isTwoColumnCapable, pageHorizontalPadding, ipadPortraitSettingsTopInset, settingsLandscapeLeftWidth, settingsLandscapeRightWidth, settingsLandscapeColumnSpacing, settingsLandscapeMaxWidth } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { notificationService } from "@bundle:com.vita0818.kikaria/entry/ets/data/NotificationService";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function SettingsPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new SettingsPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 22, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "SettingsPageContent" });
    }
}
export class SettingsPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__isDark = this.createStorageLink('kikaria_isDarkMode', false, "isDark");
        this.__displayName = new ObservedPropertySimplePU('', this, "displayName");
        this.__userHandle = new ObservedPropertySimplePU('', this, "userHandle");
        this.__presetName = new ObservedPropertySimplePU('', this, "presetName");
        this.__dailyGoal = new ObservedPropertySimplePU(20, this, "dailyGoal");
        this.__dangerPercent = new ObservedPropertySimplePU(80, this, "dangerPercent");
        this.__notificationsEnabled = new ObservedPropertySimplePU(false, this, "notificationsEnabled");
        this.__masteredCount = new ObservedPropertySimplePU(0, this, "masteredCount");
        this.__totalCount = new ObservedPropertySimplePU(0, this, "totalCount");
        this.__countdownEndDateStr = new ObservedPropertySimplePU('未设置', this, "countdownEndDateStr");
        this.__notificationTimeStr = new ObservedPropertySimplePU('21:00', this, "notificationTimeStr");
        this.__showCountdownPicker = new ObservedPropertySimplePU(false, this, "showCountdownPicker");
        this.__showNotificationPicker = new ObservedPropertySimplePU(false, this, "showNotificationPicker");
        this.__showDangerPicker = new ObservedPropertySimplePU(false, this, "showDangerPicker");
        this.__showGoalPicker = new ObservedPropertySimplePU(false, this, "showGoalPicker");
        this.__pickerEndYear = new ObservedPropertySimplePU(2026, this, "pickerEndYear");
        this.__pickerEndMonth = new ObservedPropertySimplePU(6, this, "pickerEndMonth");
        this.__pickerEndDay = new ObservedPropertySimplePU(15, this, "pickerEndDay");
        this.__pickerHour = new ObservedPropertySimplePU(21, this, "pickerHour");
        this.__pickerMinute = new ObservedPropertySimplePU(0, this, "pickerMinute");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.__dangerValue = new ObservedPropertySimplePU(80, this, "dangerValue");
        this.__useTwoColumn = new ObservedPropertySimplePU(false, this, "useTwoColumn");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: SettingsPageContent_Params) {
        if (params.displayName !== undefined) {
            this.displayName = params.displayName;
        }
        if (params.userHandle !== undefined) {
            this.userHandle = params.userHandle;
        }
        if (params.presetName !== undefined) {
            this.presetName = params.presetName;
        }
        if (params.dailyGoal !== undefined) {
            this.dailyGoal = params.dailyGoal;
        }
        if (params.dangerPercent !== undefined) {
            this.dangerPercent = params.dangerPercent;
        }
        if (params.notificationsEnabled !== undefined) {
            this.notificationsEnabled = params.notificationsEnabled;
        }
        if (params.masteredCount !== undefined) {
            this.masteredCount = params.masteredCount;
        }
        if (params.totalCount !== undefined) {
            this.totalCount = params.totalCount;
        }
        if (params.countdownEndDateStr !== undefined) {
            this.countdownEndDateStr = params.countdownEndDateStr;
        }
        if (params.notificationTimeStr !== undefined) {
            this.notificationTimeStr = params.notificationTimeStr;
        }
        if (params.showCountdownPicker !== undefined) {
            this.showCountdownPicker = params.showCountdownPicker;
        }
        if (params.showNotificationPicker !== undefined) {
            this.showNotificationPicker = params.showNotificationPicker;
        }
        if (params.showDangerPicker !== undefined) {
            this.showDangerPicker = params.showDangerPicker;
        }
        if (params.showGoalPicker !== undefined) {
            this.showGoalPicker = params.showGoalPicker;
        }
        if (params.pickerEndYear !== undefined) {
            this.pickerEndYear = params.pickerEndYear;
        }
        if (params.pickerEndMonth !== undefined) {
            this.pickerEndMonth = params.pickerEndMonth;
        }
        if (params.pickerEndDay !== undefined) {
            this.pickerEndDay = params.pickerEndDay;
        }
        if (params.pickerHour !== undefined) {
            this.pickerHour = params.pickerHour;
        }
        if (params.pickerMinute !== undefined) {
            this.pickerMinute = params.pickerMinute;
        }
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
        if (params.dangerValue !== undefined) {
            this.dangerValue = params.dangerValue;
        }
        if (params.useTwoColumn !== undefined) {
            this.useTwoColumn = params.useTwoColumn;
        }
    }
    updateStateVars(params: SettingsPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__isDark.purgeDependencyOnElmtId(rmElmtId);
        this.__displayName.purgeDependencyOnElmtId(rmElmtId);
        this.__userHandle.purgeDependencyOnElmtId(rmElmtId);
        this.__presetName.purgeDependencyOnElmtId(rmElmtId);
        this.__dailyGoal.purgeDependencyOnElmtId(rmElmtId);
        this.__dangerPercent.purgeDependencyOnElmtId(rmElmtId);
        this.__notificationsEnabled.purgeDependencyOnElmtId(rmElmtId);
        this.__masteredCount.purgeDependencyOnElmtId(rmElmtId);
        this.__totalCount.purgeDependencyOnElmtId(rmElmtId);
        this.__countdownEndDateStr.purgeDependencyOnElmtId(rmElmtId);
        this.__notificationTimeStr.purgeDependencyOnElmtId(rmElmtId);
        this.__showCountdownPicker.purgeDependencyOnElmtId(rmElmtId);
        this.__showNotificationPicker.purgeDependencyOnElmtId(rmElmtId);
        this.__showDangerPicker.purgeDependencyOnElmtId(rmElmtId);
        this.__showGoalPicker.purgeDependencyOnElmtId(rmElmtId);
        this.__pickerEndYear.purgeDependencyOnElmtId(rmElmtId);
        this.__pickerEndMonth.purgeDependencyOnElmtId(rmElmtId);
        this.__pickerEndDay.purgeDependencyOnElmtId(rmElmtId);
        this.__pickerHour.purgeDependencyOnElmtId(rmElmtId);
        this.__pickerMinute.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
        this.__dangerValue.purgeDependencyOnElmtId(rmElmtId);
        this.__useTwoColumn.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__isDark.aboutToBeDeleted();
        this.__displayName.aboutToBeDeleted();
        this.__userHandle.aboutToBeDeleted();
        this.__presetName.aboutToBeDeleted();
        this.__dailyGoal.aboutToBeDeleted();
        this.__dangerPercent.aboutToBeDeleted();
        this.__notificationsEnabled.aboutToBeDeleted();
        this.__masteredCount.aboutToBeDeleted();
        this.__totalCount.aboutToBeDeleted();
        this.__countdownEndDateStr.aboutToBeDeleted();
        this.__notificationTimeStr.aboutToBeDeleted();
        this.__showCountdownPicker.aboutToBeDeleted();
        this.__showNotificationPicker.aboutToBeDeleted();
        this.__showDangerPicker.aboutToBeDeleted();
        this.__showGoalPicker.aboutToBeDeleted();
        this.__pickerEndYear.aboutToBeDeleted();
        this.__pickerEndMonth.aboutToBeDeleted();
        this.__pickerEndDay.aboutToBeDeleted();
        this.__pickerHour.aboutToBeDeleted();
        this.__pickerMinute.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        this.__dangerValue.aboutToBeDeleted();
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
    private __displayName: ObservedPropertySimplePU<string>;
    get displayName() {
        return this.__displayName.get();
    }
    set displayName(newValue: string) {
        this.__displayName.set(newValue);
    }
    private __userHandle: ObservedPropertySimplePU<string>;
    get userHandle() {
        return this.__userHandle.get();
    }
    set userHandle(newValue: string) {
        this.__userHandle.set(newValue);
    }
    private __presetName: ObservedPropertySimplePU<string>;
    get presetName() {
        return this.__presetName.get();
    }
    set presetName(newValue: string) {
        this.__presetName.set(newValue);
    }
    private __dailyGoal: ObservedPropertySimplePU<number>;
    get dailyGoal() {
        return this.__dailyGoal.get();
    }
    set dailyGoal(newValue: number) {
        this.__dailyGoal.set(newValue);
    }
    private __dangerPercent: ObservedPropertySimplePU<number>;
    get dangerPercent() {
        return this.__dangerPercent.get();
    }
    set dangerPercent(newValue: number) {
        this.__dangerPercent.set(newValue);
    }
    private __notificationsEnabled: ObservedPropertySimplePU<boolean>;
    get notificationsEnabled() {
        return this.__notificationsEnabled.get();
    }
    set notificationsEnabled(newValue: boolean) {
        this.__notificationsEnabled.set(newValue);
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
    private __countdownEndDateStr: ObservedPropertySimplePU<string>;
    get countdownEndDateStr() {
        return this.__countdownEndDateStr.get();
    }
    set countdownEndDateStr(newValue: string) {
        this.__countdownEndDateStr.set(newValue);
    }
    private __notificationTimeStr: ObservedPropertySimplePU<string>;
    get notificationTimeStr() {
        return this.__notificationTimeStr.get();
    }
    set notificationTimeStr(newValue: string) {
        this.__notificationTimeStr.set(newValue);
    }
    private __showCountdownPicker: ObservedPropertySimplePU<boolean>;
    get showCountdownPicker() {
        return this.__showCountdownPicker.get();
    }
    set showCountdownPicker(newValue: boolean) {
        this.__showCountdownPicker.set(newValue);
    }
    private __showNotificationPicker: ObservedPropertySimplePU<boolean>;
    get showNotificationPicker() {
        return this.__showNotificationPicker.get();
    }
    set showNotificationPicker(newValue: boolean) {
        this.__showNotificationPicker.set(newValue);
    }
    private __showDangerPicker: ObservedPropertySimplePU<boolean>;
    get showDangerPicker() {
        return this.__showDangerPicker.get();
    }
    set showDangerPicker(newValue: boolean) {
        this.__showDangerPicker.set(newValue);
    }
    private __showGoalPicker: ObservedPropertySimplePU<boolean>;
    get showGoalPicker() {
        return this.__showGoalPicker.get();
    }
    set showGoalPicker(newValue: boolean) {
        this.__showGoalPicker.set(newValue);
    }
    private __pickerEndYear: ObservedPropertySimplePU<number>;
    get pickerEndYear() {
        return this.__pickerEndYear.get();
    }
    set pickerEndYear(newValue: number) {
        this.__pickerEndYear.set(newValue);
    }
    private __pickerEndMonth: ObservedPropertySimplePU<number>;
    get pickerEndMonth() {
        return this.__pickerEndMonth.get();
    }
    set pickerEndMonth(newValue: number) {
        this.__pickerEndMonth.set(newValue);
    }
    private __pickerEndDay: ObservedPropertySimplePU<number>;
    get pickerEndDay() {
        return this.__pickerEndDay.get();
    }
    set pickerEndDay(newValue: number) {
        this.__pickerEndDay.set(newValue);
    }
    private __pickerHour: ObservedPropertySimplePU<number>;
    get pickerHour() {
        return this.__pickerHour.get();
    }
    set pickerHour(newValue: number) {
        this.__pickerHour.set(newValue);
    }
    private __pickerMinute: ObservedPropertySimplePU<number>;
    get pickerMinute() {
        return this.__pickerMinute.get();
    }
    set pickerMinute(newValue: number) {
        this.__pickerMinute.set(newValue);
    }
    private __contentPadH: ObservedPropertySimplePU<number>;
    get contentPadH() {
        return this.__contentPadH.get();
    }
    set contentPadH(newValue: number) {
        this.__contentPadH.set(newValue);
    }
    private __dangerValue: ObservedPropertySimplePU<number>;
    get dangerValue() {
        return this.__dangerValue.get();
    }
    set dangerValue(newValue: number) {
        this.__dangerValue.set(newValue);
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
    }
    refreshState(): void {
        this.displayName = appState.userProfile.displayName;
        this.userHandle = appState.userProfile.userHandle;
        this.presetName = appState.currentPreset.name;
        this.dailyGoal = appState.dailyGoal;
        this.dangerPercent = appState.dangerPercent;
        this.dangerValue = appState.dangerPercent;
        this.notificationsEnabled = appState.notificationsEnabled;
        this.masteredCount = appState.masteredCount;
        this.totalCount = appState.totalCount;
        if (appState.countdownEndDate !== null) {
            const d = new Date(appState.countdownEndDate);
            this.countdownEndDateStr = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
            this.pickerEndYear = d.getFullYear();
            this.pickerEndMonth = d.getMonth() + 1;
            this.pickerEndDay = d.getDate();
        }
        else {
            this.countdownEndDateStr = '未设置';
        }
        const nt = new Date(appState.notificationTime);
        this.notificationTimeStr = `${String(nt.getHours()).padStart(2, '0')}:${String(nt.getMinutes()).padStart(2, '0')}`;
        this.pickerHour = nt.getHours();
        this.pickerMinute = nt.getMinutes();
    }
    handleNotificationToggle(value: boolean): void {
        if (value) {
            notificationService.requestPermission().then((granted: boolean) => {
                if (granted) {
                    this.notificationsEnabled = true;
                    appState.updateNotificationsEnabled(true);
                    notificationService.scheduleReminder();
                }
                else {
                    this.notificationsEnabled = false;
                    appState.updateNotificationsEnabled(false);
                }
            });
        }
        else {
            this.notificationsEnabled = false;
            appState.updateNotificationsEnabled(false);
            notificationService.cancelReminder();
        }
    }
    /* ── Section header label ── */
    sectionHeader(label: string, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(label);
            Text.fontSize(13);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(KikariaColors.SOFT_TEXT);
            Text.width('100%');
            Text.padding({ left: 4, bottom: 2 });
        }, Text);
        Text.pop();
    }
    /* ── Profile row ── */
    profileRow(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 12 });
            Row.width('100%');
            Row.padding(16);
            Row.onClick(() => {
                navPathStack.pushPathByName('EditProfilePage', undefined);
            });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width(56);
            Row.height(56);
            Row.borderRadius(28);
            Row.backgroundColor(KikariaColors.SKY);
            Row.justifyContent(FlexAlign.Center);
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.displayName.charAt(0).toUpperCase());
            Text.fontSize(28);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor('#FFFFFF');
        }, Text);
        Text.pop();
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 4 });
            Column.alignItems(HorizontalAlign.Start);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.displayName);
            Text.fontSize(17);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(KikariaColors.DEEP_TEXT);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(`@${this.userHandle}`);
            Text.fontSize(13);
            Text.fontColor(KikariaColors.SOFT_TEXT);
        }, Text);
        Text.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('编辑 ›');
            Text.fontSize(14);
            Text.fontColor(KikariaColors.SKY);
        }, Text);
        Text.pop();
        Row.pop();
    }
    /* ── Landscape profile summary (left column) ── */
    landscapeProfileSummary(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 16 });
            Column.width('100%');
            Column.alignItems(HorizontalAlign.Center);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width(72);
            Row.height(72);
            Row.borderRadius(36);
            Row.backgroundColor(KikariaColors.SKY);
            Row.justifyContent(FlexAlign.Center);
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.displayName.charAt(0).toUpperCase());
            Text.fontSize(42);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor('#FFFFFF');
        }, Text);
        Text.pop();
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 4 });
            Column.alignItems(HorizontalAlign.Center);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.displayName);
            Text.fontSize(24);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.fontFamily('serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(`@${this.userHandle}`);
            Text.fontSize(15);
            Text.fontColor(KikariaColors.SOFT_TEXT);
        }, Text);
        Text.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithLabel('编辑个人资料');
            Button.fontSize(16);
            Button.fontWeight(FontWeight.Medium);
            Button.fontColor(KikariaColors.DEEP_TEXT);
            Button.borderRadius(999);
            Button.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
            Button.backdropBlur(14);
            Button.height(44);
            Button.padding({ left: 24, right: 24 });
            Button.onClick(() => {
                navPathStack.pushPathByName('EditProfilePage', undefined);
            });
        }, Button);
        Button.pop();
        Column.pop();
    }
    /* ── Overlay backdrop ── */
    overlayBackdrop(onDismiss: () => void, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.height('100%');
            Column.backgroundColor(KikariaColors.OVERLAY_BG);
            Column.onClick(() => onDismiss());
        }, Column);
        Column.pop();
    }
    /* ── Picker button row ── */
    pickerButtonRow(onCancel: () => void, onConfirm: () => void, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 12 });
            Row.width('100%');
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithLabel('取消');
            Button.fontSize(15);
            Button.fontWeight(FontWeight.Medium);
            Button.fontColor(KikariaColors.SOFT_TEXT);
            Button.backgroundColor(KikariaColors.MIST);
            Button.borderRadius(12);
            Button.height(42);
            Button.layoutWeight(1);
            Button.onClick(() => onCancel());
        }, Button);
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithLabel('确定');
            Button.fontSize(15);
            Button.fontWeight(FontWeight.Bold);
            Button.fontColor('#FFFFFF');
            Button.backgroundColor(KikariaColors.SKY);
            Button.borderRadius(12);
            Button.height(42);
            Button.layoutWeight(1);
            Button.onClick(() => onConfirm());
        }, Button);
        Button.pop();
        Row.pop();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            NavDestination.create(() => {
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Stack.create();
                }, Stack);
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
                    // Top bar
                    Row.create();
                    // Top bar
                    Row.width('100%');
                    // Top bar
                    Row.padding({ left: 20, right: 20, top: 12 + ipadPortraitSettingsTopInset(), bottom: 12 });
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
                    Text.create('设置');
                    Text.fontSize(17);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.DEEP_TEXT);
                }, Text);
                Text.pop();
                // Top bar
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (this.useTwoColumn) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Row.create({ space: settingsLandscapeColumnSpacing() });
                                Row.width('100%');
                                Row.constraintSize({ maxWidth: settingsLandscapeMaxWidth() });
                            }, Row);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create();
                                Column.width(settingsLandscapeLeftWidth());
                                Column.alignItems(HorizontalAlign.Center);
                                Column.justifyContent(FlexAlign.Center);
                            }, Column);
                            this.landscapeProfileSummary.bind(this)();
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 14 });
                                Column.width(settingsLandscapeRightWidth());
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── Daily Goal ──
                                Column.create({ space: 4 });
                                // ── Daily Goal ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('每日目标');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsStepperRow(this, {
                                                                label: '每日掌握数量', value: this.dailyGoal, min: 1, max: 200,
                                                                valueColor: KikariaColors.DEEP_TEXT,
                                                                onDecrement: () => { if (this.dailyGoal > 1) {
                                                                    this.dailyGoal--;
                                                                    appState.updateDailyGoal(this.dailyGoal);
                                                                } },
                                                                onIncrement: () => { if (this.dailyGoal < 200) {
                                                                    this.dailyGoal++;
                                                                    appState.updateDailyGoal(this.dailyGoal);
                                                                } }
                                                            }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 277, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '每日掌握数量',
                                                                    value: this.dailyGoal,
                                                                    min: 1,
                                                                    max: 200,
                                                                    valueColor: KikariaColors.DEEP_TEXT,
                                                                    onDecrement: () => { if (this.dailyGoal > 1) {
                                                                        this.dailyGoal--;
                                                                        appState.updateDailyGoal(this.dailyGoal);
                                                                    } },
                                                                    onIncrement: () => { if (this.dailyGoal < 200) {
                                                                        this.dailyGoal++;
                                                                        appState.updateDailyGoal(this.dailyGoal);
                                                                    } }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '每日掌握数量', value: this.dailyGoal, min: 1, max: 200,
                                                                valueColor: KikariaColors.DEEP_TEXT
                                                            });
                                                        }
                                                    }, { name: "SettingsStepperRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 283, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsInfoTextRow(this, { text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}` }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 284, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}`
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}`
                                                            });
                                                        }
                                                    }, { name: "SettingsInfoTextRow" });
                                                }
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 276, col: 19 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsStepperRow(this, {
                                                                    label: '每日掌握数量', value: this.dailyGoal, min: 1, max: 200,
                                                                    valueColor: KikariaColors.DEEP_TEXT,
                                                                    onDecrement: () => { if (this.dailyGoal > 1) {
                                                                        this.dailyGoal--;
                                                                        appState.updateDailyGoal(this.dailyGoal);
                                                                    } },
                                                                    onIncrement: () => { if (this.dailyGoal < 200) {
                                                                        this.dailyGoal++;
                                                                        appState.updateDailyGoal(this.dailyGoal);
                                                                    } }
                                                                }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 277, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '每日掌握数量',
                                                                        value: this.dailyGoal,
                                                                        min: 1,
                                                                        max: 200,
                                                                        valueColor: KikariaColors.DEEP_TEXT,
                                                                        onDecrement: () => { if (this.dailyGoal > 1) {
                                                                            this.dailyGoal--;
                                                                            appState.updateDailyGoal(this.dailyGoal);
                                                                        } },
                                                                        onIncrement: () => { if (this.dailyGoal < 200) {
                                                                            this.dailyGoal++;
                                                                            appState.updateDailyGoal(this.dailyGoal);
                                                                        } }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '每日掌握数量', value: this.dailyGoal, min: 1, max: 200,
                                                                    valueColor: KikariaColors.DEEP_TEXT
                                                                });
                                                            }
                                                        }, { name: "SettingsStepperRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 283, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsInfoTextRow(this, { text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}` }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 284, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}`
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}`
                                                                });
                                                            }
                                                        }, { name: "SettingsInfoTextRow" });
                                                    }
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── Daily Goal ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── Danger ──
                                Column.create({ space: 4 });
                                // ── Danger ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('预警阈值');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsStepperRow(this, {
                                                                label: '进度低于预期时提醒', value: this.dangerPercent, min: 5, max: 100,
                                                                valueColor: KikariaColors.DEEP_TEXT,
                                                                onDecrement: () => { if (this.dangerPercent > 5) {
                                                                    this.dangerPercent -= 5;
                                                                    appState.updateDangerPercent(this.dangerPercent);
                                                                } },
                                                                onIncrement: () => { if (this.dangerPercent < 100) {
                                                                    this.dangerPercent += 5;
                                                                    appState.updateDangerPercent(this.dangerPercent);
                                                                } }
                                                            }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 292, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '进度低于预期时提醒',
                                                                    value: this.dangerPercent,
                                                                    min: 5,
                                                                    max: 100,
                                                                    valueColor: KikariaColors.DEEP_TEXT,
                                                                    onDecrement: () => { if (this.dangerPercent > 5) {
                                                                        this.dangerPercent -= 5;
                                                                        appState.updateDangerPercent(this.dangerPercent);
                                                                    } },
                                                                    onIncrement: () => { if (this.dangerPercent < 100) {
                                                                        this.dangerPercent += 5;
                                                                        appState.updateDangerPercent(this.dangerPercent);
                                                                    } }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '进度低于预期时提醒', value: this.dangerPercent, min: 5, max: 100,
                                                                valueColor: KikariaColors.DEEP_TEXT
                                                            });
                                                        }
                                                    }, { name: "SettingsStepperRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 298, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsInfoTextRow(this, { text: '当实际掌握进度低于预设的预期百分比时触发提醒。' }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 299, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    text: '当实际掌握进度低于预设的预期百分比时触发提醒。'
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                text: '当实际掌握进度低于预设的预期百分比时触发提醒。'
                                                            });
                                                        }
                                                    }, { name: "SettingsInfoTextRow" });
                                                }
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 291, col: 19 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsStepperRow(this, {
                                                                    label: '进度低于预期时提醒', value: this.dangerPercent, min: 5, max: 100,
                                                                    valueColor: KikariaColors.DEEP_TEXT,
                                                                    onDecrement: () => { if (this.dangerPercent > 5) {
                                                                        this.dangerPercent -= 5;
                                                                        appState.updateDangerPercent(this.dangerPercent);
                                                                    } },
                                                                    onIncrement: () => { if (this.dangerPercent < 100) {
                                                                        this.dangerPercent += 5;
                                                                        appState.updateDangerPercent(this.dangerPercent);
                                                                    } }
                                                                }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 292, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '进度低于预期时提醒',
                                                                        value: this.dangerPercent,
                                                                        min: 5,
                                                                        max: 100,
                                                                        valueColor: KikariaColors.DEEP_TEXT,
                                                                        onDecrement: () => { if (this.dangerPercent > 5) {
                                                                            this.dangerPercent -= 5;
                                                                            appState.updateDangerPercent(this.dangerPercent);
                                                                        } },
                                                                        onIncrement: () => { if (this.dangerPercent < 100) {
                                                                            this.dangerPercent += 5;
                                                                            appState.updateDangerPercent(this.dangerPercent);
                                                                        } }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '进度低于预期时提醒', value: this.dangerPercent, min: 5, max: 100,
                                                                    valueColor: KikariaColors.DEEP_TEXT
                                                                });
                                                            }
                                                        }, { name: "SettingsStepperRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 298, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsInfoTextRow(this, { text: '当实际掌握进度低于预设的预期百分比时触发提醒。' }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 299, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        text: '当实际掌握进度低于预设的预期百分比时触发提醒。'
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    text: '当实际掌握进度低于预设的预期百分比时触发提醒。'
                                                                });
                                                            }
                                                        }, { name: "SettingsInfoTextRow" });
                                                    }
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── Danger ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── Countdown ──
                                Column.create({ space: 4 });
                                // ── Countdown ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('倒计时');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsListRow(this, { label: '截止日期', value: this.countdownEndDateStr, onTap: () => { this.showCountdownPicker = true; } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 307, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '截止日期',
                                                                    value: this.countdownEndDateStr,
                                                                    onTap: () => { this.showCountdownPicker = true; }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '截止日期', value: this.countdownEndDateStr
                                                            });
                                                        }
                                                    }, { name: "SettingsListRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 308, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Row.create({ space: 8 });
                                                    Row.width('100%');
                                                    Row.padding({ left: 16, right: 16, top: 10, bottom: 14 });
                                                }, Row);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Button.createWithLabel('设置日期');
                                                    Button.fontSize(14);
                                                    Button.fontWeight(FontWeight.Medium);
                                                    Button.fontColor('#FFFFFF');
                                                    Button.backgroundColor(KikariaColors.SKY);
                                                    Button.borderRadius(12);
                                                    Button.height(36);
                                                    Button.padding({ left: 16, right: 16 });
                                                    Button.onClick(() => { this.showCountdownPicker = true; });
                                                }, Button);
                                                Button.pop();
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Button.createWithLabel('清除');
                                                    Button.fontSize(14);
                                                    Button.fontWeight(FontWeight.Medium);
                                                    Button.fontColor(KikariaColors.SOFT_TEXT);
                                                    Button.backgroundColor(KikariaColors.MIST);
                                                    Button.borderRadius(12);
                                                    Button.height(36);
                                                    Button.padding({ left: 16, right: 16 });
                                                    Button.onClick(() => { appState.updateCountdownRange(null, null); this.countdownEndDateStr = '未设置'; this.refreshState(); });
                                                }, Button);
                                                Button.pop();
                                                Row.pop();
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 306, col: 19 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsListRow(this, { label: '截止日期', value: this.countdownEndDateStr, onTap: () => { this.showCountdownPicker = true; } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 307, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '截止日期',
                                                                        value: this.countdownEndDateStr,
                                                                        onTap: () => { this.showCountdownPicker = true; }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '截止日期', value: this.countdownEndDateStr
                                                                });
                                                            }
                                                        }, { name: "SettingsListRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 308, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        Row.create({ space: 8 });
                                                        Row.width('100%');
                                                        Row.padding({ left: 16, right: 16, top: 10, bottom: 14 });
                                                    }, Row);
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        Button.createWithLabel('设置日期');
                                                        Button.fontSize(14);
                                                        Button.fontWeight(FontWeight.Medium);
                                                        Button.fontColor('#FFFFFF');
                                                        Button.backgroundColor(KikariaColors.SKY);
                                                        Button.borderRadius(12);
                                                        Button.height(36);
                                                        Button.padding({ left: 16, right: 16 });
                                                        Button.onClick(() => { this.showCountdownPicker = true; });
                                                    }, Button);
                                                    Button.pop();
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        Button.createWithLabel('清除');
                                                        Button.fontSize(14);
                                                        Button.fontWeight(FontWeight.Medium);
                                                        Button.fontColor(KikariaColors.SOFT_TEXT);
                                                        Button.backgroundColor(KikariaColors.MIST);
                                                        Button.borderRadius(12);
                                                        Button.height(36);
                                                        Button.padding({ left: 16, right: 16 });
                                                        Button.onClick(() => { appState.updateCountdownRange(null, null); this.countdownEndDateStr = '未设置'; this.refreshState(); });
                                                    }, Button);
                                                    Button.pop();
                                                    Row.pop();
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── Countdown ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── Notifications ──
                                Column.create({ space: 4 });
                                // ── Notifications ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('通知');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsToggleRow(this, { label: '学习进度提醒', isOn: this.__notificationsEnabled, onChange: (value: boolean) => { this.handleNotificationToggle(value); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 320, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '学习进度提醒',
                                                                    isOn: this.notificationsEnabled,
                                                                    onChange: (value: boolean) => { this.handleNotificationToggle(value); }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '学习进度提醒'
                                                            });
                                                        }
                                                    }, { name: "SettingsToggleRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 321, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsListRow(this, { label: '提醒时间', value: this.notificationTimeStr, onTap: () => { this.showNotificationPicker = true; } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 322, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '提醒时间',
                                                                    value: this.notificationTimeStr,
                                                                    onTap: () => { this.showNotificationPicker = true; }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '提醒时间', value: this.notificationTimeStr
                                                            });
                                                        }
                                                    }, { name: "SettingsListRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 323, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsInfoTextRow(this, { text: '需要先设置倒计时截止日期才能启用通知提醒。' }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 324, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    text: '需要先设置倒计时截止日期才能启用通知提醒。'
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                text: '需要先设置倒计时截止日期才能启用通知提醒。'
                                                            });
                                                        }
                                                    }, { name: "SettingsInfoTextRow" });
                                                }
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 319, col: 19 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsToggleRow(this, { label: '学习进度提醒', isOn: this.__notificationsEnabled, onChange: (value: boolean) => { this.handleNotificationToggle(value); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 320, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '学习进度提醒',
                                                                        isOn: this.notificationsEnabled,
                                                                        onChange: (value: boolean) => { this.handleNotificationToggle(value); }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '学习进度提醒'
                                                                });
                                                            }
                                                        }, { name: "SettingsToggleRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 321, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsListRow(this, { label: '提醒时间', value: this.notificationTimeStr, onTap: () => { this.showNotificationPicker = true; } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 322, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '提醒时间',
                                                                        value: this.notificationTimeStr,
                                                                        onTap: () => { this.showNotificationPicker = true; }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '提醒时间', value: this.notificationTimeStr
                                                                });
                                                            }
                                                        }, { name: "SettingsListRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 323, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsInfoTextRow(this, { text: '需要先设置倒计时截止日期才能启用通知提醒。' }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 324, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        text: '需要先设置倒计时截止日期才能启用通知提醒。'
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    text: '需要先设置倒计时截止日期才能启用通知提醒。'
                                                                });
                                                            }
                                                        }, { name: "SettingsInfoTextRow" });
                                                    }
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── Notifications ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── Appearance ──
                                Column.create({ space: 4 });
                                // ── Appearance ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('外观');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsToggleRow(this, { label: '暗色模式', isOn: this.__isDark, onChange: (_value: boolean) => { appState.toggleDarkMode(); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 332, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '暗色模式',
                                                                    isOn: this.isDark,
                                                                    onChange: (_value: boolean) => { appState.toggleDarkMode(); }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '暗色模式'
                                                            });
                                                        }
                                                    }, { name: "SettingsToggleRow" });
                                                }
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 331, col: 19 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsToggleRow(this, { label: '暗色模式', isOn: this.__isDark, onChange: (_value: boolean) => { appState.toggleDarkMode(); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 332, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '暗色模式',
                                                                        isOn: this.isDark,
                                                                        onChange: (_value: boolean) => { appState.toggleDarkMode(); }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '暗色模式'
                                                                });
                                                            }
                                                        }, { name: "SettingsToggleRow" });
                                                    }
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── Appearance ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── About ──
                                Column.create({ space: 4 });
                                // ── About ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('关于');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsListRow(this, { label: 'Markdown 格式指南', onTap: () => { navPathStack.pushPathByName('MarkdownFormatGuide', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 340, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: 'Markdown 格式指南',
                                                                    onTap: () => { navPathStack.pushPathByName('MarkdownFormatGuide', undefined); }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: 'Markdown 格式指南'
                                                            });
                                                        }
                                                    }, { name: "SettingsListRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 341, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsListRow(this, { label: '新手指引', onTap: () => { navPathStack.pushPathByName('OnboardingPage', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 342, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '新手指引',
                                                                    onTap: () => { navPathStack.pushPathByName('OnboardingPage', undefined); }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '新手指引'
                                                            });
                                                        }
                                                    }, { name: "SettingsListRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 343, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsListRow(this, { label: '预设管理', onTap: () => { navPathStack.pushPathByName('PresetSelectionPage', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 344, col: 21 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '预设管理',
                                                                    onTap: () => { navPathStack.pushPathByName('PresetSelectionPage', undefined); }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '预设管理'
                                                            });
                                                        }
                                                    }, { name: "SettingsListRow" });
                                                }
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 339, col: 19 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsListRow(this, { label: 'Markdown 格式指南', onTap: () => { navPathStack.pushPathByName('MarkdownFormatGuide', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 340, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: 'Markdown 格式指南',
                                                                        onTap: () => { navPathStack.pushPathByName('MarkdownFormatGuide', undefined); }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: 'Markdown 格式指南'
                                                                });
                                                            }
                                                        }, { name: "SettingsListRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 341, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsListRow(this, { label: '新手指引', onTap: () => { navPathStack.pushPathByName('OnboardingPage', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 342, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '新手指引',
                                                                        onTap: () => { navPathStack.pushPathByName('OnboardingPage', undefined); }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '新手指引'
                                                                });
                                                            }
                                                        }, { name: "SettingsListRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 343, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsListRow(this, { label: '预设管理', onTap: () => { navPathStack.pushPathByName('PresetSelectionPage', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 344, col: 21 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '预设管理',
                                                                        onTap: () => { navPathStack.pushPathByName('PresetSelectionPage', undefined); }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '预设管理'
                                                                });
                                                            }
                                                        }, { name: "SettingsListRow" });
                                                    }
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── About ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('Kikaria HarmonyOS · v1.0.0');
                                Text.fontSize(12);
                                Text.fontColor(KikariaColors.TERTIARY_TEXT);
                                Text.width('100%');
                                Text.textAlign(TextAlign.Center);
                                Text.padding({ top: 4 });
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Row.create();
                                Row.height(40);
                            }, Row);
                            Row.pop();
                            Column.pop();
                            Row.pop();
                        });
                    }
                    else {
                        this.ifElseBranchUpdateFunction(1, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── Profile ──
                                Column.create({ space: 4 });
                                // ── Profile ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('个人资料');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => { this.profileRow.bind(this)(); }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 362, col: 15 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => { this.profileRow.bind(this)(); }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── Profile ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── Daily Goal ──
                                Column.create({ space: 4 });
                                // ── Daily Goal ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('每日目标');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsStepperRow(this, {
                                                                label: '每日掌握数量', value: this.dailyGoal, min: 1, max: 200,
                                                                valueColor: KikariaColors.DEEP_TEXT,
                                                                onDecrement: () => { if (this.dailyGoal > 1) {
                                                                    this.dailyGoal--;
                                                                    appState.updateDailyGoal(this.dailyGoal);
                                                                } },
                                                                onIncrement: () => { if (this.dailyGoal < 200) {
                                                                    this.dailyGoal++;
                                                                    appState.updateDailyGoal(this.dailyGoal);
                                                                } }
                                                            }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 369, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '每日掌握数量',
                                                                    value: this.dailyGoal,
                                                                    min: 1,
                                                                    max: 200,
                                                                    valueColor: KikariaColors.DEEP_TEXT,
                                                                    onDecrement: () => { if (this.dailyGoal > 1) {
                                                                        this.dailyGoal--;
                                                                        appState.updateDailyGoal(this.dailyGoal);
                                                                    } },
                                                                    onIncrement: () => { if (this.dailyGoal < 200) {
                                                                        this.dailyGoal++;
                                                                        appState.updateDailyGoal(this.dailyGoal);
                                                                    } }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '每日掌握数量', value: this.dailyGoal, min: 1, max: 200,
                                                                valueColor: KikariaColors.DEEP_TEXT
                                                            });
                                                        }
                                                    }, { name: "SettingsStepperRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 375, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsInfoTextRow(this, { text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}` }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 376, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}`
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}`
                                                            });
                                                        }
                                                    }, { name: "SettingsInfoTextRow" });
                                                }
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 368, col: 15 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsStepperRow(this, {
                                                                    label: '每日掌握数量', value: this.dailyGoal, min: 1, max: 200,
                                                                    valueColor: KikariaColors.DEEP_TEXT,
                                                                    onDecrement: () => { if (this.dailyGoal > 1) {
                                                                        this.dailyGoal--;
                                                                        appState.updateDailyGoal(this.dailyGoal);
                                                                    } },
                                                                    onIncrement: () => { if (this.dailyGoal < 200) {
                                                                        this.dailyGoal++;
                                                                        appState.updateDailyGoal(this.dailyGoal);
                                                                    } }
                                                                }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 369, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '每日掌握数量',
                                                                        value: this.dailyGoal,
                                                                        min: 1,
                                                                        max: 200,
                                                                        valueColor: KikariaColors.DEEP_TEXT,
                                                                        onDecrement: () => { if (this.dailyGoal > 1) {
                                                                            this.dailyGoal--;
                                                                            appState.updateDailyGoal(this.dailyGoal);
                                                                        } },
                                                                        onIncrement: () => { if (this.dailyGoal < 200) {
                                                                            this.dailyGoal++;
                                                                            appState.updateDailyGoal(this.dailyGoal);
                                                                        } }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '每日掌握数量', value: this.dailyGoal, min: 1, max: 200,
                                                                    valueColor: KikariaColors.DEEP_TEXT
                                                                });
                                                            }
                                                        }, { name: "SettingsStepperRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 375, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsInfoTextRow(this, { text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}` }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 376, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}`
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}`
                                                                });
                                                            }
                                                        }, { name: "SettingsInfoTextRow" });
                                                    }
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── Daily Goal ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── Danger ──
                                Column.create({ space: 4 });
                                // ── Danger ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('预警阈值');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsStepperRow(this, {
                                                                label: '进度低于预期时提醒', value: this.dangerPercent, min: 5, max: 100,
                                                                valueColor: KikariaColors.DEEP_TEXT,
                                                                onDecrement: () => { if (this.dangerPercent > 5) {
                                                                    this.dangerPercent -= 5;
                                                                    appState.updateDangerPercent(this.dangerPercent);
                                                                } },
                                                                onIncrement: () => { if (this.dangerPercent < 100) {
                                                                    this.dangerPercent += 5;
                                                                    appState.updateDangerPercent(this.dangerPercent);
                                                                } }
                                                            }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 384, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '进度低于预期时提醒',
                                                                    value: this.dangerPercent,
                                                                    min: 5,
                                                                    max: 100,
                                                                    valueColor: KikariaColors.DEEP_TEXT,
                                                                    onDecrement: () => { if (this.dangerPercent > 5) {
                                                                        this.dangerPercent -= 5;
                                                                        appState.updateDangerPercent(this.dangerPercent);
                                                                    } },
                                                                    onIncrement: () => { if (this.dangerPercent < 100) {
                                                                        this.dangerPercent += 5;
                                                                        appState.updateDangerPercent(this.dangerPercent);
                                                                    } }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '进度低于预期时提醒', value: this.dangerPercent, min: 5, max: 100,
                                                                valueColor: KikariaColors.DEEP_TEXT
                                                            });
                                                        }
                                                    }, { name: "SettingsStepperRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 390, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsInfoTextRow(this, { text: '当实际掌握进度低于预设的预期百分比时触发提醒。' }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 391, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    text: '当实际掌握进度低于预设的预期百分比时触发提醒。'
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                text: '当实际掌握进度低于预设的预期百分比时触发提醒。'
                                                            });
                                                        }
                                                    }, { name: "SettingsInfoTextRow" });
                                                }
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 383, col: 15 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsStepperRow(this, {
                                                                    label: '进度低于预期时提醒', value: this.dangerPercent, min: 5, max: 100,
                                                                    valueColor: KikariaColors.DEEP_TEXT,
                                                                    onDecrement: () => { if (this.dangerPercent > 5) {
                                                                        this.dangerPercent -= 5;
                                                                        appState.updateDangerPercent(this.dangerPercent);
                                                                    } },
                                                                    onIncrement: () => { if (this.dangerPercent < 100) {
                                                                        this.dangerPercent += 5;
                                                                        appState.updateDangerPercent(this.dangerPercent);
                                                                    } }
                                                                }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 384, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '进度低于预期时提醒',
                                                                        value: this.dangerPercent,
                                                                        min: 5,
                                                                        max: 100,
                                                                        valueColor: KikariaColors.DEEP_TEXT,
                                                                        onDecrement: () => { if (this.dangerPercent > 5) {
                                                                            this.dangerPercent -= 5;
                                                                            appState.updateDangerPercent(this.dangerPercent);
                                                                        } },
                                                                        onIncrement: () => { if (this.dangerPercent < 100) {
                                                                            this.dangerPercent += 5;
                                                                            appState.updateDangerPercent(this.dangerPercent);
                                                                        } }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '进度低于预期时提醒', value: this.dangerPercent, min: 5, max: 100,
                                                                    valueColor: KikariaColors.DEEP_TEXT
                                                                });
                                                            }
                                                        }, { name: "SettingsStepperRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 390, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsInfoTextRow(this, { text: '当实际掌握进度低于预设的预期百分比时触发提醒。' }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 391, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        text: '当实际掌握进度低于预设的预期百分比时触发提醒。'
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    text: '当实际掌握进度低于预设的预期百分比时触发提醒。'
                                                                });
                                                            }
                                                        }, { name: "SettingsInfoTextRow" });
                                                    }
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── Danger ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── Countdown ──
                                Column.create({ space: 4 });
                                // ── Countdown ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('倒计时');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsListRow(this, { label: '截止日期', value: this.countdownEndDateStr, onTap: () => { this.showCountdownPicker = true; } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 399, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '截止日期',
                                                                    value: this.countdownEndDateStr,
                                                                    onTap: () => { this.showCountdownPicker = true; }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '截止日期', value: this.countdownEndDateStr
                                                            });
                                                        }
                                                    }, { name: "SettingsListRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 400, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Row.create({ space: 8 });
                                                    Row.width('100%');
                                                    Row.padding({ left: 16, right: 16, top: 10, bottom: 14 });
                                                }, Row);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Button.createWithLabel('设置日期');
                                                    Button.fontSize(14);
                                                    Button.fontWeight(FontWeight.Medium);
                                                    Button.fontColor('#FFFFFF');
                                                    Button.backgroundColor(KikariaColors.SKY);
                                                    Button.borderRadius(12);
                                                    Button.height(36);
                                                    Button.padding({ left: 16, right: 16 });
                                                    Button.onClick(() => { this.showCountdownPicker = true; });
                                                }, Button);
                                                Button.pop();
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Button.createWithLabel('清除');
                                                    Button.fontSize(14);
                                                    Button.fontWeight(FontWeight.Medium);
                                                    Button.fontColor(KikariaColors.SOFT_TEXT);
                                                    Button.backgroundColor(KikariaColors.MIST);
                                                    Button.borderRadius(12);
                                                    Button.height(36);
                                                    Button.padding({ left: 16, right: 16 });
                                                    Button.onClick(() => { appState.updateCountdownRange(null, null); this.countdownEndDateStr = '未设置'; this.refreshState(); });
                                                }, Button);
                                                Button.pop();
                                                Row.pop();
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 398, col: 15 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsListRow(this, { label: '截止日期', value: this.countdownEndDateStr, onTap: () => { this.showCountdownPicker = true; } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 399, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '截止日期',
                                                                        value: this.countdownEndDateStr,
                                                                        onTap: () => { this.showCountdownPicker = true; }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '截止日期', value: this.countdownEndDateStr
                                                                });
                                                            }
                                                        }, { name: "SettingsListRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 400, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        Row.create({ space: 8 });
                                                        Row.width('100%');
                                                        Row.padding({ left: 16, right: 16, top: 10, bottom: 14 });
                                                    }, Row);
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        Button.createWithLabel('设置日期');
                                                        Button.fontSize(14);
                                                        Button.fontWeight(FontWeight.Medium);
                                                        Button.fontColor('#FFFFFF');
                                                        Button.backgroundColor(KikariaColors.SKY);
                                                        Button.borderRadius(12);
                                                        Button.height(36);
                                                        Button.padding({ left: 16, right: 16 });
                                                        Button.onClick(() => { this.showCountdownPicker = true; });
                                                    }, Button);
                                                    Button.pop();
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        Button.createWithLabel('清除');
                                                        Button.fontSize(14);
                                                        Button.fontWeight(FontWeight.Medium);
                                                        Button.fontColor(KikariaColors.SOFT_TEXT);
                                                        Button.backgroundColor(KikariaColors.MIST);
                                                        Button.borderRadius(12);
                                                        Button.height(36);
                                                        Button.padding({ left: 16, right: 16 });
                                                        Button.onClick(() => { appState.updateCountdownRange(null, null); this.countdownEndDateStr = '未设置'; this.refreshState(); });
                                                    }, Button);
                                                    Button.pop();
                                                    Row.pop();
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── Countdown ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── Notifications ──
                                Column.create({ space: 4 });
                                // ── Notifications ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('通知');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsToggleRow(this, { label: '学习进度提醒', isOn: this.__notificationsEnabled, onChange: (value: boolean) => { this.handleNotificationToggle(value); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 412, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '学习进度提醒',
                                                                    isOn: this.notificationsEnabled,
                                                                    onChange: (value: boolean) => { this.handleNotificationToggle(value); }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '学习进度提醒'
                                                            });
                                                        }
                                                    }, { name: "SettingsToggleRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 413, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsListRow(this, { label: '提醒时间', value: this.notificationTimeStr, onTap: () => { this.showNotificationPicker = true; } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 414, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '提醒时间',
                                                                    value: this.notificationTimeStr,
                                                                    onTap: () => { this.showNotificationPicker = true; }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '提醒时间', value: this.notificationTimeStr
                                                            });
                                                        }
                                                    }, { name: "SettingsListRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 415, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsInfoTextRow(this, { text: '需要先设置倒计时截止日期才能启用通知提醒。' }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 416, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    text: '需要先设置倒计时截止日期才能启用通知提醒。'
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                text: '需要先设置倒计时截止日期才能启用通知提醒。'
                                                            });
                                                        }
                                                    }, { name: "SettingsInfoTextRow" });
                                                }
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 411, col: 15 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsToggleRow(this, { label: '学习进度提醒', isOn: this.__notificationsEnabled, onChange: (value: boolean) => { this.handleNotificationToggle(value); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 412, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '学习进度提醒',
                                                                        isOn: this.notificationsEnabled,
                                                                        onChange: (value: boolean) => { this.handleNotificationToggle(value); }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '学习进度提醒'
                                                                });
                                                            }
                                                        }, { name: "SettingsToggleRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 413, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsListRow(this, { label: '提醒时间', value: this.notificationTimeStr, onTap: () => { this.showNotificationPicker = true; } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 414, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '提醒时间',
                                                                        value: this.notificationTimeStr,
                                                                        onTap: () => { this.showNotificationPicker = true; }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '提醒时间', value: this.notificationTimeStr
                                                                });
                                                            }
                                                        }, { name: "SettingsListRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 415, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsInfoTextRow(this, { text: '需要先设置倒计时截止日期才能启用通知提醒。' }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 416, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        text: '需要先设置倒计时截止日期才能启用通知提醒。'
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    text: '需要先设置倒计时截止日期才能启用通知提醒。'
                                                                });
                                                            }
                                                        }, { name: "SettingsInfoTextRow" });
                                                    }
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── Notifications ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── Appearance ──
                                Column.create({ space: 4 });
                                // ── Appearance ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('外观');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsToggleRow(this, { label: '暗色模式', isOn: this.__isDark, onChange: (_value: boolean) => { appState.toggleDarkMode(); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 424, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '暗色模式',
                                                                    isOn: this.isDark,
                                                                    onChange: (_value: boolean) => { appState.toggleDarkMode(); }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '暗色模式'
                                                            });
                                                        }
                                                    }, { name: "SettingsToggleRow" });
                                                }
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 423, col: 15 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsToggleRow(this, { label: '暗色模式', isOn: this.__isDark, onChange: (_value: boolean) => { appState.toggleDarkMode(); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 424, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '暗色模式',
                                                                        isOn: this.isDark,
                                                                        onChange: (_value: boolean) => { appState.toggleDarkMode(); }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '暗色模式'
                                                                });
                                                            }
                                                        }, { name: "SettingsToggleRow" });
                                                    }
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── Appearance ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // ── About ──
                                Column.create({ space: 4 });
                                // ── About ──
                                Column.width('100%');
                            }, Column);
                            this.sectionHeader.bind(this)('关于');
                            {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    if (isInitialRender) {
                                        let componentCall = new SettingsSectionCard(this, {
                                            content: () => {
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsListRow(this, { label: 'Markdown 格式指南', onTap: () => { navPathStack.pushPathByName('MarkdownFormatGuide', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 432, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: 'Markdown 格式指南',
                                                                    onTap: () => { navPathStack.pushPathByName('MarkdownFormatGuide', undefined); }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: 'Markdown 格式指南'
                                                            });
                                                        }
                                                    }, { name: "SettingsListRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 433, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsListRow(this, { label: '新手指引', onTap: () => { navPathStack.pushPathByName('OnboardingPage', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 434, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '新手指引',
                                                                    onTap: () => { navPathStack.pushPathByName('OnboardingPage', undefined); }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '新手指引'
                                                            });
                                                        }
                                                    }, { name: "SettingsListRow" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 435, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {};
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                        }
                                                    }, { name: "SettingsSectionDivider" });
                                                }
                                                {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        if (isInitialRender) {
                                                            let componentCall = new SettingsListRow(this, { label: '预设管理', onTap: () => { navPathStack.pushPathByName('PresetSelectionPage', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 436, col: 17 });
                                                            ViewPU.create(componentCall);
                                                            let paramsLambda = () => {
                                                                return {
                                                                    label: '预设管理',
                                                                    onTap: () => { navPathStack.pushPathByName('PresetSelectionPage', undefined); }
                                                                };
                                                            };
                                                            componentCall.paramsGenerator_ = paramsLambda;
                                                        }
                                                        else {
                                                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                label: '预设管理'
                                                            });
                                                        }
                                                    }, { name: "SettingsListRow" });
                                                }
                                            }
                                        }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 431, col: 15 });
                                        ViewPU.create(componentCall);
                                        let paramsLambda = () => {
                                            return {
                                                content: () => {
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsListRow(this, { label: 'Markdown 格式指南', onTap: () => { navPathStack.pushPathByName('MarkdownFormatGuide', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 432, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: 'Markdown 格式指南',
                                                                        onTap: () => { navPathStack.pushPathByName('MarkdownFormatGuide', undefined); }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: 'Markdown 格式指南'
                                                                });
                                                            }
                                                        }, { name: "SettingsListRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 433, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsListRow(this, { label: '新手指引', onTap: () => { navPathStack.pushPathByName('OnboardingPage', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 434, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '新手指引',
                                                                        onTap: () => { navPathStack.pushPathByName('OnboardingPage', undefined); }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '新手指引'
                                                                });
                                                            }
                                                        }, { name: "SettingsListRow" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsSectionDivider(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 435, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {};
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {});
                                                            }
                                                        }, { name: "SettingsSectionDivider" });
                                                    }
                                                    {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            if (isInitialRender) {
                                                                let componentCall = new SettingsListRow(this, { label: '预设管理', onTap: () => { navPathStack.pushPathByName('PresetSelectionPage', undefined); } }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 436, col: 17 });
                                                                ViewPU.create(componentCall);
                                                                let paramsLambda = () => {
                                                                    return {
                                                                        label: '预设管理',
                                                                        onTap: () => { navPathStack.pushPathByName('PresetSelectionPage', undefined); }
                                                                    };
                                                                };
                                                                componentCall.paramsGenerator_ = paramsLambda;
                                                            }
                                                            else {
                                                                this.updateStateVarsOfChildByElmtId(elmtId, {
                                                                    label: '预设管理'
                                                                });
                                                            }
                                                        }, { name: "SettingsListRow" });
                                                    }
                                                }
                                            };
                                        };
                                        componentCall.paramsGenerator_ = paramsLambda;
                                    }
                                    else {
                                        this.updateStateVarsOfChildByElmtId(elmtId, {});
                                    }
                                }, { name: "SettingsSectionCard" });
                            }
                            // ── About ──
                            Column.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('Kikaria HarmonyOS · v1.0.0');
                                Text.fontSize(12);
                                Text.fontColor(KikariaColors.TERTIARY_TEXT);
                                Text.width('100%');
                                Text.textAlign(TextAlign.Center);
                                Text.padding({ top: 4 });
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Row.create();
                                Row.height(40);
                            }, Row);
                            Row.pop();
                        });
                    }
                }, If);
                If.pop();
                Column.pop();
                Scroll.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    // ── Countdown date picker overlay ──
                    if (this.showCountdownPicker) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Stack.create();
                                Stack.width('100%');
                                Stack.height('100%');
                            }, Stack);
                            this.overlayBackdrop.bind(this)(() => { this.showCountdownPicker = false; });
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 16 });
                                Column.width('80%');
                                Column.padding(24);
                                Column.borderRadius(20);
                                Column.backgroundColor(KikariaColors.CARD_BG);
                                Column.shadow({ radius: 24, color: KikariaColors.SHADOW_COLOR, offsetY: 8 });
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('设置截止日期');
                                Text.fontSize(17);
                                Text.fontWeight(FontWeight.Bold);
                                Text.fontColor(KikariaColors.DEEP_TEXT);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Row.create({ space: 8 });
                            }, Row);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                TextInput.create({ text: `${this.pickerEndYear}` });
                                TextInput.fontSize(18);
                                TextInput.fontWeight(FontWeight.Bold);
                                TextInput.fontColor(KikariaColors.DEEP_TEXT);
                                TextInput.textAlign(TextAlign.Center);
                                TextInput.backgroundColor(KikariaColors.MIST);
                                TextInput.borderRadius(10);
                                TextInput.width(70);
                                TextInput.height(44);
                                TextInput.type(InputType.Number);
                                TextInput.onChange((v: string) => { this.pickerEndYear = parseInt(v) || this.pickerEndYear; });
                            }, TextInput);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('/');
                                Text.fontSize(20);
                                Text.fontColor(KikariaColors.TERTIARY_TEXT);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                TextInput.create({ text: `${String(this.pickerEndMonth).padStart(2, '0')}` });
                                TextInput.fontSize(18);
                                TextInput.fontWeight(FontWeight.Bold);
                                TextInput.fontColor(KikariaColors.DEEP_TEXT);
                                TextInput.textAlign(TextAlign.Center);
                                TextInput.backgroundColor(KikariaColors.MIST);
                                TextInput.borderRadius(10);
                                TextInput.width(55);
                                TextInput.height(44);
                                TextInput.type(InputType.Number);
                                TextInput.onChange((v: string) => { this.pickerEndMonth = parseInt(v) || this.pickerEndMonth; });
                            }, TextInput);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('/');
                                Text.fontSize(20);
                                Text.fontColor(KikariaColors.TERTIARY_TEXT);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                TextInput.create({ text: `${String(this.pickerEndDay).padStart(2, '0')}` });
                                TextInput.fontSize(18);
                                TextInput.fontWeight(FontWeight.Bold);
                                TextInput.fontColor(KikariaColors.DEEP_TEXT);
                                TextInput.textAlign(TextAlign.Center);
                                TextInput.backgroundColor(KikariaColors.MIST);
                                TextInput.borderRadius(10);
                                TextInput.width(55);
                                TextInput.height(44);
                                TextInput.type(InputType.Number);
                                TextInput.onChange((v: string) => { this.pickerEndDay = parseInt(v) || this.pickerEndDay; });
                            }, TextInput);
                            Row.pop();
                            this.pickerButtonRow.bind(this)(() => { this.showCountdownPicker = false; }, () => {
                                const d = new Date(this.pickerEndYear, this.pickerEndMonth - 1, this.pickerEndDay, 23, 59, 59);
                                appState.updateCountdownRange(null, d.getTime());
                                this.showCountdownPicker = false;
                                this.refreshState();
                            });
                            Column.pop();
                            Stack.pop();
                        });
                    }
                    // ── Notification time picker overlay ──
                    else {
                        this.ifElseBranchUpdateFunction(1, () => {
                        });
                    }
                }, If);
                If.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    // ── Notification time picker overlay ──
                    if (this.showNotificationPicker) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Stack.create();
                                Stack.width('100%');
                                Stack.height('100%');
                            }, Stack);
                            this.overlayBackdrop.bind(this)(() => { this.showNotificationPicker = false; });
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 16 });
                                Column.width('80%');
                                Column.padding(24);
                                Column.borderRadius(20);
                                Column.backgroundColor(KikariaColors.CARD_BG);
                                Column.shadow({ radius: 24, color: KikariaColors.SHADOW_COLOR, offsetY: 8 });
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('设置提醒时间');
                                Text.fontSize(17);
                                Text.fontWeight(FontWeight.Bold);
                                Text.fontColor(KikariaColors.DEEP_TEXT);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Row.create({ space: 8 });
                            }, Row);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                TextInput.create({ text: `${String(this.pickerHour).padStart(2, '0')}` });
                                TextInput.fontSize(18);
                                TextInput.fontWeight(FontWeight.Bold);
                                TextInput.fontColor(KikariaColors.DEEP_TEXT);
                                TextInput.textAlign(TextAlign.Center);
                                TextInput.backgroundColor(KikariaColors.MIST);
                                TextInput.borderRadius(10);
                                TextInput.width(60);
                                TextInput.height(44);
                                TextInput.type(InputType.Number);
                                TextInput.onChange((v: string) => { this.pickerHour = parseInt(v) || this.pickerHour; });
                            }, TextInput);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(':');
                                Text.fontSize(20);
                                Text.fontColor(KikariaColors.TERTIARY_TEXT);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                TextInput.create({ text: `${String(this.pickerMinute).padStart(2, '0')}` });
                                TextInput.fontSize(18);
                                TextInput.fontWeight(FontWeight.Bold);
                                TextInput.fontColor(KikariaColors.DEEP_TEXT);
                                TextInput.textAlign(TextAlign.Center);
                                TextInput.backgroundColor(KikariaColors.MIST);
                                TextInput.borderRadius(10);
                                TextInput.width(60);
                                TextInput.height(44);
                                TextInput.type(InputType.Number);
                                TextInput.onChange((v: string) => { this.pickerMinute = parseInt(v) || this.pickerMinute; });
                            }, TextInput);
                            Row.pop();
                            this.pickerButtonRow.bind(this)(() => { this.showNotificationPicker = false; }, () => {
                                const d = new Date();
                                d.setHours(this.pickerHour, this.pickerMinute, 0, 0);
                                appState.updateNotificationTime(d.getTime());
                                this.showNotificationPicker = false;
                                this.refreshState();
                            });
                            Column.pop();
                            Stack.pop();
                        });
                    }
                    else {
                        this.ifElseBranchUpdateFunction(1, () => {
                        });
                    }
                }, If);
                If.pop();
                Stack.pop();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/SettingsPage" });
            NavDestination.hideTitleBar(true);
            NavDestination.width('100%');
            NavDestination.height('100%');
        }, NavDestination);
        NavDestination.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
class SettingsPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: SettingsPage_Params) {
    }
    updateStateVars(params: SettingsPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/SettingsPage", isUserCreateStack: false });
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new SettingsPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/SettingsPage.ets", line: 601, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "SettingsPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "SettingsPage";
    }
}
registerNamedRoute(() => new SettingsPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/SettingsPage", pageFullPath: "entry/src/main/ets/pages/SettingsPage", integratedHsp: "false", moduleType: "followWithHap" });
