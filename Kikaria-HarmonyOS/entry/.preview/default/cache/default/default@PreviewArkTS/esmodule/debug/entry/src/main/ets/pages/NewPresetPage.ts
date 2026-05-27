if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface NewPresetPage_Params {
}
interface NewPresetPageContent_Params {
    name?: string;
    subtitle?: string;
    description?: string;
    category?: string;
    markdownText?: string;
    parsedCount?: number;
    errorMessage?: string;
    contentPadH?: number;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { parseMarkdown } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, pageHorizontalPadding, ipadPortraitFormPageTopInset } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
import picker from "@ohos:file.picker";
import fileIo from "@ohos:file.fs";
import pasteboard from "@ohos:pasteboard";
export function NewPresetPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new NewPresetPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/NewPresetPage.ets", line: 18, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "NewPresetPageContent" });
    }
}
export class NewPresetPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__name = new ObservedPropertySimplePU('', this, "name");
        this.__subtitle = new ObservedPropertySimplePU('', this, "subtitle");
        this.__description = new ObservedPropertySimplePU('', this, "description");
        this.__category = new ObservedPropertySimplePU('自定义', this, "category");
        this.__markdownText = new ObservedPropertySimplePU('', this, "markdownText");
        this.__parsedCount = new ObservedPropertySimplePU(0, this, "parsedCount");
        this.__errorMessage = new ObservedPropertySimplePU('', this, "errorMessage");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: NewPresetPageContent_Params) {
        if (params.name !== undefined) {
            this.name = params.name;
        }
        if (params.subtitle !== undefined) {
            this.subtitle = params.subtitle;
        }
        if (params.description !== undefined) {
            this.description = params.description;
        }
        if (params.category !== undefined) {
            this.category = params.category;
        }
        if (params.markdownText !== undefined) {
            this.markdownText = params.markdownText;
        }
        if (params.parsedCount !== undefined) {
            this.parsedCount = params.parsedCount;
        }
        if (params.errorMessage !== undefined) {
            this.errorMessage = params.errorMessage;
        }
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
    }
    updateStateVars(params: NewPresetPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__name.purgeDependencyOnElmtId(rmElmtId);
        this.__subtitle.purgeDependencyOnElmtId(rmElmtId);
        this.__description.purgeDependencyOnElmtId(rmElmtId);
        this.__category.purgeDependencyOnElmtId(rmElmtId);
        this.__markdownText.purgeDependencyOnElmtId(rmElmtId);
        this.__parsedCount.purgeDependencyOnElmtId(rmElmtId);
        this.__errorMessage.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__name.aboutToBeDeleted();
        this.__subtitle.aboutToBeDeleted();
        this.__description.aboutToBeDeleted();
        this.__category.aboutToBeDeleted();
        this.__markdownText.aboutToBeDeleted();
        this.__parsedCount.aboutToBeDeleted();
        this.__errorMessage.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __name: ObservedPropertySimplePU<string>;
    get name() {
        return this.__name.get();
    }
    set name(newValue: string) {
        this.__name.set(newValue);
    }
    private __subtitle: ObservedPropertySimplePU<string>;
    get subtitle() {
        return this.__subtitle.get();
    }
    set subtitle(newValue: string) {
        this.__subtitle.set(newValue);
    }
    private __description: ObservedPropertySimplePU<string>;
    get description() {
        return this.__description.get();
    }
    set description(newValue: string) {
        this.__description.set(newValue);
    }
    private __category: ObservedPropertySimplePU<string>;
    get category() {
        return this.__category.get();
    }
    set category(newValue: string) {
        this.__category.set(newValue);
    }
    private __markdownText: ObservedPropertySimplePU<string>;
    get markdownText() {
        return this.__markdownText.get();
    }
    set markdownText(newValue: string) {
        this.__markdownText.set(newValue);
    }
    private __parsedCount: ObservedPropertySimplePU<number>;
    get parsedCount() {
        return this.__parsedCount.get();
    }
    set parsedCount(newValue: number) {
        this.__parsedCount.set(newValue);
    }
    private __errorMessage: ObservedPropertySimplePU<string>;
    get errorMessage() {
        return this.__errorMessage.get();
    }
    set errorMessage(newValue: string) {
        this.__errorMessage.set(newValue);
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
                    Column.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(38:7)", "entry");
                    Column.width('100%');
                    Column.height('100%');
                    Column.backgroundColor(KikariaColors.PAGE_BG);
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(39:9)", "entry");
                    Row.width('100%');
                    Row.padding({ left: 20, right: 20, top: 12 + ipadPortraitFormPageTopInset(), bottom: 12 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithChild();
                    Button.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(40:11)", "entry");
                    Button.backgroundColor(Color.Transparent);
                    Button.onClick(() => { navPathStack.pop(); });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('取消');
                    Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(41:13)", "entry");
                    Text.fontSize(17);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Button.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                    Blank.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(48:11)", "entry");
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('新建预设');
                    Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(50:11)", "entry");
                    Text.fontSize(17);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.DEEP_TEXT);
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                    Blank.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(55:11)", "entry");
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithChild();
                    Button.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(57:11)", "entry");
                    Button.backgroundColor(Color.Transparent);
                    Button.enabled(this.canCreate);
                    Button.onClick(() => { this.createPreset(); });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('创建');
                    Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(58:13)", "entry");
                    Text.fontSize(17);
                    Text.fontWeight(FontWeight.Bold);
                    Text.fontColor(this.canCreate ? KikariaColors.SKY : KikariaColors.TERTIARY_TEXT);
                }, Text);
                Text.pop();
                Button.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Scroll.create();
                    Scroll.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(70:9)", "entry");
                    Scroll.width('100%');
                    Scroll.constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' });
                    Scroll.layoutWeight(1);
                    Scroll.padding({ left: this.contentPadH, right: this.contentPadH });
                    Scroll.scrollBar(BarState.Off);
                }, Scroll);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 16 });
                    Column.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(71:11)", "entry");
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Import / Export row
                    Row.create({ space: 10 });
                    Row.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(73:13)", "entry");
                    // Import / Export row
                    Row.width('100%');
                    // Import / Export row
                    Row.justifyContent(FlexAlign.Start);
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(74:15)", "entry");
                    Row.height(44);
                    Row.borderRadius(14);
                    Row.padding({ left: 18, right: 18 });
                    Row.linearGradient({
                        angle: 135,
                        colors: [[KikariaColors.ACTION_GRADIENT_START, 0], [KikariaColors.ACTION_GRADIENT_END, 1]]
                    });
                    Row.onClick(() => { this.pickMarkdownFile(); });
                    Row.layoutWeight(1);
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('从文件导入 .md');
                    Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(75:17)", "entry");
                    Text.fontSize(15);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor('#FFFFFF');
                }, Text);
                Text.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (this.markdownText.trim().length > 0) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Row.create();
                                Row.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(91:17)", "entry");
                                Row.height(44);
                                Row.borderRadius(14);
                                Row.padding({ left: 18, right: 18 });
                                Row.backgroundColor(`${KikariaColors.SKY}10`);
                                Row.border({ width: 1, color: `${KikariaColors.SKY}33` });
                                Row.onClick(() => { this.exportMarkdown(); });
                                Row.layoutWeight(1);
                            }, Row);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('复制导出');
                                Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(92:19)", "entry");
                                Text.fontSize(15);
                                Text.fontWeight(FontWeight.Medium);
                                Text.fontColor(KikariaColors.SKY);
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
                // Import / Export row
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Form fields
                    Column.create({ space: 6 });
                    Column.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(110:13)", "entry");
                    // Form fields
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('预设名称');
                    Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(111:15)", "entry");
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.name, placeholder: '例如：高等数学' });
                    TextInput.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(117:15)", "entry");
                    TextInput.fontSize(16);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG);
                    TextInput.borderRadius(12);
                    TextInput.padding({ left: 16, right: 16, top: 12, bottom: 12 });
                    TextInput.width('100%');
                    TextInput.onChange((value: string) => {
                        this.name = value;
                        this.updateParsedCount();
                    });
                }, TextInput);
                // Form fields
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 6 });
                    Column.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(131:13)", "entry");
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('副标题');
                    Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(132:15)", "entry");
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.subtitle, placeholder: '简短描述' });
                    TextInput.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(138:15)", "entry");
                    TextInput.fontSize(16);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG);
                    TextInput.borderRadius(12);
                    TextInput.padding({ left: 16, right: 16, top: 12, bottom: 12 });
                    TextInput.width('100%');
                    TextInput.onChange((value: string) => { this.subtitle = value; });
                }, TextInput);
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 6 });
                    Column.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(149:13)", "entry");
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('描述');
                    Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(150:15)", "entry");
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.description, placeholder: '详细描述' });
                    TextInput.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(156:15)", "entry");
                    TextInput.fontSize(16);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG);
                    TextInput.borderRadius(12);
                    TextInput.padding({ left: 16, right: 16, top: 12, bottom: 12 });
                    TextInput.width('100%');
                    TextInput.onChange((value: string) => { this.description = value; });
                }, TextInput);
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 6 });
                    Column.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(167:13)", "entry");
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('分类');
                    Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(168:15)", "entry");
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                    Text.width('100%');
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextInput.create({ text: this.category, placeholder: '自定义' });
                    TextInput.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(174:15)", "entry");
                    TextInput.fontSize(16);
                    TextInput.fontColor(KikariaColors.DEEP_TEXT);
                    TextInput.backgroundColor(KikariaColors.CARD_BG);
                    TextInput.borderRadius(12);
                    TextInput.padding({ left: 16, right: 16, top: 12, bottom: 12 });
                    TextInput.width('100%');
                    TextInput.onChange((value: string) => { this.category = value; });
                }, TextInput);
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Markdown editor
                    Column.create({ space: 6 });
                    Column.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(186:13)", "entry");
                    // Markdown editor
                    Column.width('100%');
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(187:15)", "entry");
                    Row.width('100%');
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('Markdown 内容');
                    Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(188:17)", "entry");
                    Text.fontSize(13);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                    Blank.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(193:17)", "entry");
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(`已解析: ${this.parsedCount} 条`);
                    Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(195:17)", "entry");
                    Text.fontSize(12);
                    Text.fontColor(this.parsedCount > 0 ? KikariaColors.MASTERED_GREEN : KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    TextArea.create({ text: this.markdownText, placeholder: `# 知识点标题

tags: 标签1, 标签2

hint:
提示内容

content:
答案内容

---

# 下一个知识点
...` });
                    TextArea.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(201:15)", "entry");
                    TextArea.fontSize(14);
                    TextArea.fontColor(KikariaColors.DEEP_TEXT);
                    TextArea.backgroundColor(KikariaColors.CARD_BG);
                    TextArea.borderRadius(12);
                    TextArea.padding(12);
                    TextArea.width('100%');
                    TextArea.height(260);
                    TextArea.onChange((value: string) => {
                        this.markdownText = value;
                        this.updateParsedCount();
                    });
                }, TextArea);
                // Markdown editor
                Column.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(229:13)", "entry");
                    Row.width('100%');
                    Row.onClick(() => { navPathStack.pushPathByName('MarkdownFormatGuide', undefined); });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('格式指南 →');
                    Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(230:15)", "entry");
                    Text.fontSize(14);
                    Text.fontColor(KikariaColors.SKY);
                }, Text);
                Text.pop();
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (this.errorMessage.length > 0) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(this.errorMessage);
                                Text.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(238:15)", "entry");
                                Text.fontSize(13);
                                Text.fontColor(KikariaColors.REMOVE_CORAL);
                                Text.width('100%');
                                Text.padding(12);
                                Text.borderRadius(12);
                                Text.backgroundColor(`${KikariaColors.REMOVE_CORAL}10`);
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
                    Row.create();
                    Row.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(247:13)", "entry");
                    Row.height(40);
                }, Row);
                Row.pop();
                Column.pop();
                Scroll.pop();
                Column.pop();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/NewPresetPage" });
            NavDestination.hideTitleBar(true);
            NavDestination.width('100%');
            NavDestination.height('100%');
            NavDestination.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(37:5)", "entry");
        }, NavDestination);
        NavDestination.pop();
    }
    pickMarkdownFile(): void {
        try {
            const documentPicker = new picker.DocumentViewPicker();
            const pickOptions = new picker.DocumentSelectOptions();
            pickOptions.maxSelectNumber = 1;
            pickOptions.fileSuffixFilters = ['.md', '.txt', '.markdown'];
            documentPicker.select(pickOptions).then((uris: string[]) => {
                if (uris.length === 0) {
                    return;
                }
                const uri: string = uris[0];
                try {
                    const content: string = fileIo.readTextSync(uri);
                    if (content.length > 0) {
                        this.markdownText = content;
                        this.updateParsedCount();
                        const lastSep: number = Math.max(uri.lastIndexOf('/'), uri.lastIndexOf('\\'));
                        let filename: string = uri.substring(lastSep + 1);
                        const dotIdx: number = filename.lastIndexOf('.');
                        if (dotIdx > 0) {
                            filename = filename.substring(0, dotIdx);
                        }
                        if (this.name.trim().length === 0) {
                            this.name = filename;
                        }
                    }
                }
                catch (ioErr) {
                    this.errorMessage = '读取文件失败，请检查文件格式。';
                }
            });
        }
        catch (e) {
            this.errorMessage = '无法打开文件选择器，请重试。';
        }
    }
    exportMarkdown(): void {
        try {
            const pasteData = pasteboard.createData(pasteboard.MIMETYPE_TEXT_PLAIN, this.markdownText);
            pasteboard.getSystemPasteboard().setData(pasteData, () => {
                this.errorMessage = '';
                // show brief success via errorMessage as info
                this.errorMessage = `已复制 ${this.parsedCount} 条知识点到剪贴板`;
                setTimeout(() => { if (this.errorMessage.indexOf('已复制') >= 0) {
                    this.errorMessage = '';
                } }, 2000);
            });
        }
        catch (e) {
            this.errorMessage = '复制失败，请重试';
        }
    }
    updateParsedCount(): void {
        if (this.markdownText.trim().length === 0) {
            this.parsedCount = 0;
            return;
        }
        try {
            const points = parseMarkdown(this.markdownText);
            this.parsedCount = points.length;
            this.errorMessage = points.length === 0 ? '未能解析到有效的知识点，请检查格式。' : '';
        }
        catch (e) {
            this.parsedCount = 0;
            this.errorMessage = 'Markdown 格式有误，请检查。';
        }
    }
    createPreset(): void {
        if (!this.canCreate) {
            return;
        }
        const preset = appState.createPreset(this.name.trim(), this.subtitle.trim(), this.description.trim() || this.subtitle.trim(), this.category.trim() || '自定义', this.markdownText);
        if (preset) {
            appState.switchToPreset(preset);
            navPathStack.pop();
        }
        else {
            this.errorMessage = '创建预设失败，请重试。';
        }
    }
    rerender() {
        this.updateDirtyElements();
    }
}
class NewPresetPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: NewPresetPage_Params) {
    }
    updateStateVars(params: NewPresetPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/NewPresetPage", isUserCreateStack: false });
            Navigation.debugLine("entry/src/main/ets/pages/NewPresetPage.ets(351:5)", "entry");
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new NewPresetPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/NewPresetPage.ets", line: 352, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "NewPresetPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "NewPresetPage";
    }
}
registerNamedRoute(() => new NewPresetPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/NewPresetPage", pageFullPath: "entry/src/main/ets/pages/NewPresetPage", integratedHsp: "false", moduleType: "followWithHap" });
