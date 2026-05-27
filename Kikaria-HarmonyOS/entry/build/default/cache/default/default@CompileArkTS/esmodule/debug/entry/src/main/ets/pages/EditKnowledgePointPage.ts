if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface EditKnowledgePointPage_Params {
}
interface EditKnowledgePointPageContent_Params {
    pointId?: string;
    isNew?: boolean;
    title?: string;
    tagsText?: string;
    hint?: string;
    content?: string;
    toastMessage?: string;
    toastVisible?: boolean;
    showDeleteConfirm?: boolean;
    contentPadH?: number;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { KnowledgePoint, generateId } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, pageHorizontalPadding } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack, getRouteParams } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function EditKnowledgePointPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new EditKnowledgePointPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/EditKnowledgePointPage.ets", line: 15, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "EditKnowledgePointPageContent" });
    }
}
export class EditKnowledgePointPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__pointId = new ObservedPropertySimplePU('', this, "pointId");
        this.__isNew = new ObservedPropertySimplePU(false, this, "isNew");
        this.__title = new ObservedPropertySimplePU('', this, "title");
        this.__tagsText = new ObservedPropertySimplePU('', this, "tagsText");
        this.__hint = new ObservedPropertySimplePU('', this, "hint");
        this.__content = new ObservedPropertySimplePU('', this, "content");
        this.__toastMessage = new ObservedPropertySimplePU('', this, "toastMessage");
        this.__toastVisible = new ObservedPropertySimplePU(false, this, "toastVisible");
        this.__showDeleteConfirm = new ObservedPropertySimplePU(false, this, "showDeleteConfirm");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: EditKnowledgePointPageContent_Params) {
        if (params.pointId !== undefined) {
            this.pointId = params.pointId;
        }
        if (params.isNew !== undefined) {
            this.isNew = params.isNew;
        }
        if (params.title !== undefined) {
            this.title = params.title;
        }
        if (params.tagsText !== undefined) {
            this.tagsText = params.tagsText;
        }
        if (params.hint !== undefined) {
            this.hint = params.hint;
        }
        if (params.content !== undefined) {
            this.content = params.content;
        }
        if (params.toastMessage !== undefined) {
            this.toastMessage = params.toastMessage;
        }
        if (params.toastVisible !== undefined) {
            this.toastVisible = params.toastVisible;
        }
        if (params.showDeleteConfirm !== undefined) {
            this.showDeleteConfirm = params.showDeleteConfirm;
        }
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
    }
    updateStateVars(params: EditKnowledgePointPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__pointId.purgeDependencyOnElmtId(rmElmtId);
        this.__isNew.purgeDependencyOnElmtId(rmElmtId);
        this.__title.purgeDependencyOnElmtId(rmElmtId);
        this.__tagsText.purgeDependencyOnElmtId(rmElmtId);
        this.__hint.purgeDependencyOnElmtId(rmElmtId);
        this.__content.purgeDependencyOnElmtId(rmElmtId);
        this.__toastMessage.purgeDependencyOnElmtId(rmElmtId);
        this.__toastVisible.purgeDependencyOnElmtId(rmElmtId);
        this.__showDeleteConfirm.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__pointId.aboutToBeDeleted();
        this.__isNew.aboutToBeDeleted();
        this.__title.aboutToBeDeleted();
        this.__tagsText.aboutToBeDeleted();
        this.__hint.aboutToBeDeleted();
        this.__content.aboutToBeDeleted();
        this.__toastMessage.aboutToBeDeleted();
        this.__toastVisible.aboutToBeDeleted();
        this.__showDeleteConfirm.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __pointId: ObservedPropertySimplePU<string>;
    get pointId() {
        return this.__pointId.get();
    }
    set pointId(newValue: string) {
        this.__pointId.set(newValue);
    }
    private __isNew: ObservedPropertySimplePU<boolean>;
    get isNew() {
        return this.__isNew.get();
    }
    set isNew(newValue: boolean) {
        this.__isNew.set(newValue);
    }
    private __title: ObservedPropertySimplePU<string>;
    get title() {
        return this.__title.get();
    }
    set title(newValue: string) {
        this.__title.set(newValue);
    }
    private __tagsText: ObservedPropertySimplePU<string>;
    get tagsText() {
        return this.__tagsText.get();
    }
    set tagsText(newValue: string) {
        this.__tagsText.set(newValue);
    }
    private __hint: ObservedPropertySimplePU<string>;
    get hint() {
        return this.__hint.get();
    }
    set hint(newValue: string) {
        this.__hint.set(newValue);
    }
    private __content: ObservedPropertySimplePU<string>;
    get content() {
        return this.__content.get();
    }
    set content(newValue: string) {
        this.__content.set(newValue);
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
    private __showDeleteConfirm: ObservedPropertySimplePU<boolean>;
    get showDeleteConfirm() {
        return this.__showDeleteConfirm.get();
    }
    set showDeleteConfirm(newValue: boolean) {
        this.__showDeleteConfirm.set(newValue);
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
        const params = getRouteParams();
        if (params && params['pointId']) {
            this.pointId = params['pointId'] as string;
            this.isNew = false;
            const point = appState.knowledgePoints.find(kp => kp.id === this.pointId);
            if (point) {
                this.title = point.title;
                this.tagsText = point.tags.join(', ');
                this.hint = point.hint;
                this.content = point.content;
            }
        }
        else {
            this.isNew = true;
            this.pointId = generateId();
        }
    }
    showToast(msg: string): void {
        this.toastMessage = msg;
        this.toastVisible = true;
        setTimeout(() => { this.toastVisible = false; }, 2000);
    }
    savePoint(): void {
        if (this.title.trim().length === 0) {
            this.showToast('标题不能为空');
            return;
        }
        const tags = this.tagsText.split(/[,，]/).map(t => t.trim()).filter(t => t.length > 0);
        const now = Date.now();
        const point = new KnowledgePoint(this.pointId, this.title.trim(), tags, this.hint.trim(), this.content.trim(), false, false, now, now, 0, null);
        appState.upsertKnowledgePoint(point);
        this.showToast(this.isNew ? '知识点已创建' : '知识点已更新');
        setTimeout(() => { navPathStack.pop(); }, 400);
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
                    Text.create(this.isNew ? '新建知识点' : '编辑知识点');
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
                    Button.onClick(() => { this.savePoint(); });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('保存');
                    Text.fontSize(15);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SKY);
                }, Text);
                Text.pop();
                Button.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Scroll.create();
                    Scroll.width('100%');
                    Scroll.layoutWeight(1);
                    Scroll.scrollBar(BarState.Off);
                }, Scroll);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 14 });
                    Column.width('100%');
                    Column.constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' });
                    Column.padding({ left: this.contentPadH, right: this.contentPadH });
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Title
                    Column.create({ space: 6 });
                    // Title
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('标题');
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                    Text.padding({ left: 4 });
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.title, placeholder: '知识点标题，以 # 开头' });
                    TextInput.fontSize(17);
                    TextInput.fontWeight(FontWeight.Medium);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG);
                    TextInput.borderRadius(14);
                    TextInput.padding(16);
                    TextInput.onChange((value: string) => { this.title = value; });
                }, TextInput);
                // Title
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Tags
                    Column.create({ space: 6 });
                    // Tags
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('标签（逗号分隔）');
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                    Text.padding({ left: 4 });
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.tagsText, placeholder: '例如: 数学, 微积分, 公式' });
                    TextInput.fontSize(15);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG);
                    TextInput.borderRadius(14);
                    TextInput.padding(16);
                    TextInput.onChange((value: string) => { this.tagsText = value; });
                }, TextInput);
                // Tags
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Hint
                    Column.create({ space: 6 });
                    // Hint
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('提示');
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SKY);
                    Text.width('100%');
                    Text.padding({ left: 4 });
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextArea.create({ text: this.hint, placeholder: '提示文本（可选）' });
                    TextArea.fontSize(15);
                    TextArea.fontColor(KikariaColors.DEEP_TEXT);
                    TextArea.backgroundColor(KikariaColors.CARD_BG);
                    TextArea.borderRadius(14);
                    TextArea.padding(16);
                    TextArea.height(100);
                    TextArea.onChange((value: string) => { this.hint = value; });
                }, TextArea);
                // Hint
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Content
                    Column.create({ space: 6 });
                    // Content
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('答案');
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.MASTERED_GREEN);
                    Text.width('100%');
                    Text.padding({ left: 4 });
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextArea.create({ text: this.content, placeholder: '答案/内容（支持 Markdown 和 LaTeX）' });
                    TextArea.fontSize(15);
                    TextArea.fontColor(KikariaColors.DEEP_TEXT);
                    TextArea.backgroundColor(KikariaColors.CARD_BG);
                    TextArea.borderRadius(14);
                    TextArea.padding(16);
                    TextArea.height(160);
                    TextArea.onChange((value: string) => { this.content = value; });
                }, TextArea);
                // Content
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    // Delete button (only for existing)
                    if (!this.isNew) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Button.createWithLabel('删除此知识点');
                                Button.fontSize(15);
                                Button.fontWeight(FontWeight.Medium);
                                Button.fontColor('#FFFFFF');
                                Button.width('100%');
                                Button.height(48);
                                Button.borderRadius(14);
                                Button.linearGradient({
                                    angle: 135,
                                    colors: [[KikariaColors.REMOVE_GRADIENT_START, 0], [KikariaColors.REMOVE_GRADIENT_END, 1]]
                                });
                                Button.margin({ top: 12 });
                                Button.onClick(() => { this.showDeleteConfirm = true; });
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
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.height(40);
                }, Row);
                Row.pop();
                Column.pop();
                Scroll.pop();
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    // Delete confirmation
                    if (this.showDeleteConfirm) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create();
                                Column.width('100%');
                                Column.height('100%');
                                Column.justifyContent(FlexAlign.Center);
                                Column.backgroundColor(KikariaColors.OVERLAY_BG);
                                Column.onClick(() => { this.showDeleteConfirm = false; });
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 16 });
                                Column.width('80%');
                                Column.padding(24);
                                Column.borderRadius(20);
                                Column.backgroundColor(KikariaColors.CARD_BG);
                                Column.shadow({ radius: 24, color: KikariaColors.SHADOW_COLOR, offsetY: 8 });
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('删除知识点');
                                Text.fontSize(17);
                                Text.fontWeight(FontWeight.Bold);
                                Text.fontColor(KikariaColors.DEEP_TEXT);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('确定要删除此知识点吗？此操作不可撤销。');
                                Text.fontSize(14);
                                Text.fontColor(KikariaColors.SOFT_TEXT);
                                Text.textAlign(TextAlign.Center);
                            }, Text);
                            Text.pop();
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
                                Button.onClick(() => { this.showDeleteConfirm = false; });
                            }, Button);
                            Button.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Button.createWithLabel('确认删除');
                                Button.fontSize(15);
                                Button.fontWeight(FontWeight.Bold);
                                Button.fontColor('#FFFFFF');
                                Button.linearGradient({
                                    angle: 135,
                                    colors: [[KikariaColors.REMOVE_GRADIENT_START, 0], [KikariaColors.REMOVE_GRADIENT_END, 1]]
                                });
                                Button.borderRadius(12);
                                Button.height(42);
                                Button.layoutWeight(1);
                                Button.onClick(() => {
                                    appState.deleteKnowledgePoint(this.pointId);
                                    this.showDeleteConfirm = false;
                                    this.showToast('知识点已删除');
                                    setTimeout(() => { navPathStack.pop(); }, 400);
                                });
                            }, Button);
                            Button.pop();
                            Row.pop();
                            Column.pop();
                            Column.pop();
                        });
                    }
                    // Toast
                    else {
                        this.ifElseBranchUpdateFunction(1, () => {
                        });
                    }
                }, If);
                If.pop();
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
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/EditKnowledgePointPage" });
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
class EditKnowledgePointPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: EditKnowledgePointPage_Params) {
    }
    updateStateVars(params: EditKnowledgePointPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/EditKnowledgePointPage", isUserCreateStack: false });
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new EditKnowledgePointPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/EditKnowledgePointPage.ets", line: 299, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "EditKnowledgePointPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "EditKnowledgePointPage";
    }
}
registerNamedRoute(() => new EditKnowledgePointPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/EditKnowledgePointPage", pageFullPath: "entry/src/main/ets/pages/EditKnowledgePointPage", integratedHsp: "false", moduleType: "followWithHap" });
