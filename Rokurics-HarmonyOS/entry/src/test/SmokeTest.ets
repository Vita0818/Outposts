/**
 * Smoke test skeleton - basic model and service sanity checks
 */
import { describe, it, expect } from '@ohos/hypium'
import { RecordingMetadata, StudyFilingPath } from '../main/ets/models/RecordingModels'
import { TranscriptionResult, NoteGenerationResult } from '../main/ets/models/ProcessingModels'
import { ChatMessage, ChatMessageRole, ChatConversation } from '../main/ets/models/ChatModels'
import { formatClock, formatDuration, formatDate, fileSizeText } from '../main/ets/utils/FormatHelpers'

export default function smokeSuite() {
  describe('SmokeTest_Models', () => {
    it('RecordingMetadata_create_defaults', () => {
      const m = RecordingMetadata.create({
        id: 'test-1',
        title: 'Test Recording',
        fileName: 'test.m4a',
        relativeAudioPath: 'audio/test.m4a',
        relativeMetadataPath: 'meta/test.json',
        createdAt: new Date(),
        endedAt: new Date(),
        duration: 120,
        format: 'm4a',
        codec: 'aac',
        sampleRate: 44100,
        channels: 1,
        bitrate: 128000,
        fileSize: 1024000
      })
      expect(m.id).assertEqual('test-1')
      expect(m.title).assertEqual('Test Recording')
      expect(m.uploadStatus).assertEqual('localOnly')
      expect(m.transcriptionStatus).assertEqual('notStarted')
    })

    it('StudyFilingPath_displaySummary', () => {
      const p = new StudyFilingPath('课堂录音', '数学', '第三章', '积分')
      expect(p.displaySummary).assertEqual('课堂录音 / 数学 / 第三章 / 积分')
      expect(p.isEmpty).assertEqual(false)
    })

    it('StudyFilingPath_empty', () => {
      const p = new StudyFilingPath()
      expect(p.isEmpty).assertEqual(true)
      expect(p.displaySummary).assertEqual('未分类')
    })
  })

  describe('SmokeTest_ProcessingModels', () => {
    it('TranscriptionResult_create', () => {
      const r = TranscriptionResult.create({
        taskID: 'task-1',
        recordingID: 'rec-1',
        providerID: 'mock',
        providerName: 'Mock',
        text: '测试转写内容',
        status: 'transcribed'
      })
      expect(r.text).assertEqual('测试转写内容')
      expect(r.status).assertEqual('transcribed')
      expect(r.segments.length).assertEqual(0)
    })

    it('NoteGenerationResult_create', () => {
      const r = NoteGenerationResult.create({
        taskID: 'task-2',
        recordingID: 'rec-1',
        providerID: 'mock',
        providerName: 'Mock',
        markdown: '# 笔记',
        status: 'generated'
      })
      expect(r.markdown).assertEqual('# 笔记')
      expect(r.status).assertEqual('generated')
      expect(r.modelOutputWasTruncated).assertEqual(false)
    })
  })

  describe('SmokeTest_ChatModels', () => {
    it('ChatMessage_create', () => {
      const m = new ChatMessage(ChatMessageRole.USER, '你好')
      expect(m.role).assertEqual(ChatMessageRole.USER)
      expect(m.content).assertEqual('你好')
    })

    it('ChatConversation_defaults', () => {
      const c = new ChatConversation()
      expect(c.title).assertEqual('新对话')
      expect(c.messages.length).assertEqual(0)
    })
  })

  describe('SmokeTest_FormatHelpers', () => {
    it('formatClock_zero', () => {
      expect(formatClock(0)).assertEqual('00:00')
    })

    it('formatClock_minutes', () => {
      expect(formatClock(125)).assertEqual('02:05')
    })

    it('formatDuration_seconds', () => {
      expect(formatDuration(30)).assertEqual('30 sec')
    })

    it('formatDuration_minutes', () => {
      expect(formatDuration(150)).assertEqual('2.5 min')
    })

    it('formatDate', () => {
      const d = new Date('2026-05-26T10:30:00')
      expect(formatDate(d)).assertEqual('2026-05-26')
    })

    it('fileSizeText_bytes', () => {
      expect(fileSizeText(500)).assertEqual('500 B')
    })

    it('fileSizeText_kb', () => {
      expect(fileSizeText(1024 * 100)).assertEqual('100.0 KB')
    })

    it('fileSizeText_mb', () => {
      expect(fileSizeText(1024 * 1024 * 3)).assertEqual('3.0 MB')
    })
  })
}
