if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface SettingsPage_Params {
    settingsStore?: SettingsStore;
    profile?: UserProfile;
    displayName?: string;
    handle?: string;
    isSaving?: boolean;
    savedMessage?: string;
    aiProviderKind?: string;
    aiBaseURL?: string;
    aiModelName?: string;
    aiAPIKey?: string;
    aiTemperature?: number;
    aiMaxTokens?: number;
    isSavingAI?: boolean;
    aiSavedMessage?: string;
    isTestingConnection?: boolean;
    testConnectionResult?: string;
    lastConnectionTest?: string;
    providerHealthSummary?: string;
    providerModels?: string[];
    providerLatency?: string;
    uploadServerURL?: string;
    uploadAPIKey?: string;
    isSavingUpload?: boolean;
    uploadSavedMessage?: string;
    showProfileEditor?: boolean;
    showAIEditor?: boolean;
}
import { UserProfile } from "@bundle:com.vita0818.rokurics/entry/ets/models/UserProfile";
import { SettingsStore } from "@bundle:com.vita0818.rokurics/entry/ets/services/SettingsStore";
import { OpenAICompatibleClient } from "@bundle:com.vita0818.rokurics/entry/ets/services/OpenAICompatibleClient";
import type { AIConfiguration, ProviderHealthResult } from "@bundle:com.vita0818.rokurics/entry/ets/services/OpenAICompatibleClient";
import { colorAlpha, RokuricsColors, FontWeight } from "@bundle:com.vita0818.rokurics/entry/ets/utils/RokuricsTheme";
function formatTime(d: Date): string {
    const h = String(d.getHours()).padStart(2, '0');
    const m = String(d.getMinutes()).padStart(2, '0');
    return `${h}:${m}`;
}
function isEnglishLabel(text: string): boolean {
    return /^[A-Za-z]/.test(text);
}
function isTechValue(text: string): boolean {
    return /[.\\-_]/.test(text) || /^[a-z]/.test(text);
}
class SettingsPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.settingsStore = new SettingsStore(getContext(this));
        this.__profile = new ObservedPropertyObjectPU(new UserProfile(), this, "profile");
        this.__displayName = new ObservedPropertySimplePU('', this, "displayName");
        this.__handle = new ObservedPropertySimplePU('', this, "handle");
        this.__isSaving = new ObservedPropertySimplePU(false, this, "isSaving");
        this.__savedMessage = new ObservedPropertySimplePU('', this, "savedMessage");
        this.__aiProviderKind = new ObservedPropertySimplePU('mock', this, "aiProviderKind");
        this.__aiBaseURL = new ObservedPropertySimplePU('https://api.openai.com/v1', this, "aiBaseURL");
        this.__aiModelName = new ObservedPropertySimplePU('gpt-4o-mini', this, "aiModelName");
        this.__aiAPIKey = new ObservedPropertySimplePU('', this, "aiAPIKey");
        this.__aiTemperature = new ObservedPropertySimplePU(0.3, this, "aiTemperature");
        this.__aiMaxTokens = new ObservedPropertySimplePU(2000, this, "aiMaxTokens");
        this.__isSavingAI = new ObservedPropertySimplePU(false, this, "isSavingAI");
        this.__aiSavedMessage = new ObservedPropertySimplePU('', this, "aiSavedMessage");
        this.__isTestingConnection = new ObservedPropertySimplePU(false, this, "isTestingConnection");
        this.__testConnectionResult = new ObservedPropertySimplePU('', this, "testConnectionResult");
        this.__lastConnectionTest = new ObservedPropertySimplePU('', this, "lastConnectionTest");
        this.__providerHealthSummary = new ObservedPropertySimplePU('', this, "providerHealthSummary");
        this.__providerModels = new ObservedPropertyObjectPU([], this, "providerModels");
        this.__providerLatency = new ObservedPropertySimplePU('', this, "providerLatency");
        this.__uploadServerURL = new ObservedPropertySimplePU('', this, "uploadServerURL");
        this.__uploadAPIKey = new ObservedPropertySimplePU('', this, "uploadAPIKey");
        this.__isSavingUpload = new ObservedPropertySimplePU(false, this, "isSavingUpload");
        this.__uploadSavedMessage = new ObservedPropertySimplePU('', this, "uploadSavedMessage");
        this.__showProfileEditor = new ObservedPropertySimplePU(false, this, "showProfileEditor");
        this.__showAIEditor = new ObservedPropertySimplePU(false, this, "showAIEditor");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: SettingsPage_Params) {
        if (params.settingsStore !== undefined) {
            this.settingsStore = params.settingsStore;
        }
        if (params.profile !== undefined) {
            this.profile = params.profile;
        }
        if (params.displayName !== undefined) {
            this.displayName = params.displayName;
        }
        if (params.handle !== undefined) {
            this.handle = params.handle;
        }
        if (params.isSaving !== undefined) {
            this.isSaving = params.isSaving;
        }
        if (params.savedMessage !== undefined) {
            this.savedMessage = params.savedMessage;
        }
        if (params.aiProviderKind !== undefined) {
            this.aiProviderKind = params.aiProviderKind;
        }
        if (params.aiBaseURL !== undefined) {
            this.aiBaseURL = params.aiBaseURL;
        }
        if (params.aiModelName !== undefined) {
            this.aiModelName = params.aiModelName;
        }
        if (params.aiAPIKey !== undefined) {
            this.aiAPIKey = params.aiAPIKey;
        }
        if (params.aiTemperature !== undefined) {
            this.aiTemperature = params.aiTemperature;
        }
        if (params.aiMaxTokens !== undefined) {
            this.aiMaxTokens = params.aiMaxTokens;
        }
        if (params.isSavingAI !== undefined) {
            this.isSavingAI = params.isSavingAI;
        }
        if (params.aiSavedMessage !== undefined) {
            this.aiSavedMessage = params.aiSavedMessage;
        }
        if (params.isTestingConnection !== undefined) {
            this.isTestingConnection = params.isTestingConnection;
        }
        if (params.testConnectionResult !== undefined) {
            this.testConnectionResult = params.testConnectionResult;
        }
        if (params.lastConnectionTest !== undefined) {
            this.lastConnectionTest = params.lastConnectionTest;
        }
        if (params.providerHealthSummary !== undefined) {
            this.providerHealthSummary = params.providerHealthSummary;
        }
        if (params.providerModels !== undefined) {
            this.providerModels = params.providerModels;
        }
        if (params.providerLatency !== undefined) {
            this.providerLatency = params.providerLatency;
        }
        if (params.uploadServerURL !== undefined) {
            this.uploadServerURL = params.uploadServerURL;
        }
        if (params.uploadAPIKey !== undefined) {
            this.uploadAPIKey = params.uploadAPIKey;
        }
        if (params.isSavingUpload !== undefined) {
            this.isSavingUpload = params.isSavingUpload;
        }
        if (params.uploadSavedMessage !== undefined) {
            this.uploadSavedMessage = params.uploadSavedMessage;
        }
        if (params.showProfileEditor !== undefined) {
            this.showProfileEditor = params.showProfileEditor;
        }
        if (params.showAIEditor !== undefined) {
            this.showAIEditor = params.showAIEditor;
        }
    }
    updateStateVars(params: SettingsPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__profile.purgeDependencyOnElmtId(rmElmtId);
        this.__displayName.purgeDependencyOnElmtId(rmElmtId);
        this.__handle.purgeDependencyOnElmtId(rmElmtId);
        this.__isSaving.purgeDependencyOnElmtId(rmElmtId);
        this.__savedMessage.purgeDependencyOnElmtId(rmElmtId);
        this.__aiProviderKind.purgeDependencyOnElmtId(rmElmtId);
        this.__aiBaseURL.purgeDependencyOnElmtId(rmElmtId);
        this.__aiModelName.purgeDependencyOnElmtId(rmElmtId);
        this.__aiAPIKey.purgeDependencyOnElmtId(rmElmtId);
        this.__aiTemperature.purgeDependencyOnElmtId(rmElmtId);
        this.__aiMaxTokens.purgeDependencyOnElmtId(rmElmtId);
        this.__isSavingAI.purgeDependencyOnElmtId(rmElmtId);
        this.__aiSavedMessage.purgeDependencyOnElmtId(rmElmtId);
        this.__isTestingConnection.purgeDependencyOnElmtId(rmElmtId);
        this.__testConnectionResult.purgeDependencyOnElmtId(rmElmtId);
        this.__lastConnectionTest.purgeDependencyOnElmtId(rmElmtId);
        this.__providerHealthSummary.purgeDependencyOnElmtId(rmElmtId);
        this.__providerModels.purgeDependencyOnElmtId(rmElmtId);
        this.__providerLatency.purgeDependencyOnElmtId(rmElmtId);
        this.__uploadServerURL.purgeDependencyOnElmtId(rmElmtId);
        this.__uploadAPIKey.purgeDependencyOnElmtId(rmElmtId);
        this.__isSavingUpload.purgeDependencyOnElmtId(rmElmtId);
        this.__uploadSavedMessage.purgeDependencyOnElmtId(rmElmtId);
        this.__showProfileEditor.purgeDependencyOnElmtId(rmElmtId);
        this.__showAIEditor.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__profile.aboutToBeDeleted();
        this.__displayName.aboutToBeDeleted();
        this.__handle.aboutToBeDeleted();
        this.__isSaving.aboutToBeDeleted();
        this.__savedMessage.aboutToBeDeleted();
        this.__aiProviderKind.aboutToBeDeleted();
        this.__aiBaseURL.aboutToBeDeleted();
        this.__aiModelName.aboutToBeDeleted();
        this.__aiAPIKey.aboutToBeDeleted();
        this.__aiTemperature.aboutToBeDeleted();
        this.__aiMaxTokens.aboutToBeDeleted();
        this.__isSavingAI.aboutToBeDeleted();
        this.__aiSavedMessage.aboutToBeDeleted();
        this.__isTestingConnection.aboutToBeDeleted();
        this.__testConnectionResult.aboutToBeDeleted();
        this.__lastConnectionTest.aboutToBeDeleted();
        this.__providerHealthSummary.aboutToBeDeleted();
        this.__providerModels.aboutToBeDeleted();
        this.__providerLatency.aboutToBeDeleted();
        this.__uploadServerURL.aboutToBeDeleted();
        this.__uploadAPIKey.aboutToBeDeleted();
        this.__isSavingUpload.aboutToBeDeleted();
        this.__uploadSavedMessage.aboutToBeDeleted();
        this.__showProfileEditor.aboutToBeDeleted();
        this.__showAIEditor.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private settingsStore: SettingsStore;
    private __profile: ObservedPropertyObjectPU<UserProfile>;
    get profile() {
        return this.__profile.get();
    }
    set profile(newValue: UserProfile) {
        this.__profile.set(newValue);
    }
    private __displayName: ObservedPropertySimplePU<string>;
    get displayName() {
        return this.__displayName.get();
    }
    set displayName(newValue: string) {
        this.__displayName.set(newValue);
    }
    private __handle: ObservedPropertySimplePU<string>;
    get handle() {
        return this.__handle.get();
    }
    set handle(newValue: string) {
        this.__handle.set(newValue);
    }
    private __isSaving: ObservedPropertySimplePU<boolean>;
    get isSaving() {
        return this.__isSaving.get();
    }
    set isSaving(newValue: boolean) {
        this.__isSaving.set(newValue);
    }
    private __savedMessage: ObservedPropertySimplePU<string>;
    get savedMessage() {
        return this.__savedMessage.get();
    }
    set savedMessage(newValue: string) {
        this.__savedMessage.set(newValue);
    }
    private __aiProviderKind: ObservedPropertySimplePU<string>;
    get aiProviderKind() {
        return this.__aiProviderKind.get();
    }
    set aiProviderKind(newValue: string) {
        this.__aiProviderKind.set(newValue);
    }
    private __aiBaseURL: ObservedPropertySimplePU<string>;
    get aiBaseURL() {
        return this.__aiBaseURL.get();
    }
    set aiBaseURL(newValue: string) {
        this.__aiBaseURL.set(newValue);
    }
    private __aiModelName: ObservedPropertySimplePU<string>;
    get aiModelName() {
        return this.__aiModelName.get();
    }
    set aiModelName(newValue: string) {
        this.__aiModelName.set(newValue);
    }
    private __aiAPIKey: ObservedPropertySimplePU<string>;
    get aiAPIKey() {
        return this.__aiAPIKey.get();
    }
    set aiAPIKey(newValue: string) {
        this.__aiAPIKey.set(newValue);
    }
    private __aiTemperature: ObservedPropertySimplePU<number>;
    get aiTemperature() {
        return this.__aiTemperature.get();
    }
    set aiTemperature(newValue: number) {
        this.__aiTemperature.set(newValue);
    }
    private __aiMaxTokens: ObservedPropertySimplePU<number>;
    get aiMaxTokens() {
        return this.__aiMaxTokens.get();
    }
    set aiMaxTokens(newValue: number) {
        this.__aiMaxTokens.set(newValue);
    }
    private __isSavingAI: ObservedPropertySimplePU<boolean>;
    get isSavingAI() {
        return this.__isSavingAI.get();
    }
    set isSavingAI(newValue: boolean) {
        this.__isSavingAI.set(newValue);
    }
    private __aiSavedMessage: ObservedPropertySimplePU<string>;
    get aiSavedMessage() {
        return this.__aiSavedMessage.get();
    }
    set aiSavedMessage(newValue: string) {
        this.__aiSavedMessage.set(newValue);
    }
    private __isTestingConnection: ObservedPropertySimplePU<boolean>;
    get isTestingConnection() {
        return this.__isTestingConnection.get();
    }
    set isTestingConnection(newValue: boolean) {
        this.__isTestingConnection.set(newValue);
    }
    private __testConnectionResult: ObservedPropertySimplePU<string>;
    get testConnectionResult() {
        return this.__testConnectionResult.get();
    }
    set testConnectionResult(newValue: string) {
        this.__testConnectionResult.set(newValue);
    }
    private __lastConnectionTest: ObservedPropertySimplePU<string>;
    get lastConnectionTest() {
        return this.__lastConnectionTest.get();
    }
    set lastConnectionTest(newValue: string) {
        this.__lastConnectionTest.set(newValue);
    }
    private __providerHealthSummary: ObservedPropertySimplePU<string>;
    get providerHealthSummary() {
        return this.__providerHealthSummary.get();
    }
    set providerHealthSummary(newValue: string) {
        this.__providerHealthSummary.set(newValue);
    }
    private __providerModels: ObservedPropertyObjectPU<string[]>;
    get providerModels() {
        return this.__providerModels.get();
    }
    set providerModels(newValue: string[]) {
        this.__providerModels.set(newValue);
    }
    private __providerLatency: ObservedPropertySimplePU<string>;
    get providerLatency() {
        return this.__providerLatency.get();
    }
    set providerLatency(newValue: string) {
        this.__providerLatency.set(newValue);
    }
    private __uploadServerURL: ObservedPropertySimplePU<string>;
    get uploadServerURL() {
        return this.__uploadServerURL.get();
    }
    set uploadServerURL(newValue: string) {
        this.__uploadServerURL.set(newValue);
    }
    private __uploadAPIKey: ObservedPropertySimplePU<string>;
    get uploadAPIKey() {
        return this.__uploadAPIKey.get();
    }
    set uploadAPIKey(newValue: string) {
        this.__uploadAPIKey.set(newValue);
    }
    private __isSavingUpload: ObservedPropertySimplePU<boolean>;
    get isSavingUpload() {
        return this.__isSavingUpload.get();
    }
    set isSavingUpload(newValue: boolean) {
        this.__isSavingUpload.set(newValue);
    }
    private __uploadSavedMessage: ObservedPropertySimplePU<string>;
    get uploadSavedMessage() {
        return this.__uploadSavedMessage.get();
    }
    set uploadSavedMessage(newValue: string) {
        this.__uploadSavedMessage.set(newValue);
    }
    private __showProfileEditor: ObservedPropertySimplePU<boolean>;
    get showProfileEditor() {
        return this.__showProfileEditor.get();
    }
    set showProfileEditor(newValue: boolean) {
        this.__showProfileEditor.set(newValue);
    }
    private __showAIEditor: ObservedPropertySimplePU<boolean>;
    get showAIEditor() {
        return this.__showAIEditor.get();
    }
    set showAIEditor(newValue: boolean) {
        this.__showAIEditor.set(newValue);
    }
    aboutToAppear(): void {
        this.loadAllSettings();
    }
    async loadAllSettings(): Promise<void> {
        this.profile = await this.settingsStore.getUserProfile();
        this.displayName = this.profile.displayName;
        this.handle = this.profile.handle;
        this.aiProviderKind = await this.settingsStore.getAIProviderKind();
        this.aiBaseURL = await this.settingsStore.getAIBaseURL();
        this.aiModelName = await this.settingsStore.getAIModelName();
        this.aiAPIKey = await this.settingsStore.getAIAPIKey();
        this.aiTemperature = await this.settingsStore.getAITemperature();
        this.aiMaxTokens = await this.settingsStore.getAIMaxTokens();
        this.uploadServerURL = await this.settingsStore.getUploadServerURL();
        this.uploadAPIKey = await this.settingsStore.getUploadAPIKey();
    }
    async saveProfile(): Promise<void> {
        this.isSaving = true;
        this.savedMessage = '';
        try {
            const updated: UserProfile = new UserProfile(this.displayName, this.handle, this.profile.avatar);
            await this.settingsStore.saveUserProfile(updated);
            this.profile = updated;
            this.savedMessage = '已保存';
        }
        catch {
            this.savedMessage = '保存失败';
        }
        finally {
            this.isSaving = false;
        }
    }
    async saveAIConfig(): Promise<void> {
        this.isSavingAI = true;
        this.aiSavedMessage = '';
        try {
            await this.settingsStore.setAIProviderKind(this.aiProviderKind);
            await this.settingsStore.setAIBaseURL(this.aiBaseURL);
            await this.settingsStore.setAIModelName(this.aiModelName);
            await this.settingsStore.setAIAPIKey(this.aiAPIKey);
            await this.settingsStore.setAITemperature(this.aiTemperature);
            await this.settingsStore.setAIMaxTokens(this.aiMaxTokens);
            this.aiSavedMessage = 'AI 配置已保存';
        }
        catch {
            this.aiSavedMessage = '保存失败';
        }
        finally {
            this.isSavingAI = false;
        }
    }
    async saveUploadConfig(): Promise<void> {
        this.isSavingUpload = true;
        this.uploadSavedMessage = '';
        try {
            await this.settingsStore.setUploadServerURL(this.uploadServerURL);
            await this.settingsStore.setUploadAPIKey(this.uploadAPIKey);
            this.uploadSavedMessage = '上传配置已保存';
        }
        catch {
            this.uploadSavedMessage = '保存失败';
        }
        finally {
            this.isSavingUpload = false;
        }
    }
    async testConnection(): Promise<void> {
        this.isTestingConnection = true;
        this.testConnectionResult = '';
        this.lastConnectionTest = '';
        this.providerModels = [];
        this.providerLatency = '';
        try {
            const config: AIConfiguration = {
                baseURL: this.aiBaseURL,
                modelName: this.aiModelName,
                apiKey: this.aiAPIKey,
                temperature: this.aiTemperature,
                maxTokens: this.aiMaxTokens
            };
            const health: ProviderHealthResult = await OpenAICompatibleClient.validateConfiguration(config);
            this.testConnectionResult = health.reachable ? '连接成功' : '连接失败';
            if (health.reachable) {
                this.lastConnectionTest = formatTime(new Date());
                this.providerHealthSummary = `已连接 · ${this.aiModelName}`;
                this.providerModels = health.models.slice(0, 10);
                this.providerLatency = health.latencyMs !== null ? `${health.latencyMs}ms` : '';
            }
            else {
                this.providerHealthSummary = health.errorMessage ?? '连接失败';
            }
        }
        catch {
            this.testConnectionResult = '连接失败';
            this.providerHealthSummary = '连接失败';
        }
        finally {
            this.isTestingConnection = false;
        }
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.height('100%');
            Column.linearGradient({
                direction: GradientDirection.RightBottom,
                colors: [
                    [RokuricsColors.pageGradientStart, 1.0],
                    [RokuricsColors.pageGradientMid, 1.0],
                    [RokuricsColors.pageGradientEnd, 1.0]
                ]
            });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
            Row.padding({ left: 16, right: 16, top: 56, bottom: 24 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.width(44);
            Button.height(44);
            Button.backgroundColor(Color.Transparent);
            Button.onClick(() => this.getUIContext().getRouter().back());
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('←');
            Text.fontSize(20);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('设置');
            Text.fontSize(24);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Scroll.create();
            Scroll.width('100%');
            Scroll.layoutWeight(1);
        }, Scroll);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 28 });
            Column.width('100%');
            Column.padding({ left: 16, right: 16, bottom: 40 });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // ── Profile avatar area ──
            Column.create({ space: 10 });
            // ── Profile avatar area ──
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Stack.create();
        }, Stack);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Circle.create();
            Circle.width(72);
            Circle.height(72);
            Circle.fill(colorAlpha(RokuricsColors.aqua, '18'));
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.profile.initial);
            Text.fontSize(28);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.aqua);
        }, Text);
        Text.pop();
        Stack.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.profile.displayName);
            Text.fontSize(18);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.profile.displayHandle);
            Text.fontSize(12);
            Text.fontColor(RokuricsColors.tertiaryText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.padding({ left: 18, right: 18, top: 7, bottom: 7 });
            Button.borderRadius(16);
            Button.backgroundColor(Color.Transparent);
            Button.border({ width: 1, color: colorAlpha(RokuricsColors.softText, '30'), radius: 16 });
            Button.onClick(() => { this.showProfileEditor = !this.showProfileEditor; });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('编辑个人资料');
            Text.fontSize(13);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        Button.pop();
        // ── Profile avatar area ──
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Expandable profile editor
            if (this.showProfileEditor) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 12 });
                        Column.padding(16);
                        Column.borderRadius(16);
                        Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '50'));
                        Column.width('100%');
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 4 });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('显示名称');
                        Text.fontSize(11);
                        Text.fontColor(RokuricsColors.tertiaryText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: this.displayName, placeholder: '输入显示名称' });
                        TextInput.fontSize(15);
                        TextInput.fontColor(RokuricsColors.deepText);
                        TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '60'));
                        TextInput.borderRadius(8);
                        TextInput.padding({ left: 12, right: 12, top: 9, bottom: 9 });
                        TextInput.onChange((value: string) => { this.displayName = value; });
                    }, TextInput);
                    Column.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 4 });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('账号 (@handle)');
                        Text.fontSize(11);
                        Text.fontColor(RokuricsColors.tertiaryText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: this.handle, placeholder: '输入账号' });
                        TextInput.fontSize(15);
                        TextInput.fontColor(RokuricsColors.deepText);
                        TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '60'));
                        TextInput.borderRadius(8);
                        TextInput.padding({ left: 12, right: 12, top: 9, bottom: 9 });
                        TextInput.onChange((value: string) => { this.handle = value; });
                    }, TextInput);
                    Column.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.width('100%');
                        Button.height(42);
                        Button.borderRadius(10);
                        Button.backgroundColor(RokuricsColors.aqua);
                        Button.enabled(!this.isSaving);
                        Button.onClick(() => this.saveProfile());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.isSaving ? '保存中...' : '保存');
                        Text.fontSize(14);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(Color.White);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.savedMessage.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.savedMessage);
                                    Text.fontSize(12);
                                    Text.fontColor(this.savedMessage.startsWith('已保存') ? RokuricsColors.mint : RokuricsColors.coral);
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
                    Column.pop();
                });
            }
            // ── 转写 section ──
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // ── 转写 section ──
            Column.create({ space: 8 });
            // ── 转写 section ──
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('转写');
            Text.fontSize(13);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(RokuricsColors.tertiaryText);
            Text.padding({ left: 6 });
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.borderRadius(18);
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'));
            Column.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, 0.10],
                        [RokuricsColors.glassStroke, 0.08],
                        [RokuricsColors.glassStrokeAccent, 0.06]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 18
            } as BorderOptions);
        }, Column);
        this.SettingsRow.bind(this)('Provider', 'Mac 安全转写');
        this.SettingsDivider.bind(this)();
        this.SettingsRow.bind(this)('模型', 'whisper.cpp');
        this.SettingsDivider.bind(this)();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
            Row.padding({ left: 16, right: 16, top: 13, bottom: 13 });
            Row.onClick(() => {
                this.getUIContext().getRouter().pushUrl({ url: 'pages/AuthTestPage' });
            });
        }, Row);
        this.SettingsLinkRowContent.bind(this)('授权与测试');
        Row.pop();
        Column.pop();
        // ── 转写 section ──
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // ── AI section ──
            Column.create({ space: 8 });
            // ── AI section ──
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('AI');
            Text.fontSize(13);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(RokuricsColors.tertiaryText);
            Text.padding({ left: 6 });
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.borderRadius(18);
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'));
            Column.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, 0.10],
                        [RokuricsColors.glassStroke, 0.08],
                        [RokuricsColors.glassStrokeAccent, 0.06]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 18
            } as BorderOptions);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.onClick(() => { this.showAIEditor = !this.showAIEditor; });
        }, Row);
        this.SettingsRowContent.bind(this)('Provider', this.aiProviderKind === 'mock' ? 'Mock' : 'OpenAI');
        Row.pop();
        this.SettingsDivider.bind(this)();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.onClick(() => { this.showAIEditor = !this.showAIEditor; });
        }, Row);
        this.SettingsRowContent.bind(this)('模型', this.aiModelName);
        Row.pop();
        this.SettingsDivider.bind(this)();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.onClick(() => { this.showAIEditor = !this.showAIEditor; });
        }, Row);
        this.SettingsLinkRowContent.bind(this)('API 设置');
        Row.pop();
        this.SettingsDivider.bind(this)();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.onClick(() => this.testConnection());
        }, Row);
        this.SettingsLinkRowContent.bind(this)('测试');
        Row.pop();
        Column.pop();
        // ── AI section ──
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // ── AI detail editor (expandable) ──
            if (this.showAIEditor) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 12 });
                        Column.width('100%');
                        Column.padding(14);
                        Column.borderRadius(16);
                        Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '50'));
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Provider kind toggle
                        Row.create({ space: 0 });
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 14, right: 14, top: 6, bottom: 6 });
                        Button.borderRadius({ topLeft: 8, bottomLeft: 8 });
                        Button.backgroundColor(this.aiProviderKind === 'mock' ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '40'));
                        Button.onClick(() => { this.aiProviderKind = 'mock'; });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('Mock');
                        Text.fontSize(12);
                        Text.fontColor(this.aiProviderKind === 'mock' ? Color.White : RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 14, right: 14, top: 6, bottom: 6 });
                        Button.borderRadius({ topRight: 8, bottomRight: 8 });
                        Button.backgroundColor(this.aiProviderKind === 'openaiCompatible' ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '40'));
                        Button.onClick(() => { this.aiProviderKind = 'openaiCompatible'; });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('OpenAI');
                        Text.fontSize(12);
                        Text.fontColor(this.aiProviderKind === 'openaiCompatible' ? Color.White : RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    // Provider kind toggle
                    Row.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.aiProviderKind === 'openaiCompatible') {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.SettingsInput.bind(this)('API 地址', this.aiBaseURL, 'https://api.openai.com/v1', (v: string) => { this.aiBaseURL = v; });
                                this.SettingsInput.bind(this)('模型名称', this.aiModelName, 'gpt-4o-mini', (v: string) => { this.aiModelName = v; });
                                this.SettingsInput.bind(this)('API Key', this.aiAPIKey, 'sk-...', (v: string) => { this.aiAPIKey = v; }, true);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Row.create({ space: 8 });
                                    Row.width('100%');
                                }, Row);
                                this.SettingsInput.bind(this)('温度', String(this.aiTemperature), '0.3', (v: string) => { const n = parseFloat(v); if (!isNaN(n))
                                    this.aiTemperature = n; }, false, true);
                                this.SettingsInput.bind(this)('最大 Tokens', String(this.aiMaxTokens), '2000', (v: string) => { const n = parseInt(v); if (!isNaN(n))
                                    this.aiMaxTokens = n; }, false, true);
                                Row.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    If.create();
                                    if (this.aiProviderKind === 'openaiCompatible') {
                                        this.ifElseBranchUpdateFunction(0, () => {
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                Row.create({ space: 8 });
                                            }, Row);
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                Button.createWithChild();
                                                Button.padding({ left: 12, right: 12, top: 6, bottom: 6 });
                                                Button.border({ width: 1, color: colorAlpha(RokuricsColors.aqua, '40'), radius: 6 });
                                                Button.backgroundColor(Color.Transparent);
                                                Button.enabled(!this.isTestingConnection);
                                                Button.onClick(() => this.testConnection());
                                            }, Button);
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                Text.create(this.isTestingConnection ? '测试中...' : '测试连接');
                                                Text.fontSize(12);
                                                Text.fontColor(RokuricsColors.aqua);
                                            }, Text);
                                            Text.pop();
                                            Button.pop();
                                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                If.create();
                                                if (this.testConnectionResult.length > 0) {
                                                    this.ifElseBranchUpdateFunction(0, () => {
                                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                            Text.create(this.testConnectionResult);
                                                            Text.fontSize(11);
                                                            Text.fontColor(this.testConnectionResult === '连接成功' ? RokuricsColors.mint : RokuricsColors.coral);
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
                                        });
                                    }
                                    else {
                                        this.ifElseBranchUpdateFunction(1, () => {
                                        });
                                    }
                                }, If);
                                If.pop();
                            });
                        }
                        else {
                            this.ifElseBranchUpdateFunction(1, () => {
                            });
                        }
                    }, If);
                    If.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 16, right: 16, top: 8, bottom: 8 });
                        Button.borderRadius(8);
                        Button.backgroundColor(RokuricsColors.aqua);
                        Button.enabled(!this.isSavingAI);
                        Button.onClick(() => this.saveAIConfig());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('保存 AI 配置');
                        Text.fontSize(13);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(Color.White);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.aiSavedMessage.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.aiSavedMessage);
                                    Text.fontSize(11);
                                    Text.fontColor(this.aiSavedMessage.startsWith('AI') ? RokuricsColors.mint : RokuricsColors.coral);
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
                    Column.pop();
                });
            }
            // ── 关于 section ──
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // ── 关于 section ──
            Column.create({ space: 8 });
            // ── 关于 section ──
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('关于');
            Text.fontSize(13);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(RokuricsColors.tertiaryText);
            Text.padding({ left: 6 });
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.borderRadius(18);
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'));
            Column.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, 0.10],
                        [RokuricsColors.glassStroke, 0.08],
                        [RokuricsColors.glassStrokeAccent, 0.06]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 18
            } as BorderOptions);
        }, Column);
        this.SettingsRow.bind(this)('存储', '本机');
        this.SettingsDivider.bind(this)();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
            Row.padding({ left: 16, right: 16, top: 13, bottom: 13 });
            Row.onClick(() => {
                this.getUIContext().getRouter().pushUrl({ url: 'pages/PrivacyPolicyPage' });
            });
        }, Row);
        this.SettingsLinkRowContent.bind(this)('隐私政策');
        Row.pop();
        this.SettingsDivider.bind(this)();
        this.SettingsRow.bind(this)('版权', '1.0 (1)');
        Column.pop();
        // ── 关于 section ──
        Column.pop();
        Column.pop();
        Scroll.pop();
        Column.pop();
    }
    // ── Settings grouped card builders ──
    SettingsSection(title: string, child: WrappedBuilder<[
    ]>, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 8 });
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(title);
            Text.fontSize(13);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(RokuricsColors.tertiaryText);
            Text.padding({ left: 6 });
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.borderRadius(18);
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'));
            Column.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, 0.10],
                        [RokuricsColors.glassStroke, 0.08],
                        [RokuricsColors.glassStrokeAccent, 0.06]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 18
            } as BorderOptions);
        }, Column);
        child.builder.bind(this)();
        Column.pop();
        Column.pop();
    }
    SettingsRow(label: string, value: string, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
            Row.padding({ left: 16, right: 16, top: 13, bottom: 13 });
        }, Row);
        this.SettingsRowContent.bind(this)(label, value);
        Row.pop();
    }
    SettingsRowContent(label: string, value: string, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(label);
            Text.fontSize(15);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(RokuricsColors.deepText);
            Text.fontFamily(isEnglishLabel(label) ? 'serif' : 'sans-serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(value);
            Text.fontSize(14);
            Text.fontWeight(FontWeight.Regular);
            Text.fontColor(RokuricsColors.softText);
            Text.fontFamily(isTechValue(value) ? 'monospace' : (isEnglishLabel(value) ? 'serif' : 'sans-serif'));
        }, Text);
        Text.pop();
    }
    SettingsLinkRow(label: string, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
            Row.padding({ left: 16, right: 16, top: 13, bottom: 13 });
        }, Row);
        this.SettingsLinkRowContent.bind(this)(label);
        Row.pop();
    }
    SettingsLinkRowContent(label: string, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(label);
            Text.fontSize(15);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(RokuricsColors.deepText);
            Text.fontFamily(isEnglishLabel(label) ? 'serif' : 'sans-serif');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('查看');
            Text.fontSize(14);
            Text.fontWeight(FontWeight.Regular);
            Text.fontColor(RokuricsColors.aqua);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('›');
            Text.fontSize(16);
            Text.fontColor(RokuricsColors.tertiaryText);
            Text.margin({ left: 2 });
        }, Text);
        Text.pop();
    }
    SettingsDivider(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Divider.create();
            Divider.strokeWidth(0.5);
            Divider.color(colorAlpha(RokuricsColors.softText, '10'));
            Divider.margin({ left: 16, right: 16 });
        }, Divider);
    }
    SettingsInput(label: string, value: string, placeholder: string, onChange: (v: string) => void, isPassword?: boolean, isNumber?: boolean, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 3 });
            Column.layoutWeight(1);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(label);
            Text.fontSize(10);
            Text.fontColor(RokuricsColors.tertiaryText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            TextInput.create({ text: value, placeholder: placeholder });
            TextInput.fontSize(13);
            TextInput.fontColor(RokuricsColors.deepText);
            TextInput.type(isPassword ? InputType.Password : isNumber ? InputType.Number : InputType.Normal);
            TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'));
            TextInput.borderRadius(6);
            TextInput.padding({ left: 10, right: 10, top: 7, bottom: 7 });
            TextInput.onChange(onChange);
        }, TextInput);
        Column.pop();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "SettingsPage";
    }
}
registerNamedRoute(() => new SettingsPage(undefined, {}), "", { bundleName: "com.vita0818.rokurics", moduleName: "entry", pagePath: "pages/SettingsPage", pageFullPath: "entry/src/main/ets/pages/SettingsPage", integratedHsp: "false", moduleType: "followWithHap" });
