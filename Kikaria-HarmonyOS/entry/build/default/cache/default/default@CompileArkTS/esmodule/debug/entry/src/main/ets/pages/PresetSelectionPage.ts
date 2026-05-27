if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface PresetSelectionPage_Params {
}
interface PresetSelectionPageContent_Params {
    presets?: KnowledgePreset[];
    currentPresetID?: string;
    contentPadH?: number;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import type { KnowledgePreset } from '../model/KnowledgePoint';
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, pageHorizontalPadding, ipadPortraitListPageTopInset } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack, RouteParams } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function PresetSelectionPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new PresetSelectionPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/PresetSelectionPage.ets", line: 15, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "PresetSelectionPageContent" });
    }
}
export class PresetSelectionPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__presets = new ObservedPropertyObjectPU([], this, "presets");
        this.__currentPresetID = new ObservedPropertySimplePU('', this, "currentPresetID");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: PresetSelectionPageContent_Params) {
        if (params.presets !== undefined) {
            this.presets = params.presets;
        }
        if (params.currentPresetID !== undefined) {
            this.currentPresetID = params.currentPresetID;
        }
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
    }
    updateStateVars(params: PresetSelectionPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__presets.purgeDependencyOnElmtId(rmElmtId);
        this.__currentPresetID.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__presets.aboutToBeDeleted();
        this.__currentPresetID.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __presets: ObservedPropertyObjectPU<KnowledgePreset[]>;
    get presets() {
        return this.__presets.get();
    }
    set presets(newValue: KnowledgePreset[]) {
        this.__presets.set(newValue);
    }
    private __currentPresetID: ObservedPropertySimplePU<string>;
    get currentPresetID() {
        return this.__currentPresetID.get();
    }
    set currentPresetID(newValue: string) {
        this.__currentPresetID.set(newValue);
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
        this.presets = [...appState.presets];
        this.currentPresetID = appState.currentPresetID;
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
                    // Top bar
                    Row.create();
                    // Top bar
                    Row.width('100%');
                    // Top bar
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
                    Text.create('预设管理');
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
                    Button.createWithChild();
                    Button.backgroundColor(Color.Transparent);
                    Button.onClick(() => {
                        navPathStack.pushPathByName('NewPresetPage', undefined);
                    });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('+ 新建');
                    Text.fontSize(15);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SKY);
                }, Text);
                Text.pop();
                Button.pop();
                // Top bar
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    List.create({ space: 12 });
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
                        const preset = _item;
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
                                    Column.create({ space: 6 });
                                    Column.width('100%');
                                    Column.padding(16);
                                    Column.borderRadius(18);
                                    Column.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                                    Column.backdropBlur(14);
                                    Column.shadow({ radius: 10, color: KikariaColors.SHADOW_COLOR, offsetY: 3 });
                                }, Column);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Row.create({ space: 10 });
                                    Row.width('100%');
                                }, Row);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Column.create({ space: 4 });
                                    Column.layoutWeight(1);
                                    Column.alignItems(HorizontalAlign.Start);
                                }, Column);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Row.create({ space: 8 });
                                }, Row);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    If.create();
                                    if (this.currentPresetID === preset.id) {
                                        this.ifElseBranchUpdateFunction(0, () => {
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                Text.create('●');
                                                Text.fontSize(10);
                                                Text.fontColor(KikariaColors.MASTERED_GREEN);
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
                                    Text.create(preset.name);
                                    Text.fontSize(17);
                                    Text.fontWeight(this.currentPresetID === preset.id ? FontWeight.Bold : FontWeight.Medium);
                                    Text.fontColor(KikariaColors.DEEP_TEXT);
                                }, Text);
                                Text.pop();
                                Row.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(preset.description);
                                    Text.fontSize(13);
                                    Text.fontColor(KikariaColors.SOFT_TEXT);
                                    Text.maxLines(2);
                                    Text.textOverflow({ overflow: TextOverflow.Ellipsis });
                                    Text.width('100%');
                                }, Text);
                                Text.pop();
                                Column.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(`${preset.knowledgePointCount} 条`);
                                    Text.fontSize(12);
                                    Text.fontColor(KikariaColors.TERTIARY_TEXT);
                                }, Text);
                                Text.pop();
                                Row.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Row.create({ space: 8 });
                                    Row.width('100%');
                                    Row.padding({ top: 8 });
                                }, Row);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    If.create();
                                    if (this.currentPresetID !== preset.id) {
                                        this.ifElseBranchUpdateFunction(0, () => {
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                Button.createWithLabel('切换至此预设');
                                                Button.fontSize(14);
                                                Button.fontWeight(FontWeight.Medium);
                                                Button.fontColor('#FFFFFF');
                                                Button.linearGradient({
                                                    angle: 135,
                                                    colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
                                                });
                                                Button.borderRadius(12);
                                                Button.height(36);
                                                Button.padding({ left: 16, right: 16 });
                                                Button.onClick(() => {
                                                    const success = appState.switchToPreset(preset);
                                                    if (success) {
                                                        this.currentPresetID = preset.id;
                                                        this.refreshState();
                                                    }
                                                });
                                            }, Button);
                                            Button.pop();
                                        });
                                    }
                                    else {
                                        this.ifElseBranchUpdateFunction(1, () => {
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                Text.create('当前');
                                                Text.fontSize(14);
                                                Text.fontColor(KikariaColors.MASTERED_GREEN);
                                            }, Text);
                                            Text.pop();
                                        });
                                    }
                                }, If);
                                If.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Blank.create();
                                }, Blank);
                                Blank.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Button.createWithLabel('编辑');
                                    Button.fontSize(13);
                                    Button.fontWeight(FontWeight.Medium);
                                    Button.fontColor(KikariaColors.SKY);
                                    Button.backgroundColor(`${KikariaColors.SKY}10`);
                                    Button.borderRadius(10);
                                    Button.height(32);
                                    Button.padding({ left: 12, right: 12 });
                                    Button.onClick(() => {
                                        if (this.currentPresetID !== preset.id) {
                                            const success = appState.switchToPreset(preset);
                                            if (success) {
                                                this.currentPresetID = preset.id;
                                            }
                                        }
                                        navPathStack.pushPathByName('EditPresetPage', new RouteParams(preset.id));
                                    });
                                }, Button);
                                Button.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    If.create();
                                    if (!preset.isBuiltIn && appState.presets.length > 1) {
                                        this.ifElseBranchUpdateFunction(0, () => {
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                Button.createWithLabel('删除');
                                                Button.fontSize(13);
                                                Button.fontWeight(FontWeight.Medium);
                                                Button.fontColor(KikariaColors.REMOVE_CORAL);
                                                Button.backgroundColor(`${KikariaColors.REMOVE_CORAL}10`);
                                                Button.borderRadius(10);
                                                Button.height(32);
                                                Button.padding({ left: 12, right: 12 });
                                                Button.onClick(() => {
                                                    appState.deletePreset(preset.id);
                                                    this.refreshState();
                                                });
                                            }, Button);
                                            Button.pop();
                                        });
                                    }
                                    else {
                                        this.ifElseBranchUpdateFunction(1, () => {
                                        });
                                    }
                                }, If);
                                If.pop();
                                Row.pop();
                                Column.pop();
                                ListItem.pop();
                            };
                            this.observeComponentCreation2(itemCreation2, ListItem);
                            ListItem.pop();
                        }
                    };
                    this.forEachUpdateFunction(elmtId, this.presets, forEachItemGenFunction);
                }, ForEach);
                ForEach.pop();
                List.pop();
                Column.pop();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/PresetSelectionPage" });
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
class PresetSelectionPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: PresetSelectionPage_Params) {
    }
    updateStateVars(params: PresetSelectionPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/PresetSelectionPage", isUserCreateStack: false });
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new PresetSelectionPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/PresetSelectionPage.ets", line: 204, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "PresetSelectionPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "PresetSelectionPage";
    }
}
registerNamedRoute(() => new PresetSelectionPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/PresetSelectionPage", pageFullPath: "entry/src/main/ets/pages/PresetSelectionPage", integratedHsp: "false", moduleType: "followWithHap" });
