/**
 * AppState - Central state management for the Kikaria HarmonyOS app.
 * Manages knowledge points, presets, review state, and study tracking.
 *
 * Translated from ContentView's @State properties and KikariaAppState
 * in source/Kikaria/ContentView.swift
 */

import { KnowledgePoint, KnowledgePreset, ReviewMode, StudyActivityType,
  StudyActivityRecord, parseMarkdown, generateId } from '../model/KnowledgePoint'
import { builtInPresets, DEFAULT_PRESET_ID } from '../data/SamplePresets'

export class AppState {
  // --- Presets ---
  presets: KnowledgePreset[] = [...builtInPresets]
  currentPresetID: string = DEFAULT_PRESET_ID

  // --- Knowledge Points ---
  knowledgePoints: KnowledgePoint[] = []

  // --- Review state ---
  selectedTags: Set<string> = new Set()
  reviewMode: ReviewMode = ReviewMode.NORMAL
  currentReviewIndex: number = 0
  reviewQueue: KnowledgePoint[] = []

  // --- Study tracking ---
  activityRecords: StudyActivityRecord[] = []
  dailyGoal: number = 20

  // --- UI flags ---
  isHintVisible: boolean = false
  isContentVisible: boolean = false

  constructor() {
    this.initializeFromPreset()
  }

  // ---- Computed properties ----

  get currentPreset(): KnowledgePreset {
    return this.presets.find(p => p.id === this.currentPresetID) ?? this.presets[0]
  }

  get allTags(): string[] {
    const tagSet = new Set<string>()
    for (const kp of this.knowledgePoints) {
      for (const tag of kp.tags) {
        tagSet.add(tag)
      }
    }
    return Array.from(tagSet).sort()
  }

  get reinforcedPoints(): KnowledgePoint[] {
    return this.knowledgePoints.filter(kp => kp.isReinforced)
  }

  get reinforcedCount(): number {
    return this.reinforcedPoints.length
  }

  get masteredPoints(): KnowledgePoint[] {
    return this.knowledgePoints.filter(kp => kp.isMastered)
  }

  get masteredCount(): number {
    return this.masteredPoints.length
  }

  get totalCount(): number {
    return this.knowledgePoints.length
  }

  get activeReviewPoints(): KnowledgePoint[] {
    const filtered = this.selectedTags.size > 0
      ? this.knowledgePoints.filter(kp => kp.tags.some(t => this.selectedTags.has(t)))
      : [...this.knowledgePoints]

    switch (this.reviewMode) {
      case ReviewMode.NORMAL:
        return filtered.filter(kp => !kp.isMastered)
      case ReviewMode.REINFORCEMENT:
        return filtered.filter(kp => kp.isReinforced && !kp.isMastered)
      case ReviewMode.MASTERED:
        return filtered.filter(kp => kp.isMastered)
    }
  }

  get currentPoint(): KnowledgePoint | null {
    if (this.reviewQueue.length === 0) return null
    const idx = this.currentReviewIndex % this.reviewQueue.length
    return this.reviewQueue[idx]
  }

  get hasMoreReviewPoints(): boolean {
    return this.currentReviewIndex < this.reviewQueue.length - 1
  }

  // ---- Actions ----

  initializeFromPreset(): void {
    const preset = this.currentPreset
    this.knowledgePoints = parseMarkdown(preset.markdownText)
    this.selectedTags.clear()
    this.reviewQueue = []
    this.currentReviewIndex = 0
    this.isHintVisible = false
    this.isContentVisible = false
  }

  selectPreset(presetId: string): void {
    this.currentPresetID = presetId
    this.initializeFromPreset()
  }

  toggleTag(tag: string): void {
    if (this.selectedTags.has(tag)) {
      this.selectedTags.delete(tag)
    } else {
      this.selectedTags.add(tag)
    }
  }

  startReview(mode: ReviewMode = ReviewMode.NORMAL): void {
    this.reviewMode = mode
    const pool = this.activeReviewPoints
    // Shuffle the pool for random review
    this.reviewQueue = [...pool].sort(() => Math.random() - 0.5)
    this.currentReviewIndex = 0
    this.isHintVisible = false
    this.isContentVisible = false
  }

  showHint(): void {
    this.isHintVisible = true
    const point = this.currentPoint
    if (point) {
      this.recordActivity(StudyActivityType.VIEWED_HINT, point)
    }
  }

  showContent(): void {
    this.isContentVisible = true
    this.isHintVisible = true
    const point = this.currentPoint
    if (point) {
      this.recordActivity(StudyActivityType.REVIEWED_ANSWER, point)
    }
  }

  nextPoint(): void {
    if (this.hasMoreReviewPoints) {
      this.currentReviewIndex++
      this.isHintVisible = false
      this.isContentVisible = false
    }
  }

  toggleReinforcement(): void {
    const point = this.currentPoint
    if (!point) return

    const idx = this.knowledgePoints.findIndex(kp => kp.id === point.id)
    if (idx < 0) return

    if (point.isReinforced) {
      this.knowledgePoints[idx].clearReinforcement()
      this.recordActivity(StudyActivityType.REMOVED_REINFORCEMENT, point)
    } else {
      this.knowledgePoints[idx].addReinforcement()
      this.recordActivity(StudyActivityType.ADDED_REINFORCEMENT, point)
    }
  }

  toggleMastered(): void {
    const point = this.currentPoint
    if (!point) return

    const idx = this.knowledgePoints.findIndex(kp => kp.id === point.id)
    if (idx < 0) return

    if (point.isMastered) {
      this.knowledgePoints[idx].isMastered = false
      this.knowledgePoints[idx].updatedAt = Date.now()
      this.recordActivity(StudyActivityType.REMOVED_MASTERED, point)
    } else {
      this.knowledgePoints[idx].isMastered = true
      this.knowledgePoints[idx].updatedAt = Date.now()
      this.recordActivity(StudyActivityType.MARKED_MASTERED, point)
    }
  }

  private recordActivity(type: StudyActivityType, point: KnowledgePoint): void {
    this.activityRecords.push(new StudyActivityRecord(
      this.currentPresetID,
      type,
      point.id,
      point.title
    ))
  }
}

/** Singleton app state instance */
export const appState = new AppState()
