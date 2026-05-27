if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface RecordingDetailPage_Params {
    recordingManager?: RecordingManager;
    recordingId?: string;
    recording?: RecordingMetadata | null;
    showRenameDialog?: boolean;
    renameText?: string;
    showFilingEditor?: boolean;
    editType?: string;
    editSubject?: string;
    editChapter?: string;
    editTopic?: string;
    actualFileSize?: number;
    isPlaying?: boolean;
    playbackError?: string;
    isProcessing?: boolean;
    processingStage?: string;
    processingError?: string;
    hasTranscript?: boolean;
    hasNote?: boolean;
    showTranscriptView?: boolean;
    showNoteView?: boolean;
    transcriptText?: string;
    noteMarkdown?: string;
    isExporting?: boolean;
    exportMessage?: string;
    exportList?: string[];
    showExportList?: boolean;
    exportFormat?: ExportFormat;
    isUploading?: boolean;
    uploadProgressText?: string;
    uploadError?: string;
    avPlayer?: media.AVPlayer | null;
    pipeline?: TranscriptionPipeline | null;
    exportManager?: ExportManager | null;
    uploadClient?: RecordingUploadClient | null;
}
import media from "@ohos:multimedia.media";
import fileIo from "@ohos:file.fs";
import type Want from "@ohos:app.ability.Want";
import type common from "@ohos:app.ability.common";
import { getSharedRecordingManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/RecordingManager";
import type { RecordingManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/RecordingManager";
import { StudyFilingPath, filingLevelTitle } from "@bundle:com.vita0818.rokurics/entry/ets/models/RecordingModels";
import type { RecordingMetadata } from "@bundle:com.vita0818.rokurics/entry/ets/models/RecordingModels";
import { formatDuration, formatShortTime, fileSizeText } from "@bundle:com.vita0818.rokurics/entry/ets/utils/FormatHelpers";
import { TranscriptionPipeline } from "@bundle:com.vita0818.rokurics/entry/ets/services/TranscriptionPipeline";
import { ExportManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/ExportManager";
import type { ExportFormat } from "@bundle:com.vita0818.rokurics/entry/ets/services/ExportManager";
import { RecordingUploadClient } from "@bundle:com.vita0818.rokurics/entry/ets/services/RecordingUploadClient";
import { SettingsStore } from "@bundle:com.vita0818.rokurics/entry/ets/services/SettingsStore";
import { RokuricsColors, FontWeight } from "@bundle:com.vita0818.rokurics/entry/ets/utils/RokuricsTheme";
const LEVELS: string[] = ['type', 'subject', 'chapter', 'topic'];
const FILING_OPTIONS: Record<string, string[]> = {
    'type': ['课堂录音', '自学笔记', '会议记录', '访谈采访', '灵感记录', '其他'],
    'subject': ['数学', '物理', '化学', '生物', '计算机', '英语', '历史', '文学', '哲学', '艺术'],
    'chapter': [],
    'topic': []
};
class RecordingDetailPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.recordingManager = getSharedRecordingManager();
        this.__recordingId = new ObservedPropertySimplePU('', this, "recordingId");
        this.__recording = new ObservedPropertyObjectPU(null, this, "recording");
        this.__showRenameDialog = new ObservedPropertySimplePU(false, this, "showRenameDialog");
        this.__renameText = new ObservedPropertySimplePU('', this, "renameText");
        this.__showFilingEditor = new ObservedPropertySimplePU(false, this, "showFilingEditor");
        this.__editType = new ObservedPropertySimplePU('', this, "editType");
        this.__editSubject = new ObservedPropertySimplePU('', this, "editSubject");
        this.__editChapter = new ObservedPropertySimplePU('', this, "editChapter");
        this.__editTopic = new ObservedPropertySimplePU('', this, "editTopic");
        this.__actualFileSize = new ObservedPropertySimplePU(0, this, "actualFileSize");
        this.__isPlaying = new ObservedPropertySimplePU(false, this, "isPlaying");
        this.__playbackError = new ObservedPropertySimplePU('', this, "playbackError");
        this.__isProcessing = new ObservedPropertySimplePU(false, this, "isProcessing");
        this.__processingStage = new ObservedPropertySimplePU('', this, "processingStage");
        this.__processingError = new ObservedPropertySimplePU('', this, "processingError");
        this.__hasTranscript = new ObservedPropertySimplePU(false, this, "hasTranscript");
        this.__hasNote = new ObservedPropertySimplePU(false, this, "hasNote");
        this.__showTranscriptView = new ObservedPropertySimplePU(false, this, "showTranscriptView");
        this.__showNoteView = new ObservedPropertySimplePU(false, this, "showNoteView");
        this.__transcriptText = new ObservedPropertySimplePU('', this, "transcriptText");
        this.__noteMarkdown = new ObservedPropertySimplePU('', this, "noteMarkdown");
        this.__isExporting = new ObservedPropertySimplePU(false, this, "isExporting");
        this.__exportMessage = new ObservedPropertySimplePU('', this, "exportMessage");
        this.__exportList = new ObservedPropertyObjectPU([], this, "exportList");
        this.__showExportList = new ObservedPropertySimplePU(false, this, "showExportList");
        this.__exportFormat = new ObservedPropertySimplePU('Markdown', this, "exportFormat");
        this.__isUploading = new ObservedPropertySimplePU(false, this, "isUploading");
        this.__uploadProgressText = new ObservedPropertySimplePU('', this, "uploadProgressText");
        this.__uploadError = new ObservedPropertySimplePU('', this, "uploadError");
        this.avPlayer = null;
        this.pipeline = null;
        this.exportManager = null;
        this.uploadClient = null;
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: RecordingDetailPage_Params) {
        if (params.recordingManager !== undefined) {
            this.recordingManager = params.recordingManager;
        }
        if (params.recordingId !== undefined) {
            this.recordingId = params.recordingId;
        }
        if (params.recording !== undefined) {
            this.recording = params.recording;
        }
        if (params.showRenameDialog !== undefined) {
            this.showRenameDialog = params.showRenameDialog;
        }
        if (params.renameText !== undefined) {
            this.renameText = params.renameText;
        }
        if (params.showFilingEditor !== undefined) {
            this.showFilingEditor = params.showFilingEditor;
        }
        if (params.editType !== undefined) {
            this.editType = params.editType;
        }
        if (params.editSubject !== undefined) {
            this.editSubject = params.editSubject;
        }
        if (params.editChapter !== undefined) {
            this.editChapter = params.editChapter;
        }
        if (params.editTopic !== undefined) {
            this.editTopic = params.editTopic;
        }
        if (params.actualFileSize !== undefined) {
            this.actualFileSize = params.actualFileSize;
        }
        if (params.isPlaying !== undefined) {
            this.isPlaying = params.isPlaying;
        }
        if (params.playbackError !== undefined) {
            this.playbackError = params.playbackError;
        }
        if (params.isProcessing !== undefined) {
            this.isProcessing = params.isProcessing;
        }
        if (params.processingStage !== undefined) {
            this.processingStage = params.processingStage;
        }
        if (params.processingError !== undefined) {
            this.processingError = params.processingError;
        }
        if (params.hasTranscript !== undefined) {
            this.hasTranscript = params.hasTranscript;
        }
        if (params.hasNote !== undefined) {
            this.hasNote = params.hasNote;
        }
        if (params.showTranscriptView !== undefined) {
            this.showTranscriptView = params.showTranscriptView;
        }
        if (params.showNoteView !== undefined) {
            this.showNoteView = params.showNoteView;
        }
        if (params.transcriptText !== undefined) {
            this.transcriptText = params.transcriptText;
        }
        if (params.noteMarkdown !== undefined) {
            this.noteMarkdown = params.noteMarkdown;
        }
        if (params.isExporting !== undefined) {
            this.isExporting = params.isExporting;
        }
        if (params.exportMessage !== undefined) {
            this.exportMessage = params.exportMessage;
        }
        if (params.exportList !== undefined) {
            this.exportList = params.exportList;
        }
        if (params.showExportList !== undefined) {
            this.showExportList = params.showExportList;
        }
        if (params.exportFormat !== undefined) {
            this.exportFormat = params.exportFormat;
        }
        if (params.isUploading !== undefined) {
            this.isUploading = params.isUploading;
        }
        if (params.uploadProgressText !== undefined) {
            this.uploadProgressText = params.uploadProgressText;
        }
        if (params.uploadError !== undefined) {
            this.uploadError = params.uploadError;
        }
        if (params.avPlayer !== undefined) {
            this.avPlayer = params.avPlayer;
        }
        if (params.pipeline !== undefined) {
            this.pipeline = params.pipeline;
        }
        if (params.exportManager !== undefined) {
            this.exportManager = params.exportManager;
        }
        if (params.uploadClient !== undefined) {
            this.uploadClient = params.uploadClient;
        }
    }
    updateStateVars(params: RecordingDetailPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__recordingId.purgeDependencyOnElmtId(rmElmtId);
        this.__recording.purgeDependencyOnElmtId(rmElmtId);
        this.__showRenameDialog.purgeDependencyOnElmtId(rmElmtId);
        this.__renameText.purgeDependencyOnElmtId(rmElmtId);
        this.__showFilingEditor.purgeDependencyOnElmtId(rmElmtId);
        this.__editType.purgeDependencyOnElmtId(rmElmtId);
        this.__editSubject.purgeDependencyOnElmtId(rmElmtId);
        this.__editChapter.purgeDependencyOnElmtId(rmElmtId);
        this.__editTopic.purgeDependencyOnElmtId(rmElmtId);
        this.__actualFileSize.purgeDependencyOnElmtId(rmElmtId);
        this.__isPlaying.purgeDependencyOnElmtId(rmElmtId);
        this.__playbackError.purgeDependencyOnElmtId(rmElmtId);
        this.__isProcessing.purgeDependencyOnElmtId(rmElmtId);
        this.__processingStage.purgeDependencyOnElmtId(rmElmtId);
        this.__processingError.purgeDependencyOnElmtId(rmElmtId);
        this.__hasTranscript.purgeDependencyOnElmtId(rmElmtId);
        this.__hasNote.purgeDependencyOnElmtId(rmElmtId);
        this.__showTranscriptView.purgeDependencyOnElmtId(rmElmtId);
        this.__showNoteView.purgeDependencyOnElmtId(rmElmtId);
        this.__transcriptText.purgeDependencyOnElmtId(rmElmtId);
        this.__noteMarkdown.purgeDependencyOnElmtId(rmElmtId);
        this.__isExporting.purgeDependencyOnElmtId(rmElmtId);
        this.__exportMessage.purgeDependencyOnElmtId(rmElmtId);
        this.__exportList.purgeDependencyOnElmtId(rmElmtId);
        this.__showExportList.purgeDependencyOnElmtId(rmElmtId);
        this.__exportFormat.purgeDependencyOnElmtId(rmElmtId);
        this.__isUploading.purgeDependencyOnElmtId(rmElmtId);
        this.__uploadProgressText.purgeDependencyOnElmtId(rmElmtId);
        this.__uploadError.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__recordingId.aboutToBeDeleted();
        this.__recording.aboutToBeDeleted();
        this.__showRenameDialog.aboutToBeDeleted();
        this.__renameText.aboutToBeDeleted();
        this.__showFilingEditor.aboutToBeDeleted();
        this.__editType.aboutToBeDeleted();
        this.__editSubject.aboutToBeDeleted();
        this.__editChapter.aboutToBeDeleted();
        this.__editTopic.aboutToBeDeleted();
        this.__actualFileSize.aboutToBeDeleted();
        this.__isPlaying.aboutToBeDeleted();
        this.__playbackError.aboutToBeDeleted();
        this.__isProcessing.aboutToBeDeleted();
        this.__processingStage.aboutToBeDeleted();
        this.__processingError.aboutToBeDeleted();
        this.__hasTranscript.aboutToBeDeleted();
        this.__hasNote.aboutToBeDeleted();
        this.__showTranscriptView.aboutToBeDeleted();
        this.__showNoteView.aboutToBeDeleted();
        this.__transcriptText.aboutToBeDeleted();
        this.__noteMarkdown.aboutToBeDeleted();
        this.__isExporting.aboutToBeDeleted();
        this.__exportMessage.aboutToBeDeleted();
        this.__exportList.aboutToBeDeleted();
        this.__showExportList.aboutToBeDeleted();
        this.__exportFormat.aboutToBeDeleted();
        this.__isUploading.aboutToBeDeleted();
        this.__uploadProgressText.aboutToBeDeleted();
        this.__uploadError.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private recordingManager: RecordingManager;
    private __recordingId: ObservedPropertySimplePU<string>;
    get recordingId() {
        return this.__recordingId.get();
    }
    set recordingId(newValue: string) {
        this.__recordingId.set(newValue);
    }
    private __recording: ObservedPropertyObjectPU<RecordingMetadata | null>;
    get recording() {
        return this.__recording.get();
    }
    set recording(newValue: RecordingMetadata | null) {
        this.__recording.set(newValue);
    }
    private __showRenameDialog: ObservedPropertySimplePU<boolean>;
    get showRenameDialog() {
        return this.__showRenameDialog.get();
    }
    set showRenameDialog(newValue: boolean) {
        this.__showRenameDialog.set(newValue);
    }
    private __renameText: ObservedPropertySimplePU<string>;
    get renameText() {
        return this.__renameText.get();
    }
    set renameText(newValue: string) {
        this.__renameText.set(newValue);
    }
    private __showFilingEditor: ObservedPropertySimplePU<boolean>;
    get showFilingEditor() {
        return this.__showFilingEditor.get();
    }
    set showFilingEditor(newValue: boolean) {
        this.__showFilingEditor.set(newValue);
    }
    private __editType: ObservedPropertySimplePU<string>;
    get editType() {
        return this.__editType.get();
    }
    set editType(newValue: string) {
        this.__editType.set(newValue);
    }
    private __editSubject: ObservedPropertySimplePU<string>;
    get editSubject() {
        return this.__editSubject.get();
    }
    set editSubject(newValue: string) {
        this.__editSubject.set(newValue);
    }
    private __editChapter: ObservedPropertySimplePU<string>;
    get editChapter() {
        return this.__editChapter.get();
    }
    set editChapter(newValue: string) {
        this.__editChapter.set(newValue);
    }
    private __editTopic: ObservedPropertySimplePU<string>;
    get editTopic() {
        return this.__editTopic.get();
    }
    set editTopic(newValue: string) {
        this.__editTopic.set(newValue);
    }
    private __actualFileSize: ObservedPropertySimplePU<number>;
    get actualFileSize() {
        return this.__actualFileSize.get();
    }
    set actualFileSize(newValue: number) {
        this.__actualFileSize.set(newValue);
    }
    private __isPlaying: ObservedPropertySimplePU<boolean>;
    get isPlaying() {
        return this.__isPlaying.get();
    }
    set isPlaying(newValue: boolean) {
        this.__isPlaying.set(newValue);
    }
    private __playbackError: ObservedPropertySimplePU<string>;
    get playbackError() {
        return this.__playbackError.get();
    }
    set playbackError(newValue: string) {
        this.__playbackError.set(newValue);
    }
    private __isProcessing: ObservedPropertySimplePU<boolean>;
    get isProcessing() {
        return this.__isProcessing.get();
    }
    set isProcessing(newValue: boolean) {
        this.__isProcessing.set(newValue);
    }
    private __processingStage: ObservedPropertySimplePU<string>;
    get processingStage() {
        return this.__processingStage.get();
    }
    set processingStage(newValue: string) {
        this.__processingStage.set(newValue);
    }
    private __processingError: ObservedPropertySimplePU<string>;
    get processingError() {
        return this.__processingError.get();
    }
    set processingError(newValue: string) {
        this.__processingError.set(newValue);
    }
    private __hasTranscript: ObservedPropertySimplePU<boolean>;
    get hasTranscript() {
        return this.__hasTranscript.get();
    }
    set hasTranscript(newValue: boolean) {
        this.__hasTranscript.set(newValue);
    }
    private __hasNote: ObservedPropertySimplePU<boolean>;
    get hasNote() {
        return this.__hasNote.get();
    }
    set hasNote(newValue: boolean) {
        this.__hasNote.set(newValue);
    }
    private __showTranscriptView: ObservedPropertySimplePU<boolean>;
    get showTranscriptView() {
        return this.__showTranscriptView.get();
    }
    set showTranscriptView(newValue: boolean) {
        this.__showTranscriptView.set(newValue);
    }
    private __showNoteView: ObservedPropertySimplePU<boolean>;
    get showNoteView() {
        return this.__showNoteView.get();
    }
    set showNoteView(newValue: boolean) {
        this.__showNoteView.set(newValue);
    }
    private __transcriptText: ObservedPropertySimplePU<string>;
    get transcriptText() {
        return this.__transcriptText.get();
    }
    set transcriptText(newValue: string) {
        this.__transcriptText.set(newValue);
    }
    private __noteMarkdown: ObservedPropertySimplePU<string>;
    get noteMarkdown() {
        return this.__noteMarkdown.get();
    }
    set noteMarkdown(newValue: string) {
        this.__noteMarkdown.set(newValue);
    }
    private __isExporting: ObservedPropertySimplePU<boolean>;
    get isExporting() {
        return this.__isExporting.get();
    }
    set isExporting(newValue: boolean) {
        this.__isExporting.set(newValue);
    }
    private __exportMessage: ObservedPropertySimplePU<string>;
    get exportMessage() {
        return this.__exportMessage.get();
    }
    set exportMessage(newValue: string) {
        this.__exportMessage.set(newValue);
    }
    private __exportList: ObservedPropertyObjectPU<string[]>;
    get exportList() {
        return this.__exportList.get();
    }
    set exportList(newValue: string[]) {
        this.__exportList.set(newValue);
    }
    private __showExportList: ObservedPropertySimplePU<boolean>;
    get showExportList() {
        return this.__showExportList.get();
    }
    set showExportList(newValue: boolean) {
        this.__showExportList.set(newValue);
    }
    private __exportFormat: ObservedPropertySimplePU<ExportFormat>;
    get exportFormat() {
        return this.__exportFormat.get();
    }
    set exportFormat(newValue: ExportFormat) {
        this.__exportFormat.set(newValue);
    }
    private __isUploading: ObservedPropertySimplePU<boolean>;
    get isUploading() {
        return this.__isUploading.get();
    }
    set isUploading(newValue: boolean) {
        this.__isUploading.set(newValue);
    }
    private __uploadProgressText: ObservedPropertySimplePU<string>;
    get uploadProgressText() {
        return this.__uploadProgressText.get();
    }
    set uploadProgressText(newValue: string) {
        this.__uploadProgressText.set(newValue);
    }
    private __uploadError: ObservedPropertySimplePU<string>;
    get uploadError() {
        return this.__uploadError.get();
    }
    set uploadError(newValue: string) {
        this.__uploadError.set(newValue);
    }
    private avPlayer: media.AVPlayer | null;
    private pipeline: TranscriptionPipeline | null;
    private exportManager: ExportManager | null;
    private uploadClient: RecordingUploadClient | null;
    aboutToAppear(): void {
        this.loadData();
        this.pipeline = new TranscriptionPipeline(getContext(this));
        this.exportManager = new ExportManager(getContext(this));
    }
    async loadData(): Promise<void> {
        await this.recordingManager.reloadRecordings();
        const found: RecordingMetadata | undefined = this.recordingManager.recordings.find((r: RecordingMetadata) => r.id === this.recordingId);
        if (found) {
            this.recording = found;
            this.actualFileSize = await this.recordingManager.getAudioFileSize(found);
            // Initialize filing editor from existing values
            this.editType = found.studyFiling?.type ?? '';
            this.editSubject = found.studyFiling?.subject ?? '';
            this.editChapter = found.studyFiling?.chapter ?? '';
            this.editTopic = found.studyFiling?.topic ?? '';
        }
    }
    private getFilingValue(level: string): string {
        if (!this.recording)
            return '未填写';
        return this.recording.studyFiling?.valueForLevel(level) ?? '未填写';
    }
    private formatDateStr(d: Date): string {
        return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    }
    private async deleteAndBack(): Promise<void> {
        if (this.recording) {
            await this.recordingManager.deleteRecording(this.recording.id);
        }
        this.getUIContext().getRouter().back();
    }
    private async handleRename(): Promise<void> {
        const title = this.renameText.trim();
        if (title.length === 0 || !this.recording)
            return;
        try {
            await this.recordingManager.renameRecording(this.recording.id, title);
            this.showRenameDialog = false;
            await this.loadData();
        }
        catch {
            // ignore
        }
    }
    private openRenameDialog(): void {
        if (!this.recording)
            return;
        this.renameText = this.recording.title;
        this.showRenameDialog = true;
    }
    private async saveFiling(): Promise<void> {
        if (!this.recording)
            return;
        const filing = new StudyFilingPath(this.editType || undefined, this.editSubject || undefined, this.editChapter || undefined, this.editTopic || undefined);
        await this.recordingManager.updateStudyFiling(this.recording.id, filing);
        this.showFilingEditor = false;
        await this.loadData();
    }
    private releasePlayer(): void {
        if (this.avPlayer) {
            try {
                this.avPlayer.stop();
            }
            catch (_e) { /* ignore */ }
            try {
                this.avPlayer.release();
            }
            catch (_e) { /* ignore */ }
            this.avPlayer = null;
        }
        this.isPlaying = false;
    }
    private async togglePlayback(): Promise<void> {
        if (!this.recording)
            return;
        this.playbackError = '';
        if (this.isPlaying) {
            if (this.avPlayer) {
                try {
                    await this.avPlayer.pause();
                }
                catch (_e) { /* ignore */ }
            }
            this.isPlaying = false;
            return;
        }
        if (!this.avPlayer) {
            const audioPath = this.recordingManager.getAudioAbsolutePath(this.recording);
            try {
                if (!fileIo.accessSync(audioPath)) {
                    this.playbackError = '音频文件不存在';
                    return;
                }
                const file: fileIo.File = fileIo.openSync(audioPath, fileIo.OpenMode.READ_ONLY);
                this.avPlayer = await media.createAVPlayer();
                this.avPlayer.url = `fd://${file.fd}`;
            }
            catch (err) {
                this.playbackError = `播放器创建失败：${err}`;
                return;
            }
        }
        try {
            await this.avPlayer.play();
            this.isPlaying = true;
        }
        catch (err) {
            this.playbackError = `播放失败：${err}`;
            this.releasePlayer();
        }
    }
    initialRender() {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Stack.create();
        }, Stack);
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
            Row.padding({ left: 16, right: 16, top: 56, bottom: 16 });
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
            Button.onClick(() => this.getUIContext().getRouter().back());
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
            Text.create(this.recording?.title ?? '录音详情');
            Text.fontSize(20);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(RokuricsColors.deepText);
            Text.maxLines(1);
            Text.textOverflow({ overflow: TextOverflow.Ellipsis });
            Text.layoutWeight(1);
            Text.margin({ left: 8 });
        }, Text);
        Text.pop();
        // Header
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.recording === null) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create();
                        Column.width('100%');
                        Column.layoutWeight(1);
                        Column.justifyContent(FlexAlign.Center);
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('加载中...');
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    Column.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Scroll.create();
                        Scroll.width('100%');
                        Scroll.layoutWeight(1);
                        Scroll.scrollBar(BarState.Off);
                    }, Scroll);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 16 });
                        Column.width('100%');
                        Column.padding({ left: 16, right: 16 });
                    }, Column);
                    // Info fields
                    this.InfoRow.bind(this)('标题', this.recording!.title);
                    this.InfoRow.bind(this)('时间', formatShortTime(this.recording!.createdAt));
                    this.InfoRow.bind(this)('时长', formatDuration(this.recording!.duration));
                    this.InfoRow.bind(this)('日期', this.formatDateStr(this.recording!.createdAt));
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Technical info - glass card
                        Text.create('技术信息');
                        // Technical info - glass card
                        Text.fontSize(15);
                        // Technical info - glass card
                        Text.fontWeight(FontWeight.SemiBold);
                        // Technical info - glass card
                        Text.fontColor(RokuricsColors.deepText);
                        // Technical info - glass card
                        Text.margin({ top: 8 });
                    }, Text);
                    // Technical info - glass card
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 6 });
                        Column.padding(14);
                        Column.borderRadius(14);
                        Column.backgroundColor(RokuricsColors.glassSurface + 'A8');
                        Column.border({
                            width: 1,
                            color: {
                                colors: [
                                    [0xFFFFFF, 0.34],
                                    [0xEFFAF8, 0.12],
                                    [0x91E8D6, 0.08]
                                ],
                                direction: GradientDirection.RightBottom
                            },
                            radius: 14
                        } as BorderOptions);
                        Column.shadow({
                            color: RokuricsColors.shadowColor + '06',
                            radius: 8,
                            offsetY: 4
                        });
                    }, Column);
                    this.TechRow.bind(this)('格式', this.recording!.format.toUpperCase());
                    this.TechRow.bind(this)('编码', this.recording!.codec);
                    this.TechRow.bind(this)('采样率', `${this.recording!.sampleRate} Hz`);
                    this.TechRow.bind(this)('声道', this.recording!.channels === 1 ? '单声道' : '立体声');
                    this.TechRow.bind(this)('大小', this.actualFileSize > 0 ?
                        fileSizeText(this.actualFileSize) : fileSizeText(this.recording!.fileSize));
                    Column.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Study filing
                        Row.create();
                        // Study filing
                        Row.width('100%');
                        // Study filing
                        Row.margin({ top: 8 });
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('学习归档');
                        Text.fontSize(15);
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
                        Button.onClick(() => { this.showFilingEditor = !this.showFilingEditor; });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('编辑');
                        Text.fontSize(13);
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    // Study filing
                    Row.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.showFilingEditor) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Column.create({ space: 12 });
                                    Column.padding(16);
                                    Column.borderRadius(14);
                                    Column.backgroundColor(RokuricsColors.glassSurface + '40');
                                }, Column);
                                this.FilingEditRow.bind(this)('门类', this.editType, FILING_OPTIONS['type'], (v: string) => { this.editType = (v === this.editType ? '' : v); });
                                this.FilingEditRow.bind(this)('课程', this.editSubject, FILING_OPTIONS['subject'], (v: string) => { this.editSubject = (v === this.editSubject ? '' : v); });
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Column.create({ space: 4 });
                                }, Column);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create('章节');
                                    Text.fontSize(12);
                                    Text.fontColor(RokuricsColors.softText);
                                }, Text);
                                Text.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    TextInput.create({ text: this.editChapter, placeholder: '输入章节名' });
                                    TextInput.fontSize(14);
                                    TextInput.fontColor(RokuricsColors.deepText);
                                    TextInput.backgroundColor(RokuricsColors.glassSurface + '40');
                                    TextInput.borderRadius(8);
                                    TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
                                    TextInput.onChange((value: string) => { this.editChapter = value; });
                                }, TextInput);
                                Column.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Column.create({ space: 4 });
                                }, Column);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create('主题');
                                    Text.fontSize(12);
                                    Text.fontColor(RokuricsColors.softText);
                                }, Text);
                                Text.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    TextInput.create({ text: this.editTopic, placeholder: '输入主题名' });
                                    TextInput.fontSize(14);
                                    TextInput.fontColor(RokuricsColors.deepText);
                                    TextInput.backgroundColor(RokuricsColors.glassSurface + '40');
                                    TextInput.borderRadius(8);
                                    TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
                                    TextInput.onChange((value: string) => { this.editTopic = value; });
                                }, TextInput);
                                Column.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Button.createWithChild();
                                    Button.padding({ left: 20, right: 20, top: 8, bottom: 8 });
                                    Button.borderRadius(10);
                                    Button.backgroundColor(RokuricsColors.aqua);
                                    Button.onClick(() => this.saveFiling());
                                }, Button);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create('保存归档');
                                    Text.fontSize(14);
                                    Text.fontColor(Color.White);
                                }, Text);
                                Text.pop();
                                Button.pop();
                                Column.pop();
                            });
                        }
                        else {
                            this.ifElseBranchUpdateFunction(1, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Column.create({ space: 8 });
                                }, Column);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    ForEach.create();
                                    const forEachItemGenFunction = _item => {
                                        const level = _item;
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Row.create();
                                            Row.width('100%');
                                            Row.padding(10);
                                            Row.borderRadius(8);
                                            Row.backgroundColor(RokuricsColors.glassSurface + 'A8');
                                            Row.border({
                                                width: 1,
                                                color: {
                                                    colors: [
                                                        [0xFFFFFF, 0.20],
                                                        [0xEFFAF8, 0.08]
                                                    ],
                                                    direction: GradientDirection.RightBottom
                                                },
                                                radius: 8
                                            } as BorderOptions);
                                        }, Row);
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Text.create(filingLevelTitle(level));
                                            Text.fontSize(13);
                                            Text.fontColor(RokuricsColors.softText);
                                            Text.width(60);
                                        }, Text);
                                        Text.pop();
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Text.create(this.getFilingValue(level));
                                            Text.fontSize(15);
                                            Text.fontColor(RokuricsColors.deepText);
                                        }, Text);
                                        Text.pop();
                                        Row.pop();
                                    };
                                    this.forEachUpdateFunction(elmtId, LEVELS, forEachItemGenFunction);
                                }, ForEach);
                                ForEach.pop();
                                Column.pop();
                            });
                        }
                    }, If);
                    If.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Status - glass card
                        Text.create('处理状态');
                        // Status - glass card
                        Text.fontSize(15);
                        // Status - glass card
                        Text.fontWeight(FontWeight.SemiBold);
                        // Status - glass card
                        Text.fontColor(RokuricsColors.deepText);
                        // Status - glass card
                        Text.margin({ top: 8 });
                    }, Text);
                    // Status - glass card
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 6 });
                        Column.padding(14);
                        Column.borderRadius(14);
                        Column.backgroundColor(RokuricsColors.glassSurface + 'A8');
                        Column.border({
                            width: 1,
                            color: {
                                colors: [
                                    [0xFFFFFF, 0.34],
                                    [0xEFFAF8, 0.12],
                                    [0x91E8D6, 0.08]
                                ],
                                direction: GradientDirection.RightBottom
                            },
                            radius: 14
                        } as BorderOptions);
                        Column.shadow({
                            color: RokuricsColors.shadowColor + '06',
                            radius: 8,
                            offsetY: 4
                        });
                    }, Column);
                    this.TechRow.bind(this)('上传', this.uploadLabel(this.recording!.uploadStatus));
                    this.TechRow.bind(this)('转写', this.transcriptLabel(this.recording!.transcriptionStatus));
                    this.TechRow.bind(this)('笔记', this.noteLabel(this.recording!.noteStatus));
                    Column.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // AI Processing
                        Column.create({ space: 8 });
                        // AI Processing
                        Column.width('100%');
                        // AI Processing
                        Column.padding(14);
                        // AI Processing
                        Column.borderRadius(14);
                        // AI Processing
                        Column.backgroundColor(RokuricsColors.glassSurface + 'A8');
                        // AI Processing
                        Column.border({
                            width: 1,
                            color: {
                                colors: [
                                    [0xFFFFFF, 0.34],
                                    [0xEFFAF8, 0.12],
                                    [0x91E8D6, 0.08]
                                ],
                                direction: GradientDirection.RightBottom
                            },
                            radius: 14
                        } as BorderOptions);
                        // AI Processing
                        Column.shadow({
                            color: RokuricsColors.shadowColor + '06',
                            radius: 8,
                            offsetY: 4
                        });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('AI 处理');
                        Text.fontSize(15);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(RokuricsColors.deepText);
                        Text.width('100%');
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.isProcessing) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.processingStage);
                                    Text.fontSize(13);
                                    Text.fontColor(RokuricsColors.aqua);
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
                        if (this.processingError.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.processingError);
                                    Text.fontSize(12);
                                    Text.fontColor(RokuricsColors.coral);
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
                        Row.create({ space: 8 });
                        Row.width('100%');
                        Row.justifyContent(FlexAlign.Start);
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 14, right: 14, top: 7, bottom: 7 });
                        Button.border({ width: 1, color: RokuricsColors.aqua + '40', radius: 8 });
                        Button.backgroundColor(Color.Transparent);
                        Button.enabled(!this.isProcessing);
                        Button.onClick(() => this.runTranscription());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('转写');
                        Text.fontSize(13);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(this.hasTranscript ? RokuricsColors.mint : RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 14, right: 14, top: 7, bottom: 7 });
                        Button.border({ width: 1, color: RokuricsColors.aqua + '40', radius: 8 });
                        Button.backgroundColor(Color.Transparent);
                        Button.enabled(!this.isProcessing);
                        Button.onClick(() => this.runFullProcessing());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('生成笔记');
                        Text.fontSize(13);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(this.hasNote ? RokuricsColors.mint : RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 14, right: 14, top: 7, bottom: 7 });
                        Button.borderRadius(8);
                        Button.backgroundColor(RokuricsColors.aqua);
                        Button.enabled(!this.isProcessing);
                        Button.onClick(() => this.runFullProcessing());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('全部处理');
                        Text.fontSize(13);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(Color.White);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    Row.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        // View transcript / note links
                        if (this.hasTranscript) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Button.createWithChild();
                                    Button.backgroundColor(Color.Transparent);
                                    Button.margin({ top: 2 });
                                    Button.onClick(async () => {
                                        this.showTranscriptView = !this.showTranscriptView;
                                        if (this.showTranscriptView && this.transcriptText.length === 0) {
                                            await this.loadTranscript();
                                        }
                                    });
                                }, Button);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.showTranscriptView ? '收起转写 ▲' : '查看转写 ▼');
                                    Text.fontSize(12);
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
                        If.create();
                        if (this.showTranscriptView && this.transcriptText.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Scroll.create();
                                    Scroll.constraintSize({ maxHeight: 200 });
                                    Scroll.borderRadius(8);
                                    Scroll.backgroundColor(RokuricsColors.glassSurface + '60');
                                    Scroll.width('100%');
                                }, Scroll);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.transcriptText);
                                    Text.fontSize(13);
                                    Text.fontColor(RokuricsColors.deepText);
                                    Text.padding(12);
                                }, Text);
                                Text.pop();
                                Scroll.pop();
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
                        if (this.hasNote) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Button.createWithChild();
                                    Button.backgroundColor(Color.Transparent);
                                    Button.margin({ top: 2 });
                                    Button.onClick(async () => {
                                        this.showNoteView = !this.showNoteView;
                                        if (this.showNoteView && this.noteMarkdown.length === 0) {
                                            await this.loadNote();
                                        }
                                    });
                                }, Button);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.showNoteView ? '收起笔记 ▲' : '查看笔记 ▼');
                                    Text.fontSize(12);
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
                        If.create();
                        if (this.showNoteView && this.noteMarkdown.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Scroll.create();
                                    Scroll.constraintSize({ maxHeight: 200 });
                                    Scroll.borderRadius(8);
                                    Scroll.backgroundColor(RokuricsColors.glassSurface + '60');
                                    Scroll.width('100%');
                                }, Scroll);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.noteMarkdown);
                                    Text.fontSize(13);
                                    Text.fontColor(RokuricsColors.deepText);
                                    Text.padding(12);
                                }, Text);
                                Text.pop();
                                Scroll.pop();
                            });
                        }
                        else {
                            this.ifElseBranchUpdateFunction(1, () => {
                            });
                        }
                    }, If);
                    If.pop();
                    // AI Processing
                    Column.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Export - glass card
                        Column.create({ space: 8 });
                        // Export - glass card
                        Column.width('100%');
                        // Export - glass card
                        Column.padding(14);
                        // Export - glass card
                        Column.borderRadius(14);
                        // Export - glass card
                        Column.backgroundColor(RokuricsColors.glassSurface + 'A8');
                        // Export - glass card
                        Column.border({
                            width: 1,
                            color: {
                                colors: [
                                    [0xFFFFFF, 0.34],
                                    [0xEFFAF8, 0.12],
                                    [0x91E8D6, 0.08]
                                ],
                                direction: GradientDirection.RightBottom
                            },
                            radius: 14
                        } as BorderOptions);
                        // Export - glass card
                        Column.shadow({
                            color: RokuricsColors.shadowColor + '06',
                            radius: 8,
                            offsetY: 4
                        });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('导出');
                        Text.fontSize(15);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(RokuricsColors.deepText);
                        Text.width('100%');
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create({ space: 8 });
                        Row.width('100%');
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 14, right: 14, top: 6, bottom: 6 });
                        Button.borderRadius(14);
                        Button.backgroundColor(this.exportFormat === 'Markdown' ? RokuricsColors.aqua : RokuricsColors.glassSurface + '50');
                        Button.onClick(() => { this.exportFormat = 'Markdown'; });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('Markdown');
                        Text.fontSize(13);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(this.exportFormat === 'Markdown' ? Color.White : RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 14, right: 14, top: 6, bottom: 6 });
                        Button.borderRadius(14);
                        Button.backgroundColor(this.exportFormat === 'JSON' ? RokuricsColors.aqua : RokuricsColors.glassSurface + '50');
                        Button.onClick(() => { this.exportFormat = 'JSON'; });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('JSON');
                        Text.fontSize(13);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(this.exportFormat === 'JSON' ? Color.White : RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    Row.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create({ space: 8 });
                        Row.width('100%');
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 14, right: 14, top: 7, bottom: 7 });
                        Button.borderRadius(8);
                        Button.backgroundColor(RokuricsColors.aqua);
                        Button.enabled(!this.isExporting);
                        Button.onClick(() => this.exportRecording());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.isExporting ? '导出中...' : '导出录音信息');
                        Text.fontSize(13);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(Color.White);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 14, right: 14, top: 7, bottom: 7 });
                        Button.border({ width: 1, color: RokuricsColors.mint + '40', radius: 8 });
                        Button.backgroundColor(Color.Transparent);
                        Button.enabled(!this.isExporting);
                        Button.onClick(() => this.exportAll());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('导出全部');
                        Text.fontSize(13);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(RokuricsColors.mint);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 14, right: 14, top: 7, bottom: 7 });
                        Button.border({ width: 1, color: RokuricsColors.coral + '40', radius: 8 });
                        Button.backgroundColor(Color.Transparent);
                        Button.onClick(() => this.loadExportList());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('查看导出');
                        Text.fontSize(13);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(RokuricsColors.coral);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    Row.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.exportMessage.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.exportMessage);
                                    Text.fontSize(12);
                                    Text.fontColor(this.exportMessage.startsWith('已导出') ? RokuricsColors.mint : RokuricsColors.coral);
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
                        if (this.showExportList && this.exportList.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Column.create({ space: 4 });
                                    Column.width('100%');
                                    Column.padding(8);
                                    Column.borderRadius(8);
                                    Column.backgroundColor(RokuricsColors.glassSurface + '30');
                                }, Column);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Row.create();
                                    Row.width('100%');
                                }, Row);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create('已导出文件');
                                    Text.fontSize(12);
                                    Text.fontColor(RokuricsColors.softText);
                                }, Text);
                                Text.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Blank.create();
                                }, Blank);
                                Blank.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(`${this.exportList.length} 个`);
                                    Text.fontSize(11);
                                    Text.fontColor(RokuricsColors.tertiaryText);
                                }, Text);
                                Text.pop();
                                Row.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    List.create({ space: 2 });
                                    List.constraintSize({ maxHeight: 120 });
                                    List.scrollBar(BarState.Off);
                                }, List);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    ForEach.create();
                                    const forEachItemGenFunction = _item => {
                                        const file = _item;
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
                                                    Row.padding({ left: 10, right: 10, top: 6, bottom: 6 });
                                                    Row.borderRadius(6);
                                                    Row.backgroundColor(RokuricsColors.glassSurface + '40');
                                                }, Row);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Text.create(file);
                                                    Text.fontSize(12);
                                                    Text.fontColor(RokuricsColors.deepText);
                                                    Text.maxLines(1);
                                                    Text.textOverflow({ overflow: TextOverflow.Ellipsis });
                                                    Text.layoutWeight(1);
                                                }, Text);
                                                Text.pop();
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Button.createWithChild();
                                                    Button.width(28);
                                                    Button.height(28);
                                                    Button.backgroundColor(Color.Transparent);
                                                    Button.onClick(() => this.shareExportedFile(file));
                                                }, Button);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Text.create('↗');
                                                    Text.fontSize(12);
                                                    Text.fontColor(RokuricsColors.aqua);
                                                }, Text);
                                                Text.pop();
                                                Button.pop();
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Button.createWithChild();
                                                    Button.width(28);
                                                    Button.height(28);
                                                    Button.backgroundColor(Color.Transparent);
                                                    Button.onClick(() => this.deleteExportedFile(file));
                                                }, Button);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Text.create('✕');
                                                    Text.fontSize(12);
                                                    Text.fontColor(RokuricsColors.coral);
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
                                    this.forEachUpdateFunction(elmtId, this.exportList, forEachItemGenFunction);
                                }, ForEach);
                                ForEach.pop();
                                List.pop();
                                Column.pop();
                            });
                        }
                        else {
                            this.ifElseBranchUpdateFunction(1, () => {
                            });
                        }
                    }, If);
                    If.pop();
                    // Export - glass card
                    Column.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Actions - glass card
                        Row.create({ space: 12 });
                        // Actions - glass card
                        Row.width('100%');
                        // Actions - glass card
                        Row.justifyContent(FlexAlign.End);
                        // Actions - glass card
                        Row.margin({ top: 12, bottom: 40 });
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 16, right: 16, top: 8, bottom: 8 });
                        Button.border({ width: 1, color: RokuricsColors.aqua + '40', radius: 10 });
                        Button.backgroundColor(Color.Transparent);
                        Button.onClick(() => this.togglePlayback());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.isPlaying ? '⏸ 暂停' : '▶ 播放');
                        Text.fontSize(14);
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.playbackError.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.playbackError);
                                    Text.fontSize(11);
                                    Text.fontColor(RokuricsColors.coral);
                                    Text.maxLines(1);
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
                        Button.createWithChild();
                        Button.padding({ left: 16, right: 16, top: 8, bottom: 8 });
                        Button.border({ width: 1, color: RokuricsColors.aqua + '40', radius: 10 });
                        Button.backgroundColor(Color.Transparent);
                        Button.enabled(!this.isUploading);
                        Button.onClick(() => this.uploadRecording());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.isUploading ? (this.uploadProgressText || '上传中...') : '☁ 上传');
                        Text.fontSize(14);
                        Text.fontColor(this.recording?.uploadStatus === 'uploaded' ? RokuricsColors.mint : RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.uploadError.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.uploadError);
                                    Text.fontSize(11);
                                    Text.fontColor(RokuricsColors.coral);
                                    Text.maxLines(1);
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
                        Blank.create();
                    }, Blank);
                    Blank.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 16, right: 16, top: 8, bottom: 8 });
                        Button.border({ width: 1, color: RokuricsColors.aqua + '40', radius: 10 });
                        Button.backgroundColor(Color.Transparent);
                        Button.onClick(() => this.openRenameDialog());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('重命名');
                        Text.fontSize(14);
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 16, right: 16, top: 8, bottom: 8 });
                        Button.border({ width: 1, color: RokuricsColors.coral + '40', radius: 10 });
                        Button.backgroundColor(Color.Transparent);
                        Button.onClick(() => this.deleteAndBack());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('删除');
                        Text.fontSize(14);
                        Text.fontColor(RokuricsColors.coral);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    // Actions - glass card
                    Row.pop();
                    Column.pop();
                    Scroll.pop();
                });
            }
        }, If);
        If.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Rename dialog
            if (this.showRenameDialog) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create();
                        Column.width('100%');
                        Column.height('100%');
                        Column.justifyContent(FlexAlign.Center);
                        Column.backgroundColor('#00000050');
                        Column.position({ x: 0, y: 0 });
                        Column.onClick(() => { this.showRenameDialog = false; });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 16 });
                        Column.padding(24);
                        Column.borderRadius(20);
                        Column.backgroundColor(Color.White);
                        Column.width('85%');
                        Column.shadow({ radius: 30, color: '#00000020' });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('重命名录音');
                        Text.fontSize(18);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(RokuricsColors.deepText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: this.renameText, placeholder: '输入新标题' });
                        TextInput.fontSize(16);
                        TextInput.fontColor(RokuricsColors.deepText);
                        TextInput.backgroundColor(RokuricsColors.glassSurface + '80');
                        TextInput.borderRadius(10);
                        TextInput.padding(14);
                        TextInput.onChange((value: string) => { this.renameText = value; });
                    }, TextInput);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create({ space: 12 });
                        Row.width('100%');
                        Row.justifyContent(FlexAlign.End);
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.backgroundColor(Color.Transparent);
                        Button.onClick(() => { this.showRenameDialog = false; });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('取消');
                        Text.fontSize(14);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.padding({ left: 24, right: 24, top: 10, bottom: 10 });
                        Button.borderRadius(10);
                        Button.backgroundColor(RokuricsColors.aqua);
                        Button.onClick(() => this.handleRename());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('保存');
                        Text.fontSize(14);
                        Text.fontColor(Color.White);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    Row.pop();
                    Column.pop();
                    Column.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        Stack.pop();
    }
    private async runTranscription(): Promise<void> {
        if (!this.recording || this.isProcessing)
            return;
        this.isProcessing = true;
        this.processingError = '';
        this.processingStage = '正在转写...';
        try {
            if (!this.pipeline) {
                this.pipeline = new TranscriptionPipeline(getContext(this));
            }
            const audioPath = this.recordingManager.getAudioAbsolutePath(this.recording);
            await this.pipeline.runTranscription(this.recording, audioPath, true);
            this.hasTranscript = true;
            this.processingStage = '转写完成';
        }
        catch (err) {
            this.processingError = `转写失败: ${err}`;
        }
        finally {
            this.isProcessing = false;
        }
    }
    private async runFullProcessing(): Promise<void> {
        if (!this.recording || this.isProcessing)
            return;
        this.isProcessing = true;
        this.processingError = '';
        this.processingStage = '处理中...';
        try {
            if (!this.pipeline) {
                this.pipeline = new TranscriptionPipeline(getContext(this));
            }
            const audioPath = this.recordingManager.getAudioAbsolutePath(this.recording);
            await this.pipeline.runFullPipeline(this.recording, audioPath, true);
            if (this.pipeline.state.transcript) {
                this.hasTranscript = true;
            }
            if (this.pipeline.state.note) {
                this.hasNote = true;
            }
            this.processingStage = '处理完成';
        }
        catch (err) {
            this.processingError = `处理失败: ${err}`;
        }
        finally {
            this.isProcessing = false;
        }
    }
    private async loadTranscript(): Promise<void> {
        if (!this.recording || !this.pipeline)
            return;
        try {
            const store = this.pipeline.getTranscriptStore();
            const markdown = await store.loadTranscriptMarkdown(this.recording.id, this.recording.createdAt);
            if (markdown && markdown.length > 0) {
                this.transcriptText = markdown;
                this.hasTranscript = true;
            }
        }
        catch {
            this.transcriptText = '(无法加载转写)';
        }
    }
    private async loadNote(): Promise<void> {
        if (!this.recording || !this.pipeline)
            return;
        try {
            const store = this.pipeline.getTranscriptStore();
            const markdown = await store.loadNoteMarkdown(this.recording.id, this.recording.createdAt);
            if (markdown && markdown.length > 0) {
                this.noteMarkdown = markdown;
                this.hasNote = true;
            }
        }
        catch {
            this.noteMarkdown = '(无法加载笔记)';
        }
    }
    private async exportRecording(): Promise<void> {
        if (!this.recording || !this.exportManager || this.isExporting)
            return;
        this.isExporting = true;
        this.exportMessage = '';
        try {
            await this.exportManager.exportRecordingInfo(this.recording, this.exportFormat);
            this.exportMessage = `已导出为 ${this.exportFormat}`;
        }
        catch (err) {
            this.exportMessage = `导出失败: ${err}`;
        }
        finally {
            this.isExporting = false;
        }
    }
    private async exportAll(): Promise<void> {
        if (!this.recording || !this.exportManager || !this.pipeline || this.isExporting)
            return;
        this.isExporting = true;
        this.exportMessage = '';
        try {
            const store = this.pipeline.getTranscriptStore();
            const transcript = await store.loadTranscript(this.recording.id, this.recording.createdAt);
            const paths = await this.exportManager.exportAllForRecording(this.recording, transcript, null);
            this.exportMessage = `已导出 ${paths.length} 个文件`;
        }
        catch (err) {
            this.exportMessage = `批量导出失败: ${err}`;
        }
        finally {
            this.isExporting = false;
        }
    }
    private async uploadRecording(): Promise<void> {
        if (!this.recording || this.isUploading)
            return;
        this.isUploading = true;
        this.uploadProgressText = '准备上传...';
        this.uploadError = '';
        try {
            const settingsStore = new SettingsStore(getContext(this));
            const config = await RecordingUploadClient.loadConfig(settingsStore);
            if (!config.serverURL || config.serverURL === 'http://localhost:8000') {
                this.uploadError = '请先在设置中配置上传服务器地址';
                return;
            }
            this.uploadClient = new RecordingUploadClient(config);
            this.uploadClient.onProgress((p) => {
                this.uploadProgressText = p.description;
            });
            const audioPath = this.recordingManager.getAudioAbsolutePath(this.recording);
            const result = await this.uploadClient.uploadRecording(this.recording, audioPath);
            if (result.ok) {
                this.uploadProgressText = '上传完成';
                await this.recordingManager.reloadRecordings();
                await this.loadData();
            }
            else {
                this.uploadError = result.error ?? '上传失败';
            }
        }
        catch (err) {
            this.uploadError = `上传错误: ${err}`;
        }
        finally {
            this.isUploading = false;
        }
    }
    private async loadExportList(): Promise<void> {
        if (!this.exportManager)
            return;
        this.showExportList = !this.showExportList;
        if (this.showExportList) {
            this.exportList = await this.exportManager.listExports();
        }
    }
    private async deleteExportedFile(filename: string): Promise<void> {
        if (!this.exportManager)
            return;
        await this.exportManager.deleteExport(filename);
        this.exportList = await this.exportManager.listExports();
    }
    private async shareExportedFile(filename: string): Promise<void> {
        if (!this.exportManager)
            return;
        const fullPath = `${this.exportManager.getExportsDir()}/${filename}`;
        try {
            if (!fileIo.accessSync(fullPath)) {
                this.exportMessage = '文件不存在';
                return;
            }
            const mimeType = filename.endsWith('.json') ? 'application/json' : 'text/markdown';
            const want: Want = {
                action: 'ohos.want.action.sendData',
                uri: `file://${fullPath}`,
                type: mimeType,
                parameters: {
                    'ability.params.stream': [fullPath]
                }
            };
            const ctx = getContext(this) as common.UIAbilityContext;
            await ctx.startAbility(want);
            this.exportMessage = `已分享: ${filename}`;
        }
        catch (err) {
            this.exportMessage = `分享失败: ${err}`;
        }
    }
    aboutToDisappear(): void {
        this.releasePlayer();
    }
    InfoRow(label: string, value: string, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
            Row.padding(14);
            Row.borderRadius(12);
            Row.backgroundColor(RokuricsColors.glassSurface + '66');
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(label);
            Text.fontSize(13);
            Text.fontColor(RokuricsColors.softText);
            Text.width(70);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(value);
            Text.fontSize(16);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
            Text.layoutWeight(1);
        }, Text);
        Text.pop();
        Row.pop();
    }
    TechRow(label: string, value: string, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(label);
            Text.fontSize(12);
            Text.fontColor(RokuricsColors.tertiaryText);
            Text.width(60);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(value);
            Text.fontSize(12);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        Row.pop();
    }
    FilingEditRow(label: string, selectedValue: string, options: string[], onSelect: (v: string) => void, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 4 });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(label);
            Text.fontSize(12);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 6 });
            Row.width('100%');
            Row.flexShrink(0);
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            ForEach.create();
            const forEachItemGenFunction = _item => {
                const option = _item;
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(option);
                    Text.fontSize(12);
                    Text.fontWeight(FontWeight.Medium);
                    Text.fontColor(selectedValue === option ? Color.White : RokuricsColors.softText);
                    Text.padding({ left: 10, right: 10, top: 5, bottom: 5 });
                    Text.borderRadius(12);
                    Text.backgroundColor(selectedValue === option ? RokuricsColors.aqua : RokuricsColors.glassSurface + '50');
                    Text.onClick(() => onSelect(option));
                }, Text);
                Text.pop();
            };
            this.forEachUpdateFunction(elmtId, options, forEachItemGenFunction);
        }, ForEach);
        ForEach.pop();
        Row.pop();
        Column.pop();
    }
    private uploadLabel(s: string): string {
        if (s === 'localOnly')
            return '未上传';
        if (s === 'uploading')
            return '上传中';
        if (s === 'uploaded')
            return '已上传';
        if (s === 'failed')
            return '失败';
        return s;
    }
    private transcriptLabel(s: string): string {
        if (s === 'notStarted')
            return '未开始';
        if (s === 'transcribing')
            return '转写中';
        if (s === 'transcribed')
            return '已完成';
        if (s === 'failed')
            return '失败';
        return s;
    }
    private noteLabel(s: string): string {
        if (s === 'notStarted')
            return '未开始';
        if (s === 'generating')
            return '生成中';
        if (s === 'generated')
            return '已生成';
        if (s === 'failed')
            return '失败';
        return s;
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "RecordingDetailPage";
    }
}
registerNamedRoute(() => new RecordingDetailPage(undefined, {}), "", { bundleName: "com.vita0818.rokurics", moduleName: "entry", pagePath: "pages/RecordingDetailPage", pageFullPath: "entry/src/main/ets/pages/RecordingDetailPage", integratedHsp: "false", moduleType: "followWithHap" });
