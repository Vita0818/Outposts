if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface ReviewPage_Params {
}
interface ReviewPageContent_Params {
    isDark?: boolean;
    title?: string;
    contentPadH?: number;
    tags?: string[];
    hint?: string;
    content?: string;
    isHintVisible?: boolean;
    isContentVisible?: boolean;
    isReinforced?: boolean;
    isMastered?: boolean;
    currentIndex?: number;
    queueLength?: number;
    reviewMode?: ReviewMode;
    hasNext?: boolean;
    todayReviewCount?: number;
    useTwoColumn?: boolean;
}
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { ReviewMode } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import { KikariaColors } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
import { renderMathRichText } from "@bundle:com.vita0818.kikaria/entry/ets/model/KikariaMathRenderer";
import { isPadLandscape, isTwoColumnCapable, pageHorizontalPadding, reviewMaxWidth, reviewLandscapeLeftWidth, reviewLandscapeRightWidth, reviewLandscapeColumnSpacing, reviewLandscapeMaxWidth } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaAdaptiveLayout";
import { navPathStack } from "@bundle:com.vita0818.kikaria/entry/ets/data/NavigationService";
export function ReviewPageBuilder(name: string, param: Object, parent = null) {
    {
        (parent ? parent : this).observeComponentCreation2((elmtId, isInitialRender) => {
            if (isInitialRender) {
                let componentCall = new ReviewPageContent(parent ? parent : this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/ReviewPage.ets", line: 19, col: 3 });
                ViewPU.create(componentCall);
                let paramsLambda = () => {
                    return {};
                };
                componentCall.paramsGenerator_ = paramsLambda;
            }
            else {
                (parent ? parent : this).updateStateVarsOfChildByElmtId(elmtId, {});
            }
        }, { name: "ReviewPageContent" });
    }
}
export class ReviewPageContent extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__isDark = this.createStorageLink('kikaria_isDarkMode', false, "isDark");
        this.__title = new ObservedPropertySimplePU('', this, "title");
        this.__contentPadH = new ObservedPropertySimplePU(24, this, "contentPadH");
        this.__tags = new ObservedPropertyObjectPU([], this, "tags");
        this.__hint = new ObservedPropertySimplePU('', this, "hint");
        this.__content = new ObservedPropertySimplePU('', this, "content");
        this.__isHintVisible = new ObservedPropertySimplePU(false, this, "isHintVisible");
        this.__isContentVisible = new ObservedPropertySimplePU(false, this, "isContentVisible");
        this.__isReinforced = new ObservedPropertySimplePU(false, this, "isReinforced");
        this.__isMastered = new ObservedPropertySimplePU(false, this, "isMastered");
        this.__currentIndex = new ObservedPropertySimplePU(0, this, "currentIndex");
        this.__queueLength = new ObservedPropertySimplePU(0, this, "queueLength");
        this.__reviewMode = new ObservedPropertySimplePU(ReviewMode.NORMAL, this, "reviewMode");
        this.__hasNext = new ObservedPropertySimplePU(false, this, "hasNext");
        this.__todayReviewCount = new ObservedPropertySimplePU(0, this, "todayReviewCount");
        this.__useTwoColumn = new ObservedPropertySimplePU(false, this, "useTwoColumn");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: ReviewPageContent_Params) {
        if (params.title !== undefined) {
            this.title = params.title;
        }
        if (params.contentPadH !== undefined) {
            this.contentPadH = params.contentPadH;
        }
        if (params.tags !== undefined) {
            this.tags = params.tags;
        }
        if (params.hint !== undefined) {
            this.hint = params.hint;
        }
        if (params.content !== undefined) {
            this.content = params.content;
        }
        if (params.isHintVisible !== undefined) {
            this.isHintVisible = params.isHintVisible;
        }
        if (params.isContentVisible !== undefined) {
            this.isContentVisible = params.isContentVisible;
        }
        if (params.isReinforced !== undefined) {
            this.isReinforced = params.isReinforced;
        }
        if (params.isMastered !== undefined) {
            this.isMastered = params.isMastered;
        }
        if (params.currentIndex !== undefined) {
            this.currentIndex = params.currentIndex;
        }
        if (params.queueLength !== undefined) {
            this.queueLength = params.queueLength;
        }
        if (params.reviewMode !== undefined) {
            this.reviewMode = params.reviewMode;
        }
        if (params.hasNext !== undefined) {
            this.hasNext = params.hasNext;
        }
        if (params.todayReviewCount !== undefined) {
            this.todayReviewCount = params.todayReviewCount;
        }
        if (params.useTwoColumn !== undefined) {
            this.useTwoColumn = params.useTwoColumn;
        }
    }
    updateStateVars(params: ReviewPageContent_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__isDark.purgeDependencyOnElmtId(rmElmtId);
        this.__title.purgeDependencyOnElmtId(rmElmtId);
        this.__contentPadH.purgeDependencyOnElmtId(rmElmtId);
        this.__tags.purgeDependencyOnElmtId(rmElmtId);
        this.__hint.purgeDependencyOnElmtId(rmElmtId);
        this.__content.purgeDependencyOnElmtId(rmElmtId);
        this.__isHintVisible.purgeDependencyOnElmtId(rmElmtId);
        this.__isContentVisible.purgeDependencyOnElmtId(rmElmtId);
        this.__isReinforced.purgeDependencyOnElmtId(rmElmtId);
        this.__isMastered.purgeDependencyOnElmtId(rmElmtId);
        this.__currentIndex.purgeDependencyOnElmtId(rmElmtId);
        this.__queueLength.purgeDependencyOnElmtId(rmElmtId);
        this.__reviewMode.purgeDependencyOnElmtId(rmElmtId);
        this.__hasNext.purgeDependencyOnElmtId(rmElmtId);
        this.__todayReviewCount.purgeDependencyOnElmtId(rmElmtId);
        this.__useTwoColumn.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__isDark.aboutToBeDeleted();
        this.__title.aboutToBeDeleted();
        this.__contentPadH.aboutToBeDeleted();
        this.__tags.aboutToBeDeleted();
        this.__hint.aboutToBeDeleted();
        this.__content.aboutToBeDeleted();
        this.__isHintVisible.aboutToBeDeleted();
        this.__isContentVisible.aboutToBeDeleted();
        this.__isReinforced.aboutToBeDeleted();
        this.__isMastered.aboutToBeDeleted();
        this.__currentIndex.aboutToBeDeleted();
        this.__queueLength.aboutToBeDeleted();
        this.__reviewMode.aboutToBeDeleted();
        this.__hasNext.aboutToBeDeleted();
        this.__todayReviewCount.aboutToBeDeleted();
        this.__useTwoColumn.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __isDark: ObservedPropertyAbstractPU<boolean>;
    get isDark() {
        return this.__isDark.get();
    }
    set isDark(newValue: boolean) {
        this.__isDark.set(newValue);
    }
    private __title: ObservedPropertySimplePU<string>;
    get title() {
        return this.__title.get();
    }
    set title(newValue: string) {
        this.__title.set(newValue);
    }
    private __contentPadH: ObservedPropertySimplePU<number>;
    get contentPadH() {
        return this.__contentPadH.get();
    }
    set contentPadH(newValue: number) {
        this.__contentPadH.set(newValue);
    }
    private __tags: ObservedPropertyObjectPU<string[]>;
    get tags() {
        return this.__tags.get();
    }
    set tags(newValue: string[]) {
        this.__tags.set(newValue);
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
    private __isHintVisible: ObservedPropertySimplePU<boolean>;
    get isHintVisible() {
        return this.__isHintVisible.get();
    }
    set isHintVisible(newValue: boolean) {
        this.__isHintVisible.set(newValue);
    }
    private __isContentVisible: ObservedPropertySimplePU<boolean>;
    get isContentVisible() {
        return this.__isContentVisible.get();
    }
    set isContentVisible(newValue: boolean) {
        this.__isContentVisible.set(newValue);
    }
    private __isReinforced: ObservedPropertySimplePU<boolean>;
    get isReinforced() {
        return this.__isReinforced.get();
    }
    set isReinforced(newValue: boolean) {
        this.__isReinforced.set(newValue);
    }
    private __isMastered: ObservedPropertySimplePU<boolean>;
    get isMastered() {
        return this.__isMastered.get();
    }
    set isMastered(newValue: boolean) {
        this.__isMastered.set(newValue);
    }
    private __currentIndex: ObservedPropertySimplePU<number>;
    get currentIndex() {
        return this.__currentIndex.get();
    }
    set currentIndex(newValue: number) {
        this.__currentIndex.set(newValue);
    }
    private __queueLength: ObservedPropertySimplePU<number>;
    get queueLength() {
        return this.__queueLength.get();
    }
    set queueLength(newValue: number) {
        this.__queueLength.set(newValue);
    }
    private __reviewMode: ObservedPropertySimplePU<ReviewMode>;
    get reviewMode() {
        return this.__reviewMode.get();
    }
    set reviewMode(newValue: ReviewMode) {
        this.__reviewMode.set(newValue);
    }
    private __hasNext: ObservedPropertySimplePU<boolean>;
    get hasNext() {
        return this.__hasNext.get();
    }
    set hasNext(newValue: boolean) {
        this.__hasNext.set(newValue);
    }
    private __todayReviewCount: ObservedPropertySimplePU<number>;
    get todayReviewCount() {
        return this.__todayReviewCount.get();
    }
    set todayReviewCount(newValue: number) {
        this.__todayReviewCount.set(newValue);
    }
    private __useTwoColumn: ObservedPropertySimplePU<boolean>;
    get useTwoColumn() {
        return this.__useTwoColumn.get();
    }
    set useTwoColumn(newValue: boolean) {
        this.__useTwoColumn.set(newValue);
    }
    aboutToAppear(): void {
        this.contentPadH = pageHorizontalPadding();
        this.useTwoColumn = isTwoColumnCapable();
        this.refreshFromState();
    }
    refreshFromState(): void {
        const point = appState.currentPoint;
        if (point) {
            this.title = point.title;
            this.tags = point.tags;
            this.hint = renderMathRichText(point.hint);
            this.content = renderMathRichText(point.content);
            this.isReinforced = point.isReinforced;
            this.isMastered = point.isMastered;
        }
        this.isHintVisible = appState.isHintVisible;
        this.isContentVisible = appState.isContentVisible;
        this.currentIndex = appState.currentReviewIndex;
        this.queueLength = appState.reviewQueue.length;
        this.reviewMode = appState.reviewMode;
        this.hasNext = appState.hasMoreReviewPoints;
        this.todayReviewCount = appState.todayReviewedAnswerCount;
    }
    /* ── Gradient action button (primary style) ── */
    primaryButton(label: string, gradientStart: string, gradientEnd: string, onTap: () => void, fullWidth: boolean, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.debugLine("entry/src/main/ets/pages/ReviewPage.ets(69:5)", "entry");
            Button.width(fullWidth ? '100%' : undefined);
            Button.height(50);
            Button.borderRadius(16);
            Button.linearGradient({
                angle: 135,
                colors: [[gradientStart, 0], [gradientEnd, 1]]
            });
            Button.shadow({
                radius: 14,
                color: `${gradientStart}60`,
                offsetY: 8
            });
            Button.onClick(() => onTap());
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(label);
            Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(70:7)", "entry");
            Text.fontSize(17);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor('#FFFFFF');
        }, Text);
        Text.pop();
        Button.pop();
    }
    /* ── Bordered secondary action ── */
    secondaryButton(label: string, tint: string, onTap: () => void, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.debugLine("entry/src/main/ets/pages/ReviewPage.ets(93:5)", "entry");
            Button.width('100%');
            Button.height(48);
            Button.borderRadius(14);
            Button.backgroundColor(`${tint}10`);
            Button.border({ width: 1, color: `${tint}33` });
            Button.onClick(() => onTap());
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(label);
            Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(94:7)", "entry");
            Text.fontSize(15);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(tint);
        }, Text);
        Text.pop();
        Button.pop();
    }
    /* ── Glass card for revealed content ── */
    revealCard(label: string, labelColor: string, text: string, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
            Column.debugLine("entry/src/main/ets/pages/ReviewPage.ets(110:5)", "entry");
            Column.width('100%');
            Column.padding(20);
            Column.borderRadius(20);
            Column.backgroundColor(KikariaColors.CARD_BG);
            Column.shadow({
                radius: 12,
                color: KikariaColors.SHADOW_COLOR,
                offsetY: 4
            });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(label);
            Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(111:7)", "entry");
            Text.fontSize(13);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(labelColor);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(text);
            Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(117:7)", "entry");
            Text.fontSize(15);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
            Text.textAlign(TextAlign.Start);
            Text.lineHeight(24);
        }, Text);
        Text.pop();
        Column.pop();
    }
    /* ── Reading column (title, tags, hint, answer) ── */
    readingColumn(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 18 });
            Column.debugLine("entry/src/main/ets/pages/ReviewPage.ets(138:5)", "entry");
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.tags.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create({ space: 8 });
                        Row.debugLine("entry/src/main/ets/pages/ReviewPage.ets(140:9)", "entry");
                        Row.width('100%');
                        Row.justifyContent(FlexAlign.Center);
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        ForEach.create();
                        const forEachItemGenFunction = _item => {
                            const tag = _item;
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(tag);
                                Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(142:13)", "entry");
                                Text.fontSize(12);
                                Text.fontWeight(FontWeight.Medium);
                                Text.fontColor(KikariaColors.SOFT_TEXT);
                                Text.backgroundColor(KikariaColors.MIST);
                                Text.borderRadius(8);
                                Text.padding({ left: 10, right: 10, top: 4, bottom: 4 });
                            }, Text);
                            Text.pop();
                        };
                        this.forEachUpdateFunction(elmtId, this.tags, forEachItemGenFunction);
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
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.debugLine("entry/src/main/ets/pages/ReviewPage.ets(152:7)", "entry");
            Row.width('100%');
            Row.justifyContent(FlexAlign.Center);
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(`今日复习: ${this.todayReviewCount} 次`);
            Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(153:9)", "entry");
            Text.fontSize(12);
            Text.fontColor(KikariaColors.TERTIARY_TEXT);
        }, Text);
        Text.pop();
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.title);
            Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(157:7)", "entry");
            Text.fontSize(32);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.textAlign(TextAlign.Center);
            Text.width('100%');
            Text.padding({ top: 8 });
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.isHintVisible) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.revealCard.bind(this)('💡 提示', KikariaColors.SKY, this.hint);
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.primaryButton.bind(this)('💡 查看提示', KikariaColors.NEXT_GRADIENT_START, KikariaColors.NEXT_GRADIENT_END, () => { appState.showHint(); this.refreshFromState(); }, true);
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.isContentVisible) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.revealCard.bind(this)('📖 答案', KikariaColors.MASTERED_GREEN, this.content);
                });
            }
            else if (this.isHintVisible) {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.primaryButton.bind(this)('📖 查看答案', KikariaColors.ACTION_GRADIENT_START, KikariaColors.ACTION_GRADIENT_END, () => { appState.showContent(); this.refreshFromState(); }, true);
                });
            }
            else {
                this.ifElseBranchUpdateFunction(2, () => {
                });
            }
        }, If);
        If.pop();
        Column.pop();
    }
    /* ── Action panel (right column in two-column landscape) ── */
    actionPanel(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 12 });
            Column.debugLine("entry/src/main/ets/pages/ReviewPage.ets(180:5)", "entry");
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.reviewMode === ReviewMode.REINFORCEMENT) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.secondaryButton.bind(this)('☆ 移出重点', KikariaColors.REMOVE_CORAL, () => {
                        appState.toggleReinforcement();
                        this.refreshFromState();
                        this.advanceOrFinish();
                    });
                    this.secondaryButton.bind(this)('✓ 标记为已掌握', KikariaColors.MASTERED_GREEN, () => {
                        appState.toggleMastered();
                        this.refreshFromState();
                        this.advanceOrFinish();
                    });
                });
            }
            else if (this.reviewMode === ReviewMode.MASTERED) {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.secondaryButton.bind(this)('★ 加入重点', KikariaColors.NEXT_AMBER, () => {
                        appState.toggleReinforcement();
                        this.refreshFromState();
                        this.advanceOrFinish();
                    });
                    this.secondaryButton.bind(this)('↩ 取消掌握', KikariaColors.REMOVE_CORAL, () => {
                        appState.toggleMastered();
                        this.refreshFromState();
                        this.advanceOrFinish();
                    });
                });
            }
            else {
                this.ifElseBranchUpdateFunction(2, () => {
                    this.secondaryButton.bind(this)(this.isReinforced ? '☆ 移出重点' : '★ 加入重点', KikariaColors.NEXT_AMBER, () => {
                        appState.toggleReinforcement();
                        this.refreshFromState();
                    });
                    this.secondaryButton.bind(this)(this.isMastered ? '↩ 取消掌握' : '✓ 标记为已掌握', this.isMastered ? KikariaColors.REMOVE_CORAL : KikariaColors.MASTERED_GREEN, () => {
                        appState.toggleMastered();
                        this.refreshFromState();
                    });
                });
            }
        }, If);
        If.pop();
        this.primaryButton.bind(this)(this.hasNext ? '下一个 →' : '✓ 完成复习', KikariaColors.ACTION_GRADIENT_START, KikariaColors.ACTION_GRADIENT_END, () => { this.advanceOrFinish(); }, true);
        Column.pop();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            NavDestination.create(() => {
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create();
                    Column.debugLine("entry/src/main/ets/pages/ReviewPage.ets(212:7)", "entry");
                    Column.width('100%');
                    Column.height('100%');
                    Column.backgroundColor(KikariaColors.PAGE_BG);
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Top bar: back | mode name | progress
                    Row.create();
                    Row.debugLine("entry/src/main/ets/pages/ReviewPage.ets(214:9)", "entry");
                    // Top bar: back | mode name | progress
                    Row.width('100%');
                    // Top bar: back | mode name | progress
                    Row.padding({ left: 20, right: 20, top: 12, bottom: 12 });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithChild();
                    Button.debugLine("entry/src/main/ets/pages/ReviewPage.ets(215:11)", "entry");
                    Button.backgroundColor(Color.Transparent);
                    Button.onClick(() => {
                        navPathStack.pop();
                    });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('← 返回');
                    Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(216:13)", "entry");
                    Text.fontSize(17);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                Button.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                    Blank.debugLine("entry/src/main/ets/pages/ReviewPage.ets(225:11)", "entry");
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(this.getModeTitle());
                    Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(227:11)", "entry");
                    Text.fontSize(13);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Blank.create();
                    Blank.debugLine("entry/src/main/ets/pages/ReviewPage.ets(231:11)", "entry");
                }, Blank);
                Blank.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(`${this.currentIndex + 1} / ${this.queueLength}`);
                    Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(233:11)", "entry");
                    Text.fontSize(13);
                    Text.fontColor(KikariaColors.SOFT_TEXT);
                }, Text);
                Text.pop();
                // Top bar: back | mode name | progress
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (this.queueLength === 0) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // Empty state
                                Column.create({ space: 16 });
                                Column.debugLine("entry/src/main/ets/pages/ReviewPage.ets(242:11)", "entry");
                                // Empty state
                                Column.width('100%');
                                // Empty state
                                Column.height('100%');
                                // Empty state
                                Column.justifyContent(FlexAlign.Center);
                                // Empty state
                                Column.padding(24);
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create('没有待复习的项目');
                                Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(243:13)", "entry");
                                Text.fontSize(20);
                                Text.fontWeight(FontWeight.Medium);
                                Text.fontColor(KikariaColors.DEEP_TEXT);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(this.getEmptyMessage());
                                Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(248:13)", "entry");
                                Text.fontSize(15);
                                Text.fontColor(KikariaColors.SOFT_TEXT);
                                Text.textAlign(TextAlign.Center);
                                Text.padding({ left: 40, right: 40 });
                            }, Text);
                            Text.pop();
                            this.primaryButton.bind(this)('返回首页', KikariaColors.ACTION_GRADIENT_START, KikariaColors.ACTION_GRADIENT_END, () => {
                                navPathStack.pop();
                            }, false);
                            // Empty state
                            Column.pop();
                        });
                    }
                    else {
                        this.ifElseBranchUpdateFunction(1, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                // Review content
                                Scroll.create();
                                Scroll.debugLine("entry/src/main/ets/pages/ReviewPage.ets(264:11)", "entry");
                                // Review content
                                Scroll.width('100%');
                                // Review content
                                Scroll.layoutWeight(1);
                                // Review content
                                Scroll.scrollBar(BarState.Off);
                                // Review content
                                Scroll.align(Alignment.TopStart);
                                // Review content
                                Scroll.padding({ left: this.contentPadH, right: this.contentPadH });
                            }, Scroll);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                If.create();
                                if (this.useTwoColumn) {
                                    this.ifElseBranchUpdateFunction(0, () => {
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Row.create({ space: reviewLandscapeColumnSpacing() });
                                            Row.debugLine("entry/src/main/ets/pages/ReviewPage.ets(266:15)", "entry");
                                            Row.width('100%');
                                            Row.constraintSize({ maxWidth: reviewLandscapeMaxWidth() });
                                            Row.justifyContent(FlexAlign.Center);
                                        }, Row);
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Column.create();
                                            Column.debugLine("entry/src/main/ets/pages/ReviewPage.ets(267:17)", "entry");
                                            Column.width(reviewLandscapeLeftWidth());
                                        }, Column);
                                        this.readingColumn.bind(this)();
                                        Column.pop();
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Column.create();
                                            Column.debugLine("entry/src/main/ets/pages/ReviewPage.ets(269:17)", "entry");
                                            Column.width(reviewLandscapeRightWidth());
                                        }, Column);
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            If.create();
                                            if (this.isContentVisible) {
                                                this.ifElseBranchUpdateFunction(0, () => {
                                                    this.actionPanel.bind(this)();
                                                });
                                            }
                                            else {
                                                this.ifElseBranchUpdateFunction(1, () => {
                                                });
                                            }
                                        }, If);
                                        If.pop();
                                        Column.pop();
                                        Row.pop();
                                    });
                                }
                                else {
                                    this.ifElseBranchUpdateFunction(1, () => {
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Column.create({ space: 18 });
                                            Column.debugLine("entry/src/main/ets/pages/ReviewPage.ets(278:15)", "entry");
                                            Column.width('100%');
                                            Column.constraintSize({ maxWidth: isPadLandscape() ? reviewMaxWidth() : '100%' });
                                        }, Column);
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            If.create();
                                            if (this.tags.length > 0) {
                                                this.ifElseBranchUpdateFunction(0, () => {
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        Row.create({ space: 8 });
                                                        Row.debugLine("entry/src/main/ets/pages/ReviewPage.ets(280:19)", "entry");
                                                        Row.width('100%');
                                                        Row.justifyContent(FlexAlign.Center);
                                                    }, Row);
                                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                        ForEach.create();
                                                        const forEachItemGenFunction = _item => {
                                                            const tag = _item;
                                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                                Text.create(tag);
                                                                Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(282:23)", "entry");
                                                                Text.fontSize(12);
                                                                Text.fontWeight(FontWeight.Medium);
                                                                Text.fontColor(KikariaColors.SOFT_TEXT);
                                                                Text.backgroundColor(KikariaColors.MIST);
                                                                Text.borderRadius(8);
                                                                Text.padding({ left: 10, right: 10, top: 4, bottom: 4 });
                                                            }, Text);
                                                            Text.pop();
                                                        };
                                                        this.forEachUpdateFunction(elmtId, this.tags, forEachItemGenFunction);
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
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Row.create();
                                            Row.debugLine("entry/src/main/ets/pages/ReviewPage.ets(291:17)", "entry");
                                            Row.width('100%');
                                            Row.justifyContent(FlexAlign.Center);
                                        }, Row);
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Text.create(`今日复习: ${this.todayReviewCount} 次`);
                                            Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(292:19)", "entry");
                                            Text.fontSize(12);
                                            Text.fontColor(KikariaColors.TERTIARY_TEXT);
                                        }, Text);
                                        Text.pop();
                                        Row.pop();
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Text.create(this.title);
                                            Text.debugLine("entry/src/main/ets/pages/ReviewPage.ets(296:17)", "entry");
                                            Text.fontSize(32);
                                            Text.fontWeight(FontWeight.Bold);
                                            Text.fontColor(KikariaColors.DEEP_TEXT);
                                            Text.textAlign(TextAlign.Center);
                                            Text.width('100%');
                                            Text.padding({ top: 8 });
                                        }, Text);
                                        Text.pop();
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            If.create();
                                            if (this.isHintVisible) {
                                                this.ifElseBranchUpdateFunction(0, () => {
                                                    this.revealCard.bind(this)('💡 提示', KikariaColors.SKY, this.hint);
                                                });
                                            }
                                            else {
                                                this.ifElseBranchUpdateFunction(1, () => {
                                                    this.primaryButton.bind(this)('💡 查看提示', KikariaColors.NEXT_GRADIENT_START, KikariaColors.NEXT_GRADIENT_END, () => { appState.showHint(); this.refreshFromState(); }, true);
                                                });
                                            }
                                        }, If);
                                        If.pop();
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            If.create();
                                            if (this.isContentVisible) {
                                                this.ifElseBranchUpdateFunction(0, () => {
                                                    this.revealCard.bind(this)('📖 答案', KikariaColors.MASTERED_GREEN, this.content);
                                                    this.actionPanel.bind(this)();
                                                });
                                            }
                                            else if (this.isHintVisible) {
                                                this.ifElseBranchUpdateFunction(1, () => {
                                                    this.primaryButton.bind(this)('📖 查看答案', KikariaColors.ACTION_GRADIENT_START, KikariaColors.ACTION_GRADIENT_END, () => { appState.showContent(); this.refreshFromState(); }, true);
                                                });
                                            }
                                            else {
                                                this.ifElseBranchUpdateFunction(2, () => {
                                                });
                                            }
                                        }, If);
                                        If.pop();
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Row.create();
                                            Row.debugLine("entry/src/main/ets/pages/ReviewPage.ets(314:17)", "entry");
                                            Row.height(40);
                                        }, Row);
                                        Row.pop();
                                        Column.pop();
                                    });
                                }
                            }, If);
                            If.pop();
                            // Review content
                            Scroll.pop();
                        });
                    }
                }, If);
                If.pop();
                Column.pop();
            }, { moduleName: "entry", pagePath: "entry/src/main/ets/pages/ReviewPage" });
            NavDestination.hideTitleBar(true);
            NavDestination.width('100%');
            NavDestination.height('100%');
            NavDestination.debugLine("entry/src/main/ets/pages/ReviewPage.ets(211:5)", "entry");
        }, NavDestination);
        NavDestination.pop();
    }
    advanceOrFinish(): void {
        if (this.hasNext) {
            appState.nextPoint();
            setTimeout(() => {
                this.refreshFromState();
            }, 200);
        }
        else {
            navPathStack.pop();
        }
    }
    getModeTitle(): string {
        switch (this.reviewMode) {
            case ReviewMode.NORMAL:
                return '复习';
            case ReviewMode.REINFORCEMENT:
                return '重点复习';
            case ReviewMode.MASTERED:
                return '已掌握回顾';
        }
    }
    getEmptyMessage(): string {
        switch (this.reviewMode) {
            case ReviewMode.NORMAL:
                return '当前范围内所有知识点均已掌握！调整标签筛选或继续巩固吧。';
            case ReviewMode.REINFORCEMENT:
                return '重点集锦中暂无项目。在复习过程中将重要知识点加入重点集锦吧。';
            case ReviewMode.MASTERED:
                return '暂无已掌握的知识点。继续复习，掌握更多知识吧！';
        }
    }
    rerender() {
        this.updateDirtyElements();
    }
}
class ReviewPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: ReviewPage_Params) {
    }
    updateStateVars(params: ReviewPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Navigation.create(new NavPathStack(), { moduleName: "entry", pagePath: "entry/src/main/ets/pages/ReviewPage", isUserCreateStack: false });
            Navigation.debugLine("entry/src/main/ets/pages/ReviewPage.ets(374:5)", "entry");
            Navigation.width('100%');
            Navigation.height('100%');
            Navigation.backgroundColor(KikariaColors.PAGE_BG);
            Navigation.hideTitleBar(true);
            Navigation.hideBackButton(true);
        }, Navigation);
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new ReviewPageContent(this, {}, undefined, elmtId, () => { }, { page: "entry/src/main/ets/pages/ReviewPage.ets", line: 375, col: 7 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {};
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {});
                }
            }, { name: "ReviewPageContent" });
        }
        Navigation.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "ReviewPage";
    }
}
registerNamedRoute(() => new ReviewPage(undefined, {}), "", { bundleName: "com.vita0818.kikaria", moduleName: "entry", pagePath: "pages/ReviewPage", pageFullPath: "entry/src/main/ets/pages/ReviewPage", integratedHsp: "false", moduleType: "followWithHap" });
