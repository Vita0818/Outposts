/**
 * Chat models
 */
export enum ChatMessageRole {
    SYSTEM = "system",
    USER = "user",
    ASSISTANT = "assistant"
}
export class ChatMessage {
    id: string = '';
    role: ChatMessageRole = ChatMessageRole.USER;
    content: string = '';
    createdAt: Date = new Date();
    attachmentIDs: string[] = [];
    constructor(role: ChatMessageRole, content: string) {
        this.id = ChatMessage.generateId();
        this.role = role;
        this.content = content;
        this.createdAt = new Date();
        this.attachmentIDs = [];
    }
    static generateId(): string {
        const chars = 'abcdefghijklmnopqrstuvwxyz0123456789';
        let id = '';
        for (let i = 0; i < 36; i++) {
            if (i === 8 || i === 13 || i === 18 || i === 23) {
                id += '-';
            }
            else if (i === 14) {
                id += '4';
            }
            else {
                const idx: number = Math.floor(Math.random() * chars.length);
                id += chars.charAt(idx);
            }
        }
        return id;
    }
}
export interface ChatContextFields {
    title: string;
    itemCount: number;
    items: ChatContextItem[];
    maxContextCharacters: number;
    id?: string;
    browsePathComponents?: string[];
    sourceKind?: string | null;
    sourceItemID?: string | null;
    contextPathDisplay?: string | null;
    itemTitle?: string | null;
    totalCharacterCount?: number;
    isTruncated?: boolean;
    createdAt?: Date;
}
export class ChatContext {
    id: string = '';
    title: string = '';
    browsePathComponents: string[] = [];
    itemCount: number = 0;
    items: ChatContextItem[] = [];
    sourceKind: string | null = null;
    sourceItemID: string | null = null;
    contextPathDisplay: string | null = null;
    itemTitle: string | null = null;
    createdAt: Date = new Date();
    maxContextCharacters: number = 0;
    totalCharacterCount: number = 0;
    isTruncated: boolean = false;
    static create(fields: ChatContextFields): ChatContext {
        const ctx = new ChatContext();
        ctx.id = fields.id ?? ChatMessage.generateId();
        ctx.title = fields.title;
        ctx.browsePathComponents = fields.browsePathComponents ?? [];
        ctx.itemCount = fields.itemCount;
        ctx.items = fields.items;
        ctx.sourceKind = fields.sourceKind ?? null;
        ctx.sourceItemID = fields.sourceItemID ?? null;
        ctx.contextPathDisplay = fields.contextPathDisplay ?? null;
        ctx.itemTitle = fields.itemTitle ?? null;
        ctx.createdAt = fields.createdAt ?? new Date();
        ctx.maxContextCharacters = fields.maxContextCharacters;
        ctx.totalCharacterCount = fields.totalCharacterCount ?? fields.items.reduce((sum, i) => sum + i.content.length, 0);
        ctx.isTruncated = fields.isTruncated ?? false;
        return ctx;
    }
}
export class ChatContextItem {
    id: string = '';
    title: string = '';
    filingPath: string = '';
    content: string = '';
    sourcePath: string | null = null;
    contentCharacterCount: number = 0;
    isTruncated: boolean = false;
    constructor(id: string, title: string, filingPath: string, content: string) {
        this.id = id;
        this.title = title || '未命名知识';
        this.filingPath = filingPath;
        this.content = content.trim();
        this.contentCharacterCount = this.content.length;
        this.isTruncated = false;
    }
}
export class ChatConversation {
    id: string = '';
    title: string = '新对话';
    messages: ChatMessage[] = [];
    activeContextID: string | null = null;
    createdAt: Date = new Date();
    updatedAt: Date = new Date();
    constructor(title?: string) {
        this.id = ChatMessage.generateId();
        this.title = title ?? '新对话';
        this.messages = [];
        this.activeContextID = null;
        this.createdAt = new Date();
        this.updatedAt = new Date();
    }
}
export class ChatRequest {
    messages: ChatMessage[] = [];
    context: ChatContext | null = null;
    modelName: string | null = null;
    maxTokens: number = 2000;
    temperature: number = 0.3;
}
export class ChatResult {
    message: ChatMessage;
    providerID: string = '';
    providerName: string = '';
    modelName: string | null = null;
    finishReason: string | null = null;
    outputWasTruncated: boolean = false;
    constructor(message: ChatMessage, providerID: string, providerName: string) {
        this.message = message;
        this.providerID = providerID;
        this.providerName = providerName;
        this.modelName = null;
        this.finishReason = null;
        this.outputWasTruncated = false;
    }
}
