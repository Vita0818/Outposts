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
            Column.backgroundColor(RokuricsColors.pageBackground);
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
            Column.create({ space: 24 });
            Column.width('100%');
            Column.padding({ left: 16, right: 16, bottom: 40 });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Avatar
            Column.create({ space: 12 });
            // Avatar
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Circle.create();
            Circle.width(80);
            Circle.height(80);
            Circle.fill(colorAlpha(RokuricsColors.aqua, '20'));
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.profile.initial);
            Text.fontSize(32);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.aqua);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.profile.displayName);
            Text.fontSize(20);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.profile.displayHandle);
            Text.fontSize(14);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        // Avatar
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Profile form
            Column.create({ space: 16 });
            // Profile form
            Column.padding(20);
            // Profile form
            Column.borderRadius(20);
            // Profile form
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'));
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 6 });
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('显示名称');
            Text.fontSize(12);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            TextInput.create({ text: this.displayName, placeholder: '输入显示名称' });
            TextInput.fontSize(16);
            TextInput.fontColor(RokuricsColors.deepText);
            TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '80'));
            TextInput.borderRadius(10);
            TextInput.padding({ left: 14, right: 14, top: 10, bottom: 10 });
            TextInput.onChange((value: string) => { this.displayName = value; });
        }, TextInput);
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 6 });
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('账号 (@handle)');
            Text.fontSize(12);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            TextInput.create({ text: this.handle, placeholder: '输入账号' });
            TextInput.fontSize(16);
            TextInput.fontColor(RokuricsColors.deepText);
            TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '80'));
            TextInput.borderRadius(10);
            TextInput.padding({ left: 14, right: 14, top: 10, bottom: 10 });
            TextInput.onChange((value: string) => { this.handle = value; });
        }, TextInput);
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.width('100%');
            Button.height(48);
            Button.borderRadius(12);
            Button.backgroundColor(RokuricsColors.aqua);
            Button.enabled(!this.isSaving);
            Button.onClick(() => this.saveProfile());
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('保存');
            Text.fontSize(16);
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
                        Text.fontSize(13);
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
        // Profile form
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // AI Provider Configuration
            Column.create({ space: 16 });
            // AI Provider Configuration
            Column.padding(20);
            // AI Provider Configuration
            Column.borderRadius(20);
            // AI Provider Configuration
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'));
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('AI 提供商配置');
            Text.fontSize(16);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Provider kind toggle
            Row.create({ space: 12 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('提供商');
            Text.fontSize(14);
            Text.fontColor(RokuricsColors.softText);
            Text.width(80);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 0 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.padding({ left: 16, right: 16, top: 8, bottom: 8 });
            Button.borderRadius({ topLeft: 8, bottomLeft: 8 });
            Button.backgroundColor(this.aiProviderKind === 'mock' ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'));
            Button.onClick(() => { this.aiProviderKind = 'mock'; });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('Mock');
            Text.fontSize(13);
            Text.fontColor(this.aiProviderKind === 'mock' ? Color.White : RokuricsColors.softText);
        }, Text);
        Text.pop();
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.padding({ left: 16, right: 16, top: 8, bottom: 8 });
            Button.borderRadius({ topRight: 8, bottomRight: 8 });
            Button.backgroundColor(this.aiProviderKind === 'openaiCompatible' ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '50'));
            Button.onClick(() => { this.aiProviderKind = 'openaiCompatible'; });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('OpenAI');
            Text.fontSize(13);
            Text.fontColor(this.aiProviderKind === 'openaiCompatible' ? Color.White : RokuricsColors.softText);
        }, Text);
        Text.pop();
        Button.pop();
        Row.pop();
        // Provider kind toggle
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.aiProviderKind === 'openaiCompatible') {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 12 });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 4 });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('API 地址');
                        Text.fontSize(12);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: this.aiBaseURL, placeholder: 'https://api.openai.com/v1' });
                        TextInput.fontSize(14);
                        TextInput.fontColor(RokuricsColors.deepText);
                        TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'));
                        TextInput.borderRadius(8);
                        TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
                        TextInput.onChange((value: string) => { this.aiBaseURL = value; });
                    }, TextInput);
                    Column.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 4 });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('模型名称');
                        Text.fontSize(12);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: this.aiModelName, placeholder: 'gpt-4o-mini' });
                        TextInput.fontSize(14);
                        TextInput.fontColor(RokuricsColors.deepText);
                        TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'));
                        TextInput.borderRadius(8);
                        TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
                        TextInput.onChange((value: string) => { this.aiModelName = value; });
                    }, TextInput);
                    Column.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 4 });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('API Key');
                        Text.fontSize(12);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: this.aiAPIKey, placeholder: 'sk-...' });
                        TextInput.fontSize(14);
                        TextInput.fontColor(RokuricsColors.deepText);
                        TextInput.type(InputType.Password);
                        TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'));
                        TextInput.borderRadius(8);
                        TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
                        TextInput.onChange((value: string) => { this.aiAPIKey = value; });
                    }, TextInput);
                    Column.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create({ space: 12 });
                        Row.width('100%');
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 4 });
                        Column.layoutWeight(1);
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('温度');
                        Text.fontSize(12);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: String(this.aiTemperature), placeholder: '0.3' });
                        TextInput.fontSize(14);
                        TextInput.fontColor(RokuricsColors.deepText);
                        TextInput.type(InputType.Number);
                        TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'));
                        TextInput.borderRadius(8);
                        TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
                        TextInput.onChange((value: string) => {
                            const n = parseFloat(value);
                            if (!isNaN(n))
                                this.aiTemperature = n;
                        });
                    }, TextInput);
                    Column.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 4 });
                        Column.layoutWeight(1);
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('最大 Tokens');
                        Text.fontSize(12);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: String(this.aiMaxTokens), placeholder: '2000' });
                        TextInput.fontSize(14);
                        TextInput.fontColor(RokuricsColors.deepText);
                        TextInput.type(InputType.Number);
                        TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'));
                        TextInput.borderRadius(8);
                        TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
                        TextInput.onChange((value: string) => {
                            const n = parseInt(value);
                            if (!isNaN(n))
                                this.aiMaxTokens = n;
                        });
                    }, TextInput);
                    Column.pop();
                    Row.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.testConnectionResult.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.testConnectionResult);
                                    Text.fontSize(13);
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
                    Column.pop();
                });
            }
            // Provider health summary
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Provider health summary
            Column.create({ space: 6 });
            // Provider health summary
            Column.width('100%');
            // Provider health summary
            Column.padding(12);
            // Provider health summary
            Column.borderRadius(12);
            // Provider health summary
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '30'));
            // Provider health summary
            Column.margin({ top: 4 });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('提供商状态');
            Text.fontSize(14);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Circle.create();
            Circle.width(8);
            Circle.height(8);
            Circle.fill(this.providerHealthSummary.startsWith('已连接') ? RokuricsColors.mint : RokuricsColors.tertiaryText);
        }, Circle);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.aiProviderKind === 'openaiCompatible' ? 'OpenAI 兼容模式' : 'Mock 模式');
            Text.fontSize(13);
            Text.fontColor(RokuricsColors.softText);
            Text.margin({ left: 6 });
        }, Text);
        Text.pop();
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.aiProviderKind === 'openaiCompatible') {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create();
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('模型:');
                        Text.fontSize(11);
                        Text.fontColor(RokuricsColors.tertiaryText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.aiModelName);
                        Text.fontSize(11);
                        Text.fontColor(RokuricsColors.deepText);
                        Text.margin({ left: 4 });
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
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.providerHealthSummary.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.providerHealthSummary);
                        Text.fontSize(12);
                        Text.fontColor(this.providerHealthSummary.startsWith('已连接') ?
                            RokuricsColors.mint : RokuricsColors.coral);
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
            if (this.providerLatency.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(`延迟: ${this.providerLatency}`);
                        Text.fontSize(11);
                        Text.fontColor(RokuricsColors.tertiaryText);
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
            if (this.lastConnectionTest.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(`上次测试: ${this.lastConnectionTest}`);
                        Text.fontSize(11);
                        Text.fontColor(RokuricsColors.tertiaryText);
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
            if (this.providerModels.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(`可用模型 (${this.providerModels.length}):`);
                        Text.fontSize(11);
                        Text.fontColor(RokuricsColors.tertiaryText);
                        Text.margin({ top: 6 });
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.providerModels.join(', '));
                        Text.fontSize(10);
                        Text.fontColor(RokuricsColors.softText);
                        Text.maxLines(3);
                        Text.textOverflow({ overflow: TextOverflow.Ellipsis });
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
        // Provider health summary
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 12 });
            Row.width('100%');
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.aiProviderKind === 'openaiCompatible') {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 16, right: 16, top: 8, bottom: 8 });
                        Button.border({ width: 1, color: colorAlpha(RokuricsColors.aqua, '40'), radius: 8 });
                        Button.backgroundColor(Color.Transparent);
                        Button.enabled(!this.isTestingConnection);
                        Button.onClick(() => this.testConnection());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.isTestingConnection ? '测试中...' : '测试连接');
                        Text.fontSize(13);
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
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
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.padding({ left: 20, right: 20, top: 10, bottom: 10 });
            Button.borderRadius(10);
            Button.backgroundColor(RokuricsColors.aqua);
            Button.enabled(!this.isSavingAI);
            Button.onClick(() => this.saveAIConfig());
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('保存 AI 配置');
            Text.fontSize(14);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(Color.White);
        }, Text);
        Text.pop();
        Button.pop();
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.aiSavedMessage.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.aiSavedMessage);
                        Text.fontSize(13);
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
        // AI Provider Configuration
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Upload server configuration
            Column.create({ space: 12 });
            // Upload server configuration
            Column.padding(20);
            // Upload server configuration
            Column.borderRadius(20);
            // Upload server configuration
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'));
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('上传服务器配置');
            Text.fontSize(16);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 4 });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('服务器地址');
            Text.fontSize(12);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            TextInput.create({ text: this.uploadServerURL, placeholder: 'http://your-server.com' });
            TextInput.fontSize(14);
            TextInput.fontColor(RokuricsColors.deepText);
            TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'));
            TextInput.borderRadius(8);
            TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
            TextInput.onChange((v: string) => { this.uploadServerURL = v; });
        }, TextInput);
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 4 });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('API Key（可选）');
            Text.fontSize(12);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            TextInput.create({ text: this.uploadAPIKey, placeholder: 'Bearer token' });
            TextInput.fontSize(14);
            TextInput.fontColor(RokuricsColors.deepText);
            TextInput.type(InputType.Password);
            TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'));
            TextInput.borderRadius(8);
            TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
            TextInput.onChange((v: string) => { this.uploadAPIKey = v; });
        }, TextInput);
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.width('100%');
            Button.height(44);
            Button.borderRadius(10);
            Button.backgroundColor(RokuricsColors.aqua);
            Button.enabled(!this.isSavingUpload);
            Button.onClick(() => this.saveUploadConfig());
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('保存上传配置');
            Text.fontSize(14);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(Color.White);
        }, Text);
        Text.pop();
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.uploadSavedMessage.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.uploadSavedMessage);
                        Text.fontSize(13);
                        Text.fontColor(this.uploadSavedMessage.startsWith('上传') ? RokuricsColors.mint : RokuricsColors.coral);
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
        // Upload server configuration
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // About
            Column.create({ space: 8 });
            // About
            Column.width('100%');
            // About
            Column.padding(20);
            // About
            Column.borderRadius(20);
            // About
            Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'));
            // About
            Column.margin({ top: 12 });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('关于 Rokurics');
            Text.fontSize(15);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('版本 1.0.0');
            Text.fontSize(13);
            Text.fontColor(RokuricsColors.tertiaryText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('学习录音与 AI 笔记\nHarmonyOS 平台');
            Text.fontSize(13);
            Text.fontColor(RokuricsColors.softText);
            Text.textAlign(TextAlign.Center);
        }, Text);
        Text.pop();
        // About
        Column.pop();
        Column.pop();
        Scroll.pop();
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
