/**
 * KnowledgePoint - Core data model for a single memorization item.
 * Translated from source/Kikaria/KnowledgePoint.swift
 */
export class KnowledgePoint {
  id: string
  title: string
  tags: string[]
  hint: string
  content: string
  isReinforced: boolean
  reinforcementCount: number
  lastReinforcedAt: number | null  // timestamp
  isMastered: boolean
  createdAt: number   // timestamp
  updatedAt: number   // timestamp

  constructor(
    id: string,
    title: string,
    tags: string[],
    hint: string,
    content: string,
    isReinforced: boolean = false,
    isMastered: boolean = false,
    createdAt: number = Date.now(),
    updatedAt: number = Date.now(),
    reinforcementCount: number = 0,
    lastReinforcedAt: number | null = null
  ) {
    this.id = id
    this.title = title
    this.tags = tags
    this.hint = hint
    this.content = content
    this.reinforcementCount = Math.max(0, reinforcementCount)
    this.isReinforced = this.reinforcementCount > 0
    this.lastReinforcedAt = this.reinforcementCount > 0 ? lastReinforcedAt : null
    this.isMastered = isMastered
    this.createdAt = createdAt
    this.updatedAt = updatedAt
  }

  addReinforcement(timestamp: number = Date.now()): number {
    this.reinforcementCount = Math.max(0, this.reinforcementCount) + 1
    this.isReinforced = true
    this.lastReinforcedAt = timestamp
    this.updatedAt = timestamp
    return this.reinforcementCount
  }

  clearReinforcement(timestamp: number = Date.now()): void {
    this.reinforcementCount = 0
    this.isReinforced = false
    this.lastReinforcedAt = null
    this.updatedAt = timestamp
  }
}

/**
 * KnowledgePreset - A collection of knowledge points loaded from Markdown.
 * Translated from source/Kikaria/KnowledgePoint.swift
 */
export class KnowledgePreset {
  id: string
  name: string
  subtitle: string
  description: string
  category: string
  markdownText: string
  isBuiltIn: boolean

  constructor(
    id: string,
    name: string,
    subtitle: string,
    description: string,
    category: string,
    markdownText: string,
    isBuiltIn: boolean
  ) {
    this.id = id
    this.name = name
    this.subtitle = subtitle
    this.description = description
    this.category = category
    this.markdownText = markdownText
    this.isBuiltIn = isBuiltIn
  }

  get knowledgePointCount(): number {
    return parseMarkdown(this.markdownText).length
  }
}

/**
 * ReviewMode - The mode of the current review session.
 * Translated from source/Kikaria/ContentView.swift (ReviewMode enum)
 */
export enum ReviewMode {
  NORMAL = 'normal',
  REINFORCEMENT = 'reinforcement',
  MASTERED = 'mastered'
}

/**
 * StudyActivityType - Types of study activities tracked.
 * Translated from source/Kikaria/StudyTracking.swift
 */
export enum StudyActivityType {
  VIEWED_HINT = 'viewedHint',
  REVIEWED_ANSWER = 'reviewedAnswer',
  MARKED_MASTERED = 'markedMastered',
  REMOVED_MASTERED = 'removedMastered',
  ADDED_REINFORCEMENT = 'addedReinforcement',
  REMOVED_REINFORCEMENT = 'removedReinforcement'
}

/**
 * StudyActivityRecord - A single study activity record.
 */
export class StudyActivityRecord {
  id: string
  presetId: string
  date: number  // timestamp
  type: StudyActivityType
  pointId: string
  pointTitle: string

  constructor(
    presetId: string,
    type: StudyActivityType,
    pointId: string,
    pointTitle: string,
    id: string = generateId(),
    date: number = Date.now()
  ) {
    this.id = id
    this.presetId = presetId
    this.date = date
    this.type = type
    this.pointId = pointId
    this.pointTitle = pointTitle
  }
}

/** Generate a simple unique ID */
export function generateId(): string {
  return `${Date.now()}-${Math.random().toString(36).substring(2, 11)}`
}

/**
 * Parse Markdown text into an array of KnowledgePoints.
 * Translated from KnowledgePoint.parseMarkdown in source/Kikaria/KnowledgePoint.swift
 */
export function parseMarkdown(markdown: string): KnowledgePoint[] {
  const normalized = markdown.replace(/\r\n/g, '\n').replace(/\r/g, '\n')
  const chunks = splitMarkdownIntoChunks(normalized)
  const points: KnowledgePoint[] = []

  for (const chunk of chunks) {
    const point = parseChunk(chunk)
    if (point) {
      points.push(point)
    }
  }

  return points
}

function splitMarkdownIntoChunks(markdown: string): string[] {
  const chunks: string[] = []
  const lines = markdown.split('\n')
  let currentLines: string[] = []

  for (const line of lines) {
    if (line.trim() === '---') {
      const chunk = currentLines.join('\n').trim()
      if (chunk.length > 0) {
        chunks.push(chunk)
      }
      currentLines = []
    } else {
      currentLines.push(line)
    }
  }

  const finalChunk = currentLines.join('\n').trim()
  if (finalChunk.length > 0) {
    chunks.push(finalChunk)
  }

  return chunks
}

function parseChunk(chunk: string): KnowledgePoint | null {
  const lines = chunk.split('\n')

  // Find title (first non-empty line starting with #)
  let titleIndex = -1
  for (let i = 0; i < lines.length; i++) {
    const trimmed = lines[i].trim()
    if (trimmed.length > 0) {
      titleIndex = i
      break
    }
  }
  if (titleIndex < 0) return null

  const rawTitle = lines[titleIndex].trim()
  if (!rawTitle.startsWith('#')) return null

  const title = rawTitle.replace(/^#+\s*/, '').trim()
  if (title.length === 0) return null

  // Parse tags
  const tags = parseTags(lines)

  // Find hint: and content: markers
  let hintIndex = -1
  let contentIndex = -1
  for (let i = 0; i < lines.length; i++) {
    const trimmed = lines[i].trim().toLowerCase()
    if (trimmed === 'hint:') {
      hintIndex = i
    } else if (trimmed === 'content:') {
      contentIndex = i
    }
  }

  if (hintIndex < 0 || contentIndex < 0 || hintIndex >= contentIndex) return null

  const hint = lines.slice(hintIndex + 1, contentIndex).join('\n').trim()
  const content = lines.slice(contentIndex + 1).join('\n').trim()

  if (hint.length === 0 || content.length === 0) return null

  return new KnowledgePoint(
    generateId(),
    title,
    tags,
    hint,
    content,
    false,
    false,
    Date.now(),
    Date.now()
  )
}

function parseTags(lines: string[]): string[] {
  for (const line of lines) {
    const trimmed = line.trim().toLowerCase()
    if (trimmed.startsWith('tags:')) {
      const tagText = line.trim().substring('tags:'.length)
      return tagText
        .split(/[,，]/)
        .map(t => t.trim())
        .filter(t => t.length > 0)
    }
  }
  return []
}

/**
 * Convert KnowledgePoints back to Markdown format.
 */
export function markdownTextFromPoints(points: KnowledgePoint[]): string {
  return points.map(p => {
    return `# ${p.title}\n\ntags: ${p.tags.join(', ')}\n\nhint:\n${p.hint}\n\ncontent:\n${p.content}`
  }).join('\n\n---\n\n')
}
