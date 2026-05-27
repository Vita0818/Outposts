/**
 * Core recording metadata models
 */
export class RecordingMetadata {
    id: string = '';
    title: string = '';
    fileName: string = '';
    relativeAudioPath: string = '';
    relativeMetadataPath: string = '';
    createdAt: Date = new Date();
    endedAt: Date = new Date();
    duration: number = 0;
    format: string = '';
    codec: string = '';
    sampleRate: number = 0;
    channels: number = 0;
    bitrate: number = 0;
    fileSize: number = 0;
    uploadStatus: string = 'localOnly';
    transcriptionStatus: string = 'notStarted';
    noteStatus: string = 'notStarted';
    tags: string[] = [];
    studyFiling: StudyFilingPath | null = null;
    uploadProgressFraction: number | null = null;
    uploadProgressConfirmedBytes: number | null = null;
    uploadProgressTotalBytes: number | null = null;
    uploadPhase: string | null = null;
    uploadProgressDescription: string | null = null;
    isDeleted: boolean = false;
    deletedAt: Date | null = null;
    static create(fields: RecordingMetadataFields): RecordingMetadata {
        const m = new RecordingMetadata();
        m.id = fields.id;
        m.title = fields.title;
        m.fileName = fields.fileName;
        m.relativeAudioPath = fields.relativeAudioPath;
        m.relativeMetadataPath = fields.relativeMetadataPath;
        m.createdAt = fields.createdAt;
        m.endedAt = fields.endedAt;
        m.duration = fields.duration;
        m.format = fields.format;
        m.codec = fields.codec;
        m.sampleRate = fields.sampleRate;
        m.channels = fields.channels;
        m.bitrate = fields.bitrate;
        m.fileSize = fields.fileSize;
        m.uploadStatus = fields.uploadStatus ?? 'localOnly';
        m.transcriptionStatus = fields.transcriptionStatus ?? 'notStarted';
        m.noteStatus = fields.noteStatus ?? 'notStarted';
        m.tags = fields.tags ?? [];
        m.studyFiling = fields.studyFiling ?? null;
        m.isDeleted = fields.isDeleted ?? false;
        m.deletedAt = fields.deletedAt ?? null;
        m.uploadProgressFraction = fields.uploadProgressFraction ?? null;
        m.uploadProgressConfirmedBytes = fields.uploadProgressConfirmedBytes ?? null;
        m.uploadProgressTotalBytes = fields.uploadProgressTotalBytes ?? null;
        m.uploadPhase = fields.uploadPhase ?? null;
        m.uploadProgressDescription = fields.uploadProgressDescription ?? null;
        return m;
    }
    copyWithTitle(title: string): RecordingMetadata {
        const m = this.copy();
        m.title = title;
        return m;
    }
    copyWithUploadStatus(status: string): RecordingMetadata {
        const m = this.copy();
        m.uploadStatus = status;
        return m;
    }
    copyWithTrashState(isDeleted: boolean, deletedAt: Date | null): RecordingMetadata {
        const m = this.copy();
        m.isDeleted = isDeleted;
        m.deletedAt = deletedAt;
        return m;
    }
    copyWithStudyFiling(studyFiling: StudyFilingPath | null): RecordingMetadata {
        const m = this.copy();
        m.studyFiling = studyFiling;
        return m;
    }
    recoveringStaleUploadingStatus(): RecordingMetadata {
        if (this.uploadStatus !== 'uploading')
            return this;
        return this.copyWithUploadStatus(RecordingUploadStatus.FAILED);
    }
    private copy(): RecordingMetadata {
        const m = new RecordingMetadata();
        m.id = this.id;
        m.title = this.title;
        m.fileName = this.fileName;
        m.relativeAudioPath = this.relativeAudioPath;
        m.relativeMetadataPath = this.relativeMetadataPath;
        m.createdAt = this.createdAt;
        m.endedAt = this.endedAt;
        m.duration = this.duration;
        m.format = this.format;
        m.codec = this.codec;
        m.sampleRate = this.sampleRate;
        m.channels = this.channels;
        m.bitrate = this.bitrate;
        m.fileSize = this.fileSize;
        m.uploadStatus = this.uploadStatus;
        m.transcriptionStatus = this.transcriptionStatus;
        m.noteStatus = this.noteStatus;
        m.tags = [...this.tags];
        m.studyFiling = this.studyFiling;
        m.isDeleted = this.isDeleted;
        m.deletedAt = this.deletedAt;
        m.uploadProgressFraction = this.uploadProgressFraction;
        m.uploadProgressConfirmedBytes = this.uploadProgressConfirmedBytes;
        m.uploadProgressTotalBytes = this.uploadProgressTotalBytes;
        m.uploadPhase = this.uploadPhase;
        m.uploadProgressDescription = this.uploadProgressDescription;
        return m;
    }
    static defaultTitle(createdAt: Date): string {
        const y = createdAt.getFullYear();
        const m = String(createdAt.getMonth() + 1).padStart(2, '0');
        const d = String(createdAt.getDate()).padStart(2, '0');
        const h = String(createdAt.getHours()).padStart(2, '0');
        const min = String(createdAt.getMinutes()).padStart(2, '0');
        return `录音 ${y}-${m}-${d} ${h}:${min}`;
    }
    static fromJSON(json: Record<string, Object>): RecordingMetadata {
        const m = new RecordingMetadata();
        m.id = (json['id'] as string) ?? '';
        m.title = (json['title'] as string) ?? '';
        m.fileName = (json['fileName'] as string) ?? '';
        m.relativeAudioPath = (json['relativeAudioPath'] as string) ?? '';
        m.relativeMetadataPath = (json['relativeMetadataPath'] as string) ?? '';
        m.createdAt = new Date(json['createdAt'] as string);
        m.endedAt = new Date(json['endedAt'] as string);
        m.duration = (json['duration'] as number) ?? 0;
        m.format = (json['format'] as string) ?? '';
        m.codec = (json['codec'] as string) ?? '';
        m.sampleRate = (json['sampleRate'] as number) ?? 0;
        m.channels = (json['channels'] as number) ?? 0;
        m.bitrate = (json['bitrate'] as number) ?? 0;
        m.fileSize = (json['fileSize'] as number) ?? 0;
        m.uploadStatus = (json['uploadStatus'] as string) ?? 'localOnly';
        m.transcriptionStatus = (json['transcriptionStatus'] as string) ?? 'notStarted';
        m.noteStatus = (json['noteStatus'] as string) ?? 'notStarted';
        m.tags = (json['tags'] as string[]) ?? [];
        m.isDeleted = (json['isDeleted'] as boolean) ?? false;
        if (json['studyFiling']) {
            m.studyFiling = StudyFilingPath.fromJSON(json['studyFiling'] as Record<string, string>);
        }
        if (json['deletedAt']) {
            m.deletedAt = new Date(json['deletedAt'] as string);
        }
        return m;
    }
}
export interface RecordingMetadataFields {
    id: string;
    title: string;
    fileName: string;
    relativeAudioPath: string;
    relativeMetadataPath: string;
    createdAt: Date;
    endedAt: Date;
    duration: number;
    format: string;
    codec: string;
    sampleRate: number;
    channels: number;
    bitrate: number;
    fileSize: number;
    uploadStatus?: string;
    transcriptionStatus?: string;
    noteStatus?: string;
    tags?: string[];
    studyFiling?: StudyFilingPath | null;
    uploadProgressFraction?: number | null;
    uploadProgressConfirmedBytes?: number | null;
    uploadProgressTotalBytes?: number | null;
    uploadPhase?: string | null;
    uploadProgressDescription?: string | null;
    isDeleted?: boolean;
    deletedAt?: Date | null;
}
export enum RecordingUploadStatus {
    LOCAL_ONLY = "localOnly",
    UPLOADING = "uploading",
    UPLOADED = "uploaded",
    FAILED = "failed"
}
export enum RecordingState {
    IDLE = "idle",
    REQUESTING_PERMISSION = "requestingPermission",
    CONFIGURING_SESSION = "configuringSession",
    RECORDING = "recording",
    PAUSED = "paused",
    STOPPING = "stopping",
    FILING = "filing",
    SAVING = "saving",
    SAVED = "saved",
    PERMISSION_DENIED = "permissionDenied",
    FAILED = "failed"
}
export class StudyFilingPath {
    type: string | null = null;
    subject: string | null = null;
    chapter: string | null = null;
    topic: string | null = null;
    constructor(type?: string | null, subject?: string | null, chapter?: string | null, topic?: string | null) {
        this.type = StudyFilingPath.normalized(type);
        this.subject = StudyFilingPath.normalized(subject);
        this.chapter = StudyFilingPath.normalized(chapter);
        this.topic = StudyFilingPath.normalized(topic);
    }
    get isEmpty(): boolean {
        return !this.type && !this.subject && !this.chapter && !this.topic;
    }
    get displaySummary(): string {
        const parts = [this.type, this.subject, this.chapter, this.topic].filter(Boolean);
        const filtered: string[] = [];
        for (const p of parts) {
            if (p)
                filtered.push(p);
        }
        return filtered.length > 0 ? filtered.join(' / ') : '未分类';
    }
    valueForLevel(level: string): string | null {
        if (level === 'type')
            return this.type;
        if (level === 'subject')
            return this.subject;
        if (level === 'chapter')
            return this.chapter;
        if (level === 'topic')
            return this.topic;
        return null;
    }
    suggestedTitle(defaultTitle: string): string {
        const parts: string[] = [];
        if (this.subject)
            parts.push(this.subject);
        if (this.chapter)
            parts.push(this.chapter);
        if (this.topic)
            parts.push(this.topic);
        if (parts.length > 0)
            return parts.join(' · ');
        return this.type ?? defaultTitle;
    }
    static normalized(value?: string | null): string | null {
        if (!value)
            return null;
        const trimmed = value.trim();
        return trimmed.length > 0 ? trimmed : null;
    }
    clone(): StudyFilingPath {
        return new StudyFilingPath(this.type, this.subject, this.chapter, this.topic);
    }
    static fromJSON(json: Record<string, string>): StudyFilingPath {
        return new StudyFilingPath(json['type'], json['subject'], json['chapter'], json['topic']);
    }
    static readonly UNCategorized = '未分类';
    static readonly MISSING = '未填写';
}
export const STUDY_FILING_LEVELS: string[] = ['type', 'subject', 'chapter', 'topic'];
export function filingLevelTitle(level: string): string {
    if (level === 'type')
        return '门类';
    if (level === 'subject')
        return '课程';
    if (level === 'chapter')
        return '章节';
    if (level === 'topic')
        return '主题';
    return '文件夹';
}
