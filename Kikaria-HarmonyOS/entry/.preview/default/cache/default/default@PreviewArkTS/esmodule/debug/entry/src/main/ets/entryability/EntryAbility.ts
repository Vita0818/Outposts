import UIAbility from "@ohos:app.ability.UIAbility";
import type Want from "@ohos:app.ability.Want";
import type AbilityConstant from "@ohos:app.ability.AbilityConstant";
import ConfigurationConstant from "@ohos:app.ability.ConfigurationConstant";
import type { Configuration } from "@ohos:app.ability.Configuration";
import type window from "@ohos:window";
import { preferenceManager } from "@bundle:com.vita0818.kikaria/entry/ets/data/PreferenceManager";
import { appState } from "@bundle:com.vita0818.kikaria/entry/ets/data/AppState";
import { builtInPresets } from "@bundle:com.vita0818.kikaria/entry/ets/data/SamplePresets";
import { runSmokeTestsAndLog } from "@bundle:com.vita0818.kikaria/entry/ets/model/SmokeTest";
const DARK_MODE_KEY = 'kikaria_isDarkMode';
const DARK_MODE_MANUAL_KEY = 'kikaria_darkModeManual';
export default class EntryAbility extends UIAbility {
    onCreate(want: Want, launchParam: AbilityConstant.LaunchParam): void {
        preferenceManager.setContext(this.context);
        appState.initialize(builtInPresets);
        // System dark mode auto-follow: respect manual override, else follow system
        const manuallySet = preferenceManager.loadDarkModeManual();
        if (!manuallySet) {
            const systemDark = this.context.config.colorMode === ConfigurationConstant.ColorMode.COLOR_MODE_DARK;
            if (appState.isDarkMode !== systemDark) {
                appState.isDarkMode = systemDark;
            }
        }
        AppStorage.setAndRef<boolean>(DARK_MODE_KEY, appState.isDarkMode);
        // Smoke-test core data model (non-blocking, debug-only)
        try {
            const smokeOk = runSmokeTestsAndLog();
            console.info(`Kikaria: Smoke tests ${smokeOk ? 'PASSED' : 'FAILED'}`);
        }
        catch (e) {
            console.error(`Kikaria: Smoke test error: ${e}`);
        }
    }
    onDestroy(): void {
        // Save state before destruction
        appState.saveAppState();
    }
    onWindowStageCreate(windowStage: window.WindowStage): void {
        windowStage.loadContent('pages/Index', (err) => {
            if (err.code) {
                console.error(`Kikaria: Failed to load content: ${JSON.stringify(err)}`);
                return;
            }
            console.info('Kikaria: Window content loaded successfully');
        });
        windowStage.getMainWindow().then((mainWindow: window.Window) => {
            const bg = appState.isDarkMode ? '#0A1625' : '#EDF9FF';
            mainWindow.setWindowBackgroundColor(bg);
            mainWindow.setWindowSystemBarProperties({
                statusBarContentColor: appState.isDarkMode ? '#FFFFFF' : '#214054'
            });
        });
    }
    onWindowStageDestroy(): void {
        // Release resources
    }
    onConfigurationUpdate(newConfig: Configuration): void {
        const manuallySet = preferenceManager.loadDarkModeManual();
        if (!manuallySet) {
            const systemDark = newConfig.colorMode === ConfigurationConstant.ColorMode.COLOR_MODE_DARK;
            if (appState.isDarkMode !== systemDark) {
                appState.isDarkMode = systemDark;
                AppStorage.setAndRef<boolean>(DARK_MODE_KEY, systemDark);
            }
        }
    }
    onForeground(): void {
        // App comes to foreground
    }
    onBackground(): void {
        appState.saveAppState();
    }
}
