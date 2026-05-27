import preferences from "@ohos:data.preferences";
const STORE_NAME = 'kikaria_store';
const STATE_KEY = 'kikaria.appStateJSON';
const DARK_MODE_MANUAL_KEY = 'kikaria.darkModeManual';
class PreferenceManager {
    private context: Context | null = null;
    setContext(ctx: Context): void {
        this.context = ctx;
    }
    loadAppStateJson(): string | null {
        if (!this.context) {
            return null;
        }
        try {
            const options: preferences.Options = { name: STORE_NAME };
            const prefs = preferences.getPreferencesSync(this.context, options);
            const json = prefs.getSync(STATE_KEY, '') as string;
            if (json.length === 0) {
                return null;
            }
            return json;
        }
        catch (e) {
            console.error(`Kikaria: Failed to load app state: ${e}`);
            return null;
        }
    }
    saveAppStateJson(json: string): void {
        if (!this.context) {
            return;
        }
        try {
            const options: preferences.Options = { name: STORE_NAME };
            const prefs = preferences.getPreferencesSync(this.context, options);
            prefs.putSync(STATE_KEY, json);
            prefs.flushSync();
        }
        catch (e) {
            console.error(`Kikaria: Failed to save app state: ${e}`);
        }
    }
    loadDarkModeManual(): boolean {
        if (!this.context) {
            return false;
        }
        try {
            const options: preferences.Options = { name: STORE_NAME };
            const prefs = preferences.getPreferencesSync(this.context, options);
            return prefs.getSync(DARK_MODE_MANUAL_KEY, false) as boolean;
        }
        catch (e) {
            return false;
        }
    }
    saveDarkModeManual(value: boolean): void {
        if (!this.context) {
            return;
        }
        try {
            const options: preferences.Options = { name: STORE_NAME };
            const prefs = preferences.getPreferencesSync(this.context, options);
            prefs.putSync(DARK_MODE_MANUAL_KEY, value);
            prefs.flushSync();
        }
        catch (e) {
            console.error(`Kikaria: Failed to save dark mode manual: ${e}`);
        }
    }
}
export const preferenceManager = new PreferenceManager();
