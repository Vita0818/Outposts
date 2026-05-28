if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface RecordingLibraryPage_Params {
    recordingManager?: RecordingManager;
    folderStore?: StudyFolderStore;
    recordings?: RecordingMetadata[];
    trashedRecordings?: RecordingMetadata[];
    showTrash?: boolean;
    searchQuery?: string;
    sortOrder?: string;
    browsePath?: string[];
    currentGroups?: FilingGroup[];
    currentRecordings?: RecordingMetadata[];
    isBrowsing?: boolean;
    folderColorMap?: Record<string, string>;
    showRenameDialog?: boolean;
    renameTarget?: string;
    renameText?: string;
    renameLevel?: string;
    showDeleteConfirm?: boolean;
    deleteTarget?: string;
    deleteTargetName?: string;
    showColorPicker?: boolean;
    selectedColor?: string;
}
import { getSharedRecordingManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/RecordingManager";
import type { RecordingManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/RecordingManager";
import { StudyFilingPath, filingLevelTitle } from "@bundle:com.vita0818.rokurics/entry/ets/models/RecordingModels";
import type { RecordingMetadata } from "@bundle:com.vita0818.rokurics/entry/ets/models/RecordingModels";
import { formatDuration, formatShortTime } from "@bundle:com.vita0818.rokurics/entry/ets/utils/FormatHelpers";
import { colorAlpha, RokuricsColors, FontWeight, glassFillOpacity, glassStrokeHighOpacity, glassStrokeMidOpacity, glassAccentOpacity } from "@bundle:com.vita0818.rokurics/entry/ets/utils/RokuricsTheme";
import { StudyFolderStore } from "@bundle:com.vita0818.rokurics/entry/ets/services/StudyFolderStore";
import { BackIcon, BulletListIcon, TrashIcon, EllipsisIcon, DocBadgeIcon, NoteBadgeIcon } from "@bundle:com.vita0818.rokurics/entry/ets/utils/CustomIcons";
const LEVELS: string[] = ['type', 'subject', 'chapter', 'topic'];
const FOLDER_COLORS: string[] = ['#59C7C2', '#9EE8C7', '#73C7F0', '#E06B6E', '#B8A6D6', '#6B9FD4'];
interface FilingGroup {
    key: string;
    label: string;
    level: string;
    count: number;
}
class RecordingLibraryPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.recordingManager = getSharedRecordingManager();
        this.folderStore = new StudyFolderStore(getContext(this));
        this.__recordings = new ObservedPropertyObjectPU([], this, "recordings");
        this.__trashedRecordings = new ObservedPropertyObjectPU([], this, "trashedRecordings");
        this.__showTrash = new ObservedPropertySimplePU(false, this, "showTrash");
        this.__searchQuery = new ObservedPropertySimplePU('', this, "searchQuery");
        this.__sortOrder = new ObservedPropertySimplePU('newest', this, "sortOrder");
        this.__browsePath = new ObservedPropertyObjectPU([], this, "browsePath");
        this.__currentGroups = new ObservedPropertyObjectPU([], this, "currentGroups");
        this.__currentRecordings = new ObservedPropertyObjectPU([], this, "currentRecordings");
        this.__isBrowsing = new ObservedPropertySimplePU(false, this, "isBrowsing");
        this.__folderColorMap = new ObservedPropertyObjectPU({}, this, "folderColorMap");
        this.__showRenameDialog = new ObservedPropertySimplePU(false, this, "showRenameDialog");
        this.__renameTarget = new ObservedPropertySimplePU('', this, "renameTarget");
        this.__renameText = new ObservedPropertySimplePU('', this, "renameText");
        this.__renameLevel = new ObservedPropertySimplePU('', this, "renameLevel");
        this.__showDeleteConfirm = new ObservedPropertySimplePU(false, this, "showDeleteConfirm");
        this.__deleteTarget = new ObservedPropertySimplePU('', this, "deleteTarget");
        this.__deleteTargetName = new ObservedPropertySimplePU('', this, "deleteTargetName");
        this.__showColorPicker = new ObservedPropertySimplePU(false, this, "showColorPicker");
        this.__selectedColor = new ObservedPropertySimplePU('#59C7C2', this, "selectedColor");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: RecordingLibraryPage_Params) {
        if (params.recordingManager !== undefined) {
            this.recordingManager = params.recordingManager;
        }
        if (params.folderStore !== undefined) {
            this.folderStore = params.folderStore;
        }
        if (params.recordings !== undefined) {
            this.recordings = params.recordings;
        }
        if (params.trashedRecordings !== undefined) {
            this.trashedRecordings = params.trashedRecordings;
        }
        if (params.showTrash !== undefined) {
            this.showTrash = params.showTrash;
        }
        if (params.searchQuery !== undefined) {
            this.searchQuery = params.searchQuery;
        }
        if (params.sortOrder !== undefined) {
            this.sortOrder = params.sortOrder;
        }
        if (params.browsePath !== undefined) {
            this.browsePath = params.browsePath;
        }
        if (params.currentGroups !== undefined) {
            this.currentGroups = params.currentGroups;
        }
        if (params.currentRecordings !== undefined) {
            this.currentRecordings = params.currentRecordings;
        }
        if (params.isBrowsing !== undefined) {
            this.isBrowsing = params.isBrowsing;
        }
        if (params.folderColorMap !== undefined) {
            this.folderColorMap = params.folderColorMap;
        }
        if (params.showRenameDialog !== undefined) {
            this.showRenameDialog = params.showRenameDialog;
        }
        if (params.renameTarget !== undefined) {
            this.renameTarget = params.renameTarget;
        }
        if (params.renameText !== undefined) {
            this.renameText = params.renameText;
        }
        if (params.renameLevel !== undefined) {
            this.renameLevel = params.renameLevel;
        }
        if (params.showDeleteConfirm !== undefined) {
            this.showDeleteConfirm = params.showDeleteConfirm;
        }
        if (params.deleteTarget !== undefined) {
            this.deleteTarget = params.deleteTarget;
        }
        if (params.deleteTargetName !== undefined) {
            this.deleteTargetName = params.deleteTargetName;
        }
        if (params.showColorPicker !== undefined) {
            this.showColorPicker = params.showColorPicker;
        }
        if (params.selectedColor !== undefined) {
            this.selectedColor = params.selectedColor;
        }
    }
    updateStateVars(params: RecordingLibraryPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__recordings.purgeDependencyOnElmtId(rmElmtId);
        this.__trashedRecordings.purgeDependencyOnElmtId(rmElmtId);
        this.__showTrash.purgeDependencyOnElmtId(rmElmtId);
        this.__searchQuery.purgeDependencyOnElmtId(rmElmtId);
        this.__sortOrder.purgeDependencyOnElmtId(rmElmtId);
        this.__browsePath.purgeDependencyOnElmtId(rmElmtId);
        this.__currentGroups.purgeDependencyOnElmtId(rmElmtId);
        this.__currentRecordings.purgeDependencyOnElmtId(rmElmtId);
        this.__isBrowsing.purgeDependencyOnElmtId(rmElmtId);
        this.__folderColorMap.purgeDependencyOnElmtId(rmElmtId);
        this.__showRenameDialog.purgeDependencyOnElmtId(rmElmtId);
        this.__renameTarget.purgeDependencyOnElmtId(rmElmtId);
        this.__renameText.purgeDependencyOnElmtId(rmElmtId);
        this.__renameLevel.purgeDependencyOnElmtId(rmElmtId);
        this.__showDeleteConfirm.purgeDependencyOnElmtId(rmElmtId);
        this.__deleteTarget.purgeDependencyOnElmtId(rmElmtId);
        this.__deleteTargetName.purgeDependencyOnElmtId(rmElmtId);
        this.__showColorPicker.purgeDependencyOnElmtId(rmElmtId);
        this.__selectedColor.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__recordings.aboutToBeDeleted();
        this.__trashedRecordings.aboutToBeDeleted();
        this.__showTrash.aboutToBeDeleted();
        this.__searchQuery.aboutToBeDeleted();
        this.__sortOrder.aboutToBeDeleted();
        this.__browsePath.aboutToBeDeleted();
        this.__currentGroups.aboutToBeDeleted();
        this.__currentRecordings.aboutToBeDeleted();
        this.__isBrowsing.aboutToBeDeleted();
        this.__folderColorMap.aboutToBeDeleted();
        this.__showRenameDialog.aboutToBeDeleted();
        this.__renameTarget.aboutToBeDeleted();
        this.__renameText.aboutToBeDeleted();
        this.__renameLevel.aboutToBeDeleted();
        this.__showDeleteConfirm.aboutToBeDeleted();
        this.__deleteTarget.aboutToBeDeleted();
        this.__deleteTargetName.aboutToBeDeleted();
        this.__showColorPicker.aboutToBeDeleted();
        this.__selectedColor.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private recordingManager: RecordingManager;
    private folderStore: StudyFolderStore;
    private __recordings: ObservedPropertyObjectPU<RecordingMetadata[]>;
    get recordings() {
        return this.__recordings.get();
    }
    set recordings(newValue: RecordingMetadata[]) {
        this.__recordings.set(newValue);
    }
    private __trashedRecordings: ObservedPropertyObjectPU<RecordingMetadata[]>;
    get trashedRecordings() {
        return this.__trashedRecordings.get();
    }
    set trashedRecordings(newValue: RecordingMetadata[]) {
        this.__trashedRecordings.set(newValue);
    }
    private __showTrash: ObservedPropertySimplePU<boolean>;
    get showTrash() {
        return this.__showTrash.get();
    }
    set showTrash(newValue: boolean) {
        this.__showTrash.set(newValue);
    }
    private __searchQuery: ObservedPropertySimplePU<string>;
    get searchQuery() {
        return this.__searchQuery.get();
    }
    set searchQuery(newValue: string) {
        this.__searchQuery.set(newValue);
    }
    private __sortOrder: ObservedPropertySimplePU<string>;
    get sortOrder() {
        return this.__sortOrder.get();
    }
    set sortOrder(newValue: string) {
        this.__sortOrder.set(newValue);
    }
    private __browsePath: ObservedPropertyObjectPU<string[]>;
    get browsePath() {
        return this.__browsePath.get();
    }
    set browsePath(newValue: string[]) {
        this.__browsePath.set(newValue);
    }
    private __currentGroups: ObservedPropertyObjectPU<FilingGroup[]>;
    get currentGroups() {
        return this.__currentGroups.get();
    }
    set currentGroups(newValue: FilingGroup[]) {
        this.__currentGroups.set(newValue);
    }
    private __currentRecordings: ObservedPropertyObjectPU<RecordingMetadata[]>;
    get currentRecordings() {
        return this.__currentRecordings.get();
    }
    set currentRecordings(newValue: RecordingMetadata[]) {
        this.__currentRecordings.set(newValue);
    }
    private __isBrowsing: ObservedPropertySimplePU<boolean>;
    get isBrowsing() {
        return this.__isBrowsing.get();
    }
    set isBrowsing(newValue: boolean) {
        this.__isBrowsing.set(newValue);
    }
    private __folderColorMap: ObservedPropertyObjectPU<Record<string, string>>;
    get folderColorMap() {
        return this.__folderColorMap.get();
    }
    set folderColorMap(newValue: Record<string, string>) {
        this.__folderColorMap.set(newValue);
    }
    private __showRenameDialog: ObservedPropertySimplePU<boolean>;
    get showRenameDialog() {
        return this.__showRenameDialog.get();
    }
    set showRenameDialog(newValue: boolean) {
        this.__showRenameDialog.set(newValue);
    }
    private __renameTarget: ObservedPropertySimplePU<string>;
    get renameTarget() {
        return this.__renameTarget.get();
    }
    set renameTarget(newValue: string) {
        this.__renameTarget.set(newValue);
    }
    private __renameText: ObservedPropertySimplePU<string>;
    get renameText() {
        return this.__renameText.get();
    }
    set renameText(newValue: string) {
        this.__renameText.set(newValue);
    }
    private __renameLevel: ObservedPropertySimplePU<string>;
    get renameLevel() {
        return this.__renameLevel.get();
    }
    set renameLevel(newValue: string) {
        this.__renameLevel.set(newValue);
    }
    private __showDeleteConfirm: ObservedPropertySimplePU<boolean>;
    get showDeleteConfirm() {
        return this.__showDeleteConfirm.get();
    }
    set showDeleteConfirm(newValue: boolean) {
        this.__showDeleteConfirm.set(newValue);
    }
    private __deleteTarget: ObservedPropertySimplePU<string>;
    get deleteTarget() {
        return this.__deleteTarget.get();
    }
    set deleteTarget(newValue: string) {
        this.__deleteTarget.set(newValue);
    }
    private __deleteTargetName: ObservedPropertySimplePU<string>;
    get deleteTargetName() {
        return this.__deleteTargetName.get();
    }
    set deleteTargetName(newValue: string) {
        this.__deleteTargetName.set(newValue);
    }
    private __showColorPicker: ObservedPropertySimplePU<boolean>;
    get showColorPicker() {
        return this.__showColorPicker.get();
    }
    set showColorPicker(newValue: boolean) {
        this.__showColorPicker.set(newValue);
    }
    private __selectedColor: ObservedPropertySimplePU<string>;
    get selectedColor() {
        return this.__selectedColor.get();
    }
    set selectedColor(newValue: string) {
        this.__selectedColor.set(newValue);
    }
    aboutToAppear(): void {
        this.loadAll();
    }
    async loadAll(): Promise<void> {
        await this.recordingManager.reloadRecordings();
        this.recordings = this.recordingManager.recordings.filter((r: RecordingMetadata) => !r.isDeleted);
        this.trashedRecordings = this.recordingManager.trashedRecordings;
        await this.loadFolderColors();
        if (this.isBrowsing) {
            this.navigateToPath(this.browsePath);
        }
        else {
            this.buildRootGroups();
        }
    }
    async loadFolderColors(): Promise<void> {
        const allFolders = await this.folderStore.listFolders();
        const map: Record<string, string> = {};
        for (const f of allFolders) {
            if (f.colorToken) {
                map[f.name] = f.colorToken;
            }
        }
        this.folderColorMap = map;
    }
    private buildRootGroups(): void {
        this.isBrowsing = false;
        this.currentGroups = this.extractGroups(0, []);
        this.currentRecordings = [];
    }
    private extractGroups(levelIdx: number, parentPath: string[]): FilingGroup[] {
        if (levelIdx >= LEVELS.length)
            return [];
        const level = LEVELS[levelIdx];
        const map = new Map<string, number>();
        for (const r of this.recordings) {
            const v = r.studyFiling?.valueForLevel(level) ?? null;
            const key = v || '未分类';
            // Only count recordings that match the parent path
            if (parentPath.length > 0) {
                let matchesParent = true;
                for (let i = 0; i < parentPath.length && i < LEVELS.length; i++) {
                    const pv = r.studyFiling?.valueForLevel(LEVELS[i]) ?? null;
                    if ((pv || '未分类') !== parentPath[i]) {
                        matchesParent = false;
                        break;
                    }
                }
                if (!matchesParent)
                    continue;
            }
            map.set(key, (map.get(key) ?? 0) + 1);
        }
        const groups: FilingGroup[] = [];
        const entries = Array.from(map.entries());
        entries.sort((a, b) => a[0].localeCompare(b[0]));
        for (const entry of entries) {
            groups.push({
                key: entry[0],
                label: entry[0],
                level: level,
                count: entry[1]
            });
        }
        return groups;
    }
    private navigateToPath(path: string[]): void {
        this.isBrowsing = true;
        this.browsePath = path;
        this.currentGroups = [];
        const levelIdx = path.length;
        if (levelIdx < LEVELS.length) {
            this.currentGroups = this.extractGroups(levelIdx, path);
        }
        // Filter recordings matching the path
        this.currentRecordings = this.recordings.filter((r: RecordingMetadata) => {
            for (let i = 0; i < path.length && i < LEVELS.length; i++) {
                const v = r.studyFiling?.valueForLevel(LEVELS[i]) ?? null;
                if ((v || '未分类') !== path[i])
                    return false;
            }
            return true;
        });
    }
    private navigateBack(): void {
        if (this.browsePath.length <= 1) {
            this.browsePath = [];
            this.isBrowsing = false;
            this.currentRecordings = [];
            this.buildRootGroups();
        }
        else {
            const parentPath: string[] = [];
            for (let i = 0; i < this.browsePath.length - 1; i++) {
                parentPath.push(this.browsePath[i]);
            }
            this.navigateToPath(parentPath);
        }
    }
    private toggleTrash(): void {
        this.showTrash = !this.showTrash;
    }
    private async restoreAndReload(id: string): Promise<void> {
        await this.recordingManager.restoreRecording(id);
        this.loadAll();
    }
    private async permDeleteAndReload(id: string): Promise<void> {
        await this.recordingManager.permanentlyDelete(id);
        this.loadAll();
    }
    private async deleteAndReload(id: string): Promise<void> {
        await this.recordingManager.deleteRecording(id);
        this.loadAll();
    }
    private openDetail(id: string): void {
        this.getUIContext().getRouter().pushUrl({
            url: 'pages/RecordingDetailPage',
            params: { recordingId: id }
        });
    }
    private openRenameDialog(key: string, level: string): void {
        this.renameTarget = key;
        this.renameText = key;
        this.renameLevel = level;
        this.selectedColor = this.folderColorMap[key] ?? '#59C7C2';
        this.showRenameDialog = true;
    }
    private async commitRename(): Promise<void> {
        const newName = this.renameText.trim();
        if (newName.length === 0 || newName === this.renameTarget || this.renameTarget.length === 0) {
            this.showRenameDialog = false;
            return;
        }
        const level = this.renameLevel;
        const toUpdate = this.recordings.filter((r: RecordingMetadata) => {
            const v = r.studyFiling?.valueForLevel(level) ?? null;
            return v === this.renameTarget || (v === null && this.renameTarget === '未分类');
        });
        for (const r of toUpdate) {
            const filing = r.studyFiling ? r.studyFiling.clone() : new StudyFilingPath();
            let changed = false;
            if (level === 'type') {
                filing.type = newName;
                changed = true;
            }
            else if (level === 'subject') {
                filing.subject = newName;
                changed = true;
            }
            else if (level === 'chapter') {
                filing.chapter = newName;
                changed = true;
            }
            else if (level === 'topic') {
                filing.topic = newName;
                changed = true;
            }
            if (changed) {
                await this.recordingManager.updateStudyFiling(r.id, filing);
            }
        }
        // Update persisted folder
        const folders = await this.folderStore.listFolders(level);
        const match = folders.find(f => f.name === this.renameTarget);
        if (match) {
            await this.folderStore.renameFolder(match.id, newName);
            if (this.selectedColor !== (this.folderColorMap[this.renameTarget] ?? '#59C7C2')) {
                await this.folderStore.setColorToken(match.id, this.selectedColor);
            }
        }
        else if (this.selectedColor !== '#59C7C2') {
            // Create folder to persist color
            const record = await this.folderStore.createFolder(newName, level, this.browsePath);
            await this.folderStore.setColorToken(record.id, this.selectedColor);
        }
        this.showRenameDialog = false;
        await this.loadAll();
    }
    private openDeleteConfirm(key: string, name: string, level: string): void {
        this.deleteTarget = key;
        this.deleteTargetName = name;
        this.renameLevel = level;
        this.showDeleteConfirm = true;
    }
    private async commitDelete(): Promise<void> {
        if (this.deleteTarget.length === 0) {
            this.showDeleteConfirm = false;
            return;
        }
        const level = this.renameLevel;
        const toClear = this.recordings.filter((r: RecordingMetadata) => {
            const v = r.studyFiling?.valueForLevel(level) ?? null;
            return v === this.deleteTarget || (v === null && this.deleteTarget === '未分类');
        });
        for (const r of toClear) {
            const filing = r.studyFiling ? r.studyFiling.clone() : new StudyFilingPath();
            if (level === 'type')
                filing.type = null;
            else if (level === 'subject')
                filing.subject = null;
            else if (level === 'chapter')
                filing.chapter = null;
            else if (level === 'topic')
                filing.topic = null;
            await this.recordingManager.updateStudyFiling(r.id, filing);
        }
        const folders = await this.folderStore.listFolders(level);
        const match = folders.find(f => f.name === this.deleteTarget);
        if (match)
            await this.folderStore.deleteFolder(match.id);
        this.showDeleteConfirm = false;
        await this.loadAll();
    }
    // ── Build ──
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
            Button.createWithChild();
            Button.width(44);
            Button.height(44);
            Button.borderRadius(22);
            Button.backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity));
            Button.shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '10'),
                radius: 12,
                offsetY: 6
            });
            Button.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, glassStrokeHighOpacity],
                        [0xEFFAF8, glassStrokeMidOpacity],
                        [0x59C7C2, glassAccentOpacity]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 22
            } as BorderOptions);
            Button.onClick(() => this.getUIContext().getRouter().back());
        }, Button);
        BackIcon.bind(this)(18, RokuricsColors.deepText);
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('学习库');
            Text.fontSize(30);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(RokuricsColors.deepText);
            Text.margin({ left: 8 });
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Blank.create();
        }, Blank);
        Blank.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Flat list toggle
            Button.createWithChild();
            // Flat list toggle
            Button.backgroundColor(Color.Transparent);
            // Flat list toggle
            Button.margin({ right: 8 });
            // Flat list toggle
            Button.onClick(() => {
                if (this.isBrowsing) {
                    this.browsePath = [];
                    this.isBrowsing = false;
                    this.currentRecordings = [];
                    this.buildRootGroups();
                }
            });
        }, Button);
        BulletListIcon.bind(this)(14, RokuricsColors.aqua);
        // Flat list toggle
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.backgroundColor(Color.Transparent);
            Button.onClick(() => { this.toggleTrash(); });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(this.showTrash ? '录音' : '废纸篓');
            Text.fontSize(14);
            Text.fontColor(RokuricsColors.aqua);
            Text.fontWeight(FontWeight.Medium);
        }, Text);
        Text.pop();
        Button.pop();
        // Header
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.showTrash) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.TrashList.bind(this)();
                });
            }
            else if (this.isBrowsing) {
                this.ifElseBranchUpdateFunction(1, () => {
                    // Breadcrumb + recording list for browsed folder
                    this.BrowseContent.bind(this)();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(2, () => {
                    // Root: folder tiles + unfiled recordings
                    this.RootContent.bind(this)();
                });
            }
        }, If);
        If.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Rename dialog overlay with color picker
            if (this.showRenameDialog) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create();
                        Column.width('100%');
                        Column.height('100%');
                        Column.justifyContent(FlexAlign.Center);
                        Column.backgroundColor('#50000000');
                        Column.position({ x: 0, y: 0 });
                        Column.onClick(() => { this.showRenameDialog = false; });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 16 });
                        Column.padding(24);
                        Column.borderRadius(20);
                        Column.backgroundColor(Color.White);
                        Column.width('85%');
                        Column.shadow({ radius: 30, color: '#20000000' });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('重命名分类');
                        Text.fontSize(18);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(RokuricsColors.deepText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: this.renameText, placeholder: '新名称' });
                        TextInput.fontSize(16);
                        TextInput.fontColor(RokuricsColors.deepText);
                        TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '80'));
                        TextInput.borderRadius(10);
                        TextInput.padding(14);
                        TextInput.onChange((v: string) => { this.renameText = v; });
                    }, TextInput);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Inline color picker
                        Column.create({ space: 8 });
                        // Inline color picker
                        Column.width('100%');
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('分类颜色');
                        Text.fontSize(13);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create({ space: 8 });
                        Row.width('100%');
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        ForEach.create();
                        const forEachItemGenFunction = _item => {
                            const color = _item;
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Row.create();
                                Row.width(28);
                                Row.height(28);
                                Row.borderRadius(14);
                                Row.backgroundColor(color);
                                Row.justifyContent(FlexAlign.Center);
                                Row.border({
                                    width: this.selectedColor === color ? 2 : 1,
                                    color: this.selectedColor === color ? Color.White : colorAlpha(color, '40'),
                                    radius: 14
                                });
                                Row.onClick(() => { this.selectedColor = color; });
                            }, Row);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                If.create();
                                if (this.selectedColor === color) {
                                    this.ifElseBranchUpdateFunction(0, () => {
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Text.create('✓');
                                            Text.fontSize(11);
                                            Text.fontWeight(FontWeight.Bold);
                                            Text.fontColor(Color.White);
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
                            Row.pop();
                        };
                        this.forEachUpdateFunction(elmtId, FOLDER_COLORS, forEachItemGenFunction);
                    }, ForEach);
                    ForEach.pop();
                    Row.pop();
                    // Inline color picker
                    Column.pop();
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
                        Button.onClick(() => this.commitRename());
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
            // Delete confirm overlay
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Delete confirm overlay
            if (this.showDeleteConfirm) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create();
                        Column.width('100%');
                        Column.height('100%');
                        Column.justifyContent(FlexAlign.Center);
                        Column.backgroundColor('#50000000');
                        Column.position({ x: 0, y: 0 });
                        Column.onClick(() => { this.showDeleteConfirm = false; });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 16 });
                        Column.padding(24);
                        Column.borderRadius(20);
                        Column.backgroundColor(Color.White);
                        Column.width('85%');
                        Column.shadow({ radius: 30, color: '#20000000' });
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('删除分类');
                        Text.fontSize(18);
                        Text.fontWeight(FontWeight.SemiBold);
                        Text.fontColor(RokuricsColors.deepText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(`确定删除「${this.deleteTargetName}」？关联的录音不会被删除。`);
                        Text.fontSize(14);
                        Text.fontColor(RokuricsColors.softText);
                        Text.textAlign(TextAlign.Center);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create({ space: 12 });
                        Row.width('100%');
                        Row.justifyContent(FlexAlign.End);
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.backgroundColor(Color.Transparent);
                        Button.onClick(() => { this.showDeleteConfirm = false; });
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
                        Button.backgroundColor(RokuricsColors.coral);
                        Button.onClick(() => this.commitDelete());
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('删除');
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
    // ── Root content: folder tiles + search ──
    RootContent(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Scroll.create();
            Scroll.width('100%');
            Scroll.layoutWeight(1);
            Scroll.scrollBar(BarState.Off);
            Scroll.padding({ top: 4 });
        }, Scroll);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 16 });
            Column.width('100%');
            Column.padding({ left: 16, right: 16, bottom: 40 });
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Empty state or content
            if (this.recordings.length === 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 12 });
                        Column.width('100%');
                        Column.height(300);
                        Column.justifyContent(FlexAlign.Center);
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('暂无录音');
                        Text.fontSize(16);
                        Text.fontColor(RokuricsColors.tertiaryText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('收到或保存的录音会在这里按门类、课程、章节和主题逐层显示。');
                        Text.fontSize(13);
                        Text.fontColor(RokuricsColors.softText);
                        Text.textAlign(TextAlign.Center);
                        Text.padding({ left: 32, right: 32 });
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('返回首页开始录音');
                        Text.fontSize(13);
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Column.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Search
                        Row.create({ space: 8 });
                        // Search
                        Row.width('100%');
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        TextInput.create({ text: this.searchQuery, placeholder: '搜索录音...' });
                        TextInput.fontSize(13);
                        TextInput.layoutWeight(1);
                        TextInput.height(36);
                        TextInput.borderRadius(18);
                        TextInput.padding({ left: 14, right: 14 });
                        TextInput.backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity));
                        TextInput.onChange((v: string) => { this.searchQuery = v; });
                    }, TextInput);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.height(32);
                        Button.padding({ left: 10, right: 10 });
                        Button.borderRadius(16);
                        Button.backgroundColor(colorAlpha(RokuricsColors.aqua, '14'));
                        Button.onClick(() => { this.sortOrder = this.sortOrder === 'newest' ? 'oldest' : 'newest'; });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(this.sortOrder === 'newest' ? '↓最新' : '↑最早');
                        Text.fontSize(11);
                        Text.fontWeight(FontWeight.Medium);
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    // Search
                    Row.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        // Folder tiles
                        if (this.currentGroups.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.FolderTileGrid.bind(this)();
                            });
                        }
                        // Filter chips
                        else {
                            this.ifElseBranchUpdateFunction(1, () => {
                            });
                        }
                    }, If);
                    If.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Filter chips
                        Row.create({ space: 6 });
                        // Filter chips
                        Row.width('100%');
                    }, Row);
                    this.FilterChip.bind(this)('已转写', this.filterTranscribed(), (v: boolean) => { });
                    this.FilterChip.bind(this)('有笔记', this.filterNoteGenerated(), (v: boolean) => { });
                    this.FilterChip.bind(this)('已上传', this.filterUploaded(), (v: boolean) => { });
                    // Filter chips
                    Row.pop();
                    // Recording list
                    this.RecordingRows.bind(this)(this.getSortedRecordings());
                });
            }
        }, If);
        If.pop();
        Column.pop();
        Scroll.pop();
    }
    FolderTileGrid(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 10 });
            Column.width('100%');
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('分类浏览');
            Text.fontSize(15);
            Text.fontWeight(FontWeight.SemiBold);
            Text.fontColor(RokuricsColors.deepText);
            Text.width('100%');
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 8 });
            Row.width('100%');
            Row.justifyContent(FlexAlign.Start);
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            ForEach.create();
            const forEachItemGenFunction = _item => {
                const group = _item;
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Button.createWithChild();
                    Button.backgroundColor(Color.Transparent);
                    Button.width('48%');
                    Button.onClick(() => { });
                }, Button);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Column.create({ space: 6 });
                    Column.width('100%');
                    Column.padding(14);
                    Column.borderRadius(14);
                    Column.backgroundColor(this.folderColorMap[group.label] ?
                        colorAlpha(this.folderColorMap[group.label], '22') : colorAlpha(RokuricsColors.glassSurface, glassFillOpacity));
                    Column.border({
                        width: this.folderColorMap[group.label] ? 1 : 0,
                        color: this.folderColorMap[group.label] ?
                            colorAlpha(this.folderColorMap[group.label], '44') : Color.Transparent,
                        radius: 14
                    });
                    Column.onClick(() => {
                        this.navigateToPath([group.label]);
                    });
                }, Column);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Color dot indicator
                    Row.create();
                    // Color dot indicator
                    Row.width('100%');
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Circle.create();
                    Circle.width(10);
                    Circle.height(10);
                    Circle.fill(this.folderColorMap[group.label] ?? RokuricsColors.aqua);
                }, Circle);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (this.folderColorMap[group.label]) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(group.label);
                                Text.fontSize(14);
                                Text.fontWeight(FontWeight.SemiBold);
                                Text.fontColor(this.folderColorMap[group.label] ?? RokuricsColors.aqua);
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
                // Color dot indicator
                Row.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    If.create();
                    if (!this.folderColorMap[group.label]) {
                        this.ifElseBranchUpdateFunction(0, () => {
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(group.label);
                                Text.fontSize(14);
                                Text.fontWeight(FontWeight.SemiBold);
                                Text.fontColor(RokuricsColors.deepText);
                                Text.maxLines(1);
                                Text.width('100%');
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
                    Text.create(`${filingLevelTitle(group.level)} · ${group.count} 条`);
                    Text.fontSize(11);
                    Text.fontColor(RokuricsColors.tertiaryText);
                    Text.width('100%');
                }, Text);
                Text.pop();
                Column.pop();
                Button.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    // Context menu
                    Button.createWithChild();
                    // Context menu
                    Button.width(28);
                    // Context menu
                    Button.height(28);
                    // Context menu
                    Button.backgroundColor(Color.Transparent);
                    // Context menu
                    Button.onClick(() => this.openRenameDialog(group.label, group.level));
                }, Button);
                EllipsisIcon.bind(this)(14, RokuricsColors.tertiaryText);
                // Context menu
                Button.pop();
            };
            this.forEachUpdateFunction(elmtId, this.currentGroups, forEachItemGenFunction);
        }, ForEach);
        ForEach.pop();
        Row.pop();
        Column.pop();
    }
    // ── Browse content: breadcrumb + recordings in folder ──
    BrowseContent(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create();
            Column.width('100%');
            Column.layoutWeight(1);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Breadcrumb
            Row.create({ space: 4 });
            // Breadcrumb
            Row.width('100%');
            // Breadcrumb
            Row.padding({ left: 16, right: 16, bottom: 12 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.backgroundColor(Color.Transparent);
            Button.onClick(() => {
                this.browsePath = [];
                this.isBrowsing = false;
                this.buildRootGroups();
            });
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('全部');
            Text.fontSize(13);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            ForEach.create();
            const forEachItemGenFunction = (_item, index: number) => {
                const crumb = _item;
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.onClick(() => {
                        if (index < this.browsePath.length - 1) {
                            const parentPath: string[] = [];
                            for (let i = 0; i <= index; i++)
                                parentPath.push(this.browsePath[i]);
                            this.navigateToPath(parentPath);
                        }
                    });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create('›');
                    Text.fontSize(13);
                    Text.fontColor(RokuricsColors.tertiaryText);
                    Text.margin({ left: 2, right: 2 });
                }, Text);
                Text.pop();
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(crumb);
                    Text.fontSize(13);
                    Text.fontColor(index === this.browsePath.length - 1 ?
                        RokuricsColors.aqua : RokuricsColors.softText);
                    Text.fontWeight(index === this.browsePath.length - 1 ?
                        FontWeight.SemiBold : FontWeight.Regular);
                }, Text);
                Text.pop();
                Row.pop();
            };
            this.forEachUpdateFunction(elmtId, this.browsePath, forEachItemGenFunction, undefined, true, false);
        }, ForEach);
        ForEach.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(`(${this.currentRecordings.length})`);
            Text.fontSize(11);
            Text.fontColor(RokuricsColors.tertiaryText);
        }, Text);
        Text.pop();
        // Breadcrumb
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Sub-folder tiles if any at this level
            if (this.currentGroups.length > 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Row.create({ space: 8 });
                        Row.width('100%');
                        Row.padding({ left: 16, right: 16, bottom: 12 });
                        Row.flexShrink(0);
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        ForEach.create();
                        const forEachItemGenFunction = _item => {
                            const group = _item;
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Button.createWithChild();
                                Button.backgroundColor(Color.Transparent);
                                Button.onClick(() => {
                                    const newPath: string[] = [];
                                    for (const p of this.browsePath)
                                        newPath.push(p);
                                    newPath.push(group.label);
                                    this.navigateToPath(newPath);
                                });
                            }, Button);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Column.create({ space: 4 });
                                Column.padding({ left: 12, right: 12, top: 8, bottom: 8 });
                                Column.borderRadius(12);
                                Column.backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity));
                                Column.border({
                                    width: 1,
                                    color: {
                                        colors: [
                                            [0xFFFFFF, glassStrokeHighOpacity],
                                            [0xEFFAF8, glassStrokeMidOpacity]
                                        ],
                                        direction: GradientDirection.RightBottom
                                    },
                                    radius: 12
                                } as BorderOptions);
                            }, Column);
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(group.label);
                                Text.fontSize(13);
                                Text.fontWeight(FontWeight.SemiBold);
                                Text.fontColor(RokuricsColors.deepText);
                                Text.maxLines(1);
                            }, Text);
                            Text.pop();
                            this.observeComponentCreation2((elmtId, isInitialRender) => {
                                Text.create(`${group.count} 条`);
                                Text.fontSize(11);
                                Text.fontColor(RokuricsColors.tertiaryText);
                            }, Text);
                            Text.pop();
                            Column.pop();
                            Button.pop();
                        };
                        this.forEachUpdateFunction(elmtId, this.currentGroups, forEachItemGenFunction);
                    }, ForEach);
                    ForEach.pop();
                    Row.pop();
                });
            }
            // Recording rows
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        // Recording rows
        this.RecordingRows.bind(this)(ObservedObject.GetRawObject(this.currentRecordings));
        Column.pop();
    }
    // ── Recording rows (shared) ──
    RecordingRows(items: RecordingMetadata[], parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (items.length === 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 8 });
                        Column.width('100%');
                        Column.height(200);
                        Column.justifyContent(FlexAlign.Center);
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('此分类下暂无录音');
                        Text.fontSize(15);
                        Text.fontColor(RokuricsColors.tertiaryText);
                    }, Text);
                    Text.pop();
                    Column.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        List.create({ space: 10 });
                        List.width('100%');
                        List.layoutWeight(1);
                        List.padding({ left: 16, right: 16 });
                        List.scrollBar(BarState.Off);
                    }, List);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        ForEach.create();
                        const forEachItemGenFunction = _item => {
                            const recording = _item;
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
                                        Row.padding(14);
                                        Row.borderRadius(16);
                                        Row.backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity));
                                        Row.shadow({
                                            color: colorAlpha(RokuricsColors.shadowColor, '08'),
                                            radius: 10, offsetY: 5
                                        });
                                        Row.border({
                                            width: 1,
                                            color: {
                                                colors: [
                                                    [0xFFFFFF, glassStrokeHighOpacity],
                                                    [0xEFFAF8, glassStrokeMidOpacity],
                                                    [0x91E8D6, glassAccentOpacity]
                                                ],
                                                direction: GradientDirection.RightBottom
                                            },
                                            radius: 16
                                        } as BorderOptions);
                                        Row.onClick(() => this.openDetail(recording.id));
                                    }, Row);
                                    // Waveform icon
                                    this.WaveformGlyph.bind(this)();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Column.create({ space: 5 });
                                        Column.layoutWeight(1);
                                        Column.alignItems(HorizontalAlign.Start);
                                        Column.margin({ left: 12 });
                                    }, Column);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(recording.title);
                                        Text.fontSize(16);
                                        Text.fontWeight(FontWeight.SemiBold);
                                        Text.fontColor(RokuricsColors.deepText);
                                        Text.maxLines(1);
                                        Text.textOverflow({ overflow: TextOverflow.Ellipsis });
                                    }, Text);
                                    Text.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Row.create({ space: 8 });
                                    }, Row);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(formatShortTime(recording.createdAt));
                                        Text.fontSize(12);
                                        Text.fontColor(RokuricsColors.softText);
                                    }, Text);
                                    Text.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create('·');
                                        Text.fontSize(12);
                                        Text.fontColor(RokuricsColors.tertiaryText);
                                    }, Text);
                                    Text.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(formatDuration(recording.duration));
                                        Text.fontSize(12);
                                        Text.fontColor(RokuricsColors.tertiaryText);
                                    }, Text);
                                    Text.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        If.create();
                                        if (recording.uploadStatus === 'uploaded') {
                                            this.ifElseBranchUpdateFunction(0, () => {
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Row.create({ space: 3 });
                                                    Row.padding({ left: 6, right: 6, top: 2, bottom: 2 });
                                                    Row.borderRadius(6);
                                                    Row.backgroundColor(colorAlpha(RokuricsColors.mint, '18'));
                                                }, Row);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Circle.create();
                                                    Circle.width(6);
                                                    Circle.height(6);
                                                    Circle.fill(RokuricsColors.mint);
                                                }, Circle);
                                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                                    Text.create('已上传');
                                                    Text.fontSize(10);
                                                    Text.fontColor(RokuricsColors.mint);
                                                }, Text);
                                                Text.pop();
                                                Row.pop();
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
                                        if (recording.transcriptionStatus === 'transcribed') {
                                            this.ifElseBranchUpdateFunction(0, () => {
                                                DocBadgeIcon.bind(this)(14, RokuricsColors.aqua);
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
                                        if (recording.noteStatus === 'generated') {
                                            this.ifElseBranchUpdateFunction(0, () => {
                                                NoteBadgeIcon.bind(this)(14, RokuricsColors.mint);
                                            });
                                        }
                                        else {
                                            this.ifElseBranchUpdateFunction(1, () => {
                                            });
                                        }
                                    }, If);
                                    If.pop();
                                    Row.pop();
                                    Column.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Button.createWithChild();
                                        Button.width(36);
                                        Button.height(36);
                                        Button.backgroundColor(Color.Transparent);
                                        Button.onClick(() => this.deleteAndReload(recording.id));
                                    }, Button);
                                    TrashIcon.bind(this)(16, RokuricsColors.tertiaryText);
                                    Button.pop();
                                    Row.pop();
                                    ListItem.pop();
                                };
                                this.observeComponentCreation2(itemCreation2, ListItem);
                                ListItem.pop();
                            }
                        };
                        this.forEachUpdateFunction(elmtId, items, forEachItemGenFunction);
                    }, ForEach);
                    ForEach.pop();
                    List.pop();
                });
            }
        }, If);
        If.pop();
    }
    WaveformGlyph(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Stack.create();
            Stack.width(50);
            Stack.height(50);
            Stack.borderRadius(25);
            Stack.backgroundColor(colorAlpha(RokuricsColors.glassSurface, '56'));
            Stack.shadow({ color: colorAlpha(RokuricsColors.shadowColor, '08'), radius: 9, offsetY: 4 });
            Stack.border({
                width: 1,
                color: {
                    colors: [
                        [0xFFFFFF, 0.38],
                        [0xEFFAF8, 0.12],
                        [0x59C7C2, 0.10]
                    ],
                    direction: GradientDirection.RightBottom
                },
                radius: 25
            } as BorderOptions);
        }, Stack);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 2.5 });
            Row.justifyContent(FlexAlign.Center);
            Row.alignItems(VerticalAlign.Center);
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Rect.create();
            Rect.width(3);
            Rect.height(8);
            Rect.radius(1.5);
            Rect.fill(RokuricsColors.aqua);
        }, Rect);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Rect.create();
            Rect.width(3);
            Rect.height(18);
            Rect.radius(1.5);
            Rect.fill(RokuricsColors.aqua);
        }, Rect);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Rect.create();
            Rect.width(3);
            Rect.height(12);
            Rect.radius(1.5);
            Rect.fill(RokuricsColors.aqua);
        }, Rect);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Rect.create();
            Rect.width(3);
            Rect.height(24);
            Rect.radius(1.5);
            Rect.fill(RokuricsColors.aqua);
        }, Rect);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Rect.create();
            Rect.width(3);
            Rect.height(15);
            Rect.radius(1.5);
            Rect.fill(RokuricsColors.aqua);
        }, Rect);
        Row.pop();
        Stack.pop();
    }
    TrashList(parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.trashedRecordings.length === 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 12 });
                        Column.width('100%');
                        Column.height('60%');
                        Column.justifyContent(FlexAlign.Center);
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('废纸篓为空');
                        Text.fontSize(16);
                        Text.fontColor(RokuricsColors.tertiaryText);
                    }, Text);
                    Text.pop();
                    Column.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        List.create({ space: 10 });
                        List.width('100%');
                        List.layoutWeight(1);
                        List.padding({ left: 16, right: 16 });
                        List.scrollBar(BarState.Off);
                    }, List);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        ForEach.create();
                        const forEachItemGenFunction = _item => {
                            const recording = _item;
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
                                        Row.padding(14);
                                        Row.borderRadius(16);
                                        Row.backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity));
                                        Row.shadow({ color: colorAlpha(RokuricsColors.shadowColor, '08'), radius: 8, offsetY: 4 });
                                        Row.border({
                                            width: 1,
                                            color: {
                                                colors: [
                                                    [0xFFFFFF, glassStrokeHighOpacity],
                                                    [0xEFFAF8, glassStrokeMidOpacity],
                                                    [0xE06B6E, 0.08]
                                                ],
                                                direction: GradientDirection.RightBottom
                                            },
                                            radius: 16
                                        } as BorderOptions);
                                    }, Row);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Column.create({ space: 5 });
                                        Column.layoutWeight(1);
                                        Column.alignItems(HorizontalAlign.Start);
                                    }, Column);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(recording.title);
                                        Text.fontSize(16);
                                        Text.fontWeight(FontWeight.SemiBold);
                                        Text.fontColor(RokuricsColors.deepText);
                                        Text.maxLines(1);
                                    }, Text);
                                    Text.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Row.create({ space: 8 });
                                    }, Row);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(formatShortTime(recording.createdAt));
                                        Text.fontSize(12);
                                        Text.fontColor(RokuricsColors.softText);
                                    }, Text);
                                    Text.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(formatDuration(recording.duration));
                                        Text.fontSize(12);
                                        Text.fontColor(RokuricsColors.tertiaryText);
                                    }, Text);
                                    Text.pop();
                                    Row.pop();
                                    Column.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Button.createWithChild();
                                        Button.backgroundColor(Color.Transparent);
                                        Button.margin({ right: 8 });
                                        Button.onClick(() => this.restoreAndReload(recording.id));
                                    }, Button);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create('恢复');
                                        Text.fontSize(13);
                                        Text.fontWeight(FontWeight.Medium);
                                        Text.fontColor(RokuricsColors.aqua);
                                    }, Text);
                                    Text.pop();
                                    Button.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Button.createWithChild();
                                        Button.backgroundColor(Color.Transparent);
                                        Button.onClick(() => this.permDeleteAndReload(recording.id));
                                    }, Button);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create('删除');
                                        Text.fontSize(13);
                                        Text.fontWeight(FontWeight.Medium);
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
                        this.forEachUpdateFunction(elmtId, this.trashedRecordings, forEachItemGenFunction);
                    }, ForEach);
                    ForEach.pop();
                    List.pop();
                });
            }
        }, If);
        If.pop();
    }
    FilterChip(label: string, active: boolean, onToggle: (v: boolean) => void, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create({ space: 4 });
            Row.padding({ left: 10, right: 10, top: 5, bottom: 5 });
            Row.borderRadius(12);
            Row.backgroundColor(active ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, glassFillOpacity));
            Row.border({ width: active ? 0 : 1, color: colorAlpha(RokuricsColors.softText, '18'), radius: 12 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (active) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('✓');
                        Text.fontSize(10);
                        Text.fontColor(Color.White);
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
            Text.create(label);
            Text.fontSize(11);
            Text.fontWeight(FontWeight.Medium);
            Text.fontColor(active ? Color.White : RokuricsColors.softText);
        }, Text);
        Text.pop();
        Row.pop();
    }
    private getSortedRecordings(): RecordingMetadata[] {
        let result = this.recordings;
        const q = this.searchQuery.trim().toLowerCase();
        if (q.length > 0) {
            result = result.filter((r: RecordingMetadata) => r.title.toLowerCase().indexOf(q) >= 0);
        }
        result.sort((a: RecordingMetadata, b: RecordingMetadata) => {
            if (this.sortOrder === 'newest')
                return b.createdAt.getTime() - a.createdAt.getTime();
            return a.createdAt.getTime() - b.createdAt.getTime();
        });
        return result;
    }
    private filterTranscribed(): boolean { return false; }
    private filterNoteGenerated(): boolean { return false; }
    private filterUploaded(): boolean { return false; }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "RecordingLibraryPage";
    }
}
registerNamedRoute(() => new RecordingLibraryPage(undefined, {}), "", { bundleName: "com.vita0818.rokurics", moduleName: "entry", pagePath: "pages/RecordingLibraryPage", pageFullPath: "entry/src/main/ets/pages/RecordingLibraryPage", integratedHsp: "false", moduleType: "followWithHap" });
