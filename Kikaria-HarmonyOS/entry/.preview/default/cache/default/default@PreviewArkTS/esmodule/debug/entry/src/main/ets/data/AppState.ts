import { KnowledgePoint, KnowledgePreset, ReviewMode, StudyActivityType, StudyActivityRecord, PresetStudyState, parseMarkdown, generateId, countdownDays } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import type { UserProfile, DailyReviewRecord } from "@bundle:com.vita0818.kikaria/entry/ets/model/KnowledgePoint";
import { preferenceManager } from "@bundle:com.vita0818.kikaria/entry/ets/data/PreferenceManager";
const CURRENT_SCHEMA_VERSION = 4;
export class AppState {
    schemaVersion: number = CURRENT_SCHEMA_VERSION;
    presets: KnowledgePreset[] = [];
    presetStates: Record<string, PresetStudyState> = {};
    currentPresetID: string = '';
    userProfile: UserProfile = { displayName: 'Vita', userHandle: 'vita_0818', avatarIconName: 'person' };
    hasCompletedProfileSetup: boolean = false;
    hasCompletedOnboarding: boolean = false;
    // Active preset derived state
    knowledgePoints: KnowledgePoint[] = [];
    selectedTags: Set<string> = new Set();
    dailyReviewRecords: Record<string, DailyReviewRecord> = {};
    activityRecords: StudyActivityRecord[] = [];
    dailyGoal: number = 20;
    countdownStartDate: number | null = null;
    countdownEndDate: number | null = null;
    notificationsEnabled: boolean = false;
    notificationTime: number = PresetStudyState.defaultNotificationTime();
    dangerPercent: number = 80;
    isDarkMode: boolean = false;
    // Review state
    reviewMode: ReviewMode = ReviewMode.NORMAL;
    currentReviewIndex: number = 0;
    reviewQueue: KnowledgePoint[] = [];
    isHintVisible: boolean = false;
    isContentVisible: boolean = false;
    private hasLoadedInitialState: boolean = false;
    private isApplyingState: boolean = false;
    constructor() { }
    get currentPreset(): KnowledgePreset {
        const found = this.presets.find(p => p.id === this.currentPresetID);
        return found !== undefined ? found : this.presets[0];
    }
    get allTags(): string[] {
        const tagSet = new Set<string>();
        for (const kp of this.knowledgePoints) {
            for (const tag of kp.tags) {
                tagSet.add(tag);
            }
        }
        return Array.from(tagSet).sort();
    }
    get reinforcedPoints(): KnowledgePoint[] {
        return this.knowledgePoints.filter(kp => kp.isReinforced);
    }
    get reinforcedCount(): number {
        return this.reinforcedPoints.length;
    }
    get masteredPoints(): KnowledgePoint[] {
        return this.knowledgePoints.filter(kp => kp.isMastered);
    }
    get masteredCount(): number {
        return this.masteredPoints.length;
    }
    get totalCount(): number {
        return this.knowledgePoints.length;
    }
    get selectedScopeCountText(): string {
        return this.selectedTags.size === 0 ? `${this.allTags.length}` : `${this.selectedTags.size}`;
    }
    get countdownDayCount(): number | null {
        return countdownDays(this.countdownEndDate);
    }
    get todayReviewedAnswerCount(): number {
        return this.recordsOnDate(Date.now(), StudyActivityType.REVIEWED_ANSWER)
            .filter(r => r.presetId === this.currentPresetID).length;
    }
    get todayViewedHintCount(): number {
        return this.recordsOnDate(Date.now(), StudyActivityType.VIEWED_HINT)
            .filter(r => r.presetId === this.currentPresetID).length;
    }
    get todayMarkedMasteredCount(): number {
        const ids = new Set<string>();
        const records = this.recordsOnDate(Date.now(), StudyActivityType.MARKED_MASTERED);
        for (const r of records) {
            if (r.presetId === this.currentPresetID) {
                ids.add(r.pointId);
            }
        }
        return ids.size;
    }
    get activeReviewPoints(): KnowledgePoint[] {
        let filtered: KnowledgePoint[];
        if (this.selectedTags.size > 0) {
            filtered = this.knowledgePoints.filter(kp => {
                for (const tag of kp.tags) {
                    if (this.selectedTags.has(tag)) {
                        return true;
                    }
                }
                return false;
            });
        }
        else {
            filtered = Array.from(this.knowledgePoints);
        }
        switch (this.reviewMode) {
            case ReviewMode.NORMAL:
                return filtered.filter(kp => !kp.isMastered);
            case ReviewMode.REINFORCEMENT:
                return filtered.filter(kp => kp.isReinforced && !kp.isMastered);
            case ReviewMode.MASTERED:
                return filtered.filter(kp => kp.isMastered);
        }
    }
    get currentPoint(): KnowledgePoint | null {
        if (this.reviewQueue.length === 0) {
            return null;
        }
        const idx = this.currentReviewIndex % this.reviewQueue.length;
        return this.reviewQueue[idx];
    }
    get hasMoreReviewPoints(): boolean {
        return this.currentReviewIndex < this.reviewQueue.length - 1;
    }
    // ---- Initialization ----
    initialize(builtInPresets: KnowledgePreset[]): void {
        this.presets = builtInPresets;
        this.currentPresetID = builtInPresets.length > 0 ? builtInPresets[0].id : '';
        const json = preferenceManager.loadAppStateJson();
        if (json && json.length > 0) {
            try {
                const data: Record<string, Object> = JSON.parse(json) as Record<string, Object>;
                this.loadSavedState(data, builtInPresets);
            }
            catch (e) {
                console.error(`Kikaria: Failed to parse saved state: ${e}`);
                this.ensurePresetStatesExist();
            }
        }
        else {
            this.ensurePresetStatesExist();
        }
        this.restoreActivePresetState();
        this.hasLoadedInitialState = true;
    }
    private loadSavedState(data: Record<string, Object>, builtInPresets: KnowledgePreset[]): void {
        if (data['schemaVersion'] !== undefined) {
            this.schemaVersion = data['schemaVersion'] as number;
        }
        if (data['currentPresetID'] !== undefined) {
            this.currentPresetID = data['currentPresetID'] as string;
        }
        if (data['userProfile'] !== undefined) {
            const up = data['userProfile'] as Record<string, string>;
            this.userProfile = {
                displayName: up['displayName'] !== undefined ? up['displayName'] : 'Vita',
                userHandle: up['userHandle'] !== undefined ? up['userHandle'] : 'user',
                avatarIconName: up['avatarIconName'] !== undefined ? up['avatarIconName'] : 'person'
            };
        }
        if (data['hasCompletedProfileSetup'] !== undefined) {
            this.hasCompletedProfileSetup = data['hasCompletedProfileSetup'] as boolean;
        }
        if (data['hasCompletedOnboarding'] !== undefined) {
            this.hasCompletedOnboarding = data['hasCompletedOnboarding'] as boolean;
        }
        if (data['isDarkMode'] !== undefined) {
            this.isDarkMode = data['isDarkMode'] as boolean;
        }
        if (data['presetStates'] !== undefined) {
            const states = data['presetStates'] as Record<string, Object>;
            this.presetStates = this.deserializePresetStates(states);
        }
        if (data['presets'] !== undefined) {
            const savedPresets = data['presets'] as Record<string, Object>[];
            this.presets = this.mergePresets(builtInPresets, savedPresets);
        }
        // Ensure all built-in presets have states
        this.ensurePresetStatesExist();
    }
    private deserializePresetStates(states: Record<string, Object>): Record<string, PresetStudyState> {
        const result: Record<string, PresetStudyState> = {};
        for (const key of Object.keys(states)) {
            const s = states[key] as Record<string, Object>;
            const kpsRaw = s['knowledgePoints'] as Record<string, Object>[];
            const kps: KnowledgePoint[] = [];
            for (const kp of kpsRaw) {
                let lra: number | null = kp['lastReinforcedAt'] as number | null;
                if (lra === -1) {
                    lra = null;
                }
                kps.push(new KnowledgePoint(kp['id'] as string, kp['title'] as string, kp['tags'] as string[], kp['hint'] as string, kp['content'] as string, kp['isReinforced'] as boolean, kp['isMastered'] as boolean, kp['createdAt'] as number, kp['updatedAt'] as number, kp['reinforcementCount'] as number, lra));
            }
            const tagsRaw = s['selectedTags'] as string[];
            const selectedTags = new Set<string>();
            for (const t of tagsRaw) {
                selectedTags.add(t);
            }
            const drrRaw = s['dailyReviewRecords'] as Record<string, Object>;
            const drr: Record<string, DailyReviewRecord> = {};
            for (const drrKey of Object.keys(drrRaw)) {
                const v = drrRaw[drrKey] as Record<string, number>;
                drr[drrKey] = { date: v['date'], count: v['count'] };
            }
            const actsRaw = s['activityRecords'] as Record<string, Object>[];
            const acts: StudyActivityRecord[] = [];
            for (const a of actsRaw) {
                acts.push(new StudyActivityRecord(a['presetId'] as string, a['type'] as StudyActivityType, a['pointId'] as string, a['pointTitle'] as string, a['id'] as string, a['date'] as number));
            }
            let countdownStart: number | null = s['countdownStartDate'] as number | null;
            if (countdownStart === -1) {
                countdownStart = null;
            }
            let countdownEnd: number | null = s['countdownEndDate'] as number | null;
            if (countdownEnd === -1) {
                countdownEnd = null;
            }
            result[key] = new PresetStudyState(s['presetId'] as string, kps, s['markdownText'] as string, selectedTags, drr, acts, s['dailyGoal'] as number, countdownStart, countdownEnd, s['notificationsEnabled'] as boolean, s['notificationTime'] as number, s['dangerPercent'] as number);
        }
        return result;
    }
    private mergePresets(builtIn: KnowledgePreset[], saved: Record<string, Object>[]): KnowledgePreset[] {
        const result: KnowledgePreset[] = Array.from(builtIn);
        const existingIds = new Set(result.map(p => p.id));
        for (const sp of saved) {
            if (!(sp['isBuiltIn'] as boolean) && !existingIds.has(sp['id'] as string)) {
                result.push(new KnowledgePreset(sp['id'] as string, sp['name'] as string, sp['subtitle'] as string, sp['description'] as string, sp['category'] as string, sp['markdownText'] as string, false));
                existingIds.add(sp['id'] as string);
            }
        }
        return result;
    }
    private ensurePresetStatesExist(): void {
        const validIDs = new Set(this.presets.map(p => p.id));
        const cleaned: Record<string, PresetStudyState> = {};
        for (const key of Object.keys(this.presetStates)) {
            if (validIDs.has(key)) {
                cleaned[key] = this.presetStates[key];
            }
        }
        this.presetStates = cleaned;
        for (const preset of this.presets) {
            if (this.presetStates[preset.id] === undefined) {
                const state = this.initialStudyStateFor(preset);
                if (state !== null) {
                    this.presetStates[preset.id] = state;
                }
            }
            if (preset.isBuiltIn && this.presetStates[preset.id] !== undefined) {
                const existing = this.presetStates[preset.id];
                if (existing.markdownText !== preset.markdownText) {
                    const newState = this.initialStudyStateFor(preset);
                    if (newState !== null) {
                        this.presetStates[preset.id] = newState;
                    }
                }
            }
        }
    }
    private initialStudyStateFor(preset: KnowledgePreset): PresetStudyState | null {
        const parsed = parseMarkdown(preset.markdownText);
        if (parsed.length === 0) {
            return null;
        }
        return new PresetStudyState(preset.id, parsed, preset.markdownText);
    }
    private restoreActivePresetState(): void {
        const state = this.presetStates[this.currentPresetID];
        if (state === undefined) {
            return;
        }
        this.isApplyingState = true;
        this.knowledgePoints = state.knowledgePoints;
        this.selectedTags = this.validSelectedTags(state.selectedTags);
        this.dailyReviewRecords = state.dailyReviewRecords;
        this.activityRecords = state.activityRecords;
        this.dailyGoal = state.dailyGoal;
        this.countdownStartDate = state.countdownStartDate;
        this.countdownEndDate = state.countdownEndDate;
        this.notificationsEnabled = state.notificationsEnabled;
        this.notificationTime = state.notificationTime;
        this.dangerPercent = state.dangerPercent;
        this.isApplyingState = false;
    }
    private validSelectedTags(tags: Set<string>): Set<string> {
        const available = new Set<string>();
        for (const kp of this.knowledgePoints) {
            for (const t of kp.tags) {
                available.add(t);
            }
        }
        const result = new Set<string>();
        const tagArray = Array.from(tags);
        for (const t of tagArray) {
            if (available.has(t)) {
                result.add(t);
            }
        }
        return result;
    }
    // ---- Preset switching ----
    switchToPreset(preset: KnowledgePreset): boolean {
        if (this.presetStates[preset.id] === undefined) {
            const state = this.initialStudyStateFor(preset);
            if (state === null) {
                return false;
            }
            this.presetStates[preset.id] = state;
        }
        this.saveCurrentPresetState();
        this.currentPresetID = preset.id;
        this.restoreActivePresetState();
        this.saveAppState();
        return true;
    }
    private saveCurrentPresetState(): void {
        const snapshot = this.makePresetStateSnapshot();
        this.presetStates[this.currentPresetID] = snapshot;
    }
    private makePresetStateSnapshot(): PresetStudyState {
        return new PresetStudyState(this.currentPresetID, this.knowledgePoints, this.presetStates[this.currentPresetID] !== undefined ?
            this.presetStates[this.currentPresetID].markdownText : '', new Set(this.selectedTags), this.makeDailyReviewRecordsCopy(), Array.from(this.activityRecords), this.dailyGoal, this.countdownStartDate, this.countdownEndDate, this.notificationsEnabled, this.notificationTime, this.dangerPercent);
    }
    private makeDailyReviewRecordsCopy(): Record<string, DailyReviewRecord> {
        const result: Record<string, DailyReviewRecord> = {};
        for (const key of Object.keys(this.dailyReviewRecords)) {
            const orig = this.dailyReviewRecords[key];
            result[key] = { date: orig.date, count: orig.count };
        }
        return result;
    }
    // ---- Persistence ----
    saveAppState(): void {
        if (!this.hasLoadedInitialState || this.isApplyingState) {
            return;
        }
        this.presetStates[this.currentPresetID] = this.makePresetStateSnapshot();
        const jsonObj: Record<string, Object> = {};
        jsonObj['schemaVersion'] = this.schemaVersion;
        jsonObj['currentPresetID'] = this.currentPresetID;
        jsonObj['userProfile'] = this.userProfile;
        jsonObj['hasCompletedProfileSetup'] = this.hasCompletedProfileSetup;
        jsonObj['hasCompletedOnboarding'] = this.hasCompletedOnboarding;
        jsonObj['isDarkMode'] = this.isDarkMode;
        jsonObj['presetStates'] = this.serializePresetStates();
        jsonObj['presets'] = this.serializePresets();
        preferenceManager.saveAppStateJson(JSON.stringify(jsonObj));
    }
    private serializePresetStates(): Record<string, Object> {
        const result: Record<string, Object> = {};
        for (const key of Object.keys(this.presetStates)) {
            const s = this.presetStates[key];
            const stateObj: Record<string, Object> = {};
            stateObj['presetId'] = s.presetId;
            stateObj['markdownText'] = s.markdownText;
            stateObj['selectedTags'] = Array.from(s.selectedTags);
            stateObj['dailyReviewRecords'] = s.dailyReviewRecords;
            stateObj['activityRecords'] = s.activityRecords;
            stateObj['dailyGoal'] = s.dailyGoal;
            stateObj['countdownStartDate'] = s.countdownStartDate !== null ? s.countdownStartDate : -1;
            stateObj['countdownEndDate'] = s.countdownEndDate !== null ? s.countdownEndDate : -1;
            stateObj['notificationsEnabled'] = s.notificationsEnabled;
            stateObj['notificationTime'] = s.notificationTime;
            stateObj['dangerPercent'] = s.dangerPercent;
            // Serialize knowledgePoints as plain objects
            const kpsArr: Record<string, Object>[] = [];
            for (const kp of s.knowledgePoints) {
                const kpObj: Record<string, Object> = {};
                kpObj['id'] = kp.id;
                kpObj['title'] = kp.title;
                kpObj['tags'] = kp.tags;
                kpObj['hint'] = kp.hint;
                kpObj['content'] = kp.content;
                kpObj['isReinforced'] = kp.isReinforced;
                kpObj['reinforcementCount'] = kp.reinforcementCount;
                kpObj['lastReinforcedAt'] = kp.lastReinforcedAt !== null ? kp.lastReinforcedAt : -1;
                kpObj['isMastered'] = kp.isMastered;
                kpObj['createdAt'] = kp.createdAt;
                kpObj['updatedAt'] = kp.updatedAt;
                kpsArr.push(kpObj);
            }
            stateObj['knowledgePoints'] = kpsArr;
            result[key] = stateObj;
        }
        return result;
    }
    private serializePresets(): Record<string, Object>[] {
        const result: Record<string, Object>[] = [];
        for (const p of this.presets) {
            const presetObj: Record<string, Object> = {};
            presetObj['id'] = p.id;
            presetObj['name'] = p.name;
            presetObj['subtitle'] = p.subtitle;
            presetObj['description'] = p.description;
            presetObj['category'] = p.category;
            presetObj['markdownText'] = p.markdownText;
            presetObj['isBuiltIn'] = p.isBuiltIn;
            result.push(presetObj);
        }
        return result;
    }
    // ---- Tag filtering ----
    toggleTag(tag: string): void {
        if (this.selectedTags.has(tag)) {
            this.selectedTags.delete(tag);
        }
        else {
            this.selectedTags.add(tag);
        }
        this.saveAppState();
    }
    // ---- Review ----
    startReview(mode: ReviewMode = ReviewMode.NORMAL): void {
        this.reviewMode = mode;
        const pool = this.activeReviewPoints;
        // Fisher-Yates shuffle (no destructuring)
        const shuffled: KnowledgePoint[] = new Array(pool.length);
        for (let i = 0; i < pool.length; i++) {
            shuffled[i] = pool[i];
        }
        for (let i = shuffled.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            const temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }
        this.reviewQueue = shuffled;
        this.currentReviewIndex = 0;
        this.isHintVisible = false;
        this.isContentVisible = false;
    }
    showHint(): void {
        this.isHintVisible = true;
        const point = this.currentPoint;
        if (point !== null) {
            this.recordActivity(StudyActivityType.VIEWED_HINT, point);
        }
    }
    showContent(): void {
        this.isContentVisible = true;
        this.isHintVisible = true;
        const point = this.currentPoint;
        if (point !== null) {
            this.recordActivity(StudyActivityType.REVIEWED_ANSWER, point);
            this.recordDailyReview(point.id);
        }
        this.saveAppState();
    }
    private recordDailyReview(pointId: string): void {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const todayKey = today.getTime();
        const existing = this.dailyReviewRecords[pointId];
        if (existing !== undefined) {
            if (existing.date === todayKey) {
                existing.count = existing.count + 1;
            }
            else {
                this.dailyReviewRecords[pointId] = { date: todayKey, count: 1 };
            }
        }
        else {
            this.dailyReviewRecords[pointId] = { date: todayKey, count: 1 };
        }
    }
    nextPoint(): void {
        if (this.hasMoreReviewPoints) {
            this.currentReviewIndex = this.currentReviewIndex + 1;
            this.isHintVisible = false;
            this.isContentVisible = false;
        }
    }
    toggleReinforcement(): void {
        const point = this.currentPoint;
        if (point === null) {
            return;
        }
        const idx = this.knowledgePoints.findIndex(kp => kp.id === point.id);
        if (idx < 0) {
            return;
        }
        if (point.isReinforced) {
            this.knowledgePoints[idx].clearReinforcement();
            this.recordActivity(StudyActivityType.REMOVED_REINFORCEMENT, point);
        }
        else {
            this.knowledgePoints[idx].addReinforcement();
            this.recordActivity(StudyActivityType.ADDED_REINFORCEMENT, point);
        }
        this.saveAppState();
    }
    toggleMastered(): void {
        const point = this.currentPoint;
        if (point === null) {
            return;
        }
        const idx = this.knowledgePoints.findIndex(kp => kp.id === point.id);
        if (idx < 0) {
            return;
        }
        const kp = this.knowledgePoints[idx];
        if (kp.isMastered) {
            kp.isMastered = false;
            kp.updatedAt = Date.now();
            this.recordActivity(StudyActivityType.REMOVED_MASTERED, kp);
        }
        else {
            kp.isMastered = true;
            kp.updatedAt = Date.now();
            this.recordActivity(StudyActivityType.MARKED_MASTERED, kp);
        }
        this.saveAppState();
    }
    togglePointReinforcement(pointId: string): void {
        const idx = this.knowledgePoints.findIndex(kp => kp.id === pointId);
        if (idx < 0) {
            return;
        }
        const kp = this.knowledgePoints[idx];
        if (kp.isReinforced) {
            kp.clearReinforcement();
        }
        else {
            kp.addReinforcement();
        }
        this.saveAppState();
    }
    togglePointMastered(pointId: string): void {
        const idx = this.knowledgePoints.findIndex(kp => kp.id === pointId);
        if (idx < 0) {
            return;
        }
        const kp = this.knowledgePoints[idx];
        kp.isMastered = !kp.isMastered;
        kp.updatedAt = Date.now();
        const type = kp.isMastered ? StudyActivityType.MARKED_MASTERED : StudyActivityType.REMOVED_MASTERED;
        this.recordActivity(type, kp);
        this.saveAppState();
    }
    // ---- Preset management ----
    createPreset(name: string, subtitle: string, description: string, category: string, markdownText: string): KnowledgePreset | null {
        const id = `custom-${generateId()}`;
        const preset = new KnowledgePreset(id, name, subtitle, description, category, markdownText, false);
        this.presets.push(preset);
        const parsed = parseMarkdown(markdownText);
        this.presetStates[id] = new PresetStudyState(id, parsed, markdownText);
        this.saveAppState();
        return preset;
    }
    deletePreset(presetId: string): boolean {
        if (this.presets.length <= 1) {
            return false;
        }
        const idx = this.presets.findIndex(p => p.id === presetId);
        if (idx < 0) {
            return false;
        }
        this.presets.splice(idx, 1);
        // Set to undefined instead of delete (ArkTS no-delete)
        const newStates: Record<string, PresetStudyState> = {};
        for (const key of Object.keys(this.presetStates)) {
            if (key !== presetId) {
                newStates[key] = this.presetStates[key];
            }
        }
        this.presetStates = newStates;
        if (this.currentPresetID === presetId) {
            this.currentPresetID = this.presets[0].id;
            this.restoreActivePresetState();
        }
        this.saveAppState();
        return true;
    }
    updatePresetMetadata(presetId: string, name: string, subtitle: string, description: string): void {
        for (const preset of this.presets) {
            if (preset.id === presetId) {
                preset.name = name;
                preset.subtitle = subtitle;
                preset.description = description;
                break;
            }
        }
        this.saveAppState();
    }
    deleteKnowledgePoint(pointId: string): void {
        const idx = this.knowledgePoints.findIndex(kp => kp.id === pointId);
        if (idx < 0) {
            return;
        }
        this.knowledgePoints.splice(idx, 1);
        const state = this.presetStates[this.currentPresetID];
        if (state !== undefined) {
            state.knowledgePoints = this.knowledgePoints;
        }
        this.saveAppState();
    }
    upsertKnowledgePoint(point: KnowledgePoint): void {
        const idx = this.knowledgePoints.findIndex(kp => kp.id === point.id);
        if (idx >= 0) {
            this.knowledgePoints[idx] = point;
        }
        else {
            this.knowledgePoints.push(point);
        }
        const state = this.presetStates[this.currentPresetID];
        if (state !== undefined) {
            state.knowledgePoints = this.knowledgePoints;
        }
        this.saveAppState();
    }
    // ---- Settings ----
    updateDailyGoal(goal: number): void {
        this.dailyGoal = Math.max(1, Math.min(goal, 200));
        this.saveAppState();
    }
    updateCountdownRange(startDate: number | null, endDate: number | null): void {
        this.countdownStartDate = startDate;
        this.countdownEndDate = endDate;
        this.saveAppState();
    }
    updateNotificationsEnabled(enabled: boolean): void {
        this.notificationsEnabled = enabled;
        this.saveAppState();
    }
    updateNotificationTime(time: number): void {
        this.notificationTime = time;
        this.saveAppState();
    }
    updateDangerPercent(percent: number): void {
        this.dangerPercent = Math.min(Math.max(percent, 1), 100);
        this.saveAppState();
    }
    toggleDarkMode(): void {
        this.isDarkMode = !this.isDarkMode;
        AppStorage.setAndRef<boolean>('kikaria_isDarkMode', this.isDarkMode).set(this.isDarkMode);
        preferenceManager.saveDarkModeManual(true);
        this.saveAppState();
    }
    updateProfile(profile: UserProfile): void {
        this.userProfile = profile;
        this.saveAppState();
    }
    completeProfileSetup(): void {
        this.hasCompletedProfileSetup = true;
        this.saveAppState();
    }
    completeOnboarding(): void {
        this.hasCompletedOnboarding = true;
        this.saveAppState();
    }
    // ---- Helpers ----
    private recordsOnDate(timestamp: number, type: StudyActivityType): StudyActivityRecord[] {
        const d = new Date(timestamp);
        d.setHours(0, 0, 0, 0);
        const dayStart = d.getTime();
        d.setHours(23, 59, 59, 999);
        const dayEnd = d.getTime();
        const result: StudyActivityRecord[] = [];
        for (const r of this.activityRecords) {
            if (r.type === type && r.date >= dayStart && r.date <= dayEnd) {
                result.push(r);
            }
        }
        return result;
    }
    getCurrentPresetActivityRecords(): StudyActivityRecord[] {
        const result: StudyActivityRecord[] = [];
        for (const r of this.activityRecords) {
            if (r.presetId === this.currentPresetID) {
                result.push(r);
            }
        }
        return result;
    }
    private recordActivity(type: StudyActivityType, point: KnowledgePoint): void {
        this.activityRecords.push(new StudyActivityRecord(this.currentPresetID, type, point.id, point.title));
    }
}
/** Singleton app state instance */
export const appState = new AppState();
