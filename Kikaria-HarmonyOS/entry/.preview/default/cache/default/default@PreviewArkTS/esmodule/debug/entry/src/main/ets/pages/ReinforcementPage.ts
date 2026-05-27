if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface ReinforcementPage_Params {
}
interface ReinforcementPageContent_Params {
    reinforcedPoints?: KnowledgePoint[];
    expandedId?: string;
    contentPadH?: number;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { ReviewMode } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import type { KnowledgePoint } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, pageHorizontalPadding, ipadPortraitListPageTopInset } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function ReinforcementPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new ReinforcementPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/ReinforcementPage.ets", line: 15, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "ReinforcementPageContent" });
    }
}
export class ReinforcementPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__reinforcedPoints = new ObservedPropertyObjectPU([], this, "reinforcedPoints");
        this.__expandedId = new ObservedPropertySimplePU('', this, "expandedId");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: ReinforcementPageContent_Params) {
        if (params.reinforcedPoints !== undefined) {
            this.reinforcedPoints = params.reinforcedPoints;
        }
        if (params.expandedId !== undefined) {
            this.expandedId = params.expandedId;
        }
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
    }
    updateStateVars(params: ReinforcementPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__reinforcedPoints.purgeDependencyOnElmtId(rmElmtId);
        this.__expandedId.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__reinforcedPoints.aboutToBeDeleted();
        this.__expandedId.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __reinforcedPoints: ObservedPropertyObjectPU<KnowledgePoint[]>;
    get reinforcedPoints() {
        return this.__reinforcedPoints.get();
    }
    set reinforcedPoints(newValue: KnowledgePoint[]) {
        this.__reinforcedPoints.set(newValue);
    }
    private __expandedId: ObservedPropertySimplePU<string>;
    get expandedId() {
        return this.__expandedId.get();
    }
    set expandedId(newValue: string) {
        this.__expandedId.set(newValue);
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
    onPageShow(): void {
        this.refreshState();
    }
    refreshState(): void {
        this.reinforcedPoints = [...appState.reinforcedPoints];
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            NavDestination.create(() => {
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create();
                    Column.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(39:7)", "entry");
                    Column.width('100%');
                    Column.height('100%');
                    Column.backgroundColor(KikariaColors.PAGE_BG);
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(40:9)", "entry");
                    Row.width('100%');
                    Row.padding({ left: 20, right: 20, top: 12 + ipadPortraitListPageTopInset(), bottom: 12 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithChild();
                    Button.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(41:11)", "entry");
                    Button.backgroundColor(Color.Transparent);
                    Button.onClick(() => { navPathStack.pop(); });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('← 返回');
                    Text.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(42:13)", "entry");
                    Text.fontSize(17);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Button.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                    Blank.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(49:11)", "entry");
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(`重点集锦 · ${this.reinforcedPoints.length}`);
                    Text.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(51:11)", "entry");
                    Text.fontSize(17);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.DEEP_TEXT);
                }, Text);
                Text.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (this.reinforcedPoints.length === 0) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 16 });
                                Column.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(60:11)", "entry");
                                Column.width('100%');
                                Column.height('100%');
                                Column.justifyContent(FlexAlign.Center);
                                Column.padding(24);
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('暂无重点项目');
                                Text.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(61:13)", "entry");
                                Text.fontSize(20);
                                Text.fontWeight(FontWeight.Medium);
                                Text.fontColor(KikariaColors.DEEP_TEXT);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('在复习过程中点击"加入重点"，即可将重要知识点加入此处集中复习。');
                                Text.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(66:13)", "entry");
                                Text.fontSize(15);
                                Text.fontColor(KikariaColors.SOFT_TEXT);
                                Text.textAlign(TextAlign.Center);
                                Text.padding({ left: 40, right: 40 });
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Button.createWithLabel('返回首页');
                                Button.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(72:13)", "entry");
                                Button.fontSize(17);
                                Button.fontWeight(FontWeight.Medium);
                                Button.fontColor('#FFFFFF');
                                Button.linearGradient({
                                    angle: 135,
                                    colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
                                });
                                Button.borderRadius(16);
                                Button.height(50);
                                Button.padding({ left: 32, right: 32 });
                                Button.onClick(() => { navPathStack.pop(); });
                            }, Button);
                            Button.pop();
                            Column.pop();
                        });
                    }
                    else {
                        this.ifElseBranchUpdateFunction(1, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create();
                                Column.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(90:11)", "entry");
                                Column.width('100%');
                                Column.layoutWeight(1);
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Row.create();
                                Row.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(91:13)", "entry");
                                Row.width('100%');
                                Row.padding({ left: 24, right: 24, top: 12, bottom: 8 });
                            }, Row);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Button.createWithLabel(`复习全部 (${this.reinforcedPoints.length})`);
                                Button.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(92:15)", "entry");
                                Button.fontSize(15);
                                Button.fontWeight(FontWeight.Medium);
                                Button.fontColor('#FFFFFF');
                                Button.linearGradient({
                                    angle: 135,
                                    colors: [[KikariaColors.NEXT_GRADIENT_START, 0], [KikariaColors.NEXT_GRADIENT_END, 1]]
                                });
                                Button.borderRadius(16);
                                Button.height(44);
                                Button.width('100%');
                                Button.shadow({ radius: 10, color: KikariaColors.SHADOW_COLOR, offsetY: 4 });
                                Button.onClick(() => {
                                    appState.startReview(ReviewMode.REINFORCEMENT);
                                    navPathStack.pushPathByName('ReviewPage', undefined);
                                });
                            }, Button);
                            Button.pop();
                            Row.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                List.create({ space: 10 });
                                List.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(112:13)", "entry");
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
                                    const point = _item;
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
                                            ListItem.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(114:17)", "entry");
                                        };
                                        const deepRenderFunction = (elmtId, isInitialRender) => {
                                            itemCreation(elmtId, isInitialRender);
                                            this.buildPointCard.bind(this)(point);
                                            ListItem.pop();
                                        };
                                        this.observeComponentCreation2(itemCreation2, ListItem);
                                        ListItem.pop();
                                    }
                                };
                                this.forEachUpdateFunction(elmtId, this.reinforcedPoints, forEachItemGenFunction);
                            }, ForEach);
                            ForEach.pop();
                            List.pop();
                            Column.pop();
                        });
                    }
                }, If);
                If.pop();
                Column.pop();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/ReinforcementPage" });
            NavDestination.hideTitleBar(true);
            NavDestination.width('100%');
            NavDestination.height('100%');
            NavDestination.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(38:5)", "entry");
        }, NavDestination);
        NavDestination.pop();
    }
    buildPointCard(point: KnowledgePoint, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
            Column.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(141:5)", "entry");
            Column.width('100%');
            Column.padding(16);
            Column.borderRadius(18);
            Column.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
            Column.backdropBlur(12);
            Column.shadow({ radius: 8, color: KikariaColors.SHADOW_COLOR, offsetY: 2 });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(142:7)", "entry");
            Row.width('100%');
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 4 });
            Column.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(143:9)", "entry");
            Column.layoutWeight(1);
            Column.alignItems(HorizontalAlign.Start);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 8 });
            Row.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(144:11)", "entry");
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('★');
            Text.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(145:13)", "entry");
            Text.fontSize(16);
            Text.fontColor(KikariaColors.NEXT_AMBER);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(point.title);
            Text.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(149:13)", "entry");
            Text.fontSize(17);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(KikariaColors.DEEP_TEXT);
        }, Text);
        Text.pop();
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (point.tags.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(point.tags.join(', '));
                        Text.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(156:13)", "entry");
                        Text.fontSize(12);
                        Text.fontColor(KikariaColors.SOFT_TEXT);
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
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(164:9)", "entry");
            Button.backgroundColor(Color.Transparent);
            Button.width(32);
            Button.height(32);
            Button.onClick(() => {
                this.expandedId = this.expandedId === point.id ? '' : point.id;
            });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.expandedId === point.id ? '▲' : '▼');
            Text.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(165:11)", "entry");
            Text.fontSize(16);
            Text.fontColor(KikariaColors.SOFT_TEXT);
        }, Text);
        Text.pop();
        Button.pop();
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.expandedId === point.id) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 8 });
                        Column.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(179:9)", "entry");
                        Column.width('100%');
                        Column.padding({ top: 8 });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (point.hint.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(point.hint);
                                    Text.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(181:13)", "entry");
                                    Text.fontSize(14);
                                    Text.fontColor(KikariaColors.SOFT_TEXT);
                                    Text.width('100%');
                                    Text.textAlign(TextAlign.Start);
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
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(point.content);
                        Text.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(188:11)", "entry");
                        Text.fontSize(14);
                        Text.fontColor(KikariaColors.DEEP_TEXT);
                        Text.width('100%');
                        Text.textAlign(TextAlign.Start);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithLabel('移出重点');
                        Button.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(194:11)", "entry");
                        Button.fontSize(14);
                        Button.fontWeight(FontWeight.Medium);
                        Button.fontColor(KikariaColors.REMOVE_CORAL);
                        Button.backgroundColor(`${KikariaColors.REMOVE_CORAL}10`);
                        Button.borderRadius(12);
                        Button.height(38);
                        Button.width('100%');
                        Button.border({ width: 1, color: `${KikariaColors.REMOVE_CORAL}33` });
                        Button.onClick(() => {
                            appState.togglePointReinforcement(point.id);
                            this.refreshState();
                            this.expandedId = '';
                        });
                    }, Button);
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
        Column.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
class ReinforcementPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: ReinforcementPage_Params) {
    }
    updateStateVars(params: ReinforcementPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/ReinforcementPage", isUserCreateStack: false });
            Navigation.debugLine("entry/src/main/ets/pages/ReinforcementPage.ets(226:5)", "entry");
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new ReinforcementPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/ReinforcementPage.ets", line: 227, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "ReinforcementPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "ReinforcementPage";
    }
}
registerNamedRoute(() => new ReinforcementPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/ReinforcementPage", pageFullPath: "entry/src/main/ets/pages/ReinforcementPage", integratedHsp: "false", moduleType: "followWithHap" });
