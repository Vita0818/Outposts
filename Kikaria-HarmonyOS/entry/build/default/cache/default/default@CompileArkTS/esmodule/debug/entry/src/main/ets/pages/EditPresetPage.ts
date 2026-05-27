if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface EditPresetPage_Params {
}
interface EditPresetPageContent_Params {
    presetId?: string;
    presetName?: string;
    presetSubtitle?: string;
    presetDescription?: string;
    knowledgePoints?: KnowledgePoint[];
    searchQuery?: string;
    isEditingName?: boolean;
    editNameText?: string;
    editDescText?: string;
    showDeletePresetConfirm?: boolean;
    showDeletePointId?: string;
    toastMessage?: string;
    toastVisible?: boolean;
    contentPadH?: number;
}
import pasteboard from "@ohos:pasteboard";
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { markdownTextFromPoints } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import type { KnowledgePoint } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, pageHorizontalPadding, ipadPortraitFormPageTopInset } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack, RouteParams } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function EditPresetPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new EditPresetPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/EditPresetPage.ets", line: 15, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "EditPresetPageContent" });
    }
}
export class EditPresetPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__presetId = new ObservedPropertySimplePU('', this, "presetId");
        this.__presetName = new ObservedPropertySimplePU('', this, "presetName");
        this.__presetSubtitle = new ObservedPropertySimplePU('', this, "presetSubtitle");
        this.__presetDescription = new ObservedPropertySimplePU('', this, "presetDescription");
        this.__knowledgePoints = new ObservedPropertyObjectPU([], this, "knowledgePoints");
        this.__searchQuery = new ObservedPropertySimplePU('', this, "searchQuery");
        this.__isEditingName = new ObservedPropertySimplePU(false, this, "isEditingName");
        this.__editNameText = new ObservedPropertySimplePU('', this, "editNameText");
        this.__editDescText = new ObservedPropertySimplePU('', this, "editDescText");
        this.__showDeletePresetConfirm = new ObservedPropertySimplePU(false, this, "showDeletePresetConfirm");
        this.__showDeletePointId = new ObservedPropertySimplePU('', this, "showDeletePointId");
        this.__toastMessage = new ObservedPropertySimplePU('', this, "toastMessage");
        this.__toastVisible = new ObservedPropertySimplePU(false, this, "toastVisible");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: EditPresetPageContent_Params) {
        if (params.presetId !== undefined) {
            this.presetId = params.presetId;
        }
        if (params.presetName !== undefined) {
            this.presetName = params.presetName;
        }
        if (params.presetSubtitle !== undefined) {
            this.presetSubtitle = params.presetSubtitle;
        }
        if (params.presetDescription !== undefined) {
            this.presetDescription = params.presetDescription;
        }
        if (params.knowledgePoints !== undefined) {
            this.knowledgePoints = params.knowledgePoints;
        }
        if (params.searchQuery !== undefined) {
            this.searchQuery = params.searchQuery;
        }
        if (params.isEditingName !== undefined) {
            this.isEditingName = params.isEditingName;
        }
        if (params.editNameText !== undefined) {
            this.editNameText = params.editNameText;
        }
        if (params.editDescText !== undefined) {
            this.editDescText = params.editDescText;
        }
        if (params.showDeletePresetConfirm !== undefined) {
            this.showDeletePresetConfirm = params.showDeletePresetConfirm;
        }
        if (params.showDeletePointId !== undefined) {
            this.showDeletePointId = params.showDeletePointId;
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
    updateStateVars(params: EditPresetPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__presetId.purgeDependencyOnElmtId(rmElmtId);
        this.__presetName.purgeDependencyOnElmtId(rmElmtId);
        this.__presetSubtitle.purgeDependencyOnElmtId(rmElmtId);
        this.__presetDescription.purgeDependencyOnElmtId(rmElmtId);
        this.__knowledgePoints.purgeDependencyOnElmtId(rmElmtId);
        this.__searchQuery.purgeDependencyOnElmtId(rmElmtId);
        this.__isEditingName.purgeDependencyOnElmtId(rmElmtId);
        this.__editNameText.purgeDependencyOnElmtId(rmElmtId);
        this.__editDescText.purgeDependencyOnElmtId(rmElmtId);
        this.__showDeletePresetConfirm.purgeDependencyOnElmtId(rmElmtId);
        this.__showDeletePointId.purgeDependencyOnElmtId(rmElmtId);
        this.__toastMessage.purgeDependencyOnElmtId(rmElmtId);
        this.__toastVisible.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__presetId.aboutToBeDeleted();
        this.__presetName.aboutToBeDeleted();
        this.__presetSubtitle.aboutToBeDeleted();
        this.__presetDescription.aboutToBeDeleted();
        this.__knowledgePoints.aboutToBeDeleted();
        this.__searchQuery.aboutToBeDeleted();
        this.__isEditingName.aboutToBeDeleted();
        this.__editNameText.aboutToBeDeleted();
        this.__editDescText.aboutToBeDeleted();
        this.__showDeletePresetConfirm.aboutToBeDeleted();
        this.__showDeletePointId.aboutToBeDeleted();
        this.__toastMessage.aboutToBeDeleted();
        this.__toastVisible.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __presetId: ObservedPropertySimplePU<string>;
    get presetId() {
        return this.__presetId.get();
    }
    set presetId(newValue: string) {
        this.__presetId.set(newValue);
    }
    private __presetName: ObservedPropertySimplePU<string>;
    get presetName() {
        return this.__presetName.get();
    }
    set presetName(newValue: string) {
        this.__presetName.set(newValue);
    }
    private __presetSubtitle: ObservedPropertySimplePU<string>;
    get presetSubtitle() {
        return this.__presetSubtitle.get();
    }
    set presetSubtitle(newValue: string) {
        this.__presetSubtitle.set(newValue);
    }
    private __presetDescription: ObservedPropertySimplePU<string>;
    get presetDescription() {
        return this.__presetDescription.get();
    }
    set presetDescription(newValue: string) {
        this.__presetDescription.set(newValue);
    }
    private __knowledgePoints: ObservedPropertyObjectPU<KnowledgePoint[]>;
    get knowledgePoints() {
        return this.__knowledgePoints.get();
    }
    set knowledgePoints(newValue: KnowledgePoint[]) {
        this.__knowledgePoints.set(newValue);
    }
    private __searchQuery: ObservedPropertySimplePU<string>;
    get searchQuery() {
        return this.__searchQuery.get();
    }
    set searchQuery(newValue: string) {
        this.__searchQuery.set(newValue);
    }
    private __isEditingName: ObservedPropertySimplePU<boolean>;
    get isEditingName() {
        return this.__isEditingName.get();
    }
    set isEditingName(newValue: boolean) {
        this.__isEditingName.set(newValue);
    }
    private __editNameText: ObservedPropertySimplePU<string>;
    get editNameText() {
        return this.__editNameText.get();
    }
    set editNameText(newValue: string) {
        this.__editNameText.set(newValue);
    }
    private __editDescText: ObservedPropertySimplePU<string>;
    get editDescText() {
        return this.__editDescText.get();
    }
    set editDescText(newValue: string) {
        this.__editDescText.set(newValue);
    }
    private __showDeletePresetConfirm: ObservedPropertySimplePU<boolean>;
    get showDeletePresetConfirm() {
        return this.__showDeletePresetConfirm.get();
    }
    set showDeletePresetConfirm(newValue: boolean) {
        this.__showDeletePresetConfirm.set(newValue);
    }
    private __showDeletePointId: ObservedPropertySimplePU<string>;
    get showDeletePointId() {
        return this.__showDeletePointId.get();
    }
    set showDeletePointId(newValue: string) {
        this.__showDeletePointId.set(newValue);
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
        this.presetId = appState.currentPresetID;
        this.refreshState();
    }
    onPageShow(): void {
        this.refreshState();
    }
    refreshState(): void {
        const preset = appState.presets.find(p => p.id === this.presetId);
        if (preset) {
            this.presetName = preset.name;
            this.presetSubtitle = preset.subtitle;
            this.presetDescription = preset.description;
            this.editNameText = preset.name;
            this.editDescText = preset.description;
        }
        this.knowledgePoints = [...appState.knowledgePoints];
    }
    showToast(msg: string): void {
        this.toastMessage = msg;
        this.toastVisible = true;
        setTimeout(() => { this.toastVisible = false; }, 2000);
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
                    // Top bar
                    Row.create();
                    // Top bar
                    Row.width('100%');
                    // Top bar
                    Row.padding({ left: 20, right: 20, top: 12 + ipadPortraitFormPageTopInset(), bottom: 12 });
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
                    Text.create('编辑预设');
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
                        appState.updatePresetMetadata(this.presetId, this.editNameText, this.presetSubtitle, this.editDescText);
                        this.presetName = this.editNameText;
                        this.presetDescription = this.editDescText;
                        this.isEditingName = false;
                        this.showToast('预设信息已保存');
                    });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('保存');
                    Text.fontSize(15);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SKY);
                }, Text);
                Text.pop();
                Button.pop();
                // Top bar
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
                    // Preset name
                    Column.create({ space: 6 });
                    // Preset name
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('预设名称');
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                    Text.padding({ left: 4 });
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.editNameText, placeholder: '输入预设名称' });
                    TextInput.fontSize(17);
                    TextInput.fontWeight(FontWeight.Medium);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG);
                    TextInput.borderRadius(14);
                    TextInput.padding(16);
                    TextInput.onChange((value: string) => {
                        this.editNameText = value;
                    });
                }, TextInput);
                // Preset name
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Preset description
                    Column.create({ space: 6 });
                    // Preset description
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('描述');
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                    Text.padding({ left: 4 });
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.editDescText, placeholder: '输入预设描述' });
                    TextInput.fontSize(15);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG);
                    TextInput.borderRadius(14);
                    TextInput.padding(16);
                    TextInput.maxLength(200);
                    TextInput.onChange((value: string) => {
                        this.editDescText = value;
                    });
                }, TextInput);
                // Preset description
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Knowledge points header with search
                    Row.create();
                    // Knowledge points header with search
                    Row.width('100%');
                    // Knowledge points header with search
                    Row.padding({ left: 4, right: 4 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(`知识点 (${this.knowledgePoints.length})`);
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (this.knowledgePoints.length > 0) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Button.createWithLabel('复制导出');
                                Button.fontSize(13);
                                Button.fontWeight(FontWeight.Medium);
                                Button.fontColor(KikariaColors.SKY);
                                Button.backgroundColor(Color.Transparent);
                                Button.onClick(() => {
                                    const md = markdownTextFromPoints(this.knowledgePoints);
                                    try {
                                        const pasteData = pasteboard.createData(pasteboard.MIMETYPE_TEXT_PLAIN, md);
                                        pasteboard.getSystemPasteboard().setData(pasteData, () => {
                                            this.showToast(`已复制 ${this.knowledgePoints.length} 条知识点到剪贴板`);
                                        });
                                    }
                                    catch (e) {
                                        this.showToast('复制失败，请重试');
                                    }
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
                // Knowledge points header with search
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Search bar
                    Row.create();
                    // Search bar
                    Row.width('100%');
                    // Search bar
                    Row.padding(14);
                    // Search bar
                    Row.borderRadius(14);
                    // Search bar
                    Row.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                    // Search bar
                    Row.backdropBlur(12);
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('🔍');
                    Text.fontSize(14);
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.searchQuery, placeholder: '搜索知识点标题或标签...' });
                    TextInput.fontSize(15);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.layoutWeight(1);
                    TextInput.backgroundColor(Color.Transparent);
                    TextInput.onChange((value: string) => {
                        this.searchQuery = value;
                    });
                }, TextInput);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (this.searchQuery.length > 0) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('✕');
                                Text.fontSize(14);
                                Text.fontColor(KikariaColors.TERTIARY_TEXT);
                                Text.onClick(() => { this.searchQuery = ''; });
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
                // Search bar
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Create new knowledge point button
                    Row.create();
                    // Create new knowledge point button
                    Row.width('100%');
                    // Create new knowledge point button
                    Row.height(48);
                    // Create new knowledge point button
                    Row.justifyContent(FlexAlign.Center);
                    // Create new knowledge point button
                    Row.borderRadius(14);
                    // Create new knowledge point button
                    Row.backgroundColor(`${KikariaColors.SKY}10`);
                    // Create new knowledge point button
                    Row.border({ width: 1, color: `${KikariaColors.SKY}33`, style: BorderStyle.Dashed });
                    // Create new knowledge point button
                    Row.onClick(() => {
                        navPathStack.pushPathByName('EditKnowledgePointPage', undefined);
                    });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('+ 新建知识点');
                    Text.fontSize(15);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SKY);
                }, Text);
                Text.pop();
                // Create new knowledge point button
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    // Knowledge point list
                    if (this.filteredPoints.length === 0) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 8 });
                                Column.width('100%');
                                Column.padding(30);
                                Column.justifyContent(FlexAlign.Center);
                                Column.borderRadius(16);
                                Column.backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT);
                                Column.backdropBlur(12);
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(this.searchQuery.length > 0 ? '没有匹配的知识点' : '暂无知识点');
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
                                ForEach.create();
                                const forEachItemGenFunction = _item => {
                                    const kp = _item;
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Column.create({ space: 8 });
                                        Column.width('100%');
                                        Column.padding(14);
                                        Column.borderRadius(14);
                                        Column.backgroundColor(KikariaColors.CARD_BG);
                                        Column.shadow({ radius: 6, color: KikariaColors.SHADOW_COLOR, offsetY: 2 });
                                        Column.margin({ bottom: 8 });
                                    }, Column);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Row.create({ space: 8 });
                                        Row.width('100%');
                                        Row.onClick(() => {
                                            navPathStack.pushPathByName('EditKnowledgePointPage', new RouteParams(undefined, kp.id));
                                        });
                                    }, Row);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Column.create({ space: 6 });
                                        Column.layoutWeight(1);
                                        Column.alignItems(HorizontalAlign.Start);
                                    }, Column);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Row.create({ space: 8 });
                                    }, Row);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(kp.title);
                                        Text.fontSize(15);
                                        Text.fontWeight(FontWeight.Medium);
                                        Text.fontColor(KikariaColors.DEEP_TEXT);
                                        Text.maxLines(2);
                                        Text.textOverflow({ overflow: TextOverflow.Ellipsis });
                                        Text.layoutWeight(1);
                                    }, Text);
                                    Text.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        If.create();
                                        if (kp.isMastered) {
                                            this.ifElseBranchUpdateFunction(0, () => {
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Text.create('✓');
                                                    Text.fontSize(12);
                                                    Text.fontWeight(FontWeight.Bold);
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
                                        If.create();
                                        if (kp.isReinforced) {
                                            this.ifElseBranchUpdateFunction(0, () => {
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Text.create('★');
                                                    Text.fontSize(12);
                                                    Text.fontColor(KikariaColors.NEXT_AMBER);
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
                                    Row.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        If.create();
                                        if (kp.tags.length > 0) {
                                            this.ifElseBranchUpdateFunction(0, () => {
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Row.create({ space: 6 });
                                                }, Row);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    ForEach.create();
                                                    const forEachItemGenFunction = _item => {
                                                        const tag = _item;
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            Text.create(tag);
                                                            Text.fontSize(10);
                                                            Text.fontColor(KikariaColors.SOFT_TEXT);
                                                            Text.backgroundColor(KikariaColors.MIST);
                                                            Text.borderRadius(6);
                                                            Text.padding({ left: 8, right: 8, top: 2, bottom: 2 });
                                                        }, Text);
                                                        Text.pop();
                                                    };
                                                    this.forEachUpdateFunction(elmtId, kp.tags.slice(0, 4), forEachItemGenFunction);
                                                }, ForEach);
                                                ForEach.pop();
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
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Button.createWithChild();
                                        Button.width(32);
                                        Button.height(32);
                                        Button.borderRadius(16);
                                        Button.backgroundColor(`${KikariaColors.REMOVE_CORAL}10`);
                                        Button.onClick(() => {
                                            appState.deleteKnowledgePoint(kp.id);
                                            this.refreshState();
                                            this.showToast(`已删除: ${kp.title}`);
                                        });
                                    }, Button);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create('✕');
                                        Text.fontSize(14);
                                        Text.fontColor(KikariaColors.REMOVE_CORAL);
                                    }, Text);
                                    Text.pop();
                                    Button.pop();
                                    Row.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Divider.create();
                                        Divider.strokeWidth(0.5);
                                        Divider.color(KikariaColors.MIST);
                                    }, Divider);
                                    Column.pop();
                                };
                                this.forEachUpdateFunction(elmtId, this.filteredPoints, forEachItemGenFunction);
                            }, ForEach);
                            ForEach.pop();
                        });
                    }
                }, If);
                If.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    // Delete preset button
                    if (!appState.currentPreset.isBuiltIn && appState.presets.length > 1) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create();
                                Column.width('100%');
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Button.createWithLabel('删除此预设');
                                Button.fontSize(15);
                                Button.fontWeight(FontWeight.Medium);
                                Button.fontColor('#FFFFFF');
                                Button.width('100%');
                                Button.height(48);
                                Button.borderRadius(14);
                                Button.backgroundColor(KikariaColors.REMOVE_CORAL);
                                Button.margin({ top: 16 });
                                Button.onClick(() => {
                                    this.showDeletePresetConfirm = true;
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
                    // Delete preset confirmation dialog
                    if (this.showDeletePresetConfirm) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create();
                                Column.width('100%');
                                Column.height('100%');
                                Column.justifyContent(FlexAlign.Center);
                                Column.backgroundColor(KikariaColors.OVERLAY_BG);
                                Column.onClick(() => {
                                    this.showDeletePresetConfirm = false;
                                });
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
                                Text.create('删除预设');
                                Text.fontSize(17);
                                Text.fontWeight(FontWeight.Bold);
                                Text.fontColor(KikariaColors.DEEP_TEXT);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(`确定要删除「${this.presetName}」吗？此操作不可撤销，所有该预设下的学习进度将丢失。`);
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
                                Button.onClick(() => {
                                    this.showDeletePresetConfirm = false;
                                });
                            }, Button);
                            Button.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Button.createWithLabel('确认删除');
                                Button.fontSize(15);
                                Button.fontWeight(FontWeight.Bold);
                                Button.fontColor('#FFFFFF');
                                Button.backgroundColor(KikariaColors.REMOVE_CORAL);
                                Button.borderRadius(12);
                                Button.height(42);
                                Button.layoutWeight(1);
                                Button.onClick(() => {
                                    const success = appState.deletePreset(this.presetId);
                                    this.showDeletePresetConfirm = false;
                                    if (success) {
                                        navPathStack.pop();
                                    }
                                    else {
                                        this.showToast('无法删除最后一个预设');
                                    }
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
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/EditPresetPage" });
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
class EditPresetPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: EditPresetPage_Params) {
    }
    updateStateVars(params: EditPresetPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/EditPresetPage", isUserCreateStack: false });
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new EditPresetPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/EditPresetPage.ets", line: 448, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "EditPresetPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "EditPresetPage";
    }
}
registerNamedRoute(() => new EditPresetPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/EditPresetPage", pageFullPath: "entry/src/main/ets/pages/EditPresetPage", integratedHsp: "false", moduleType: "followWithHap" });
