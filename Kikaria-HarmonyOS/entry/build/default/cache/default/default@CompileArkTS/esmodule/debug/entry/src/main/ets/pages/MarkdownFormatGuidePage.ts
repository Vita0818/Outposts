if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface MarkdownFormatGuidePage_Params {
}
interface MarkdownFormatGuideContent_Params {
    contentPadH?: number;
}
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { isPadLandscape, pageHorizontalPadding } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
/**
 * NavDestination-compatible builder for NavPathStack migration.
 * Usage: navPathStack.pushPathByName('MarkdownFormatGuide', undefined)
 * Register in root Navigation's navDestination builder.
 */
export function MarkdownFormatGuideBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new MarkdownFormatGuideContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/MarkdownFormatGuidePage.ets", line: 17, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "MarkdownFormatGuideContent" });
    }
}
export class MarkdownFormatGuideContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: MarkdownFormatGuideContent_Params) {
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
    }
    updateStateVars(params: MarkdownFormatGuideContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__contentPadH.aboutToBeDeleted();
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
    aboutToAppear(): void {
        this.contentPadH = pageHorizontalPadding();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            NavDestination.create(() => {
                this.pageContent.bind(this)();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/MarkdownFormatGuidePage" });
            NavDestination.hideTitleBar(true);
            NavDestination.width('100%');
            NavDestination.height('100%');
        }, NavDestination);
        NavDestination.pop();
    }
    pageContent(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Scroll.create();
            Scroll.width('100%');
            Scroll.height('100%');
            Scroll.backgroundColor(KikariaColors.PAGE_BG);
            Scroll.scrollBar(BarState.Off);
        }, Scroll);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 18 });
            Column.width('100%');
            Column.constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' });
            Column.padding({ left: this.contentPadH, right: this.contentPadH });
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
            Text.create('Markdown 格式指南');
            Text.fontSize(17);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(KikariaColors.DEEP_TEXT);
        }, Text);
        Text.pop();
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('Kikaria 使用 Markdown 格式存储知识点。每个知识点由以下部分组成：');
            Text.fontSize(15);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('基本结构');
            Text.fontSize(17);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(`# 知识点标题\n\ntags: 标签1, 标签2\n\nhint:\n这里是提示内容。可以包含多行文字，\n支持 Markdown 基本语法。\n\ncontent:\n这里是答案内容。同样支持多行文字，\n以及 LaTeX 数学公式：$E = mc^2$。`);
            Text.fontSize(13);
            Text.fontColor(KikariaColors.SOFT_TEXT);
            Text.backgroundColor(KikariaColors.MIST);
            Text.borderRadius(12);
            Text.padding(16);
            Text.width('100%');
            Text.fontFamily('monospace');
        }, Text);
        Text.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('多个知识点');
            Text.fontSize(17);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('使用三个短横线 --- 分隔多个知识点：');
            Text.fontSize(15);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(`# 第一个知识点\ntags: 标签A\n\nhint:\n提示A\n\ncontent:\n答案A\n\n---\n\n# 第二个知识点\ntags: 标签B\n\nhint:\n提示B\n\ncontent:\n答案B`);
            Text.fontSize(13);
            Text.fontColor(KikariaColors.SOFT_TEXT);
            Text.backgroundColor(KikariaColors.MIST);
            Text.borderRadius(12);
            Text.padding(16);
            Text.width('100%');
            Text.fontFamily('monospace');
        }, Text);
        Text.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('标签格式');
            Text.fontSize(17);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('tags: 后跟逗号（中英文均可）分隔的标签列表。标签用于筛选和组织知识点。');
            Text.fontSize(15);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
        }, Text);
        Text.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('数学公式');
            Text.fontSize(17);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('Kikaria 内嵌支持 LaTeX 数学公式：\n行内公式：$E = mc^2$\n块级公式：$$\\int_a^b f(x) dx$$\n\n(注：HarmonyOS 版本目前以文本形式展示公式)');
            Text.fontSize(15);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
        }, Text);
        Text.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('小贴士');
            Text.fontSize(17);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('• 标题以 # 开头，后接知识点名称\n• hint: 和 content: 必须各占一行，以冒号结尾\n• 提示和答案的内容从下一行开始\n• 每个知识点至少需要标题、tags、hint 和 content\n• 标签有助于在首页按范围筛选\n• 使用 --- 分隔不同的知识点');
            Text.fontSize(15);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
        }, Text);
        Text.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.height(40);
        }, Row);
        Row.pop();
        Column.pop();
        Scroll.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
class MarkdownFormatGuidePage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: MarkdownFormatGuidePage_Params) {
    }
    updateStateVars(params: MarkdownFormatGuidePage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/MarkdownFormatGuidePage", isUserCreateStack: false });
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new MarkdownFormatGuideContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/MarkdownFormatGuidePage.ets", line: 132, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "MarkdownFormatGuideContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "MarkdownFormatGuidePage";
    }
}
registerNamedRoute(() => new MarkdownFormatGuidePage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/MarkdownFormatGuidePage", pageFullPath: "entry/src/main/ets/pages/MarkdownFormatGuidePage", integratedHsp: "false", moduleType: "followWithHap" });
