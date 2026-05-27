import type { RecordingMetadata } from '../models/RecordingModels';
import type { TranscriptionResult, NoteGenerationResult } from '../models/ProcessingModels';
import { MockTranscriptionProvider, MockNoteGenerationProvider, OpenAICompatibleTranscriptionProvider, OpenAICompatibleNoteGenProvider } from "@bundle:com.vita0818.rokurics/entry/ets/providers/ProviderInterfaces";
import type { TranscriptionProviderRequest, NoteGenRequest } from "@bundle:com.vita0818.rokurics/entry/ets/providers/ProviderInterfaces";
import { TranscriptStore } from "@bundle:com.vita0818.rokurics/entry/ets/services/TranscriptStore";
import { ExportManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/ExportManager";
import { SettingsStore } from "@bundle:com.vita0818.rokurics/entry/ets/services/SettingsStore";
import type { AIConfiguration } from './OpenAICompatibleClient';
const TAG = 'RokuricsPipeline';
export enum PipelineStage {
    IDLE = "idle",
    TRANSCRIBING = "transcribing",
    TRANSCRIPT_SAVING = "transcriptSaving",
    GENERATING_NOTE = "generatingNote",
    NOTE_SAVING = "noteSaving",
    EXPORTING = "exporting",
    COMPLETED = "completed",
    FAILED = "failed"
}
interface PipelineStateData {
    stage: PipelineStage;
    progressMessage: string;
    transcript: TranscriptionResult | null;
    note: NoteGenerationResult | null;
    error: string | null;
    needsConfiguration: boolean;
    configurationMessage: string | null;
}
class PipelineStateHolder {
    stage: PipelineStage = PipelineStage.IDLE;
    progressMessage: string = '';
    transcript: TranscriptionResult | null = null;
    note: NoteGenerationResult | null = null;
    error: string | null = null;
    needsConfiguration: boolean = false;
    configurationMessage: string | null = null;
}
export class TranscriptionPipeline {
    private transcriptStore: TranscriptStore;
    private exportManager: ExportManager;
    private settingsStore: SettingsStore;
    private stateListeners: ((state: PipelineStateHolder) => void)[] = [];
    private _state: PipelineStateHolder = new PipelineStateHolder();
    constructor(context: Context) {
        this.transcriptStore = new TranscriptStore(context);
        this.exportManager = new ExportManager(context);
        this.settingsStore = new SettingsStore(context);
    }
    get state(): PipelineStateHolder { return this._state; }
    onStateChange(listener: (state: PipelineStateHolder) => void): void {
        this.stateListeners.push(listener);
    }
    private notifyListeners(): void {
        for (const l of this.stateListeners) {
            l(this._state);
        }
    }
    private setStage(stage: PipelineStage, msg: string): void {
        this._state.stage = stage;
        this._state.progressMessage = msg;
        this.notifyListeners();
    }
    private clearResults(): void {
        this._state.transcript = null;
        this._state.note = null;
        this._state.error = null;
        this._state.needsConfiguration = false;
        this._state.configurationMessage = null;
        this.notifyListeners();
    }
    private makeTaskID(): string {
        const chars = 'abcdefghijklmnopqrstuvwxyz0123456789';
        let id = '';
        for (let i = 0; i < 16; i++) {
            id += chars.charAt(Math.floor(Math.random() * chars.length));
        }
        return 'task_' + id;
    }
    get resultsReady(): boolean {
        return this._state.stage === PipelineStage.COMPLETED &&
            this._state.transcript !== null;
    }
    async runFullPipeline(recording: RecordingMetadata, audioAbsolutePath: string, forceMock: boolean): Promise<void> {
        this._state.stage = PipelineStage.IDLE;
        this._state.progressMessage = '开始处理';
        this.clearResults();
        try {
            await this.runTranscription(recording, audioAbsolutePath, forceMock);
            if (this._state.transcript && this._state.transcript.text.length > 0) {
                await this.runNoteGeneration(recording, forceMock);
            }
            this._state.stage = PipelineStage.COMPLETED;
            this._state.progressMessage = '处理完成';
            this.notifyListeners();
        }
        catch (err) {
            const msg = err instanceof Error ? err.message : String(err);
            this._state.stage = PipelineStage.FAILED;
            this._state.progressMessage = '处理失败';
            this._state.error = msg;
            this.notifyListeners();
        }
    }
    async runTranscription(recording: RecordingMetadata, audioAbsolutePath: string, forceMock: boolean): Promise<void> {
        this.setStage(PipelineStage.TRANSCRIBING, '正在转写录音');
        const taskID = this.makeTaskID();
        let useMock = forceMock;
        if (!forceMock) {
            const kind = await this.settingsStore.getAIProviderKind();
            useMock = kind !== 'openaiCompatible';
        }
        if (!useMock) {
            const config = await this.loadAIConfig();
            const provider = new OpenAICompatibleTranscriptionProvider(config);
            try {
                await provider.validateConfiguration();
            }
            catch (_e) {
                this._state.needsConfiguration = true;
                this._state.configurationMessage = '转写需要配置 OpenAI-compatible API（Whisper 模型）。请在设置页配置 AI 提供商。当前使用 Mock 模式继续。';
                this.notifyListeners();
                useMock = true;
            }
            if (!useMock) {
                const request: TranscriptionProviderRequest = {
                    taskID: taskID,
                    recordingID: recording.id,
                    audioFilePath: audioAbsolutePath,
                    language: null,
                    outputDirectory: this.transcriptStore.outputDirectory(recording.id, recording.createdAt)
                };
                try {
                    const result = await provider.transcribe(request);
                    this.setStage(PipelineStage.TRANSCRIPT_SAVING, '正在保存转写');
                    this._state.transcript = result;
                    this.notifyListeners();
                    this.transcriptStore.save(result, recording.title, recording.id, recording.createdAt);
                    return;
                }
                catch (err) {
                    console.error('[' + TAG + '] real transcription failed, falling back to mock: ' + String(err));
                    useMock = true;
                }
            }
        }
        // Mock fallback
        const mockProvider = new MockTranscriptionProvider();
        const mockRequest: TranscriptionProviderRequest = {
            taskID: taskID,
            recordingID: recording.id,
            audioFilePath: audioAbsolutePath,
            language: null,
            outputDirectory: ''
        };
        const mockResult = await mockProvider.transcribe(mockRequest);
        this.setStage(PipelineStage.TRANSCRIPT_SAVING, '正在保存转写 (Mock)');
        this._state.transcript = mockResult;
        this.notifyListeners();
        this.transcriptStore.save(mockResult, recording.title, recording.id, recording.createdAt);
    }
    async runNoteGeneration(recording: RecordingMetadata, forceMock: boolean): Promise<void> {
        this.setStage(PipelineStage.GENERATING_NOTE, '正在生成 AI 笔记');
        const taskID = this.makeTaskID();
        let useMock = forceMock;
        if (!forceMock) {
            const kind = await this.settingsStore.getAIProviderKind();
            useMock = kind !== 'openaiCompatible';
        }
        if (!useMock) {
            const config = await this.loadAIConfig();
            const provider = new OpenAICompatibleNoteGenProvider(config);
            try {
                await provider.validateConfiguration();
            }
            catch (_e) {
                if (!this._state.needsConfiguration) {
                    this._state.needsConfiguration = true;
                    this._state.configurationMessage = '笔记生成需要配置 AI 提供商 API Key。当前使用 Mock 模式。';
                    this.notifyListeners();
                }
                useMock = true;
            }
            if (!useMock) {
                const request: NoteGenRequest = {
                    taskID: taskID,
                    recordingID: recording.id,
                    title: recording.title,
                    transcriptMarkdown: this._state.transcript?.text ?? null
                };
                try {
                    const result = await provider.generateNote(request);
                    this.setStage(PipelineStage.NOTE_SAVING, '正在保存笔记');
                    this._state.note = result;
                    this.notifyListeners();
                    this.transcriptStore.saveNote(result, recording.title, recording.id, recording.createdAt);
                    return;
                }
                catch (err) {
                    console.error('[' + TAG + '] real note generation failed, falling back to mock: ' + String(err));
                    useMock = true;
                }
            }
        }
        // Mock fallback
        const mockProvider = new MockNoteGenerationProvider();
        const mockRequest: NoteGenRequest = {
            taskID: taskID,
            recordingID: recording.id,
            title: recording.title,
            transcriptMarkdown: this._state.transcript?.text ?? null
        };
        const mockResult = await mockProvider.generateNote(mockRequest);
        this.setStage(PipelineStage.NOTE_SAVING, '正在保存笔记 (Mock)');
        this._state.note = mockResult;
        this.notifyListeners();
        this.transcriptStore.saveNote(mockResult, recording.title, recording.id, recording.createdAt);
    }
    async exportResults(recording: RecordingMetadata): Promise<string[]> {
        this.setStage(PipelineStage.EXPORTING, '正在导出');
        const paths = await this.exportManager.exportAllForRecording(recording, this._state.transcript, this._state.note);
        return paths;
    }
    getExportManager(): ExportManager {
        return this.exportManager;
    }
    getTranscriptStore(): TranscriptStore {
        return this.transcriptStore;
    }
    reset(): void {
        this._state.stage = PipelineStage.IDLE;
        this._state.progressMessage = '';
        this.clearResults();
    }
    private async loadAIConfig(): Promise<AIConfiguration> {
        return {
            baseURL: await this.settingsStore.getAIBaseURL(),
            modelName: await this.settingsStore.getAIModelName(),
            apiKey: await this.settingsStore.getAIAPIKey(),
            temperature: await this.settingsStore.getAITemperature(),
            maxTokens: await this.settingsStore.getAIMaxTokens()
        };
    }
}
