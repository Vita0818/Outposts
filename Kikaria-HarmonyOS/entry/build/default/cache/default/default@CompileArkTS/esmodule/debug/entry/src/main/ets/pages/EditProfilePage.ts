if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface EditProfilePage_Params {
}
interface EditProfilePageContent_Params {
    displayName?: string;
    userHandle?: string;
    contentPadH?: number;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, pageHorizontalPadding } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function EditProfilePageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new EditProfilePageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/EditProfilePage.ets", line: 13, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "EditProfilePageContent" });
    }
}
export class EditProfilePageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__displayName = new ObservedPropertySimplePU('', this, "displayName");
        this.__userHandle = new ObservedPropertySimplePU('', this, "userHandle");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: EditProfilePageContent_Params) {
        if (params.displayName !== undefined) {
            this.displayName = params.displayName;
        }
        if (params.userHandle !== undefined) {
            this.userHandle = params.userHandle;
        }
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
    }
    updateStateVars(params: EditProfilePageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__displayName.purgeDependencyOnElmtId(rmElmtId);
        this.__userHandle.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__displayName.aboutToBeDeleted();
        this.__userHandle.aboutToBeDeleted();
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
    private __contentPadH: ObservedPropertySimplePU<number>;
    get contentPadH() {
        return this.__contentPadH.get();
    }
    set contentPadH(newValue: number) {
        this.__contentPadH.set(newValue);
    }
    aboutToAppear(): void {
        this.contentPadH = pageHorizontalPadding();
        this.displayName = appState.userProfile.displayName;
        this.userHandle = appState.userProfile.userHandle;
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            NavDestination.create(() => {
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 18 });
                    Column.width('100%');
                    Column.constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' });
                    Column.padding({ left: this.contentPadH, right: this.contentPadH });
                    Column.backgroundColor(KikariaColors.PAGE_BG);
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Top bar
                    Row.create();
                    // Top bar
                    Row.width('100%');
                    // Top bar
                    Row.padding({ left: 20, right: 20, top: 12, bottom: 12 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithChild();
                    Button.backgroundColor(Color.Transparent);
                    Button.onClick(() => { navPathStack.pop(); });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('取消');
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
                    Text.create('编辑资料');
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
                        const trimmedName = this.displayName.trim();
                        const trimmedHandle = this.userHandle.trim();
                        if (trimmedName.length === 0) {
                            return;
                        }
                        appState.userProfile = {
                            displayName: trimmedName,
                            userHandle: trimmedHandle.length > 0 ? trimmedHandle.replace(/^@/, '') : 'user',
                            avatarIconName: 'person'
                        };
                        appState.saveAppState();
                        navPathStack.pop();
                    });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('保存');
                    Text.fontSize(17);
                    Text.fontWeight(FontWeight.Bold);
                    Text.fontColor(KikariaColors.SKY);
                }, Text);
                Text.pop();
                Button.pop();
                // Top bar
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Avatar placeholder
                    Column.create({ space: 12 });
                    // Avatar placeholder
                    Column.width('100%');
                    // Avatar placeholder
                    Column.padding({ top: 12, bottom: 8 });
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.width(80);
                    Row.height(80);
                    Row.borderRadius(40);
                    Row.backgroundColor(KikariaColors.SKY);
                    Row.justifyContent(FlexAlign.Center);
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(this.displayName.trim().charAt(0).toUpperCase());
                    Text.fontSize(40);
                    Text.fontWeight(FontWeight.Bold);
                    Text.fontColor('#FFFFFF');
                }, Text);
                Text.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('轻触更换头像（暂不支持）');
                    Text.fontSize(12);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                // Avatar placeholder
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Name field
                    Column.create({ space: 8 });
                    // Name field
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('显示名称');
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.displayName, placeholder: '输入名称' });
                    TextInput.fontSize(17);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG);
                    TextInput.borderRadius(12);
                    TextInput.padding({ left: 16, right: 16, top: 12, bottom: 12 });
                    TextInput.width('100%');
                    TextInput.onChange((value: string) => {
                        this.displayName = value;
                    });
                }, TextInput);
                // Name field
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Handle field
                    Column.create({ space: 8 });
                    // Handle field
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('用户标识');
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.userHandle, placeholder: 'user_handle' });
                    TextInput.fontSize(17);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG);
                    TextInput.borderRadius(12);
                    TextInput.padding({ left: 16, right: 16, top: 12, bottom: 12 });
                    TextInput.width('100%');
                    TextInput.onChange((value: string) => {
                        this.userHandle = value.replace(/^@/, '');
                    });
                }, TextInput);
                // Handle field
                Column.pop();
                Column.pop();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/EditProfilePage" });
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
class EditProfilePage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: EditProfilePage_Params) {
    }
    updateStateVars(params: EditProfilePage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/EditProfilePage", isUserCreateStack: false });
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new EditProfilePageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/EditProfilePage.ets", line: 154, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "EditProfilePageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "EditProfilePage";
    }
}
registerNamedRoute(() => new EditProfilePage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/EditProfilePage", pageFullPath: "entry/src/main/ets/pages/EditProfilePage", integratedHsp: "false", moduleType: "followWithHap" });
