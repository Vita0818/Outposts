if (!("finalizeConstruction" in ViewPU.prototype)) {
    Reflect.set(ViewPU.prototype, "finalizeConstruction", () => { });
}
interface StudyLibraryBrowserPage_Params {
    recordingManager?: RecordingManager;
    allRecordings?: RecordingMetadata[];
    groups?: FilingGroup[];
    currentPath?: string[];
    breadcrumb?: string[];
    showRecordings?: RecordingMetadata[];
    showCreateFolder?: boolean;
    newFolderName?: string;
    newFolderLevel?: string;
    newFolderColor?: string;
    showRenameFolder?: boolean;
    renameTargetId?: string;
    renameText?: string;
    showDeleteConfirm?: boolean;
    deleteTargetId?: string;
    deleteTargetName?: string;
    editMessage?: string;
    folderColorMap?: Record<string, string>;
}
import { getSharedRecordingManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/RecordingManager";
import type { RecordingManager } from "@bundle:com.vita0818.rokurics/entry/ets/services/RecordingManager";
import { StudyFilingPath, filingLevelTitle } from "@bundle:com.vita0818.rokurics/entry/ets/models/RecordingModels";
import type { RecordingMetadata } from "@bundle:com.vita0818.rokurics/entry/ets/models/RecordingModels";
import { formatDuration, formatShortTime } from "@bundle:com.vita0818.rokurics/entry/ets/utils/FormatHelpers";
import { RokuricsColors, FontWeight } from "@bundle:com.vita0818.rokurics/entry/ets/utils/RokuricsTheme";
import { StudyFolderStore } from "@bundle:com.vita0818.rokurics/entry/ets/services/StudyFolderStore";
const HIERARCHY_LEVELS: string[] = ['type', 'subject', 'chapter', 'topic'];
const FOLDER_COLORS: string[] = ['#59C7C2', '#9EE8C7', '#73C7F0', '#E06B6E', '#B8A6D6', '#6B9FD4'];
interface FilingGroup {
    path: string;
    level: string;
    label: string;
    count: number;
    children: FilingGroup[];
    recordings: RecordingMetadata[];
}
class StudyLibraryBrowserPage extends ViewPU {
    constructor(parent, params, __localStorage, elmtId = -1, paramsLambda = undefined, extraInfo) {
        super(parent, __localStorage, elmtId, extraInfo);
        if (typeof paramsLambda === "function") {
            this.paramsGenerator_ = paramsLambda;
        }
        this.recordingManager = getSharedRecordingManager();
        this.__allRecordings = new ObservedPropertyObjectPU([], this, "allRecordings");
        this.__groups = new ObservedPropertyObjectPU([], this, "groups");
        this.__currentPath = new ObservedPropertyObjectPU([], this, "currentPath");
        this.__breadcrumb = new ObservedPropertyObjectPU(['全部'], this, "breadcrumb");
        this.__showRecordings = new ObservedPropertyObjectPU([], this, "showRecordings");
        this.__showCreateFolder = new ObservedPropertySimplePU(false, this, "showCreateFolder");
        this.__newFolderName = new ObservedPropertySimplePU('', this, "newFolderName");
        this.__newFolderLevel = new ObservedPropertySimplePU('', this, "newFolderLevel");
        this.__newFolderColor = new ObservedPropertySimplePU('#59C7C2', this, "newFolderColor");
        this.__showRenameFolder = new ObservedPropertySimplePU(false, this, "showRenameFolder");
        this.__renameTargetId = new ObservedPropertySimplePU('', this, "renameTargetId");
        this.__renameText = new ObservedPropertySimplePU('', this, "renameText");
        this.__showDeleteConfirm = new ObservedPropertySimplePU(false, this, "showDeleteConfirm");
        this.__deleteTargetId = new ObservedPropertySimplePU('', this, "deleteTargetId");
        this.__deleteTargetName = new ObservedPropertySimplePU('', this, "deleteTargetName");
        this.__editMessage = new ObservedPropertySimplePU('', this, "editMessage");
        this.__folderColorMap = new ObservedPropertyObjectPU({}, this, "folderColorMap");
        this.setInitiallyProvidedValue(params);
        this.finalizeConstruction();
    }
    setInitiallyProvidedValue(params: StudyLibraryBrowserPage_Params) {
        if (params.recordingManager !== undefined) {
            this.recordingManager = params.recordingManager;
        }
        if (params.allRecordings !== undefined) {
            this.allRecordings = params.allRecordings;
        }
        if (params.groups !== undefined) {
            this.groups = params.groups;
        }
        if (params.currentPath !== undefined) {
            this.currentPath = params.currentPath;
        }
        if (params.breadcrumb !== undefined) {
            this.breadcrumb = params.breadcrumb;
        }
        if (params.showRecordings !== undefined) {
            this.showRecordings = params.showRecordings;
        }
        if (params.showCreateFolder !== undefined) {
            this.showCreateFolder = params.showCreateFolder;
        }
        if (params.newFolderName !== undefined) {
            this.newFolderName = params.newFolderName;
        }
        if (params.newFolderLevel !== undefined) {
            this.newFolderLevel = params.newFolderLevel;
        }
        if (params.newFolderColor !== undefined) {
            this.newFolderColor = params.newFolderColor;
        }
        if (params.showRenameFolder !== undefined) {
            this.showRenameFolder = params.showRenameFolder;
        }
        if (params.renameTargetId !== undefined) {
            this.renameTargetId = params.renameTargetId;
        }
        if (params.renameText !== undefined) {
            this.renameText = params.renameText;
        }
        if (params.showDeleteConfirm !== undefined) {
            this.showDeleteConfirm = params.showDeleteConfirm;
        }
        if (params.deleteTargetId !== undefined) {
            this.deleteTargetId = params.deleteTargetId;
        }
        if (params.deleteTargetName !== undefined) {
            this.deleteTargetName = params.deleteTargetName;
        }
        if (params.editMessage !== undefined) {
            this.editMessage = params.editMessage;
        }
        if (params.folderColorMap !== undefined) {
            this.folderColorMap = params.folderColorMap;
        }
    }
    updateStateVars(params: StudyLibraryBrowserPage_Params) {
    }
    purgeVariableDependenciesOnElmtId(rmElmtId) {
        this.__allRecordings.purgeDependencyOnElmtId(rmElmtId);
        this.__groups.purgeDependencyOnElmtId(rmElmtId);
        this.__currentPath.purgeDependencyOnElmtId(rmElmtId);
        this.__breadcrumb.purgeDependencyOnElmtId(rmElmtId);
        this.__showRecordings.purgeDependencyOnElmtId(rmElmtId);
        this.__showCreateFolder.purgeDependencyOnElmtId(rmElmtId);
        this.__newFolderName.purgeDependencyOnElmtId(rmElmtId);
        this.__newFolderLevel.purgeDependencyOnElmtId(rmElmtId);
        this.__newFolderColor.purgeDependencyOnElmtId(rmElmtId);
        this.__showRenameFolder.purgeDependencyOnElmtId(rmElmtId);
        this.__renameTargetId.purgeDependencyOnElmtId(rmElmtId);
        this.__renameText.purgeDependencyOnElmtId(rmElmtId);
        this.__showDeleteConfirm.purgeDependencyOnElmtId(rmElmtId);
        this.__deleteTargetId.purgeDependencyOnElmtId(rmElmtId);
        this.__deleteTargetName.purgeDependencyOnElmtId(rmElmtId);
        this.__editMessage.purgeDependencyOnElmtId(rmElmtId);
        this.__folderColorMap.purgeDependencyOnElmtId(rmElmtId);
    }
    aboutToBeDeleted() {
        this.__allRecordings.aboutToBeDeleted();
        this.__groups.aboutToBeDeleted();
        this.__currentPath.aboutToBeDeleted();
        this.__breadcrumb.aboutToBeDeleted();
        this.__showRecordings.aboutToBeDeleted();
        this.__showCreateFolder.aboutToBeDeleted();
        this.__newFolderName.aboutToBeDeleted();
        this.__newFolderLevel.aboutToBeDeleted();
        this.__newFolderColor.aboutToBeDeleted();
        this.__showRenameFolder.aboutToBeDeleted();
        this.__renameTargetId.aboutToBeDeleted();
        this.__renameText.aboutToBeDeleted();
        this.__showDeleteConfirm.aboutToBeDeleted();
        this.__deleteTargetId.aboutToBeDeleted();
        this.__deleteTargetName.aboutToBeDeleted();
        this.__editMessage.aboutToBeDeleted();
        this.__folderColorMap.aboutToBeDeleted();
        SubscriberManager.Get().delete(this.id__());
        this.aboutToBeDeletedInternal();
    }
    private recordingManager: RecordingManager;
    private __allRecordings: ObservedPropertyObjectPU<RecordingMetadata[]>;
    get allRecordings() {
        return this.__allRecordings.get();
    }
    set allRecordings(newValue: RecordingMetadata[]) {
        this.__allRecordings.set(newValue);
    }
    private __groups: ObservedPropertyObjectPU<FilingGroup[]>;
    get groups() {
        return this.__groups.get();
    }
    set groups(newValue: FilingGroup[]) {
        this.__groups.set(newValue);
    }
    private __currentPath: ObservedPropertyObjectPU<string[]>;
    get currentPath() {
        return this.__currentPath.get();
    }
    set currentPath(newValue: string[]) {
        this.__currentPath.set(newValue);
    }
    private __breadcrumb: ObservedPropertyObjectPU<string[]>;
    get breadcrumb() {
        return this.__breadcrumb.get();
    }
    set breadcrumb(newValue: string[]) {
        this.__breadcrumb.set(newValue);
    }
    private __showRecordings: ObservedPropertyObjectPU<RecordingMetadata[]>;
    get showRecordings() {
        return this.__showRecordings.get();
    }
    set showRecordings(newValue: RecordingMetadata[]) {
        this.__showRecordings.set(newValue);
    }
    private __showCreateFolder: ObservedPropertySimplePU<boolean>;
    get showCreateFolder() {
        return this.__showCreateFolder.get();
    }
    set showCreateFolder(newValue: boolean) {
        this.__showCreateFolder.set(newValue);
    }
    private __newFolderName: ObservedPropertySimplePU<string>;
    get newFolderName() {
        return this.__newFolderName.get();
    }
    set newFolderName(newValue: string) {
        this.__newFolderName.set(newValue);
    }
    private __newFolderLevel: ObservedPropertySimplePU<string>;
    get newFolderLevel() {
        return this.__newFolderLevel.get();
    }
    set newFolderLevel(newValue: string) {
        this.__newFolderLevel.set(newValue);
    }
    private __newFolderColor: ObservedPropertySimplePU<string>;
    get newFolderColor() {
        return this.__newFolderColor.get();
    }
    set newFolderColor(newValue: string) {
        this.__newFolderColor.set(newValue);
    }
    private __showRenameFolder: ObservedPropertySimplePU<boolean>;
    get showRenameFolder() {
        return this.__showRenameFolder.get();
    }
    set showRenameFolder(newValue: boolean) {
        this.__showRenameFolder.set(newValue);
    }
    private __renameTargetId: ObservedPropertySimplePU<string>;
    get renameTargetId() {
        return this.__renameTargetId.get();
    }
    set renameTargetId(newValue: string) {
        this.__renameTargetId.set(newValue);
    }
    private __renameText: ObservedPropertySimplePU<string>;
    get renameText() {
        return this.__renameText.get();
    }
    set renameText(newValue: string) {
        this.__renameText.set(newValue);
    }
    private __showDeleteConfirm: ObservedPropertySimplePU<boolean>;
    get showDeleteConfirm() {
        return this.__showDeleteConfirm.get();
    }
    set showDeleteConfirm(newValue: boolean) {
        this.__showDeleteConfirm.set(newValue);
    }
    private __deleteTargetId: ObservedPropertySimplePU<string>;
    get deleteTargetId() {
        return this.__deleteTargetId.get();
    }
    set deleteTargetId(newValue: string) {
        this.__deleteTargetId.set(newValue);
    }
    private __deleteTargetName: ObservedPropertySimplePU<string>;
    get deleteTargetName() {
        return this.__deleteTargetName.get();
    }
    set deleteTargetName(newValue: string) {
        this.__deleteTargetName.set(newValue);
    }
    private __editMessage: ObservedPropertySimplePU<string>;
    get editMessage() {
        return this.__editMessage.get();
    }
    set editMessage(newValue: string) {
        this.__editMessage.set(newValue);
    }
    private __folderColorMap: ObservedPropertyObjectPU<Record<string, string>>;
    get folderColorMap() {
        return this.__folderColorMap.get();
    }
    set folderColorMap(newValue: Record<string, string>) {
        this.__folderColorMap.set(newValue);
    }
    aboutToAppear(): void {
        this.loadData();
    }
    async loadData(): Promise<void> {
        await this.recordingManager.reloadRecordings();
        this.allRecordings = this.recordingManager.recordings.filter((r: RecordingMetadata) => !r.isDeleted);
        this.groups = this.buildGroups(this.allRecordings, 0, []);
        await this.loadFolderColors();
        this.navigateTo([]);
    }
    private async loadFolderColors(): Promise<void> {
        const store = new StudyFolderStore(getContext(this));
        const allFolders = await store.listFolders();
        const map: Record<string, string> = {};
        for (const f of allFolders) {
            if (f.colorToken) {
                map[f.name] = f.colorToken;
            }
        }
        this.folderColorMap = map;
    }
    private buildGroups(recordings: RecordingMetadata[], levelIndex: number, parentPath: string[]): FilingGroup[] {
        if (levelIndex >= HIERARCHY_LEVELS.length)
            return [];
        const level = HIERARCHY_LEVELS[levelIndex];
        const map = new Map<string, RecordingMetadata[]>();
        for (const r of recordings) {
            const value = r.studyFiling?.valueForLevel(level) ?? null;
            const key = value || '未分类';
            if (!map.has(key))
                map.set(key, []);
            map.get(key)!.push(r);
        }
        const groupList: FilingGroup[] = [];
        const entries = Array.from(map.entries());
        entries.sort((a, b) => a[0].localeCompare(b[0]));
        for (const entry of entries) {
            const label = entry[0];
            const recs = entry[1];
            const path: string[] = [];
            for (const p of parentPath) {
                path.push(p);
            }
            path.push(label);
            const children = levelIndex < HIERARCHY_LEVELS.length - 1
                ? this.buildGroups(recs, levelIndex + 1, path)
                : [];
            const allChildren = this.flattenCount(children);
            const filingGroup: FilingGroup = {
                path: path.join(' / '),
                level: level,
                label: label,
                count: recs.length,
                children: children,
                recordings: levelIndex === HIERARCHY_LEVELS.length - 1 ? recs : allChildren
            };
            groupList.push(filingGroup);
        }
        return groupList;
    }
    private flattenCount(groups: FilingGroup[]): RecordingMetadata[] {
        const result: RecordingMetadata[] = [];
        for (const g of groups) {
            if (g.children.length === 0) {
                for (const r of g.recordings)
                    result.push(r);
            }
            else {
                for (const r of this.flattenCount(g.children))
                    result.push(r);
            }
        }
        return result;
    }
    private navigateTo(path: string[]): void {
        this.currentPath = path;
        this.breadcrumb = ['全部', ...path];
        if (path.length === 0) {
            // Show top-level groups
            this.groups = this.buildGroups(this.allRecordings, 0, []);
            this.showRecordings = [];
        }
        else {
            this.showRecordings = this.getRecordingsAtPath(path);
        }
    }
    private getRecordingsAtPath(path: string[]): RecordingMetadata[] {
        let filtered = this.allRecordings;
        for (let i = 0; i < path.length; i++) {
            const level = HIERARCHY_LEVELS[i];
            const segment = path[i];
            filtered = filtered.filter((r: RecordingMetadata) => {
                const v = r.studyFiling?.valueForLevel(level) ?? null;
                return (v || '未分类') === segment;
            });
        }
        return filtered;
    }
    private openDetail(id: string): void {
        this.getUIContext().getRouter().pushUrl({ url: 'pages/RecordingDetailPage', params: { recordingId: id } });
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
            Row.padding({ left: 16, right: 16, top: 56, bottom: 12 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Button.createWithChild();
            Button.width(44);
            Button.height(44);
            Button.backgroundColor(Color.Transparent);
            Button.onClick(() => this.getUIContext().getRouter().back());
        }, Button);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('←');
            Text.fontSize(20);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        Button.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create('学习库浏览');
            Text.fontSize(24);
            Text.fontWeight(FontWeight.Bold);
            Text.fontColor(RokuricsColors.deepText);
        }, Text);
        Text.pop();
        // Header
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            // Breadcrumb
            Row.create({ space: 4 });
            // Breadcrumb
            Row.width('100%');
            // Breadcrumb
            Row.padding({ left: 16, right: 16, bottom: 12 });
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            ForEach.create();
            const forEachItemGenFunction = (_item, index: number) => {
                const crumb = _item;
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Row.create();
                    Row.onClick(() => {
                        if (index === 0) {
                            this.navigateTo([]);
                        }
                        else {
                            this.navigateTo(this.currentPath.slice(0, index));
                        }
                    });
                }, Row);
                this.observeComponentCreation2((elmtId, isInitialRender) => {
                    Text.create(crumb);
                    Text.fontSize(13);
                    Text.fontColor(index === this.breadcrumb.length - 1 ?
                        RokuricsColors.aqua : RokuricsColors.softText);
                    Text.fontWeight(index === this.breadcrumb.length - 1 ?
                        FontWeight.SemiBold : FontWeight.Regular);
                }, Text);
                Text.pop();
                Row.pop();
            };
            this.forEachUpdateFunction(elmtId, this.breadcrumb, forEachItemGenFunction, undefined, true, false);
        }, ForEach);
        ForEach.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.breadcrumb.length > 1) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create(`(${this.showRecordings.length})`);
                        Text.fontSize(12);
                        Text.fontColor(RokuricsColors.tertiaryText);
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
        // Breadcrumb
        Row.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (this.allRecordings.length === 0) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create({ space: 12 });
                        Column.width('100%');
                        Column.height('60%');
                        Column.justifyContent(FlexAlign.Center);
                    }, Column);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('暂无已归档录音');
                        Text.fontSize(16);
                        Text.fontColor(RokuricsColors.tertiaryText);
                    }, Text);
                    Text.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('完成录音后在详情页设置学习归档');
                        Text.fontSize(13);
                        Text.fontColor(RokuricsColors.softText);
                    }, Text);
                    Text.pop();
                    Column.pop();
                });
            }
            else if (this.currentPath.length === 0) {
                this.ifElseBranchUpdateFunction(1, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Create folder button
                        Row.create();
                        // Create folder button
                        Row.width('100%');
                        // Create folder button
                        Row.padding({ left: 16, right: 16, bottom: 8 });
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Blank.create();
                    }, Blank);
                    Blank.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.backgroundColor(Color.Transparent);
                        Button.onClick(() => {
                            this.showCreateFolder = !this.showCreateFolder;
                            this.newFolderName = '';
                            this.newFolderLevel = 'type';
                        });
                    }, Button);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('+ 新建分类');
                        Text.fontSize(13);
                        Text.fontColor(RokuricsColors.aqua);
                    }, Text);
                    Text.pop();
                    Button.pop();
                    // Create folder button
                    Row.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        // Create folder dialog inline
                        if (this.showCreateFolder) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Column.create({ space: 8 });
                                    Column.width('90%');
                                    Column.padding(14);
                                    Column.borderRadius(14);
                                    Column.backgroundColor(RokuricsColors.glassSurface + '66');
                                    Column.margin({ left: 16, right: 16, bottom: 8 });
                                }, Column);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Row.create();
                                    Row.width('100%');
                                }, Row);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create('新建分类');
                                    Text.fontSize(14);
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
                                    Button.width(32);
                                    Button.height(32);
                                    Button.backgroundColor(Color.Transparent);
                                    Button.onClick(() => { this.showCreateFolder = false; });
                                }, Button);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create('✕');
                                    Text.fontSize(14);
                                    Text.fontColor(RokuricsColors.softText);
                                }, Text);
                                Text.pop();
                                Button.pop();
                                Row.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Row.create({ space: 6 });
                                }, Row);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    ForEach.create();
                                    const forEachItemGenFunction = _item => {
                                        const level = _item;
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Text.create(filingLevelTitle(level));
                                            Text.fontSize(12);
                                            Text.fontColor(this.newFolderLevel === level ? Color.White : RokuricsColors.softText);
                                            Text.padding({ left: 10, right: 10, top: 5, bottom: 5 });
                                            Text.borderRadius(12);
                                            Text.backgroundColor(this.newFolderLevel === level ? RokuricsColors.aqua : RokuricsColors.glassSurface + '50');
                                            Text.onClick(() => { this.newFolderLevel = level; });
                                        }, Text);
                                        Text.pop();
                                    };
                                    this.forEachUpdateFunction(elmtId, ['type', 'subject', 'chapter'], forEachItemGenFunction);
                                }, ForEach);
                                ForEach.pop();
                                Row.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    // Color chips
                                    Row.create({ space: 6 });
                                }, Row);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create('颜色');
                                    Text.fontSize(12);
                                    Text.fontColor(RokuricsColors.softText);
                                    Text.margin({ right: 4 });
                                }, Text);
                                Text.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    ForEach.create();
                                    const forEachItemGenFunction = _item => {
                                        const color = _item;
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            Row.create();
                                            Row.width(24);
                                            Row.height(24);
                                            Row.borderRadius(12);
                                            Row.backgroundColor(color);
                                            Row.justifyContent(FlexAlign.Center);
                                            Row.border({ width: this.newFolderColor === color ? 2 : 0, color: Color.White });
                                            Row.onClick(() => { this.newFolderColor = color; });
                                        }, Row);
                                        this.observeComponentCreation2((elmtId, isInitialRender) => {
                                            If.create();
                                            if (this.newFolderColor === color) {
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
                                        Row.pop();
                                    };
                                    this.forEachUpdateFunction(elmtId, FOLDER_COLORS, forEachItemGenFunction);
                                }, ForEach);
                                ForEach.pop();
                                // Color chips
                                Row.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Row.create({ space: 8 });
                                    Row.width('100%');
                                }, Row);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    TextInput.create({ text: this.newFolderName, placeholder: '分类名称' });
                                    TextInput.fontSize(14);
                                    TextInput.layoutWeight(1);
                                    TextInput.borderRadius(8);
                                    TextInput.padding({ left: 12, right: 12, top: 8, bottom: 8 });
                                    TextInput.backgroundColor(RokuricsColors.glassSurface + '40');
                                    TextInput.onChange((v: string) => { this.newFolderName = v; });
                                }, TextInput);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Button.createWithChild();
                                    Button.padding({ left: 16, right: 16, top: 8, bottom: 8 });
                                    Button.borderRadius(8);
                                    Button.backgroundColor(this.newFolderName.trim().length > 0 ? RokuricsColors.aqua : RokuricsColors.tertiaryText);
                                    Button.enabled(this.newFolderName.trim().length > 0);
                                    Button.onClick(async () => { await this.createFolder(); });
                                }, Button);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create('创建');
                                    Text.fontSize(13);
                                    Text.fontColor(Color.White);
                                }, Text);
                                Text.pop();
                                Button.pop();
                                Row.pop();
                                Column.pop();
                            });
                        }
                        // Group view - show filing categories
                        else {
                            this.ifElseBranchUpdateFunction(1, () => {
                            });
                        }
                    }, If);
                    If.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        // Group view - show filing categories
                        List.create({ space: 8 });
                        // Group view - show filing categories
                        List.width('100%');
                        // Group view - show filing categories
                        List.layoutWeight(1);
                        // Group view - show filing categories
                        List.padding({ left: 16, right: 16 });
                        // Group view - show filing categories
                        List.scrollBar(BarState.Off);
                    }, List);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        ForEach.create();
                        const forEachItemGenFunction = _item => {
                            const group = _item;
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
                                        Row.padding(16);
                                        Row.borderRadius(14);
                                        Row.backgroundColor(this.folderColorMap[group.label] ?
                                            this.folderColorMap[group.label] + '22' : RokuricsColors.glassSurface + '66');
                                        Row.border({ width: this.folderColorMap[group.label] ? 1 : 0,
                                            color: this.folderColorMap[group.label] ?
                                                this.folderColorMap[group.label] + '44' : Color.Transparent });
                                        Row.onClick(() => {
                                            const path = [group.label];
                                            this.navigateTo(path);
                                        });
                                    }, Row);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Column.create({ space: 4 });
                                        Column.layoutWeight(1);
                                        Column.alignItems(HorizontalAlign.Start);
                                    }, Column);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(group.label);
                                        Text.fontSize(16);
                                        Text.fontWeight(FontWeight.SemiBold);
                                        Text.fontColor(RokuricsColors.deepText);
                                    }, Text);
                                    Text.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create(`${filingLevelTitle(group.level)} · ${group.count} 条录音`);
                                        Text.fontSize(12);
                                        Text.fontColor(RokuricsColors.tertiaryText);
                                    }, Text);
                                    Text.pop();
                                    Column.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        // Context menu for folder actions
                                        Button.createWithChild();
                                        // Context menu for folder actions
                                        Button.width(32);
                                        // Context menu for folder actions
                                        Button.height(32);
                                        // Context menu for folder actions
                                        Button.backgroundColor(Color.Transparent);
                                        // Context menu for folder actions
                                        Button.margin({ right: 4 });
                                        // Context menu for folder actions
                                        Button.onClick(() => {
                                            this.renameTargetId = group.label;
                                            this.renameText = group.label;
                                            this.showRenameFolder = true;
                                        });
                                    }, Button);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create('…');
                                        Text.fontSize(18);
                                        Text.fontColor(RokuricsColors.softText);
                                        Text.fontWeight(FontWeight.Bold);
                                    }, Text);
                                    Text.pop();
                                    // Context menu for folder actions
                                    Button.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Button.createWithChild();
                                        Button.width(32);
                                        Button.height(32);
                                        Button.backgroundColor(Color.Transparent);
                                        Button.onClick(() => {
                                            this.deleteTargetId = group.label;
                                            this.deleteTargetName = group.label;
                                            this.showDeleteConfirm = true;
                                        });
                                    }, Button);
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create('✕');
                                        Text.fontSize(14);
                                        Text.fontColor(RokuricsColors.coral);
                                    }, Text);
                                    Text.pop();
                                    Button.pop();
                                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                                        Text.create('›');
                                        Text.fontSize(22);
                                        Text.fontColor(RokuricsColors.softText);
                                    }, Text);
                                    Text.pop();
                                    Row.pop();
                                    ListItem.pop();
                                };
                                this.observeComponentCreation2(itemCreation2, ListItem);
                                ListItem.pop();
                            }
                        };
                        this.forEachUpdateFunction(elmtId, this.groups, forEachItemGenFunction);
                    }, ForEach);
                    ForEach.pop();
                    // Group view - show filing categories
                    List.pop();
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        // Also show unfiled recordings section
                        if (this.unfiledCount > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Row.create();
                                    Row.width('100%');
                                    Row.padding({ left: 16, right: 16, top: 8, bottom: 8 });
                                    Row.onClick(() => {
                                        this.showRecordings = this.unfiledRecordings;
                                        this.breadcrumb = ['全部', '未归档'];
                                    });
                                }, Row);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(`未归档录音 (${this.unfiledCount})`);
                                    Text.fontSize(15);
                                    Text.fontWeight(FontWeight.SemiBold);
                                    Text.fontColor(RokuricsColors.deepText);
                                }, Text);
                                Text.pop();
                                Row.pop();
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    List.create({ space: 6 });
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
                                                this.RecordingRow.bind(this)(recording);
                                                ListItem.pop();
                                            };
                                            this.observeComponentCreation2(itemCreation2, ListItem);
                                            ListItem.pop();
                                        }
                                    };
                                    this.forEachUpdateFunction(elmtId, this.unfiledRecordings, forEachItemGenFunction);
                                }, ForEach);
                                ForEach.pop();
                                List.pop();
                            });
                        }
                        else {
                            this.ifElseBranchUpdateFunction(1, () => {
                            });
                        }
                    }, If);
                    If.pop();
                });
            }
            else {
                this.ifElseBranchUpdateFunction(2, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        // Recording list at current path
                        if (this.showRecordings.length === 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Column.create({ space: 12 });
                                    Column.width('100%');
                                    Column.height('60%');
                                    Column.justifyContent(FlexAlign.Center);
                                }, Column);
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create('此分类下暂无录音');
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
                                    List.create({ space: 6 });
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
                                                this.RecordingRow.bind(this)(recording);
                                                ListItem.pop();
                                            };
                                            this.observeComponentCreation2(itemCreation2, ListItem);
                                            ListItem.pop();
                                        }
                                    };
                                    this.forEachUpdateFunction(elmtId, this.showRecordings, forEachItemGenFunction);
                                }, ForEach);
                                ForEach.pop();
                                List.pop();
                            });
                        }
                    }, If);
                    If.pop();
                });
            }
        }, If);
        If.pop();
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Rename folder dialog
            if (this.showRenameFolder) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create();
                        Column.width('100%');
                        Column.height('100%');
                        Column.justifyContent(FlexAlign.Center);
                        Column.backgroundColor('#00000050');
                        Column.position({ x: 0, y: 0 });
                        Column.onClick(() => { this.showRenameFolder = false; this.editMessage = ''; });
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
                        TextInput.backgroundColor(RokuricsColors.glassSurface + '80');
                        TextInput.borderRadius(10);
                        TextInput.padding(14);
                        TextInput.onChange((v: string) => { this.renameText = v; });
                    }, TextInput);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        If.create();
                        if (this.editMessage.length > 0) {
                            this.ifElseBranchUpdateFunction(0, () => {
                                this.observeComponentCreation2((elmtId, isInitialRender) => {
                                    Text.create(this.editMessage);
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
                        Row.create({ space: 12 });
                        Row.width('100%');
                        Row.justifyContent(FlexAlign.End);
                    }, Row);
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Button.createWithChild();
                        Button.backgroundColor(Color.Transparent);
                        Button.onClick(() => { this.showRenameFolder = false; this.editMessage = ''; });
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
                        Button.onClick(async () => { await this.commitRenameFolder(); });
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
            // Delete confirmation dialog
            else {
                this.ifElseBranchUpdateFunction(1, () => {
                });
            }
        }, If);
        If.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            // Delete confirmation dialog
            if (this.showDeleteConfirm) {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Column.create();
                        Column.width('100%');
                        Column.height('100%');
                        Column.justifyContent(FlexAlign.Center);
                        Column.backgroundColor('#00000050');
                        Column.position({ x: 0, y: 0 });
                        Column.onClick(() => { this.showDeleteConfirm = false; });
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
                        Button.onClick(async () => { await this.commitDeleteFolder(); });
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
    RecordingRow(recording: RecordingMetadata, parent = null) {
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Row.create();
            Row.width('100%');
            Row.padding({ left: 14, right: 14, top: 12, bottom: 12 });
            Row.borderRadius(12);
            Row.backgroundColor(RokuricsColors.glassSurface + '50');
            Row.onClick(() => this.openDetail(recording.id));
        }, Row);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Column.create({ space: 3 });
            Column.layoutWeight(1);
            Column.alignItems(HorizontalAlign.Start);
        }, Column);
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(recording.title);
            Text.fontSize(15);
            Text.fontWeight(FontWeight.Medium);
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
            Text.fontSize(11);
            Text.fontColor(RokuricsColors.softText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Text.create(formatDuration(recording.duration));
            Text.fontSize(11);
            Text.fontColor(RokuricsColors.tertiaryText);
        }, Text);
        Text.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            If.create();
            if (recording.transcriptionStatus === 'transcribed') {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('📝');
                        Text.fontSize(9);
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
            if (recording.noteStatus === 'generated') {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('📋');
                        Text.fontSize(9);
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
            if (recording.uploadStatus === 'uploaded') {
                this.ifElseBranchUpdateFunction(0, () => {
                    this.observeComponentCreation2((elmtId, isInitialRender) => {
                        Text.create('☁');
                        Text.fontSize(9);
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
        Column.pop();
        this.observeComponentCreation2((elmtId, isInitialRender) => {
            Image.create({ "id": 125832664, "type": 40000, params: [], "bundleName": "com.vita0818.rokurics", "moduleName": "entry" });
            Image.width(16);
            Image.height(16);
            Image.fillColor(RokuricsColors.softText);
        }, Image);
        Row.pop();
    }
    private async createFolder(): Promise<void> {
        const name = this.newFolderName.trim();
        if (name.length === 0)
            return;
        const store = new StudyFolderStore(getContext(this));
        const record = await store.createFolder(name, this.newFolderLevel, this.currentPath);
        await store.setColorToken(record.id, this.newFolderColor);
        this.showCreateFolder = false;
        this.newFolderName = '';
        this.newFolderColor = '#59C7C2';
        await this.loadData();
    }
    private async commitRenameFolder(): Promise<void> {
        const newName = this.renameText.trim();
        if (newName.length === 0 || this.renameTargetId.length === 0) {
            this.editMessage = '名称不能为空';
            return;
        }
        if (newName === this.renameTargetId) {
            this.showRenameFolder = false;
            return;
        }
        // Determine level from current path depth
        const levelIdx = this.currentPath.length;
        if (levelIdx >= HIERARCHY_LEVELS.length) {
            this.showRenameFolder = false;
            return;
        }
        const level = HIERARCHY_LEVELS[levelIdx];
        const toUpdate = this.allRecordings.filter((r: RecordingMetadata) => {
            const v = r.studyFiling?.valueForLevel(level) ?? null;
            return v === this.renameTargetId || (v === null && this.renameTargetId === '未分类');
        });
        for (const r of toUpdate) {
            const filing = r.studyFiling ?? new StudyFilingPath();
            if (level === 'type')
                filing.type = newName;
            else if (level === 'subject')
                filing.subject = newName;
            else if (level === 'chapter')
                filing.chapter = newName;
            else if (level === 'topic')
                filing.topic = newName;
            await this.recordingManager.updateStudyFiling(r.id, filing);
        }
        // Also update persisted folder store
        const store = new StudyFolderStore(getContext(this));
        const folders = await store.listFolders(level);
        const match = folders.find(f => f.name === this.renameTargetId);
        if (match)
            await store.renameFolder(match.id, newName);
        this.showRenameFolder = false;
        this.editMessage = '';
        await this.loadData();
    }
    private async commitDeleteFolder(): Promise<void> {
        if (this.deleteTargetId.length === 0)
            return;
        const levelIdx = this.currentPath.length;
        if (levelIdx >= HIERARCHY_LEVELS.length) {
            this.showDeleteConfirm = false;
            return;
        }
        const level = HIERARCHY_LEVELS[levelIdx];
        const toClear = this.allRecordings.filter((r: RecordingMetadata) => {
            const v = r.studyFiling?.valueForLevel(level) ?? null;
            return v === this.deleteTargetId || (v === null && this.deleteTargetId === '未分类');
        });
        for (const r of toClear) {
            const filing = r.studyFiling ?? new StudyFilingPath();
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
        // Also delete from persisted folder store
        const store = new StudyFolderStore(getContext(this));
        const folders = await store.listFolders(level);
        const match = folders.find(f => f.name === this.deleteTargetId);
        if (match)
            await store.deleteFolder(match.id);
        this.showDeleteConfirm = false;
        await this.loadData();
    }
    rerender() {
        this.updateDirtyElements();
    }
    static getEntryName(): string {
        return "StudyLibraryBrowserPage";
    }
}
registerNamedRoute(() => new StudyLibraryBrowserPage(undefined, {}), "", { bundleName: "com.vita0818.rokurics", moduleName: "entry", pagePath: "pages/StudyLibraryBrowserPage", pageFullPath: "entry/src/main/ets/pages/StudyLibraryBrowserPage", integratedHsp: "false", moduleType: "followWithHap" });
