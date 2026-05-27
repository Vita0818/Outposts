import { ChatResult, ChatMessage, ChatMessageRole } from "@bundle:com.vita0818.rokurics/entry/ets/models/ChatModels";
import type { ChatRequest } from "@bundle:com.vita0818.rokurics/entry/ets/models/ChatModels";
import { TranscriptionResult, NoteGenerationResult } from "@bundle:com.vita0818.rokurics/entry/ets/models/ProcessingModels";
import { OpenAICompatibleClient } from "@bundle:com.vita0818.rokurics/entry/ets/services/OpenAICompatibleClient";
import type { AIConfiguration } from "@bundle:com.vita0818.rokurics/entry/ets/services/OpenAICompatibleClient";
// ---------- Transcription ----------
export interface TranscriptionProviderRequest {
    taskID: string;
    recordingID: string;
    audioFilePath: string;
    language: string | null;
    outputDirectory: string;
}
// ---------- Mock Implementations ----------
export class MockTranscriptionProvider {
    readonly id: string = 'mock';
    readonly displayName: string = 'Mock Transcription';
    async validateConfiguration(): Promise<void> { return; }
    async transcribe(request: TranscriptionProviderRequest): Promise<TranscriptionResult> {
        await new Promise<void>(r => setTimeout(r, 500));
        return TranscriptionResult.create({
            taskID: request.taskID,
            recordingID: request.recordingID,
            providerID: this.id,
            providerName: this.displayName,
            text: '这是一段模拟的转写文本。用于在未配置真实转写引擎时展示转写功能的工作流程。',
            modelName: 'mock-local',
            language: request.language ?? 'auto',
            status: 'transcribed'
        });
    }
}
export class MockNoteGenerationProvider {
    readonly id: string = 'mockNoteGen';
    readonly displayName: string = 'Mock Note Generation';
    async validateConfiguration(): Promise<void> { return; }
    async generateNote(request: NoteGenRequest): Promise<NoteGenerationResult> {
        await new Promise<void>(r => setTimeout(r, 300));
        const markdown = `# ${request.title}

## 基本信息
- 录音 ID：${request.recordingID}
- 生成时间：${new Date().toISOString()}

## 摘要
这是一份模拟笔记，用于验证笔记生成流程的完整性。

## 大纲
1. 第一节
2. 第二节
3. 第三节

## 重点
- 重点内容 A
- 重点内容 B

---
*由 ${this.displayName} 生成*
`;
        return NoteGenerationResult.create({
            taskID: request.taskID,
            recordingID: request.recordingID,
            providerID: this.id,
            providerName: this.displayName,
            markdown: markdown,
            status: 'generated'
        });
    }
}
export class OpenAICompatibleTranscriptionProvider {
    readonly id: string = 'openaiWhisper';
    readonly displayName: string = 'OpenAI Whisper (需配置)';
    private config: AIConfiguration;
    constructor(config: AIConfiguration) {
        this.config = config;
    }
    async validateConfiguration(): Promise<void> {
        if (!this.config.baseURL || !this.config.modelName) {
            throw new Error('转写服务未配置，请先在设置中配置 AI 提供商');
        }
    }
    async transcribe(request: TranscriptionProviderRequest): Promise<TranscriptionResult> {
        await this.validateConfiguration();
        const transcriptModel = this.config.modelName.includes('whisper') ? this.config.modelName : 'whisper-1';
        return TranscriptionResult.create({
            taskID: request.taskID,
            recordingID: request.recordingID,
            providerID: this.id,
            providerName: this.displayName,
            text: '',
            modelName: transcriptModel,
            language: request.language ?? 'auto',
            status: 'transcribed'
        });
    }
}
export class OpenAICompatibleNoteGenProvider {
    readonly id: string = 'openaiNoteGen';
    readonly displayName: string = 'OpenAI Note Generation';
    private config: AIConfiguration;
    constructor(config: AIConfiguration) {
        this.config = config;
    }
    async validateConfiguration(): Promise<void> {
        if (!this.config.baseURL || !this.config.modelName || !this.config.apiKey) {
            throw new Error('笔记生成服务未配置，请先在设置中配置 AI 提供商');
        }
    }
    async generateNote(request: NoteGenRequest): Promise<NoteGenerationResult> {
        await this.validateConfiguration();
        const startedAt = new Date();
        const transcriptContent = request.transcriptMarkdown ?? '';
        const truncatedContent = transcriptContent.length > 8000
            ? transcriptContent.substring(0, 8000) : transcriptContent;
        const wasTruncated = transcriptContent.length > 8000;
        const systemPrompt = `你是 Rokurics 的中文课堂笔记整理助手。你的任务是把课堂录音转写整理成清晰、准确、适合复习的 Markdown 笔记。只输出最终 Markdown，不要输出思考过程、草稿、推理步骤或任何与笔记无关的说明。`;
        const userPrompt = `请根据以下转写生成 Markdown 笔记。

录音标题：${request.title}
${wasTruncated ? '注意：转写内容过长，已截取前 8000 字。' : '基于完整转写生成。'}

转写内容：
${truncatedContent}`;
        const markdown = await OpenAICompatibleClient.chatCompletionSimple(this.config, systemPrompt, userPrompt, 180000);
        return NoteGenerationResult.create({
            taskID: request.taskID,
            recordingID: request.recordingID,
            providerID: this.id,
            providerName: this.displayName,
            markdown: markdown,
            modelName: this.config.modelName,
            status: 'generated',
            modelOutputWasTruncated: false,
            transcriptInputWasTruncated: wasTruncated
        });
    }
}
export enum ProcessingProviderKind {
    MOCK = "mock",
    OPENAI_COMPATIBLE = "openaiCompatible"
}
export interface NoteGenRequest {
    taskID: string;
    recordingID: string;
    title: string;
    transcriptMarkdown: string | null;
}
export class MockChatProvider {
    readonly id: string = 'mockChat';
    readonly displayName: string = 'Mock Chat';
    async validateConfiguration(): Promise<void> { return; }
    async send(request: ChatRequest): Promise<ChatResult> {
        const msgs = request.messages;
        let userContent = '';
        for (let i = msgs.length - 1; i >= 0; i--) {
            if (msgs[i].role === ChatMessageRole.USER) {
                userContent = msgs[i].content;
                break;
            }
        }
        const echoContent = userContent
            ? `这是模拟回复。\n\n你问的是：「${userContent.substring(0, 50)}」\n\n当前为 Mock 模式，请配置真实 AI 提供商以获取智能回复。`
            : '这是一条模拟回复。当前为 Mock 模式。';
        const reply = new ChatMessage(ChatMessageRole.ASSISTANT, echoContent);
        const result = new ChatResult(reply, this.id, this.displayName);
        result.modelName = 'mock-model';
        result.finishReason = 'stop';
        return result;
    }
}
export class OpenAICompatibleChatProvider {
    readonly id: string = 'openaiCompatible';
    readonly displayName: string = 'OpenAI Compatible';
    private config: AIConfiguration;
    constructor(config: AIConfiguration) {
        this.config = config;
    }
    async validateConfiguration(): Promise<void> {
        if (!this.config.baseURL || !this.config.modelName) {
            throw new Error('AI 未配置，请先到设置页配置 API 地址和模型名称');
        }
    }
    async send(request: ChatRequest): Promise<ChatResult> {
        return await OpenAICompatibleClient.chatCompletion(this.config, request, 120000);
    }
}
export enum ChatProviderKind {
    MOCK = "mock",
    OPENAI_COMPATIBLE = "openaiCompatible"
}
export function createChatProvider(kind: ChatProviderKind, config: AIConfiguration | null): MockChatProvider | OpenAICompatibleChatProvider {
    if (kind === ChatProviderKind.OPENAI_COMPATIBLE && config) {
        return new OpenAICompatibleChatProvider(config);
    }
    return new MockChatProvider();
}
