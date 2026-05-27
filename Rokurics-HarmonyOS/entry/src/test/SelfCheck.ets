/**
 * Self-check validation script
 * Verifies model integrity and service structure without full test framework.
 * Run via: hvigorw test or DevEco Studio test runner.
 * Falls back to console-based validation if test runner unavailable.
 */
import { RecordingMetadata, StudyFilingPath, RecordingUploadStatus } from '../main/ets/models/RecordingModels'
import { TranscriptionResult, NoteGenerationResult, TranscriptionSegment } from '../main/ets/models/ProcessingModels'
import { ChatMessage, ChatMessageRole, ChatConversation, ChatResult, ChatContext, ChatContextItem } from '../main/ets/models/ChatModels'
import { UserProfile } from '../main/ets/models/UserProfile'
import { formatClock, formatDuration, formatDate, fileSizeText } from '../main/ets/utils/FormatHelpers'

const PASS = '✓'
const FAIL = '✗'

function check(name: string, fn: () => boolean): boolean {
  const ok = fn()
  console.info(`  ${ok ? PASS : FAIL} ${name}`)
  return ok
}

export default function selfCheck(): { passed: number, failed: number } {
  console.info('=== Rokurics Self-Check ===')
  let passed = 0
  let failed = 0
  const pass = (n: string, f: () => boolean) => {
    if (check(n, f)) passed++ else failed++
  }

  // ── RecordingMetadata ──
  pass('RecordingMetadata.create defaults', () => {
    const m = RecordingMetadata.create({
      id: 'sc-1', title: 'Test', fileName: 't.m4a',
      relativeAudioPath: 'a/t.m4a', relativeMetadataPath: 'm/t.json',
      createdAt: new Date(), endedAt: new Date(), duration: 60,
      format: 'm4a', codec: 'aac', sampleRate: 16000,
      channels: 1, bitrate: 64000, fileSize: 480000
    })
    return m.id === 'sc-1' && m.uploadStatus === 'localOnly'
  })

  pass('RecordingMetadata.copyWithTitle', () => {
    const m = RecordingMetadata.create({
      id: 'sc-2', title: 'Orig', fileName: 'x.m4a',
      relativeAudioPath: 'a/x.m4a', relativeMetadataPath: 'm/x.json',
      createdAt: new Date(), endedAt: new Date(), duration: 10,
      format: 'm4a', codec: 'aac', sampleRate: 8000,
      channels: 1, bitrate: 32000, fileSize: 40000
    })
    const renamed = m.copyWithTitle('Renamed')
    return renamed.title === 'Renamed' && renamed.id === 'sc-2'
  })

  pass('RecordingMetadata.copyWithStudyFiling', () => {
    const m = RecordingMetadata.create({
      id: 'sc-3', title: 'T', fileName: 'y.m4a',
      relativeAudioPath: 'a/y.m4a', relativeMetadataPath: 'm/y.json',
      createdAt: new Date(), endedAt: new Date(), duration: 5,
      format: 'm4a', codec: 'aac', sampleRate: 8000,
      channels: 1, bitrate: 32000, fileSize: 20000
    })
    const f = new StudyFilingPath('数学')
    const updated = m.copyWithStudyFiling(f)
    return updated.studyFiling?.type === '数学'
  })

  pass('RecordingMetadata.recoveringStaleUploadingStatus', () => {
    const m = RecordingMetadata.create({
      id: 'sc-4', title: 'U', fileName: 'u.m4a',
      relativeAudioPath: 'a/u.m4a', relativeMetadataPath: 'm/u.json',
      createdAt: new Date(), endedAt: new Date(), duration: 1,
      format: 'm4a', codec: 'aac', sampleRate: 8000,
      channels: 1, bitrate: 32000, fileSize: 8000,
      uploadStatus: 'uploading'
    })
    const recovered = m.recoveringStaleUploadingStatus()
    return recovered.uploadStatus === 'failed'
  })

  pass('RecordingMetadata.defaultTitle format', () => {
    const d = new Date('2026-05-26T14:30:00')
    const t = RecordingMetadata.defaultTitle(d)
    return t.startsWith('录音 2026-05-26')
  })

  pass('RecordingMetadata.fromJSON roundtrip', () => {
    const orig = RecordingMetadata.create({
      id: 'sc-5', title: 'JSON Test', fileName: 'j.m4a',
      relativeAudioPath: 'a/j.m4a', relativeMetadataPath: 'm/j.json',
      createdAt: new Date('2026-05-26T10:00:00Z'), endedAt: new Date('2026-05-26T10:05:00Z'),
      duration: 300, format: 'm4a', codec: 'aac', sampleRate: 16000,
      channels: 2, bitrate: 128000, fileSize: 1920000,
      tags: ['tag1', 'tag2']
    })
    const jsonObj: Record<string, Object> = {}
    jsonObj['id'] = orig.id
    jsonObj['title'] = orig.title
    jsonObj['fileName'] = orig.fileName
    jsonObj['relativeAudioPath'] = orig.relativeAudioPath
    jsonObj['relativeMetadataPath'] = orig.relativeMetadataPath
    jsonObj['createdAt'] = orig.createdAt.toISOString()
    jsonObj['endedAt'] = orig.endedAt.toISOString()
    jsonObj['duration'] = orig.duration
    jsonObj['format'] = orig.format
    jsonObj['codec'] = orig.codec
    jsonObj['sampleRate'] = orig.sampleRate
    jsonObj['channels'] = orig.channels
    jsonObj['bitrate'] = orig.bitrate
    jsonObj['fileSize'] = orig.fileSize
    jsonObj['tags'] = orig.tags
    const restored = RecordingMetadata.fromJSON(jsonObj)
    return restored.id === orig.id && restored.title === orig.title && restored.duration === 300
  })

  // ── StudyFilingPath ──
  pass('StudyFilingPath.displaySummary', () => {
    const p = new StudyFilingPath('课堂录音', '数学', null, null)
    return p.displaySummary === '课堂录音 / 数学' && !p.isEmpty
  })

  pass('StudyFilingPath.isEmpty', () => {
    return new StudyFilingPath().isEmpty === true
  })

  pass('StudyFilingPath.valueForLevel', () => {
    const p = new StudyFilingPath('课程', '物理', '力学', '牛顿定律')
    return p.valueForLevel('type') === '课程' &&
      p.valueForLevel('subject') === '物理' &&
      p.valueForLevel('chapter') === '力学' &&
      p.valueForLevel('topic') === '牛顿定律' &&
      p.valueForLevel('nonexistent') === null
  })

  pass('StudyFilingPath.suggestedTitle', () => {
    const p = new StudyFilingPath(null, '化学', '有机', '烷烃')
    const title = p.suggestedTitle('默认标题')
    return title === '化学 · 有机 · 烷烃'
  })

  pass('StudyFilingPath.suggestedTitle fallback', () => {
    const p = new StudyFilingPath('课堂录音', null, null, null)
    return p.suggestedTitle('默认') === '课堂录音'
  })

  pass('StudyFilingPath.fromJSON', () => {
    const json: Record<string, string> = { type: '课堂', subject: '历史' }
    const p = StudyFilingPath.fromJSON(json)
    return p.type === '课堂' && p.subject === '历史' && p.chapter === null
  })

  pass('StudyFilingPath constants', () => {
    return StudyFilingPath.UNCategorized === '未分类' && StudyFilingPath.MISSING === '未填写'
  })

  // ── RecordingUploadStatus enum ──
  pass('RecordingUploadStatus values', () => {
    return RecordingUploadStatus.LOCAL_ONLY === 'localOnly' &&
      RecordingUploadStatus.UPLOADING === 'uploading' &&
      RecordingUploadStatus.UPLOADED === 'uploaded' &&
      RecordingUploadStatus.FAILED === 'failed'
  })

  // ── TranscriptionResult & Segments ──
  pass('TranscriptionResult.create', () => {
    const r = TranscriptionResult.create({
      taskID: 't1', recordingID: 'r1', providerID: 'p1',
      providerName: 'Mock', text: '测试文本', status: 'transcribed'
    })
    return r.text.length > 0 && r.status === 'transcribed'
  })

  pass('TranscriptionSegment', () => {
    const seg = new TranscriptionSegment('seg-1', 1.5, 3.2, 'hello')
    return seg.id === 'seg-1' && seg.startTime === 1.5 && seg.endTime === 3.2 && seg.text === 'hello'
  })

  // ── NoteGenerationResult ──
  pass('NoteGenerationResult.create', () => {
    const r = NoteGenerationResult.create({
      taskID: 't2', recordingID: 'r1', providerID: 'p2',
      providerName: 'Mock', markdown: '# Note', status: 'generated'
    })
    return r.markdown === '# Note' && r.status === 'generated'
  })

  pass('NoteGenerationResult truncation flags', () => {
    const r = NoteGenerationResult.create({
      taskID: 't3', recordingID: 'r1', providerID: 'p2',
      providerName: 'Mock', markdown: '# N',
      modelOutputWasTruncated: true, transcriptInputWasTruncated: true
    })
    return r.modelOutputWasTruncated && r.transcriptInputWasTruncated
  })

  // ── Chat models ──
  pass('ChatMessage.create', () => {
    const m = new ChatMessage(ChatMessageRole.USER, 'hello')
    return m.role === ChatMessageRole.USER && m.content === 'hello'
  })

  pass('ChatConversation.defaults', () => {
    const c = new ChatConversation()
    return c.title === '新对话' && c.messages.length === 0
  })

  pass('ChatResult', () => {
    const msg = new ChatMessage(ChatMessageRole.ASSISTANT, 'reply')
    const r = new ChatResult(msg, 'provider-1', 'TestProvider')
    r.modelName = 'gpt-4'
    r.finishReason = 'stop'
    return r.message.content === 'reply' && r.providerName === 'TestProvider' && r.modelName === 'gpt-4'
  })

  pass('ChatContext.create', () => {
    const item = new ChatContextItem('ci-1', '知识项', '/path', '内容文本')
    const ctx = ChatContext.create({
      title: '测试上下文',
      itemCount: 1,
      items: [item],
      maxContextCharacters: 8000
    })
    return ctx.title === '测试上下文' && ctx.items.length === 1 &&
      ctx.items[0].title === '知识项' && ctx.totalCharacterCount === 4
  })

  pass('ChatContextItem properties', () => {
    const item = new ChatContextItem('ci-2', '标题', '路径/to', '正文内容')
    return item.title === '标题' && item.filingPath === '路径/to' &&
      item.contentCharacterCount === 4 && !item.isTruncated
  })

  // ── UserProfile ──
  pass('UserProfile defaults', () => {
    const p = new UserProfile()
    return p.displayName === '用户' && p.handle === 'rokurics_user' && p.displayHandle === '@rokurics_user'
  })

  pass('UserProfile.initial', () => {
    const p = new UserProfile('张三', 'zhang3')
    return p.initial === '张'
  })

  pass('UserProfile.normalizedHandle strips @', () => {
    const p = new UserProfile('李四', '@@lisi')
    return p.handle === 'lisi'
  })

  // ── Format helpers ──
  pass('formatClock zero', () => formatClock(0) === '00:00')
  pass('formatClock 125s -> 02:05', () => formatClock(125) === '02:05')
  pass('formatDuration seconds', () => formatDuration(30) === '30 sec')
  pass('formatDuration minutes', () => formatDuration(150) === '2.5 min')
  pass('formatDate', () => {
    const d = new Date('2026-05-26T10:30:00')
    return formatDate(d) === '2026-05-26'
  })
  pass('fileSizeText B', () => fileSizeText(500) === '500 B')
  pass('fileSizeText KB', () => fileSizeText(102400) === '100.0 KB')
  pass('fileSizeText MB', () => fileSizeText(3145728) === '3.0 MB')

  console.info(`=== Result: ${passed} passed, ${failed} failed ===`)
  return { passed, failed }
}

// Direct execution for non-test environments
if (typeof globalThis !== 'undefined') {
  const result = selfCheck()
  if (result.failed > 0) {
    console.error(`Self-check FAILED: ${result.failed} checks failed`)
  } else {
    console.info(`Self-check PASSED: all ${result.passed} checks passed`)
  }
}
