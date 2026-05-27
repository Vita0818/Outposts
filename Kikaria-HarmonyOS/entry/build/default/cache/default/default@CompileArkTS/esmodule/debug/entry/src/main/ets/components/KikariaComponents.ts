if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface AdaptiveBackButton_Params {
    label?: string;
    onTap?: () => void;
}
interface DashboardMetricColumn_Params {
    title?: string;
    valueText?: string;
    tintColor?: string;
    isExpanded?: boolean;
}
interface ProfileAvatar_Params {
    displayName?: string;
    avatarSize?: number;
    avatarFontSize?: number;
    avatarColor?: string;
    textColor?: string;
}
interface FloatingInfoCard_Params {
    label?: string;
    text?: string;
    labelColor?: string;
}
interface TagChip_Params {
    label?: string;
    isSelected?: boolean;
    onTap?: () => void;
}
interface KikariaButton_Params {
    label?: string;
    buttonType?: string;
    fontSize?: number;
    isEnabled?: boolean;
    onTap?: () => void;
}
interface SettingsStepperRow_Params {
    label?: string;
    value?: number;
    min?: number;
    max?: number;
    valueColor?: string;
    onDecrement?: () => void;
    onIncrement?: () => void;
}
interface SettingsInfoTextRow_Params {
    text?: string;
}
interface SettingsToggleRow_Params {
    label?: string;
    isOn?: boolean;
    onChange?: (value: boolean) => void;
}
interface SettingsListRow_Params {
    label?: string;
    value?: string;
    showArrow?: boolean;
    labelColor?: string;
    valueColor?: string;
    onTap?: () => void;
}
interface SettingsSectionDivider_Params {
}
interface SettingsSectionCard_Params {
    content?: () => void;
}
interface KikariaGlassCard_Params {
    cornerRadius?: number;
    paddingSize?: number;
    content?: () => void;
}
interface KikariaCard_Params {
    cornerRadius?: number;
    content?: () => void;
}
interface LiquidGlassCircle_Params {
    circleSize?: number;
    content?: () => void;
}
interface LiquidGlassCapsule_Params {
    paddingH?: number;
    paddingV?: number;
    content?: () => void;
}
interface LiquidGlassCard_Params {
    cornerRadius?: number;
    paddingSize?: number;
    content?: () => void;
}
import { KikariaColors, isDarkModeEnabled } from "@bundle:com.vita0818.kikaria/entry/ets/components/KikariaTheme";
/* ── Typed gradient descriptor (ArkTS strict mode) ── */
export class KikariaGradient {
    angle: number;
    colors: [
        string,
        number
    ][];
    constructor(startColor: string, endColor: string, angle: number = 135) {
        this.angle = angle;
        this.colors = [[startColor, 0], [endColor, 1]];
    }
}
/* ── Glass style helpers ── */
function glassFillOpacity(): number {
    return isDarkModeEnabled() ? 0.13 : 0.48;
}
function glassStrokeColor(): string {
    return isDarkModeEnabled() ? '#FFFFFF1A' : '#FFFFFF6B';
}
function glassAccentColor(): string {
    return isDarkModeEnabled() ? '#6BD6EF38' : '#91E0E821';
}
function glassShadowOpacity(): number {
    return isDarkModeEnabled() ? 0.08 : 0.12;
}
export class LiquidGlassCard extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__cornerRadius = new SynchedPropertySimpleOneWayPU(params.cornerRadius, this, "cornerRadius");
        this.__paddingSize = new SynchedPropertySimpleOneWayPU(params.paddingSize, this, "paddingSize");
        this.content = undefined;
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: LiquidGlassCard_Params) {
        if (params.cornerRadius === undefined) {
            this.__cornerRadius.set(25);
        }
        if (params.paddingSize === undefined) {
            this.__paddingSize.set(20);
        }
        if (params.content !== undefined) {
            this.content = params.content;
        }
    }
    updateStateVars(params: LiquidGlassCard_Params) {
        this.__cornerRadius.reset(params.cornerRadius);
        this.__paddingSize.reset(params.paddingSize);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__cornerRadius.purgeDependencyOnElmtId(rmElmtId);
        this.__paddingSize.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__cornerRadius.aboutToBeDeleted();
        this.__paddingSize.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __cornerRadius: SynchedPropertySimpleOneWayPU<number>;
    get cornerRadius() {
        return this.__cornerRadius.get();
    }
    set cornerRadius(newValue: number) {
        this.__cornerRadius.set(newValue);
    }
    private __paddingSize: SynchedPropertySimpleOneWayPU<number>;
    get paddingSize() {
        return this.__paddingSize.get();
    }
    set paddingSize(newValue: number) {
        this.__paddingSize.set(newValue);
    }
    private __content;
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.borderRadius(this.cornerRadius);
            Column.backgroundColor(KikariaColors.GLASS_SURFACE + (isDarkModeEnabled() ? '33' : '7A'));
            Column.backdropBlur(24);
            Column.shadow({
                radius: 17,
                color: KikariaColors.SHADOW_COLOR,
                offsetY: 9
            });
            Column.padding(this.paddingSize);
        }, Column);
        this.content.bind(this)();
        Column.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class LiquidGlassCapsule extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__paddingH = new SynchedPropertySimpleOneWayPU(params.paddingH, this, "paddingH");
        this.__paddingV = new SynchedPropertySimpleOneWayPU(params.paddingV, this, "paddingV");
        this.content = undefined;
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: LiquidGlassCapsule_Params) {
        if (params.paddingH === undefined) {
            this.__paddingH.set(24);
        }
        if (params.paddingV === undefined) {
            this.__paddingV.set(16);
        }
        if (params.content !== undefined) {
            this.content = params.content;
        }
    }
    updateStateVars(params: LiquidGlassCapsule_Params) {
        this.__paddingH.reset(params.paddingH);
        this.__paddingV.reset(params.paddingV);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__paddingH.purgeDependencyOnElmtId(rmElmtId);
        this.__paddingV.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__paddingH.aboutToBeDeleted();
        this.__paddingV.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __paddingH: SynchedPropertySimpleOneWayPU<number>;
    get paddingH() {
        return this.__paddingH.get();
    }
    set paddingH(newValue: number) {
        this.__paddingH.set(newValue);
    }
    private __paddingV: SynchedPropertySimpleOneWayPU<number>;
    get paddingV() {
        return this.__paddingV.get();
    }
    set paddingV(newValue: number) {
        this.__paddingV.set(newValue);
    }
    private __content;
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.borderRadius(999);
            Row.backgroundColor(KikariaColors.GLASS_SURFACE + (isDarkModeEnabled() ? '33' : '7A'));
            Row.backdropBlur(20);
            Row.shadow({
                radius: 14,
                color: KikariaColors.SHADOW_COLOR,
                offsetY: 7
            });
            Row.padding({ left: this.paddingH, right: this.paddingH, top: this.paddingV, bottom: this.paddingV });
        }, Row);
        this.content.bind(this)();
        Row.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class LiquidGlassCircle extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__circleSize = new SynchedPropertySimpleOneWayPU(params.circleSize, this, "circleSize");
        this.content = undefined;
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: LiquidGlassCircle_Params) {
        if (params.circleSize === undefined) {
            this.__circleSize.set(200);
        }
        if (params.content !== undefined) {
            this.content = params.content;
        }
    }
    updateStateVars(params: LiquidGlassCircle_Params) {
        this.__circleSize.reset(params.circleSize);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__circleSize.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__circleSize.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __circleSize: SynchedPropertySimpleOneWayPU<number>;
    get circleSize() {
        return this.__circleSize.get();
    }
    set circleSize(newValue: number) {
        this.__circleSize.set(newValue);
    }
    private __content;
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Stack.create();
            Stack.width(this.circleSize);
            Stack.height(this.circleSize);
            Stack.borderRadius(this.circleSize / 2);
            Stack.backgroundColor(KikariaColors.GLASS_SURFACE + (isDarkModeEnabled() ? '33' : '7A'));
            Stack.backdropBlur(20);
            Stack.shadow({
                radius: 14,
                color: KikariaColors.SHADOW_COLOR,
                offsetY: 7
            });
            Stack.alignContent(Alignment.Center);
        }, Stack);
        this.content.bind(this)();
        Stack.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class KikariaCard extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__cornerRadius = new SynchedPropertySimpleOneWayPU(params.cornerRadius, this, "cornerRadius");
        this.content = undefined;
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: KikariaCard_Params) {
        if (params.cornerRadius === undefined) {
            this.__cornerRadius.set(20);
        }
        if (params.content !== undefined) {
            this.content = params.content;
        }
    }
    updateStateVars(params: KikariaCard_Params) {
        this.__cornerRadius.reset(params.cornerRadius);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__cornerRadius.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__cornerRadius.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __cornerRadius: SynchedPropertySimpleOneWayPU<number>;
    get cornerRadius() {
        return this.__cornerRadius.get();
    }
    set cornerRadius(newValue: number) {
        this.__cornerRadius.set(newValue);
    }
    private __content;
    initialRender() {
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new LiquidGlassCard(this, {
                        cornerRadius: this.cornerRadius,
                        content: () => {
                            this.content.bind(this)();
                        }
                    }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/components/KikariaComponents.ets", line: 111, col: 5 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {
                            cornerRadius: this.cornerRadius,
                            content: () => {
                                this.content.bind(this)();
                            }
                        };
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {
                        cornerRadius: this.cornerRadius
                    });
                }
            }, { name: "LiquidGlassCard" });
        }
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class KikariaGlassCard extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__cornerRadius = new SynchedPropertySimpleOneWayPU(params.cornerRadius, this, "cornerRadius");
        this.__paddingSize = new SynchedPropertySimpleOneWayPU(params.paddingSize, this, "paddingSize");
        this.content = undefined;
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: KikariaGlassCard_Params) {
        if (params.cornerRadius === undefined) {
            this.__cornerRadius.set(25);
        }
        if (params.paddingSize === undefined) {
            this.__paddingSize.set(20);
        }
        if (params.content !== undefined) {
            this.content = params.content;
        }
    }
    updateStateVars(params: KikariaGlassCard_Params) {
        this.__cornerRadius.reset(params.cornerRadius);
        this.__paddingSize.reset(params.paddingSize);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__cornerRadius.purgeDependencyOnElmtId(rmElmtId);
        this.__paddingSize.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__cornerRadius.aboutToBeDeleted();
        this.__paddingSize.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __cornerRadius: SynchedPropertySimpleOneWayPU<number>;
    get cornerRadius() {
        return this.__cornerRadius.get();
    }
    set cornerRadius(newValue: number) {
        this.__cornerRadius.set(newValue);
    }
    private __paddingSize: SynchedPropertySimpleOneWayPU<number>;
    get paddingSize() {
        return this.__paddingSize.get();
    }
    set paddingSize(newValue: number) {
        this.__paddingSize.set(newValue);
    }
    private __content;
    initialRender() {
        {
            this.observeComponentCreation2((elmtId, isInitialRender) => {
                if (isInitialRender) {
                    let componentCall = new LiquidGlassCard(this, {
                        cornerRadius: this.cornerRadius, paddingSize: this.paddingSize,
                        content: () => {
                            this.content.bind(this)();
                        }
                    }, undefined, elmtId, () => { }, { page: "entry/src/main/ets/components/KikariaComponents.ets", line: 125, col: 5 });
                    ViewPU.create(componentCall);
                    let paramsLambda = () => {
                        return {
                            cornerRadius: this.cornerRadius,
                            paddingSize: this.paddingSize,
                            content: () => {
                                this.content.bind(this)();
                            }
                        };
                    };
                    componentCall.paramsGenerator_ = paramsLambda;
                }
                else {
                    this.updateStateVarsOfChildByElmtId(elmtId, {
                        cornerRadius: this.cornerRadius, paddingSize: this.paddingSize
                    });
                }
            }, { name: "LiquidGlassCard" });
        }
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class SettingsSectionCard extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.content = undefined;
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: SettingsSectionCard_Params) {
        if (params.content !== undefined) {
            this.content = params.content;
        }
    }
    updateStateVars(params: SettingsSectionCard_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __content;
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.borderRadius(22);
            Column.backgroundColor(KikariaColors.CARD_BG);
            Column.shadow({
                radius: 8,
                color: KikariaColors.SHADOW_COLOR,
                offsetY: 2
            });
            Column.padding(0);
        }, Column);
        this.content.bind(this)();
        Column.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class SettingsSectionDivider extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: SettingsSectionDivider_Params) {
    }
    updateStateVars(params: SettingsSectionDivider_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
    }
    aboutToBeDeleted() {
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
            Row.height(0.5);
            Row.backgroundColor(KikariaColors.BLUE_GRAY_LIGHT + '1E');
            Row.margin({ left: 16, right: 16 });
        }, Row);
        Row.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class SettingsListRow extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__label = new SynchedPropertySimpleOneWayPU(params.label, this, "label");
        this.__value = new SynchedPropertySimpleOneWayPU(params.value, this, "value");
        this.__showArrow = new SynchedPropertySimpleOneWayPU(params.showArrow, this, "showArrow");
        this.__labelColor = new SynchedPropertySimpleOneWayPU(params.labelColor, this, "labelColor");
        this.__valueColor = new SynchedPropertySimpleOneWayPU(params.valueColor, this, "valueColor");
        this.onTap = () => { };
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: SettingsListRow_Params) {
        if (params.label === undefined) {
            this.__label.set('');
        }
        if (params.value === undefined) {
            this.__value.set('');
        }
        if (params.showArrow === undefined) {
            this.__showArrow.set(true);
        }
        if (params.labelColor === undefined) {
            this.__labelColor.set(KikariaColors.DEEP_TEXT);
        }
        if (params.valueColor === undefined) {
            this.__valueColor.set(KikariaColors.SKY);
        }
        if (params.onTap !== undefined) {
            this.onTap = params.onTap;
        }
    }
    updateStateVars(params: SettingsListRow_Params) {
        this.__label.reset(params.label);
        this.__value.reset(params.value);
        this.__showArrow.reset(params.showArrow);
        this.__labelColor.reset(params.labelColor);
        this.__valueColor.reset(params.valueColor);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__label.purgeDependencyOnElmtId(rmElmtId);
        this.__value.purgeDependencyOnElmtId(rmElmtId);
        this.__showArrow.purgeDependencyOnElmtId(rmElmtId);
        this.__labelColor.purgeDependencyOnElmtId(rmElmtId);
        this.__valueColor.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__label.aboutToBeDeleted();
        this.__value.aboutToBeDeleted();
        this.__showArrow.aboutToBeDeleted();
        this.__labelColor.aboutToBeDeleted();
        this.__valueColor.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __label: SynchedPropertySimpleOneWayPU<string>;
    get label() {
        return this.__label.get();
    }
    set label(newValue: string) {
        this.__label.set(newValue);
    }
    private __value: SynchedPropertySimpleOneWayPU<string>;
    get value() {
        return this.__value.get();
    }
    set value(newValue: string) {
        this.__value.set(newValue);
    }
    private __showArrow: SynchedPropertySimpleOneWayPU<boolean>;
    get showArrow() {
        return this.__showArrow.get();
    }
    set showArrow(newValue: boolean) {
        this.__showArrow.set(newValue);
    }
    private __labelColor: SynchedPropertySimpleOneWayPU<string>;
    get labelColor() {
        return this.__labelColor.get();
    }
    set labelColor(newValue: string) {
        this.__labelColor.set(newValue);
    }
    private __valueColor: SynchedPropertySimpleOneWayPU<string>;
    get valueColor() {
        return this.__valueColor.get();
    }
    set valueColor(newValue: string) {
        this.__valueColor.set(newValue);
    }
    private onTap: () => void;
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
            Row.padding({ left: 16, right: 16, top: 14, bottom: 14 });
            Row.onClick(() => this.onTap());
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.label);
            Text.fontSize(15);
            Text.fontColor(this.labelColor);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.value.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.value);
                        Text.fontSize(15);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(this.valueColor);
                        Text.padding({ right: 4 });
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
            if (this.showArrow) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('›');
                        Text.fontSize(16);
                        Text.fontColor(KikariaColors.TERTIARY_TEXT);
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
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class SettingsToggleRow extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__label = new SynchedPropertySimpleOneWayPU(params.label, this, "label");
        this.__isOn = new SynchedPropertySimpleTwoWayPU(params.isOn, this, "isOn");
        this.onChange = (_v: boolean) => { };
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: SettingsToggleRow_Params) {
        if (params.label === undefined) {
            this.__label.set('');
        }
        if (params.onChange !== undefined) {
            this.onChange = params.onChange;
        }
    }
    updateStateVars(params: SettingsToggleRow_Params) {
        this.__label.reset(params.label);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__label.purgeDependencyOnElmtId(rmElmtId);
        this.__isOn.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__label.aboutToBeDeleted();
        this.__isOn.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __label: SynchedPropertySimpleOneWayPU<string>;
    get label() {
        return this.__label.get();
    }
    set label(newValue: string) {
        this.__label.set(newValue);
    }
    private __isOn: SynchedPropertySimpleTwoWayPU<boolean>;
    get isOn() {
        return this.__isOn.get();
    }
    set isOn(newValue: boolean) {
        this.__isOn.set(newValue);
    }
    private onChange: (value: boolean) => void;
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
            Row.padding({ left: 16, right: 16, top: 14, bottom: 14 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.label);
            Text.fontSize(15);
            Text.fontColor(KikariaColors.DEEP_TEXT);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Toggle.create({ type: ToggleType.Switch, isOn: this.isOn });
            Toggle.onChange((value: boolean) => {
                this.onChange(value);
            });
        }, Toggle);
        Toggle.pop();
        Row.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class SettingsInfoTextRow extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__text = new SynchedPropertySimpleOneWayPU(params.text, this, "text");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: SettingsInfoTextRow_Params) {
        if (params.text === undefined) {
            this.__text.set('');
        }
    }
    updateStateVars(params: SettingsInfoTextRow_Params) {
        this.__text.reset(params.text);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__text.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__text.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __text: SynchedPropertySimpleOneWayPU<string>;
    get text() {
        return this.__text.get();
    }
    set text(newValue: string) {
        this.__text.set(newValue);
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.text);
            Text.fontSize(12);
            Text.fontColor(KikariaColors.TERTIARY_TEXT);
            Text.width('100%');
            Text.padding({ left: 16, right: 16, top: 8, bottom: 8 });
        }, Text);
        Text.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class SettingsStepperRow extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__label = new SynchedPropertySimpleOneWayPU(params.label, this, "label");
        this.__value = new SynchedPropertySimpleOneWayPU(params.value, this, "value");
        this.__min = new SynchedPropertySimpleOneWayPU(params.min, this, "min");
        this.__max = new SynchedPropertySimpleOneWayPU(params.max, this, "max");
        this.__valueColor = new SynchedPropertySimpleOneWayPU(params.valueColor, this, "valueColor");
        this.onDecrement = () => { };
        this.onIncrement = () => { };
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: SettingsStepperRow_Params) {
        if (params.label === undefined) {
            this.__label.set('');
        }
        if (params.value === undefined) {
            this.__value.set(20);
        }
        if (params.min === undefined) {
            this.__min.set(1);
        }
        if (params.max === undefined) {
            this.__max.set(200);
        }
        if (params.valueColor === undefined) {
            this.__valueColor.set(KikariaColors.DEEP_TEXT);
        }
        if (params.onDecrement !== undefined) {
            this.onDecrement = params.onDecrement;
        }
        if (params.onIncrement !== undefined) {
            this.onIncrement = params.onIncrement;
        }
    }
    updateStateVars(params: SettingsStepperRow_Params) {
        this.__label.reset(params.label);
        this.__value.reset(params.value);
        this.__min.reset(params.min);
        this.__max.reset(params.max);
        this.__valueColor.reset(params.valueColor);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__label.purgeDependencyOnElmtId(rmElmtId);
        this.__value.purgeDependencyOnElmtId(rmElmtId);
        this.__min.purgeDependencyOnElmtId(rmElmtId);
        this.__max.purgeDependencyOnElmtId(rmElmtId);
        this.__valueColor.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__label.aboutToBeDeleted();
        this.__value.aboutToBeDeleted();
        this.__min.aboutToBeDeleted();
        this.__max.aboutToBeDeleted();
        this.__valueColor.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __label: SynchedPropertySimpleOneWayPU<string>;
    get label() {
        return this.__label.get();
    }
    set label(newValue: string) {
        this.__label.set(newValue);
    }
    private __value: SynchedPropertySimpleOneWayPU<number>;
    get value() {
        return this.__value.get();
    }
    set value(newValue: number) {
        this.__value.set(newValue);
    }
    private __min: SynchedPropertySimpleOneWayPU<number>;
    get min() {
        return this.__min.get();
    }
    set min(newValue: number) {
        this.__min.set(newValue);
    }
    private __max: SynchedPropertySimpleOneWayPU<number>;
    get max() {
        return this.__max.get();
    }
    set max(newValue: number) {
        this.__max.set(newValue);
    }
    private __valueColor: SynchedPropertySimpleOneWayPU<string>;
    get valueColor() {
        return this.__valueColor.get();
    }
    set valueColor(newValue: string) {
        this.__valueColor.set(newValue);
    }
    private onDecrement: () => void;
    private onIncrement: () => void;
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
            Row.padding({ left: 16, right: 16, top: 14, bottom: 14 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.label);
            Text.fontSize(15);
            Text.fontColor(KikariaColors.DEEP_TEXT);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 8 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithLabel('-');
            Button.fontSize(20);
            Button.fontWeight(FontWeight.Bold);
            Button.fontColor(KikariaColors.SKY);
            Button.width(36);
            Button.height(36);
            Button.backgroundColor(KikariaColors.MIST);
            Button.borderRadius(18);
            Button.onClick(() => this.onDecrement());
        }, Button);
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(`${this.value}`);
            Text.fontSize(18);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(this.valueColor);
            Text.width(40);
            Text.textAlign(TextAlign.Center);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithLabel('+');
            Button.fontSize(20);
            Button.fontWeight(FontWeight.Bold);
            Button.fontColor(KikariaColors.SKY);
            Button.width(36);
            Button.height(36);
            Button.backgroundColor(KikariaColors.MIST);
            Button.borderRadius(18);
            Button.onClick(() => this.onIncrement());
        }, Button);
        Button.pop();
        Row.pop();
        Row.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class KikariaButton extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__label = new SynchedPropertySimpleOneWayPU(params.label, this, "label");
        this.__buttonType = new SynchedPropertySimpleOneWayPU(params.buttonType, this, "buttonType");
        this.__fontSize = new SynchedPropertySimpleOneWayPU(params.fontSize, this, "fontSize");
        this.__isEnabled = new SynchedPropertySimpleOneWayPU(params.isEnabled, this, "isEnabled");
        this.onTap = () => { };
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: KikariaButton_Params) {
        if (params.label === undefined) {
            this.__label.set('');
        }
        if (params.buttonType === undefined) {
            this.__buttonType.set('primary');
        }
        if (params.fontSize === undefined) {
            this.__fontSize.set(17);
        }
        if (params.isEnabled === undefined) {
            this.__isEnabled.set(true);
        }
        if (params.onTap !== undefined) {
            this.onTap = params.onTap;
        }
    }
    updateStateVars(params: KikariaButton_Params) {
        this.__label.reset(params.label);
        this.__buttonType.reset(params.buttonType);
        this.__fontSize.reset(params.fontSize);
        this.__isEnabled.reset(params.isEnabled);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__label.purgeDependencyOnElmtId(rmElmtId);
        this.__buttonType.purgeDependencyOnElmtId(rmElmtId);
        this.__fontSize.purgeDependencyOnElmtId(rmElmtId);
        this.__isEnabled.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__label.aboutToBeDeleted();
        this.__buttonType.aboutToBeDeleted();
        this.__fontSize.aboutToBeDeleted();
        this.__isEnabled.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __label: SynchedPropertySimpleOneWayPU<string>;
    get label() {
        return this.__label.get();
    }
    set label(newValue: string) {
        this.__label.set(newValue);
    }
    private __buttonType: SynchedPropertySimpleOneWayPU<string>;
    get buttonType() {
        return this.__buttonType.get();
    }
    set buttonType(newValue: string) {
        this.__buttonType.set(newValue);
    }
    private __fontSize: SynchedPropertySimpleOneWayPU<number>;
    get fontSize() {
        return this.__fontSize.get();
    }
    set fontSize(newValue: number) {
        this.__fontSize.set(newValue);
    }
    private __isEnabled: SynchedPropertySimpleOneWayPU<boolean>;
    get isEnabled() {
        return this.__isEnabled.get();
    }
    set isEnabled(newValue: boolean) {
        this.__isEnabled.set(newValue);
    }
    private onTap: () => void;
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithLabel(this.label);
            Button.fontSize(this.fontSize);
            Button.fontWeight(FontWeight.Medium);
            Button.fontColor(this.getTextColor());
            Button.linearGradient(this.getGradient());
            Button.borderRadius(16);
            Button.height(50);
            Button.width('100%');
            Button.enabled(this.isEnabled);
            Button.opacity(this.isEnabled ? 1.0 : 0.5);
            Button.shadow({
                radius: 12,
                color: KikariaColors.SHADOW_COLOR,
                offsetY: 6
            });
            Button.onClick(() => {
                if (this.isEnabled) {
                    this.onTap();
                }
            });
        }, Button);
        Button.pop();
    }
    getGradient(): KikariaGradient {
        switch (this.buttonType) {
            case 'primary':
                return new KikariaGradient(KikariaColors.ACTION_GRADIENT_START, KikariaColors.ACTION_GRADIENT_END);
            case 'mastered':
                return new KikariaGradient(KikariaColors.MASTERED_GRADIENT_START, KikariaColors.MASTERED_GRADIENT_END);
            case 'next':
                return new KikariaGradient(KikariaColors.NEXT_GRADIENT_START, KikariaColors.NEXT_GRADIENT_END);
            case 'danger':
                return new KikariaGradient(KikariaColors.REMOVE_GRADIENT_START, KikariaColors.REMOVE_GRADIENT_END);
            case 'secondary':
                return new KikariaGradient(KikariaColors.MIST, KikariaColors.MIST);
            default:
                return new KikariaGradient(KikariaColors.SKY, KikariaColors.CYAN);
        }
    }
    getTextColor(): string {
        switch (this.buttonType) {
            case 'secondary':
            case 'ghost':
                return KikariaColors.DEEP_TEXT;
            default:
                return '#FFFFFF';
        }
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class TagChip extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__label = new SynchedPropertySimpleOneWayPU(params.label, this, "label");
        this.__isSelected = new SynchedPropertySimpleOneWayPU(params.isSelected, this, "isSelected");
        this.onTap = () => { };
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: TagChip_Params) {
        if (params.label === undefined) {
            this.__label.set('');
        }
        if (params.isSelected === undefined) {
            this.__isSelected.set(false);
        }
        if (params.onTap !== undefined) {
            this.onTap = params.onTap;
        }
    }
    updateStateVars(params: TagChip_Params) {
        this.__label.reset(params.label);
        this.__isSelected.reset(params.isSelected);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__label.purgeDependencyOnElmtId(rmElmtId);
        this.__isSelected.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__label.aboutToBeDeleted();
        this.__isSelected.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __label: SynchedPropertySimpleOneWayPU<string>;
    get label() {
        return this.__label.get();
    }
    set label(newValue: string) {
        this.__label.set(newValue);
    }
    private __isSelected: SynchedPropertySimpleOneWayPU<boolean>;
    get isSelected() {
        return this.__isSelected.get();
    }
    set isSelected(newValue: boolean) {
        this.__isSelected.set(newValue);
    }
    private onTap: () => void;
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.label);
            Text.fontSize(12);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(this.isSelected ? '#FFFFFF' : KikariaColors.SOFT_TEXT);
            Text.backgroundColor(this.isSelected ? KikariaColors.SKY : KikariaColors.MIST);
            Text.borderRadius(12);
            Text.padding({ left: 12, right: 12, top: 6, bottom: 6 });
            Text.onClick(() => this.onTap());
        }, Text);
        Text.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class FloatingInfoCard extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__label = new SynchedPropertySimpleOneWayPU(params.label, this, "label");
        this.__text = new SynchedPropertySimpleOneWayPU(params.text, this, "text");
        this.__labelColor = new SynchedPropertySimpleOneWayPU(params.labelColor, this, "labelColor");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: FloatingInfoCard_Params) {
        if (params.label === undefined) {
            this.__label.set('');
        }
        if (params.text === undefined) {
            this.__text.set('');
        }
        if (params.labelColor === undefined) {
            this.__labelColor.set('#63BAF5');
        }
    }
    updateStateVars(params: FloatingInfoCard_Params) {
        this.__label.reset(params.label);
        this.__text.reset(params.text);
        this.__labelColor.reset(params.labelColor);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__label.purgeDependencyOnElmtId(rmElmtId);
        this.__text.purgeDependencyOnElmtId(rmElmtId);
        this.__labelColor.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__label.aboutToBeDeleted();
        this.__text.aboutToBeDeleted();
        this.__labelColor.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __label: SynchedPropertySimpleOneWayPU<string>;
    get label() {
        return this.__label.get();
    }
    set label(newValue: string) {
        this.__label.set(newValue);
    }
    private __text: SynchedPropertySimpleOneWayPU<string>;
    get text() {
        return this.__text.get();
    }
    set text(newValue: string) {
        this.__text.set(newValue);
    }
    private __labelColor: SynchedPropertySimpleOneWayPU<string>;
    get labelColor() {
        return this.__labelColor.get();
    }
    set labelColor(newValue: string) {
        this.__labelColor.set(newValue);
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
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
            Text.create(this.label);
            Text.fontSize(13);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(this.labelColor);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.text);
            Text.fontSize(15);
            Text.fontColor(KikariaColors.DEEP_TEXT);
            Text.width('100%');
            Text.textAlign(TextAlign.Start);
            Text.lineHeight(24);
        }, Text);
        Text.pop();
        Column.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class ProfileAvatar extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__displayName = new SynchedPropertySimpleOneWayPU(params.displayName, this, "displayName");
        this.__avatarSize = new SynchedPropertySimpleOneWayPU(params.avatarSize, this, "avatarSize");
        this.__avatarFontSize = new SynchedPropertySimpleOneWayPU(params.avatarFontSize, this, "avatarFontSize");
        this.__avatarColor = new SynchedPropertySimpleOneWayPU(params.avatarColor, this, "avatarColor");
        this.__textColor = new SynchedPropertySimpleOneWayPU(params.textColor, this, "textColor");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: ProfileAvatar_Params) {
        if (params.displayName === undefined) {
            this.__displayName.set('V');
        }
        if (params.avatarSize === undefined) {
            this.__avatarSize.set(56);
        }
        if (params.avatarFontSize === undefined) {
            this.__avatarFontSize.set(28);
        }
        if (params.avatarColor === undefined) {
            this.__avatarColor.set('#63BAF5');
        }
        if (params.textColor === undefined) {
            this.__textColor.set('#FFFFFF');
        }
    }
    updateStateVars(params: ProfileAvatar_Params) {
        this.__displayName.reset(params.displayName);
        this.__avatarSize.reset(params.avatarSize);
        this.__avatarFontSize.reset(params.avatarFontSize);
        this.__avatarColor.reset(params.avatarColor);
        this.__textColor.reset(params.textColor);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__displayName.purgeDependencyOnElmtId(rmElmtId);
        this.__avatarSize.purgeDependencyOnElmtId(rmElmtId);
        this.__avatarFontSize.purgeDependencyOnElmtId(rmElmtId);
        this.__avatarColor.purgeDependencyOnElmtId(rmElmtId);
        this.__textColor.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__displayName.aboutToBeDeleted();
        this.__avatarSize.aboutToBeDeleted();
        this.__avatarFontSize.aboutToBeDeleted();
        this.__avatarColor.aboutToBeDeleted();
        this.__textColor.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __displayName: SynchedPropertySimpleOneWayPU<string>;
    get displayName() {
        return this.__displayName.get();
    }
    set displayName(newValue: string) {
        this.__displayName.set(newValue);
    }
    private __avatarSize: SynchedPropertySimpleOneWayPU<number>;
    get avatarSize() {
        return this.__avatarSize.get();
    }
    set avatarSize(newValue: number) {
        this.__avatarSize.set(newValue);
    }
    private __avatarFontSize: SynchedPropertySimpleOneWayPU<number>;
    get avatarFontSize() {
        return this.__avatarFontSize.get();
    }
    set avatarFontSize(newValue: number) {
        this.__avatarFontSize.set(newValue);
    }
    private __avatarColor: SynchedPropertySimpleOneWayPU<string>;
    get avatarColor() {
        return this.__avatarColor.get();
    }
    set avatarColor(newValue: string) {
        this.__avatarColor.set(newValue);
    }
    private __textColor: SynchedPropertySimpleOneWayPU<string>;
    get textColor() {
        return this.__textColor.get();
    }
    set textColor(newValue: string) {
        this.__textColor.set(newValue);
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width(this.avatarSize);
            Row.height(this.avatarSize);
            Row.borderRadius(this.avatarSize / 2);
            Row.backgroundColor(this.avatarColor);
            Row.justifyContent(FlexAlign.Center);
            Row.shadow({
                radius: 12,
                color: KikariaColors.SHADOW_COLOR,
                offsetY: 6
            });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.displayName.charAt(0).toUpperCase());
            Text.fontSize(this.avatarFontSize);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(this.textColor);
        }, Text);
        Text.pop();
        Row.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class DashboardMetricColumn extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__title = new SynchedPropertySimpleOneWayPU(params.title, this, "title");
        this.__valueText = new SynchedPropertySimpleOneWayPU(params.valueText, this, "valueText");
        this.__tintColor = new SynchedPropertySimpleOneWayPU(params.tintColor, this, "tintColor");
        this.__isExpanded = new SynchedPropertySimpleOneWayPU(params.isExpanded, this, "isExpanded");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: DashboardMetricColumn_Params) {
        if (params.title === undefined) {
            this.__title.set('');
        }
        if (params.valueText === undefined) {
            this.__valueText.set('');
        }
        if (params.tintColor === undefined) {
            this.__tintColor.set('#63BAF5');
        }
        if (params.isExpanded === undefined) {
            this.__isExpanded.set(false);
        }
    }
    updateStateVars(params: DashboardMetricColumn_Params) {
        this.__title.reset(params.title);
        this.__valueText.reset(params.valueText);
        this.__tintColor.reset(params.tintColor);
        this.__isExpanded.reset(params.isExpanded);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__title.purgeDependencyOnElmtId(rmElmtId);
        this.__valueText.purgeDependencyOnElmtId(rmElmtId);
        this.__tintColor.purgeDependencyOnElmtId(rmElmtId);
        this.__isExpanded.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__title.aboutToBeDeleted();
        this.__valueText.aboutToBeDeleted();
        this.__tintColor.aboutToBeDeleted();
        this.__isExpanded.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __title: SynchedPropertySimpleOneWayPU<string>;
    get title() {
        return this.__title.get();
    }
    set title(newValue: string) {
        this.__title.set(newValue);
    }
    private __valueText: SynchedPropertySimpleOneWayPU<string>;
    get valueText() {
        return this.__valueText.get();
    }
    set valueText(newValue: string) {
        this.__valueText.set(newValue);
    }
    private __tintColor: SynchedPropertySimpleOneWayPU<string>;
    get tintColor() {
        return this.__tintColor.get();
    }
    set tintColor(newValue: string) {
        this.__tintColor.set(newValue);
    }
    private __isExpanded: SynchedPropertySimpleOneWayPU<boolean>;
    get isExpanded() {
        return this.__isExpanded.get();
    }
    set isExpanded(newValue: boolean) {
        this.__isExpanded.set(newValue);
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 6 });
            Column.layoutWeight(1);
            Column.padding(16);
            Column.alignItems(HorizontalAlign.Center);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.valueText);
            Text.fontSize(this.isExpanded ? 28 : 26);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(this.tintColor);
            Text.fontFamily('serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.title);
            Text.fontSize(13);
            Text.fontColor(KikariaColors.SOFT_TEXT);
        }, Text);
        Text.pop();
        Column.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
export class AdaptiveBackButton extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.__label = new SynchedPropertySimpleOneWayPU(params.label, this, "label");
        this.onTap = () => { };
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: AdaptiveBackButton_Params) {
        if (params.label === undefined) {
            this.__label.set('← 返回');
        }
        if (params.onTap !== undefined) {
            this.onTap = params.onTap;
        }
    }
    updateStateVars(params: AdaptiveBackButton_Params) {
        this.__label.reset(params.label);
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__label.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__label.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private __label: SynchedPropertySimpleOneWayPU<string>;
    get label() {
        return this.__label.get();
    }
    set label(newValue: string) {
        this.__label.set(newValue);
    }
    private onTap: () => void;
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.backgroundColor(Color.Transparent);
            Button.onClick(() => this.onTap());
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.label);
            Text.fontSize(17);
            Text.fontColor(KikariaColors.SOFT_TEXT);
        }, Text);
        Text.pop();
        Button.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
}
