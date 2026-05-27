if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface OnboardingPage_Params {
}
interface OnboardingPageContent_Params {
    currentPage?: number;
    contentPadH?: number;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, pageHorizontalPadding } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function OnboardingPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new OnboardingPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/OnboardingPage.ets", line: 14, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "OnboardingPageContent" });
    }
}
export class OnboardingPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__currentPage = new ObservedPropertySimplePU(0, this, "currentPage");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: OnboardingPageContent_Params) {
        if (params.currentPage !== undefined) {
            this.currentPage = params.currentPage;
        }
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
    }
    updateStateVars(params: OnboardingPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__currentPage.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__currentPage.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __currentPage: ObservedPropertySimplePU<number>;
    get currentPage() {
        return this.__currentPage.get();
    }
    set currentPage(newValue: number) {
        this.__currentPage.set(newValue);
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
                    Row.padding({ left: 20, right: 20, top: 12, bottom: 12 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithLabel('跳过');
                    Button.fontSize(15);
                    Button.fontWeight(FontWeight.Medium);
                    Button.fontColor(KikariaColors.SKY);
                    Button.backgroundColor(Color.Transparent);
                    Button.onClick(() => {
                        appState.completeOnboarding();
                        navPathStack.pop();
                    });
                }, Button);
                Button.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Page indicator
                    Row.create({ space: 8 });
                    // Page indicator
                    Row.justifyContent(FlexAlign.Center);
                    // Page indicator
                    Row.width('100%');
                    // Page indicator
                    Row.padding({ top: 16, bottom: 16 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    ForEach.create();
                    const forEachItemGenFunction = _item => {
                        const i = _item;
                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                            Row.create();
                            Row.width(this.currentPage === i ? 24 : 8);
                            Row.height(8);
                            Row.borderRadius(4);
                            Row.backgroundColor(this.currentPage === i ? KikariaColors.SKY : KikariaColors.MIST);
                        }, Row);
                        Row.pop();
                    };
                    this.forEachUpdateFunction(elmtId, [0, 1, 2], forEachItemGenFunction);
                }, ForEach);
                ForEach.pop();
                // Page indicator
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Swipeable content area
                    Column.create();
                    // Swipeable content area
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (this.currentPage === 0) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 20 });
                                Column.width('100%');
                                Column.layoutWeight(1);
                                Column.justifyContent(FlexAlign.Center);
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('📚');
                                Text.fontSize(64);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('欢迎使用 Kikaria');
                                Text.fontSize(28);
                                Text.fontWeight(FontWeight.Bold);
                                Text.fontColor(KikariaColors.DEEP_TEXT);
                                Text.fontFamily('serif');
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('Kikaria 帮助你系统化地学习和记忆知识点。\n通过间隔重复和主动回忆，提高学习效率。');
                                Text.fontSize(16);
                                Text.fontColor(KikariaColors.SOFT_TEXT);
                                Text.textAlign(TextAlign.Center);
                                Text.lineHeight(26);
                                Text.padding({ left: 32, right: 32 });
                            }, Text);
                            Text.pop();
                            Column.pop();
                        });
                    }
                    else if (this.currentPage === 1) {
                        this.ifElseBranchUpdateFunction(1, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 20 });
                                Column.width('100%');
                                Column.layoutWeight(1);
                                Column.justifyContent(FlexAlign.Center);
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('🔄');
                                Text.fontSize(64);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('间隔复习');
                                Text.fontSize(28);
                                Text.fontWeight(FontWeight.Bold);
                                Text.fontColor(KikariaColors.DEEP_TEXT);
                                Text.fontFamily('serif');
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('每次学习时，先看提示尝试回忆，再看答案验证。\n将重要知识点加入「重点集锦」强化练习，\n掌握的标记为「已掌握」跟踪进度。');
                                Text.fontSize(16);
                                Text.fontColor(KikariaColors.SOFT_TEXT);
                                Text.textAlign(TextAlign.Center);
                                Text.lineHeight(26);
                                Text.padding({ left: 32, right: 32 });
                            }, Text);
                            Text.pop();
                            Column.pop();
                        });
                    }
                    else {
                        this.ifElseBranchUpdateFunction(2, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 20 });
                                Column.width('100%');
                                Column.layoutWeight(1);
                                Column.justifyContent(FlexAlign.Center);
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('📝');
                                Text.fontSize(64);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('Markdown 预设');
                                Text.fontSize(28);
                                Text.fontWeight(FontWeight.Bold);
                                Text.fontColor(KikariaColors.DEEP_TEXT);
                                Text.fontFamily('serif');
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('通过 Markdown 创建自定义知识库。\n内置多个学科预设帮助你快速开始，\n也可以创建自己的专属预设。');
                                Text.fontSize(16);
                                Text.fontColor(KikariaColors.SOFT_TEXT);
                                Text.textAlign(TextAlign.Center);
                                Text.lineHeight(26);
                                Text.padding({ left: 32, right: 32 });
                            }, Text);
                            Text.pop();
                            Column.pop();
                        });
                    }
                }, If);
                If.pop();
                // Swipeable content area
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Navigation buttons
                    Row.create({ space: 16 });
                    // Navigation buttons
                    Row.width('100%');
                    // Navigation buttons
                    Row.constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' });
                    // Navigation buttons
                    Row.padding({ left: this.contentPadH, right: this.contentPadH, bottom: 24 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithLabel('上一步');
                    Button.fontSize(15);
                    Button.fontColor(KikariaColors.SOFT_TEXT);
                    Button.backgroundColor(KikariaColors.MIST);
                    Button.borderRadius(14);
                    Button.height(44);
                    Button.layoutWeight(1);
                    Button.enabled(this.currentPage > 0);
                    Button.onClick(() => {
                        if (this.currentPage > 0) {
                            this.currentPage--;
                        }
                    });
                }, Button);
                Button.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (this.currentPage < 2) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Button.createWithLabel('下一步');
                                Button.fontSize(15);
                                Button.fontWeight(FontWeight.Medium);
                                Button.fontColor('#FFFFFF');
                                Button.linearGradient({
                                    angle: 135,
                                    colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
                                });
                                Button.borderRadius(14);
                                Button.height(44);
                                Button.layoutWeight(1);
                                Button.onClick(() => { this.currentPage++; });
                            }, Button);
                            Button.pop();
                        });
                    }
                    else {
                        this.ifElseBranchUpdateFunction(1, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Button.createWithLabel('开始使用');
                                Button.fontSize(15);
                                Button.fontWeight(FontWeight.Bold);
                                Button.fontColor('#FFFFFF');
                                Button.linearGradient({
                                    angle: 135,
                                    colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
                                });
                                Button.borderRadius(14);
                                Button.height(44);
                                Button.layoutWeight(1);
                                Button.shadow({ radius: 10, color: KikariaColors.SHADOW_COLOR, offsetY: 4 });
                                Button.onClick(() => {
                                    appState.completeOnboarding();
                                    navPathStack.pop();
                                });
                            }, Button);
                            Button.pop();
                        });
                    }
                }, If);
                If.pop();
                // Navigation buttons
                Row.pop();
                Column.pop();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/OnboardingPage" });
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
class OnboardingPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: OnboardingPage_Params) {
    }
    updateStateVars(params: OnboardingPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/OnboardingPage", isUserCreateStack: false });
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new OnboardingPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/OnboardingPage.ets", line: 192, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "OnboardingPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "OnboardingPage";
    }
}
registerNamedRoute(() => new OnboardingPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/OnboardingPage", pageFullPath: "entry/src/main/ets/pages/OnboardingPage", integratedHsp: "false", moduleType: "followWithHap" });
