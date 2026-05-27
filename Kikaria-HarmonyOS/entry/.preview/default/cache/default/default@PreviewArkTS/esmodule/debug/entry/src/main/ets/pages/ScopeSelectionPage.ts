if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface ScopeSelectionPage_Params {
}
interface ScopeSelectionPageContent_Params {
    allTags?: string[];
    selectedTags?: Set<string>;
    contentPadH?: number;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, pageHorizontalPadding, ipadPortraitListPageTopInset } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function ScopeSelectionPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new ScopeSelectionPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/ScopeSelectionPage.ets", line: 14, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "ScopeSelectionPageContent" });
    }
}
export class ScopeSelectionPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__allTags = new ObservedPropertyObjectPU([], this, "allTags");
        this.__selectedTags = new ObservedPropertyObjectPU(new Set(), this, "selectedTags");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: ScopeSelectionPageContent_Params) {
        if (params.allTags !== undefined) {
            this.allTags = params.allTags;
        }
        if (params.selectedTags !== undefined) {
            this.selectedTags = params.selectedTags;
        }
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
    }
    updateStateVars(params: ScopeSelectionPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__allTags.purgeDependencyOnElmtId(rmElmtId);
        this.__selectedTags.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__allTags.aboutToBeDeleted();
        this.__selectedTags.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
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
    private __contentPadH: ObservedPropertySimplePU<number>;
    get contentPadH() {
        return this.__contentPadH.get();
    }
    set contentPadH(newValue: number) {
        this.__contentPadH.set(newValue);
    }
    aboutToAppear(): void {
        this.contentPadH = pageHorizontalPadding();
        this.allTags = appState.allTags;
        this.selectedTags = new Set(appState.selectedTags);
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            NavDestination.create(() => {
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create();
                    Column.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(31:7)", "entry");
                    Column.width('100%');
                    Column.height('100%');
                    Column.backgroundColor(KikariaColors.PAGE_BG);
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Top bar
                    Row.create();
                    Row.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(33:9)", "entry");
                    // Top bar
                    Row.width('100%');
                    // Top bar
                    Row.padding({ left: 20, right: 20, top: 12 + ipadPortraitListPageTopInset(), bottom: 12 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithChild();
                    Button.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(34:11)", "entry");
                    Button.backgroundColor(Color.Transparent);
                    Button.onClick(() => { navPathStack.pop(); });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('← 返回');
                    Text.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(35:13)", "entry");
                    Text.fontSize(17);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Button.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                    Blank.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(42:11)", "entry");
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('范围');
                    Text.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(44:11)", "entry");
                    Text.fontSize(17);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.DEEP_TEXT);
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                    Blank.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(49:11)", "entry");
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(`${this.selectedTags.size}/${this.allTags.length}`);
                    Text.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(51:11)", "entry");
                    Text.fontSize(14);
                    Text.fontColor(KikariaColors.SKY);
                }, Text);
                Text.pop();
                // Top bar
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Scroll.create();
                    Scroll.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(58:9)", "entry");
                    Scroll.width('100%');
                    Scroll.layoutWeight(1);
                    Scroll.scrollBar(BarState.Off);
                }, Scroll);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 12 });
                    Column.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(59:11)", "entry");
                    Column.width('100%');
                    Column.constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' });
                    Column.padding({ left: this.contentPadH, right: this.contentPadH });
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create({ space: 12 });
                    Row.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(60:13)", "entry");
                    Row.width('100%');
                    Row.padding({ top: 8 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithLabel('全选');
                    Button.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(61:15)", "entry");
                    Button.fontSize(14);
                    Button.fontWeight(FontWeight.Medium);
                    Button.fontColor(KikariaColors.SKY);
                    Button.backgroundColor(KikariaColors.MIST);
                    Button.borderRadius(12);
                    Button.height(36);
                    Button.padding({ left: 16, right: 16 });
                    Button.onClick(() => {
                        this.selectedTags = new Set([...this.allTags]);
                    });
                }, Button);
                Button.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithLabel('取消全选');
                    Button.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(73:15)", "entry");
                    Button.fontSize(14);
                    Button.fontWeight(FontWeight.Medium);
                    Button.fontColor(KikariaColors.SOFT_TEXT);
                    Button.backgroundColor(KikariaColors.MIST);
                    Button.borderRadius(12);
                    Button.height(36);
                    Button.padding({ left: 16, right: 16 });
                    Button.onClick(() => {
                        this.selectedTags = new Set();
                    });
                }, Button);
                Button.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Flex.create({ wrap: FlexWrap.Wrap, justifyContent: FlexAlign.Start });
                    Flex.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(88:13)", "entry");
                    Flex.width('100%');
                }, Flex);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    ForEach.create();
                    const forEachItemGenFunction = _item => {
                        const tag = _item;
                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                            Text.create(tag);
                            Text.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(90:17)", "entry");
                            Text.fontSize(14);
                            Text.fontWeight(FontWeight.Medium);
                            Text.fontColor(this.selectedTags.has(tag) ? '#FFFFFF' : KikariaColors.SOFT_TEXT);
                            Text.backgroundColor(this.selectedTags.has(tag) ? KikariaColors.SKY : KikariaColors.MIST);
                            Text.borderRadius(16);
                            Text.padding({ left: 16, right: 16, top: 8, bottom: 8 });
                            Text.margin({ right: 10, bottom: 10 });
                            Text.onClick(() => {
                                if (this.selectedTags.has(tag)) {
                                    this.selectedTags.delete(tag);
                                }
                                else {
                                    this.selectedTags.add(tag);
                                }
                            });
                        }, Text);
                        Text.pop();
                    };
                    this.forEachUpdateFunction(elmtId, this.allTags, forEachItemGenFunction);
                }, ForEach);
                ForEach.pop();
                Flex.pop();
                Column.pop();
                Scroll.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(117:9)", "entry");
                    Row.width('100%');
                    Row.padding({ left: 24, right: 24, top: 12, bottom: 24 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithLabel('应用并开始复习');
                    Button.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(118:11)", "entry");
                    Button.fontSize(17);
                    Button.fontWeight(FontWeight.Medium);
                    Button.fontColor('#FFFFFF');
                    Button.linearGradient({
                        angle: 135,
                        colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
                    });
                    Button.borderRadius(16);
                    Button.height(50);
                    Button.width('100%');
                    Button.shadow({ radius: 12, color: KikariaColors.SHADOW_COLOR, offsetY: 6 });
                    Button.onClick(() => {
                        appState.selectedTags = new Set(this.selectedTags);
                        appState.saveAppState();
                        navPathStack.pop();
                    });
                }, Button);
                Button.pop();
                Row.pop();
                Column.pop();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/ScopeSelectionPage" });
            NavDestination.hideTitleBar(true);
            NavDestination.width('100%');
            NavDestination.height('100%');
            NavDestination.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(30:5)", "entry");
        }, NavDestination);
        NavDestination.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
class ScopeSelectionPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: ScopeSelectionPage_Params) {
    }
    updateStateVars(params: ScopeSelectionPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/ScopeSelectionPage", isUserCreateStack: false });
            Navigation.debugLine("entry/src/main/ets/pages/ScopeSelectionPage.ets(153:5)", "entry");
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new ScopeSelectionPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/ScopeSelectionPage.ets", line: 154, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "ScopeSelectionPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "ScopeSelectionPage";
    }
}
registerNamedRoute(() => new ScopeSelectionPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/ScopeSelectionPage", pageFullPath: "entry/src/main/ets/pages/ScopeSelectionPage", integratedHsp: "false", moduleType: "followWithHap" });
