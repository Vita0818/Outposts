if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface InitialProfileSetupPage_Params {
}
interface InitialProfileSetupPageContent_Params {
    displayName?: string;
    userHandle?: string;
    toastMessage?: string;
    toastVisible?: boolean;
    contentPadH?: number;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { ProfileAvatar } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaComponents";
import { isTabletWidth, pageHorizontalPadding, formMaxWidth, setupCardMaxWidth, avatarSize } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function InitialProfileSetupPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new InitialProfileSetupPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/InitialProfileSetupPage.ets", line: 18, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "InitialProfileSetupPageContent" });
    }
}
export class InitialProfileSetupPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__displayName = new ObservedPropertySimplePU('', this, "displayName");
        this.__userHandle = new ObservedPropertySimplePU('', this, "userHandle");
        this.__toastMessage = new ObservedPropertySimplePU('', this, "toastMessage");
        this.__toastVisible = new ObservedPropertySimplePU(false, this, "toastVisible");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: InitialProfileSetupPageContent_Params) {
        if (params.displayName !== undefined) {
            this.displayName = params.displayName;
        }
        if (params.userHandle !== undefined) {
            this.userHandle = params.userHandle;
        }
        if (params.toastMessage !== undefined) {
            this.toastMessage = params.toastMessage;
        }
        if (params.toastVisible !== undefined) {
            this.toastVisible = params.toastVisible;
        }
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
    }
    updateStateVars(params: InitialProfileSetupPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__displayName.purgeDependencyOnElmtId(rmElmtId);
        this.__userHandle.purgeDependencyOnElmtId(rmElmtId);
        this.__toastMessage.purgeDependencyOnElmtId(rmElmtId);
        this.__toastVisible.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__displayName.aboutToBeDeleted();
        this.__userHandle.aboutToBeDeleted();
        this.__toastMessage.aboutToBeDeleted();
        this.__toastVisible.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
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
    private __toastMessage: ObservedPropertySimplePU<string>;
    get toastMessage() {
        return this.__toastMessage.get();
    }
    set toastMessage(newValue: string) {
        this.__toastMessage.set(newValue);
    }
    private __toastVisible: ObservedPropertySimplePU<boolean>;
    get toastVisible() {
        return this.__toastVisible.get();
    }
    set toastVisible(newValue: boolean) {
        this.__toastVisible.set(newValue);
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
        // Pre-fill with existing profile values if they're not defaults
        const p = appState.userProfile;
        this.displayName = p.displayName === 'Vita' ? '' : p.displayName;
        this.userHandle = p.userHandle === 'vita_0818' ? '' : p.userHandle;
    }
    showToast(msg: string): void {
        this.toastMessage = msg;
        this.toastVisible = true;
        setTimeout(() => { this.toastVisible = false; }, 2000);
    }
    saveProfile(): void {
        if (!this.canSave) {
            return;
        }
        const trimmedHandle = this.userHandle.trim().replace(/^@/, '');
        const finalHandle = trimmedHandle.length > 0 ? trimmedHandle : this.generatedHandle();
        appState.userProfile = {
            displayName: this.trimmedDisplayName,
            userHandle: finalHandle,
            avatarIconName: 'person'
        };
        appState.completeProfileSetup();
        appState.saveAppState();
        // Navigate to onboarding next
        navPathStack.replacePathByName('OnboardingPage', undefined);
    }
    generatedHandle(): string {
        const normalized = this.trimmedDisplayName
            .toLowerCase()
            .replace(/[^a-z0-9_一-鿿]/g, '_')
            .replace(/_+/g, '_')
            .replace(/^_|_$/g, '');
        return normalized.length > 0 ? normalized : 'kikaria_user';
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            NavDestination.create(() => {
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Stack.create();
                    Stack.width('100%');
                    Stack.height('100%');
                }, Stack);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Scroll.create();
                    Scroll.width('100%');
                    Scroll.height('100%');
                    Scroll.backgroundColor(KikariaColors.PAGE_BG);
                    Scroll.scrollBar(BarState.Off);
                    Scroll.align(Alignment.Center);
                }, Scroll);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: this.spacing });
                    Column.padding(this.isExpanded ? 32 : 24);
                    Column.width(`${this.setupMaxW}vp`);
                    Column.borderRadius(this.cardRadius);
                    Column.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                    Column.backdropBlur(24);
                    Column.shadow({
                        radius: this.isExpanded ? 28 : 24,
                        color: KikariaColors.SHADOW_COLOR,
                        offsetY: this.isExpanded ? 16 : 14
                    });
                    Column.padding({ left: this.contentPadH, right: this.contentPadH });
                    Column.constraintSize({ maxWidth: formMaxWidth() });
                    Column.width('100%');
                    Column.height('100%');
                    Column.justifyContent(FlexAlign.Center);
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: this.isExpanded ? 12 : 10 });
                    Column.width('100%');
                    Column.alignItems(HorizontalAlign.Center);
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('欢迎使用 Kikaria');
                    Text.fontSize(this.titleSize);
                    Text.fontWeight(FontWeight.Bold);
                    Text.fontColor(KikariaColors.DEEP_TEXT);
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('先设置你的个人资料');
                    Text.fontSize(this.subtitleSize);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Column.pop();
                {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        if (isInitialRender) {
                            let componentCall = new 
                            // Avatar
                            ProfileAvatar(this, {
                                displayName: this.trimmedDisplayName.length > 0 ? this.trimmedDisplayName : 'K',
                                avatarSize: this.avatarSz,
                                avatarFontSize: Math.round(this.avatarSz * 0.38),
                                avatarColor: KikariaColors.SKY,
                                textColor: '#FFFFFF'
                            }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/InitialProfileSetupPage.ets", line: 133, col: 13 });
                            ViewPU.create(componentCall);
                            let paramsLambda = () => {
                                return {
                                    displayName: this.trimmedDisplayName.length > 0 ? this.trimmedDisplayName : 'K',
                                    avatarSize: this.avatarSz,
                                    avatarFontSize: Math.round(this.avatarSz * 0.38),
                                    avatarColor: KikariaColors.SKY,
                                    textColor: '#FFFFFF'
                                };
                            };
                            componentCall.paramsGenerator_ = paramsLambda;
                        }
                        else {
                            this.updateStateVarsOfChildByElmtId(elmtId, {
                                displayName: this.trimmedDisplayName.length > 0 ? this.trimmedDisplayName : 'K',
                                avatarSize: this.avatarSz,
                                avatarFontSize: Math.round(this.avatarSz * 0.38),
                                avatarColor: KikariaColors.SKY,
                                textColor: '#FFFFFF'
                            });
                        }
                    }, { name: "ProfileAvatar" });
                }
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Form fields
                    Column.create({ space: this.innerSpacing });
                    // Form fields
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 6 });
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('昵称');
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                    Text.padding({ left: 4 });
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.displayName, placeholder: '输入你的昵称' });
                    TextInput.fontSize(17);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                    TextInput.borderRadius(14);
                    TextInput.padding(16);
                    TextInput.width('100%');
                    TextInput.onChange((value: string) => {
                        this.displayName = value;
                    });
                }, TextInput);
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 6 });
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('用户名');
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                    Text.padding({ left: 4 });
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.userHandle, placeholder: 'kikaria_user' });
                    TextInput.fontSize(17);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                    TextInput.borderRadius(14);
                    TextInput.padding(16);
                    TextInput.width('100%');
                    TextInput.onChange((value: string) => {
                        this.userHandle = value.replace(/^@/, '');
                    });
                }, TextInput);
                Column.pop();
                // Form fields
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Start button
                    Button.createWithChild();
                    // Start button
                    Button.width('100%');
                    // Start button
                    Button.height(this.isExpanded ? 56 : 52);
                    // Start button
                    Button.borderRadius(999);
                    // Start button
                    Button.linearGradient({
                        angle: 135,
                        colors: [[KikariaColors.SKY, 0], [KikariaColors.CYAN, 1]]
                    });
                    // Start button
                    Button.shadow({
                        radius: this.canSave ? 16 : 4,
                        color: `${KikariaColors.SKY}${this.canSave ? '38' : '0A'}`,
                        offsetY: 8
                    });
                    // Start button
                    Button.enabled(this.canSave);
                    // Start button
                    Button.opacity(this.canSave ? 1.0 : 0.48);
                    // Start button
                    Button.margin({ top: 4 });
                    // Start button
                    Button.onClick(() => {
                        this.saveProfile();
                    });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('开始使用');
                    Text.fontSize(this.buttonSize);
                    Text.fontWeight(FontWeight.Bold);
                    Text.fontColor('#FFFFFF');
                }, Text);
                Text.pop();
                // Start button
                Button.pop();
                Column.pop();
                Scroll.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    // Toast
                    if (this.toastVisible) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Row.create();
                                Row.borderRadius(20);
                                Row.backgroundColor(`${KikariaColors.DEEP_TEXT}E6`);
                                Row.position({ top: 80, left: '10%' });
                                Row.width('80%');
                                Row.justifyContent(FlexAlign.Center);
                            }, Row);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(this.toastMessage);
                                Text.fontSize(15);
                                Text.fontColor('#FFFFFF');
                                Text.padding({ left: 20, right: 20, top: 12, bottom: 12 });
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
                Stack.pop();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/InitialProfileSetupPage" });
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
class InitialProfileSetupPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: InitialProfileSetupPage_Params) {
    }
    updateStateVars(params: InitialProfileSetupPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/InitialProfileSetupPage", isUserCreateStack: false });
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new InitialProfileSetupPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/InitialProfileSetupPage.ets", line: 264, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "InitialProfileSetupPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "InitialProfileSetupPage";
    }
}
registerNamedRoute(() => new InitialProfileSetupPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/InitialProfileSetupPage", pageFullPath: "entry/src/main/ets/pages/InitialProfileSetupPage", integratedHsp: "false", moduleType: "followWithHap" });
