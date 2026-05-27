if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface ReviewHistoryPage_Params {
}
interface ReviewHistoryPageContent_Params {
    contentPadH?: number;
    historyRecords?: StudyActivityRecord[];
    groupedRecords?: Record<string, StudyActivityRecord[]>;
    sortedDates?: string[];
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { StudyActivityType } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import type { StudyActivityRecord } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, pageHorizontalPadding, ipadPortraitListPageTopInset } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function ReviewHistoryPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new ReviewHistoryPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/ReviewHistoryPage.ets", line: 15, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "ReviewHistoryPageContent" });
    }
}
export class ReviewHistoryPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.__historyRecords = new ObservedPropertyObjectPU([], this, "historyRecords");
        this.__groupedRecords = new ObservedPropertyObjectPU({}, this, "groupedRecords");
        this.__sortedDates = new ObservedPropertyObjectPU([], this, "sortedDates");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: ReviewHistoryPageContent_Params) {
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
        if (params.historyRecords !== undefined) {
            this.historyRecords = params.historyRecords;
        }
        if (params.groupedRecords !== undefined) {
            this.groupedRecords = params.groupedRecords;
        }
        if (params.sortedDates !== undefined) {
            this.sortedDates = params.sortedDates;
        }
    }
    updateStateVars(params: ReviewHistoryPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
        this.__historyRecords.purgeDependencyOnElmtId(rmElmtId);
        this.__groupedRecords.purgeDependencyOnElmtId(rmElmtId);
        this.__sortedDates.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__contentPadH.aboutToBeDeleted();
        this.__historyRecords.aboutToBeDeleted();
        this.__groupedRecords.aboutToBeDeleted();
        this.__sortedDates.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __contentPadH: ObservedPropertySimplePU<number>;
    get contentPadH() {
        return this.__contentPadH.get();
    }
    set contentPadH(newValue: number) {
        this.__contentPadH.set(newValue);
    }
    private __historyRecords: ObservedPropertyObjectPU<StudyActivityRecord[]>;
    get historyRecords() {
        return this.__historyRecords.get();
    }
    set historyRecords(newValue: StudyActivityRecord[]) {
        this.__historyRecords.set(newValue);
    }
    private __groupedRecords: ObservedPropertyObjectPU<Record<string, StudyActivityRecord[]>>;
    get groupedRecords() {
        return this.__groupedRecords.get();
    }
    set groupedRecords(newValue: Record<string, StudyActivityRecord[]>) {
        this.__groupedRecords.set(newValue);
    }
    private __sortedDates: ObservedPropertyObjectPU<string[]>;
    get sortedDates() {
        return this.__sortedDates.get();
    }
    set sortedDates(newValue: string[]) {
        this.__sortedDates.set(newValue);
    }
    aboutToAppear(): void {
        this.contentPadH = pageHorizontalPadding();
        this.refreshState();
    }
    refreshState(): void {
        this.historyRecords = [...appState.getCurrentPresetActivityRecords()]
            .sort((a, b) => b.date - a.date);
        const groups: Record<string, StudyActivityRecord[]> = {};
        for (const record of this.historyRecords) {
            const d = new Date(record.date);
            d.setHours(0, 0, 0, 0);
            const key = d.getTime().toString();
            if (!groups[key]) {
                groups[key] = [];
            }
            groups[key].push(record);
        }
        this.groupedRecords = groups;
        this.sortedDates = Object.keys(groups).sort((a, b) => parseInt(b) - parseInt(a));
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            NavDestination.create(() => {
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create();
                    Column.width('100%');
                    Column.height('100%');
                    Column.backgroundColor(KikariaColors.PAGE_BG);
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.width('100%');
                    Row.padding({ left: 20, right: 20, top: 12 + ipadPortraitListPageTopInset(), bottom: 12 });
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
                    Text.create('复习历史');
                    Text.fontSize(17);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.DEEP_TEXT);
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(`${this.historyRecords.length}条`);
                    Text.fontSize(13);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (this.sortedDates.length === 0) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 12 });
                                Column.width('100%');
                                Column.height('100%');
                                Column.justifyContent(FlexAlign.Center);
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('暂无记录');
                                Text.fontSize(20);
                                Text.fontWeight(FontWeight.Medium);
                                Text.fontColor(KikariaColors.DEEP_TEXT);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('完成复习后，活动记录将出现在这里。');
                                Text.fontSize(15);
                                Text.fontColor(KikariaColors.SOFT_TEXT);
                            }, Text);
                            Text.pop();
                            Column.pop();
                        });
                    }
                    else {
                        this.ifElseBranchUpdateFunction(1, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                List.create({ space: 0 });
                                List.width('100%');
                                List.constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' });
                                List.layoutWeight(1);
                                List.padding({ left: this.contentPadH, right: this.contentPadH });
                                List.scrollBar(BarState.Off);
                                List.divider({ strokeWidth: 0 });
                            }, List);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                ForEach.create();
                                const forEachItemGenFunction = _item => {
                                    const dateKey = _item;
                                    {
                                        const itemCreation = (elmtId, isInitialRender) => {
                                            ViewStackProcessor.StartGetAccessRecordingFor(elmtId);
                                            ListItem.create(deepRenderFunction, true);
                                            if (!isInitialRender) {
                                                ListItem.pop();
                                            }
                                            ViewStackProcessor.StopGetAccessRecording();
                                        };
                                        const itemCreation2 = (elmtId, isInitialRender) => {
                                            ListItem.create(deepRenderFunction, true);
                                        };
                                        const deepRenderFunction = (elmtId, isInitialRender) => {
                                            itemCreation(elmtId, isInitialRender);
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                Column.create({ space: 8 });
                                                Column.width('100%');
                                                Column.borderRadius(18);
                                                Column.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                                                Column.backdropBlur(10);
                                                Column.shadow({ radius: 6, color: KikariaColors.SHADOW_COLOR, offsetY: 2 });
                                                Column.margin({ bottom: 8 });
                                            }, Column);
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                // Date header
                                                Row.create();
                                                // Date header
                                                Row.width('100%');
                                            }, Row);
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                Text.create(this.formatDate(parseInt(dateKey)));
                                                Text.fontSize(14);
                                                Text.fontWeight(FontWeight.Medium);
                                                Text.fontColor(KikariaColors.SOFT_TEXT);
                                                Text.padding({ left: 16, top: 16, bottom: 8 });
                                            }, Text);
                                            Text.pop();
                                            // Date header
                                            Row.pop();
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                ForEach.create();
                                                const forEachItemGenFunction = _item => {
                                                    const record = _item;
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        Row.create({ space: 10 });
                                                        Row.width('100%');
                                                        Row.padding({ left: 16, right: 16, top: 10, bottom: 10 });
                                                        Row.border({ width: { bottom: 1 }, color: KikariaColors.MIST });
                                                    }, Row);
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        Text.create(this.activityIcon(record.type));
                                                        Text.fontSize(14);
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
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        Text.create(this.formatTime(record.date));
                                                        Text.fontSize(12);
                                                        Text.fontColor(KikariaColors.TERTIARY_TEXT);
                                                    }, Text);
                                                    Text.pop();
                                                    Row.pop();
                                                };
                                                this.forEachUpdateFunction(elmtId, this.groupedRecords[dateKey], forEachItemGenFunction);
                                            }, ForEach);
                                            ForEach.pop();
                                            Column.pop();
                                            ListItem.pop();
                                        };
                                        this.observeComponentCreation2(itemCreation2, ListItem);
                                        ListItem.pop();
                                    }
                                };
                                this.forEachUpdateFunction(elmtId, this.sortedDates, forEachItemGenFunction);
                            }, ForEach);
                            ForEach.pop();
                            List.pop();
                        });
                    }
                }, If);
                If.pop();
                Column.pop();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/ReviewHistoryPage" });
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
            case StudyActivityType.ADDED_REINFORCEMENT: return '标重点';
            case StudyActivityType.REMOVED_REINFORCEMENT: return '取消重点';
            case StudyActivityType.VIEWED_HINT: return '看提示';
            case StudyActivityType.REVIEWED_ANSWER: return '复习';
        }
    }
    formatDate(timestamp: number): string {
        const d = new Date(timestamp);
        return `${d.getFullYear()}/${d.getMonth() + 1}/${d.getDate()}`;
    }
    formatTime(timestamp: number): string {
        const d = new Date(timestamp);
        return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
    }
    rerender() {
        this.updateDirtyElements();
    }
}
class ReviewHistoryPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: ReviewHistoryPage_Params) {
    }
    updateStateVars(params: ReviewHistoryPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/ReviewHistoryPage", isUserCreateStack: false });
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new ReviewHistoryPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/ReviewHistoryPage.ets", line: 193, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "ReviewHistoryPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "ReviewHistoryPage";
    }
}
registerNamedRoute(() => new ReviewHistoryPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/ReviewHistoryPage", pageFullPath: "entry/src/main/ets/pages/ReviewHistoryPage", integratedHsp: "false", moduleType: "followWithHap" });
