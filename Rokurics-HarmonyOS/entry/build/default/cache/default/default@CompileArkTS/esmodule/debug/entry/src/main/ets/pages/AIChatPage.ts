if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface AIChatPage_Params {
    chatProvider?: MockChatProvider | OpenAICompatibleChatProvider;
    chatStore?: ChatStore;
    settingsStore?: SettingsStore;
    messages?: ChatMessage[];
    inputText?: string;
    isGenerating?: boolean;
    errorText?: string;
    activeConversation?: ChatConversation | null;
    recentConversations?: ChatConversation[];
    showConversationList?: boolean;
    conversationTitle?: string;
    providerDisplayName?: string;
    displayName?: string;
}
import { ChatMessage, ChatMessageRole, ChatConversation, ChatRequest } from "@bundle:com.vita0818.rokurics/entry/ets/models/ChatModels";
import type { ChatResult } from "@bundle:com.vita0818.rokurics/entry/ets/models/ChatModels";
import { MockChatProvider, OpenAICompatibleChatProvider } from "@bundle:com.vita0818.rokurics/entry/ets/providers/ProviderInterfaces";
import { ChatStore } from "@bundle:com.vita0818.rokurics/entry/ets/services/ChatStore";
import { SettingsStore } from "@bundle:com.vita0818.rokurics/entry/ets/services/SettingsStore";
import type { AIConfiguration } from '../services/OpenAICompatibleClient';
import { RokuricsColors, FontWeight } from "@bundle:com.vita0818.rokurics/entry/ets/utils/RokuricsTheme";
import { SendIcon } from "@bundle:com.vita0818.rokurics/entry/ets/utils/CustomIcons";
class AIChatPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.chatProvider = new MockChatProvider();
        this.chatStore = new ChatStore(getContext(this));
        this.settingsStore = new SettingsStore(getContext(this));
        this.__messages = new ObservedPropertyObjectPU([], this, "messages");
        this.__inputText = new ObservedPropertySimplePU('', this, "inputText");
        this.__isGenerating = new ObservedPropertySimplePU(false, this, "isGenerating");
        this.__errorText = new ObservedPropertySimplePU('', this, "errorText");
        this.__activeConversation = new ObservedPropertyObjectPU(null, this, "activeConversation");
        this.__recentConversations = new ObservedPropertyObjectPU([], this, "recentConversations");
        this.__showConversationList = new ObservedPropertySimplePU(false, this, "showConversationList");
        this.__conversationTitle = new ObservedPropertySimplePU('新对话', this, "conversationTitle");
        this.__providerDisplayName = new ObservedPropertySimplePU('Mock', this, "providerDisplayName");
        this.__displayName = new ObservedPropertySimplePU('', this, "displayName");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: AIChatPage_Params) {
        if (params.chatProvider !== undefined) {
            this.chatProvider = params.chatProvider;
        }
        if (params.chatStore !== undefined) {
            this.chatStore = params.chatStore;
        }
        if (params.settingsStore !== undefined) {
            this.settingsStore = params.settingsStore;
        }
        if (params.messages !== undefined) {
            this.messages = params.messages;
        }
        if (params.inputText !== undefined) {
            this.inputText = params.inputText;
        }
        if (params.isGenerating !== undefined) {
            this.isGenerating = params.isGenerating;
        }
        if (params.errorText !== undefined) {
            this.errorText = params.errorText;
        }
        if (params.activeConversation !== undefined) {
            this.activeConversation = params.activeConversation;
        }
        if (params.recentConversations !== undefined) {
            this.recentConversations = params.recentConversations;
        }
        if (params.showConversationList !== undefined) {
            this.showConversationList = params.showConversationList;
        }
        if (params.conversationTitle !== undefined) {
            this.conversationTitle = params.conversationTitle;
        }
        if (params.providerDisplayName !== undefined) {
            this.providerDisplayName = params.providerDisplayName;
        }
        if (params.displayName !== undefined) {
            this.displayName = params.displayName;
        }
    }
    updateStateVars(params: AIChatPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__messages.purgeDependencyOnElmtId(rmElmtId);
        this.__inputText.purgeDependencyOnElmtId(rmElmtId);
        this.__isGenerating.purgeDependencyOnElmtId(rmElmtId);
        this.__errorText.purgeDependencyOnElmtId(rmElmtId);
        this.__activeConversation.purgeDependencyOnElmtId(rmElmtId);
        this.__recentConversations.purgeDependencyOnElmtId(rmElmtId);
        this.__showConversationList.purgeDependencyOnElmtId(rmElmtId);
        this.__conversationTitle.purgeDependencyOnElmtId(rmElmtId);
        this.__providerDisplayName.purgeDependencyOnElmtId(rmElmtId);
        this.__displayName.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__messages.aboutToBeDeleted();
        this.__inputText.aboutToBeDeleted();
        this.__isGenerating.aboutToBeDeleted();
        this.__errorText.aboutToBeDeleted();
        this.__activeConversation.aboutToBeDeleted();
        this.__recentConversations.aboutToBeDeleted();
        this.__showConversationList.aboutToBeDeleted();
        this.__conversationTitle.aboutToBeDeleted();
        this.__providerDisplayName.aboutToBeDeleted();
        this.__displayName.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private chatProvider: MockChatProvider | OpenAICompatibleChatProvider;
    private chatStore: ChatStore;
    private settingsStore: SettingsStore;
    private __messages: ObservedPropertyObjectPU<ChatMessage[]>;
    get messages() {
        return this.__messages.get();
    }
    set messages(newValue: ChatMessage[]) {
        this.__messages.set(newValue);
    }
    private __inputText: ObservedPropertySimplePU<string>;
    get inputText() {
        return this.__inputText.get();
    }
    set inputText(newValue: string) {
        this.__inputText.set(newValue);
    }
    private __isGenerating: ObservedPropertySimplePU<boolean>;
    get isGenerating() {
        return this.__isGenerating.get();
    }
    set isGenerating(newValue: boolean) {
        this.__isGenerating.set(newValue);
    }
    private __errorText: ObservedPropertySimplePU<string>;
    get errorText() {
        return this.__errorText.get();
    }
    set errorText(newValue: string) {
        this.__errorText.set(newValue);
    }
    private __activeConversation: ObservedPropertyObjectPU<ChatConversation | null>;
    get activeConversation() {
        return this.__activeConversation.get();
    }
    set activeConversation(newValue: ChatConversation | null) {
        this.__activeConversation.set(newValue);
    }
    private __recentConversations: ObservedPropertyObjectPU<ChatConversation[]>;
    get recentConversations() {
        return this.__recentConversations.get();
    }
    set recentConversations(newValue: ChatConversation[]) {
        this.__recentConversations.set(newValue);
    }
    private __showConversationList: ObservedPropertySimplePU<boolean>;
    get showConversationList() {
        return this.__showConversationList.get();
    }
    set showConversationList(newValue: boolean) {
        this.__showConversationList.set(newValue);
    }
    private __conversationTitle: ObservedPropertySimplePU<string>;
    get conversationTitle() {
        return this.__conversationTitle.get();
    }
    set conversationTitle(newValue: string) {
        this.__conversationTitle.set(newValue);
    }
    private __providerDisplayName: ObservedPropertySimplePU<string>;
    get providerDisplayName() {
        return this.__providerDisplayName.get();
    }
    set providerDisplayName(newValue: string) {
        this.__providerDisplayName.set(newValue);
    }
    private __displayName: ObservedPropertySimplePU<string>;
    get displayName() {
        return this.__displayName.get();
    }
    set displayName(newValue: string) {
        this.__displayName.set(newValue);
    }
    async aboutToAppear(): Promise<void> {
        await this.initProvider();
        await this.loadDisplayName();
        this.loadRecentConversations();
    }
    async initProvider(): Promise<void> {
        const kind = await this.settingsStore.getAIProviderKind();
        if (kind === 'openaiCompatible') {
            const config: AIConfiguration = {
                baseURL: await this.settingsStore.getAIBaseURL(),
                modelName: await this.settingsStore.getAIModelName(),
                apiKey: await this.settingsStore.getAIAPIKey(),
                temperature: await this.settingsStore.getAITemperature(),
                maxTokens: await this.settingsStore.getAIMaxTokens()
            };
            const provider = new OpenAICompatibleChatProvider(config);
            this.chatProvider = provider;
            this.providerDisplayName = `OpenAI (${config.modelName})`;
        }
    }
    async loadDisplayName(): Promise<void> {
        const settings = this.settingsStore;
        const profile = await settings.getUserProfile();
        this.displayName = profile.displayName;
    }
    async loadRecentConversations(): Promise<void> {
        this.recentConversations = await this.chatStore.loadAllConversations();
    }
    async startNewConversation(): Promise<void> {
        await this.saveCurrentConversation();
        const conv = new ChatConversation();
        conv.title = '新对话';
        this.activeConversation = conv;
        this.messages = [];
        this.conversationTitle = conv.title;
        this.showConversationList = false;
        this.errorText = '';
    }
    async selectConversation(id: string): Promise<void> {
        await this.saveCurrentConversation();
        const conv = await this.chatStore.loadConversation(id);
        if (conv) {
            this.activeConversation = conv;
            this.messages = conv.messages;
            this.conversationTitle = conv.title;
            this.showConversationList = false;
            this.errorText = '';
        }
    }
    async deleteConversation(id: string): Promise<void> {
        await this.chatStore.deleteConversation(id);
        if (this.activeConversation?.id === id) {
            this.activeConversation = null;
            this.messages = [];
            this.conversationTitle = '新对话';
        }
        await this.loadRecentConversations();
    }
    async saveCurrentConversation(): Promise<void> {
        if (this.messages.length === 0)
            return;
        let conv: ChatConversation;
        if (this.activeConversation) {
            conv = this.activeConversation;
        }
        else {
            conv = new ChatConversation();
        }
        conv.messages = this.messages;
        conv.updatedAt = new Date();
        const firstUserMsg = this.messages.find(m => m.role === ChatMessageRole.USER);
        if (firstUserMsg && conv.title === '新对话') {
            conv.title = firstUserMsg.content.substring(0, 30) + (firstUserMsg.content.length > 30 ? '...' : '');
        }
        await this.chatStore.saveConversation(conv);
        this.activeConversation = conv;
        this.conversationTitle = conv.title;
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.height('100%');
            Column.backgroundColor(RokuricsColors.pageBackground);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Header
            Row.create();
            // Header
            Row.width('100%');
            // Header
            Row.padding({ left: 16, right: 16, top: 56, bottom: 12 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Glass circle back button
            Button.createWithChild();
            // Glass circle back button
            Button.width(44);
            // Glass circle back button
            Button.height(44);
            // Glass circle back button
            Button.borderRadius(22);
            // Glass circle back button
            Button.backgroundColor(RokuricsColors.glassSurface + '66');
            // Glass circle back button
            Button.shadow({
                color: RokuricsColors.shadowColor + '10',
                radius: 12,
                offsetY: 6
            });
            // Glass circle back button
            Button.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, 0.44],
                        [0xEFFAF8, 0.14],
                        [0x59C7C2, 0.12]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 22
            } as BorderOptions);
            // Glass circle back button
            Button.onClick(() => {
                this.saveCurrentConversation();
                this.getUIContext().getRouter().back();
            });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('←');
            Text.fontSize(18);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        // Glass circle back button
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.conversationTitle);
            Text.fontSize(20);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
            Text.maxLines(1);
            Text.textOverflow({ overflow: TextOverflow.Ellipsis });
            Text.layoutWeight(1);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.backgroundColor(Color.Transparent);
            Button.margin({ right: 8 });
            Button.onClick(() => this.startNewConversation());
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('+新');
            Text.fontSize(14);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(RokuricsColors.aqua);
        }, Text);
        Text.pop();
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.width(44);
            Button.height(44);
            Button.backgroundColor(Color.Transparent);
            Button.onClick(() => {
                this.loadRecentConversations();
                this.showConversationList = !this.showConversationList;
            });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('☰');
            Text.fontSize(18);
            Text.fontColor(RokuricsColors.aqua);
        }, Text);
        Text.pop();
        Button.pop();
        // Header
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Conversation list overlay with glass card
            if (this.showConversationList) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create();
                        Column.width('100%');
                        Column.backgroundColor(RokuricsColors.glassSurface + 'F0');
                        Column.borderRadius({ bottomLeft: 16, bottomRight: 16 });
                        Column.shadow({
                            color: RokuricsColors.shadowColor + '20',
                            radius: 16,
                            offsetY: 8
                        });
                        Column.zIndex(10);
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create();
                        Row.width('100%');
                        Row.padding(16);
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('对话历史');
                        Text.fontSize(16);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(RokuricsColors.deepText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Blank.create();
                    }, Blank);
                    Blank.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.backgroundColor(Color.Transparent);
                        Button.onClick(() => this.startNewConversation());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('+ 新建');
                        Text.fontSize(14);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    Row.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.recentConversations.length === 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create('暂无历史对话');
                                    Text.fontSize(13);
                                    Text.fontColor(RokuricsColors.tertiaryText);
                                    Text.margin({ top: 16, bottom: 24 });
                                }, Text);
                                Text.pop();
                            });
                        }
                        else {
                            this.ifElseBranchUpdateFunction(1, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    List.create({ space: 6 });
                                    List.width('100%');
                                    List.constraintSize({ maxHeight: 280 });
                                    List.scrollBar(BarState.Off);
                                }, List);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    ForEach.create();
                                    const forEachItemGenFunction = _item => {
                                        const conv = _item;
                                        {
                                            const itemCreation = (elmtId, isInitialRender) => {
                                                ViewStackProcessor.StartGetAccessRecordingFor(elmtId);
                                                ListItem.create(deepRenderFunction, true);
                                                if (!isInitialRender) {
                                                    ListItem.pop();
                                                }
                                                ViewStackProcessor.StopGetAccessRecording();
                                            };
                                            const itemCreation2 = (elmtId, isInitialRender) => {
                                                ListItem.create(deepRenderFunction, true);
                                            };
                                            const deepRenderFunction = (elmtId, isInitialRender) => {
                                                itemCreation(elmtId, isInitialRender);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Row.create();
                                                    Row.width('100%');
                                                    Row.padding({ left: 14, right: 14, top: 10, bottom: 10 });
                                                    Row.borderRadius(12);
                                                    Row.backgroundColor(conv.id === this.activeConversation?.id ?
                                                        RokuricsColors.aqua + '14' : RokuricsColors.glassSurface + '50');
                                                    Row.onClick(() => this.selectConversation(conv.id));
                                                }, Row);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Column.create({ space: 2 });
                                                    Column.layoutWeight(1);
                                                    Column.alignItems(HorizontalAlign.Start);
                                                }, Column);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Text.create(conv.title);
                                                    Text.fontSize(14);
                                                    Text.fontWeight(FontWeight.Medium);
                                                    Text.fontColor(conv.id === this.activeConversation?.id ?
                                                        RokuricsColors.aqua : RokuricsColors.deepText);
                                                    Text.maxLines(1);
                                                    Text.textOverflow({ overflow: TextOverflow.Ellipsis });
                                                }, Text);
                                                Text.pop();
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Text.create(`${conv.messages.length} 条消息`);
                                                    Text.fontSize(11);
                                                    Text.fontColor(RokuricsColors.tertiaryText);
                                                }, Text);
                                                Text.pop();
                                                Column.pop();
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Button.createWithChild();
                                                    Button.width(32);
                                                    Button.height(32);
                                                    Button.backgroundColor(Color.Transparent);
                                                    Button.onClick(() => this.deleteConversation(conv.id));
                                                }, Button);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Text.create('🗑');
                                                    Text.fontSize(14);
                                                    Text.fontColor(RokuricsColors.tertiaryText);
                                                }, Text);
                                                Text.pop();
                                                Button.pop();
                                                Row.pop();
                                                ListItem.pop();
                                            };
                                            this.observeComponentCreation2(itemCreation2, ListItem);
                                            ListItem.pop();
                                        }
                                    };
                                    this.forEachUpdateFunction(elmtId, this.recentConversations, forEachItemGenFunction);
                                }, ForEach);
                                ForEach.pop();
                                List.pop();
                            });
                        }
                    }, If);
                    If.pop();
                    Column.pop();
                });
            }
            // Messages area
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Messages area
            if (this.messages.length === 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Greeting (mirrors Apple ChatGreeting)
                        Column.create({ space: 16 });
                        // Greeting (mirrors Apple ChatGreeting)
                        Column.width('100%');
                        // Greeting (mirrors Apple ChatGreeting)
                        Column.layoutWeight(1);
                        // Greeting (mirrors Apple ChatGreeting)
                        Column.justifyContent(FlexAlign.Center);
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('Rokurics AI');
                        Text.fontSize(28);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(`你好${this.displayName ? '，' + this.displayName : ''}`);
                        Text.fontSize(24);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(RokuricsColors.deepText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('基于你的学习资料进行智能问答');
                        Text.fontSize(14);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Quick prompt buttons
                        Row.create({ space: 8 });
                        // Quick prompt buttons
                        Row.margin({ top: 12 });
                    }, Row);
                    this.QuickPrompt.bind(this)('总结要点');
                    this.QuickPrompt.bind(this)('生成大纲');
                    this.QuickPrompt.bind(this)('解答疑问');
                    // Quick prompt buttons
                    Row.pop();
                    // Greeting (mirrors Apple ChatGreeting)
                    Column.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        List.create({ space: 12 });
                        List.width('100%');
                        List.layoutWeight(1);
                        List.padding({ left: 16, right: 16 });
                        List.scrollBar(BarState.Off);
                    }, List);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        ForEach.create();
                        const forEachItemGenFunction = _item => {
                            const msg = _item;
                            {
                                const itemCreation = (elmtId, isInitialRender) => {
                                    ViewStackProcessor.StartGetAccessRecordingFor(elmtId);
                                    ListItem.create(deepRenderFunction, true);
                                    if (!isInitialRender) {
                                        ListItem.pop();
                                    }
                                    ViewStackProcessor.StopGetAccessRecording();
                                };
                                const itemCreation2 = (elmtId, isInitialRender) => {
                                    ListItem.create(deepRenderFunction, true);
                                };
                                const deepRenderFunction = (elmtId, isInitialRender) => {
                                    itemCreation(elmtId, isInitialRender);
                                    this.ChatBubble.bind(this)(msg);
                                    ListItem.pop();
                                };
                                this.observeComponentCreation2(itemCreation2, ListItem);
                                ListItem.pop();
                            }
                        };
                        this.forEachUpdateFunction(elmtId, this.messages, forEachItemGenFunction);
                    }, ForEach);
                    ForEach.pop();
                    List.pop();
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Error
            if (this.errorText.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.errorText);
                        Text.fontSize(12);
                        Text.fontColor(RokuricsColors.coral);
                        Text.padding({ left: 16, right: 16, top: 4, bottom: 4 });
                    }, Text);
                    Text.pop();
                });
            }
            // Input bar
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Input bar
            Row.create();
            // Input bar
            Row.width('100%');
            // Input bar
            Row.padding({ left: 16, right: 16, top: 8, bottom: 32 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            TextInput.create({ text: this.inputText, placeholder: '输入消息...' });
            TextInput.fontSize(15);
            TextInput.layoutWeight(1);
            TextInput.height(44);
            TextInput.borderRadius(22);
            TextInput.padding({ left: 16, right: 16 });
            TextInput.backgroundColor(RokuricsColors.glassSurface + '80');
            TextInput.onChange((value: string) => { this.inputText = value; });
        }, TextInput);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.width(44);
            Button.height(44);
            Button.borderRadius(22);
            Button.backgroundColor(this.inputText.trim().length > 0 && !this.isGenerating ?
                RokuricsColors.aqua : RokuricsColors.tertiaryText);
            Button.shadow({
                color: (this.inputText.trim().length > 0 && !this.isGenerating ?
                    RokuricsColors.aqua : RokuricsColors.tertiaryText) + '30',
                radius: 10,
                offsetY: 4
            });
            Button.enabled(this.inputText.trim().length > 0 && !this.isGenerating);
            Button.onClick(() => this.sendMessage());
        }, Button);
        SendIcon.bind(this)(16, '#FFFFFF');
        Button.pop();
        // Input bar
        Row.pop();
        Column.pop();
    }
    ChatBubble(msg: ChatMessage, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (msg.role === ChatMessageRole.ASSISTANT) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create();
                        Column.alignItems(HorizontalAlign.Start);
                        Column.constraintSize({ maxWidth: '80%' });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(msg.content);
                        Text.fontSize(15);
                        Text.fontColor(RokuricsColors.deepText);
                        Text.padding(14);
                        Text.borderRadius({ topLeft: 4, topRight: 16, bottomLeft: 16, bottomRight: 16 });
                        Text.backgroundColor(RokuricsColors.glassSurface + 'B8');
                        Text.border({
                            width: 1,
                            color: {
                                colors: [
                                    [0xFFFFFF, 0.30],
                                    [0xEFFAF8, 0.10]
                                ],
                                direction: GradientDirection.RightBottom
                            },
                            radius: { topLeft: 4, topRight: 16, bottomLeft: 16, bottomRight: 16 }
                        } as BorderOptions);
                    }, Text);
                    Text.pop();
                    Column.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Blank.create();
                    }, Blank);
                    Blank.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Blank.create();
                    }, Blank);
                    Blank.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create();
                        Column.alignItems(HorizontalAlign.End);
                        Column.constraintSize({ maxWidth: '80%' });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(msg.content);
                        Text.fontSize(15);
                        Text.fontColor(Color.White);
                        Text.padding(14);
                        Text.borderRadius({ topLeft: 16, topRight: 4, bottomLeft: 16, bottomRight: 16 });
                        Text.backgroundColor(RokuricsColors.aqua);
                        Text.shadow({
                            color: RokuricsColors.aqua + '20',
                            radius: 8,
                            offsetY: 4
                        });
                    }, Text);
                    Text.pop();
                    Column.pop();
                });
            }
        }, If);
        If.pop();
        Row.pop();
    }
    QuickPrompt(text: string, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(text);
            Text.fontSize(13);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(RokuricsColors.aqua);
            Text.padding({ left: 14, right: 14, top: 8, bottom: 8 });
            Text.borderRadius(16);
            Text.backgroundColor(RokuricsColors.aqua + '10');
            Text.border({ width: 1, color: RokuricsColors.aqua + '40', radius: 16 });
            Text.onClick(() => {
                this.inputText = text;
                this.sendMessage();
            });
        }, Text);
        Text.pop();
    }
    async sendMessage(): Promise<void> {
        const text: string = this.inputText.trim();
        if (text.length === 0 || this.isGenerating)
            return;
        this.inputText = '';
        this.isGenerating = true;
        this.errorText = '';
        const userMsg: ChatMessage = new ChatMessage(ChatMessageRole.USER, text);
        const newMessages: ChatMessage[] = [];
        for (const m of this.messages) {
            newMessages.push(m);
        }
        newMessages.push(userMsg);
        this.messages = newMessages;
        try {
            const request: ChatRequest = new ChatRequest();
            request.messages = this.messages;
            request.context = null;
            request.maxTokens = 2000;
            request.temperature = 0.3;
            const result: ChatResult = await this.chatProvider.send(request);
            const updated: ChatMessage[] = [];
            for (const m of this.messages) {
                updated.push(m);
            }
            updated.push(result.message);
            this.messages = updated;
            await this.saveCurrentConversation();
        }
        catch {
            this.errorText = '发送失败';
        }
        finally {
            this.isGenerating = false;
        }
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "AIChatPage";
    }
}
registerNamedRoute(() => new AIChatPage(undefined, {}), "", { bundleName: "com.vita0818.rokurics", moduleName: "entry", pagePath: "pages/AIChatPage", pageFullPath: "entry/src/main/ets/pages/AIChatPage", integratedHsp: "false", moduleType: "followWithHap" });
