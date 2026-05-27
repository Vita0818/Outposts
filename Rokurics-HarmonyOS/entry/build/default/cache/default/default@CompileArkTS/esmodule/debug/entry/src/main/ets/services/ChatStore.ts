import fileIo from "@ohos:file.fs";
import { ChatConversation, ChatMessage, ChatMessageRole } from "@bundle:com.vita0818.rokurics/entry/ets/models/ChatModels";
const TAG = 'RokuricsChatStore';
function writeTextFile(path: string, content: string): void {
    const file: fileIo.File = fileIo.openSync(path, fileIo.OpenMode.CREATE | fileIo.OpenMode.WRITE_ONLY | fileIo.OpenMode.TRUNC);
    try {
        fileIo.writeSync(file.fd, content);
    }
    finally {
        fileIo.closeSync(file);
    }
}
function readTextFile(path: string): string {
    const file: fileIo.File = fileIo.openSync(path, fileIo.OpenMode.READ_ONLY);
    try {
        const stat: fileIo.Stat = fileIo.statSync(path);
        const buf: ArrayBuffer = new ArrayBuffer(stat.size);
        fileIo.readSync(file.fd, buf);
        const bytes: Uint8Array = new Uint8Array(buf);
        let result: string = '';
        for (let i: number = 0; i < bytes.length; i++) {
            result += String.fromCharCode(bytes[i]);
        }
        return result;
    }
    finally {
        fileIo.closeSync(file);
    }
}
export class ChatStore {
    private baseDir: string;
    constructor(context: Context) {
        this.baseDir = context.filesDir + '/Rokurics/Chats';
        this.ensureDirectories();
    }
    private ensureDirectories(): void {
        try {
            if (!fileIo.accessSync(this.baseDir)) {
                fileIo.mkdirSync(this.baseDir, true);
            }
        }
        catch (_e) { /* ignore */ }
    }
    private conversationPath(id: string): string {
        if (id.includes('..') || id.includes('/')) {
            throw new Error(`Invalid conversation ID: ${id}`);
        }
        return `${this.baseDir}/${id}.json`;
    }
    async saveConversation(conversation: ChatConversation): Promise<void> {
        this.ensureDirectories();
        const path = this.conversationPath(conversation.id);
        const payload: Record<string, Object> = {
            'id': conversation.id,
            'title': conversation.title,
            'activeContextID': conversation.activeContextID ?? '',
            'createdAt': conversation.createdAt.toISOString(),
            'updatedAt': conversation.updatedAt.toISOString(),
            'messages': conversation.messages.map(m => {
                const msgObj: Record<string, Object> = {
                    'id': m.id,
                    'role': m.role as string,
                    'content': m.content,
                    'createdAt': m.createdAt.toISOString()
                };
                return msgObj;
            })
        };
        const json = JSON.stringify(payload);
        writeTextFile(path, json);
        console.info(`[${TAG}] conversation saved: ${path}`);
    }
    async loadConversation(id: string): Promise<ChatConversation | null> {
        const path = this.conversationPath(id);
        try {
            const text = readTextFile(path);
            const json: Record<string, Object> = JSON.parse(text) as Record<string, Object>;
            const conv = new ChatConversation((json['title'] as string) ?? '新对话');
            conv.id = (json['id'] as string) ?? '';
            conv.activeContextID = (json['activeContextID'] as string) ?? null;
            conv.createdAt = new Date((json['createdAt'] as string) ?? new Date().toISOString());
            conv.updatedAt = new Date((json['updatedAt'] as string) ?? new Date().toISOString());
            const msgs: Array<Record<string, Object>> = (json['messages'] as Array<Record<string, Object>>) ?? [];
            conv.messages = msgs.map(m => {
                const roleStr: string = (m['role'] as string) ?? 'user';
                const role: ChatMessageRole = roleStr === 'assistant' ? ChatMessageRole.ASSISTANT :
                    roleStr === 'system' ? ChatMessageRole.SYSTEM : ChatMessageRole.USER;
                const msg = new ChatMessage(role, (m['content'] as string) ?? '');
                msg.id = (m['id'] as string) ?? '';
                msg.createdAt = new Date((m['createdAt'] as string) ?? new Date().toISOString());
                return msg;
            });
            return conv;
        }
        catch {
            return null;
        }
    }
    async loadAllConversations(): Promise<ChatConversation[]> {
        this.ensureDirectories();
        const conversations: ChatConversation[] = [];
        try {
            const files: string[] = fileIo.listFileSync(this.baseDir);
            for (const file of files) {
                if (!file.endsWith('.json'))
                    continue;
                const id = file.replace('.json', '');
                const conv = await this.loadConversation(id);
                if (conv) {
                    conversations.push(conv);
                }
            }
            conversations.sort((a, b) => b.updatedAt.getTime() - a.updatedAt.getTime());
        }
        catch { /* ignore */ }
        return conversations;
    }
    async deleteConversation(id: string): Promise<void> {
        const path = this.conversationPath(id);
        try {
            if (fileIo.accessSync(path)) {
                fileIo.unlinkSync(path);
            }
        }
        catch { /* ignore */ }
    }
}
