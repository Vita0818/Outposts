import UIAbility from "@ohos:app.ability.UIAbility";
import type Want from "@ohos:app.ability.Want";
import type AbilityConstant from "@ohos:app.ability.AbilityConstant";
import type window from "@ohos:window";
export default class EntryAbility extends UIAbility {
    onCreate(want: Want, launchParam: AbilityConstant.LaunchParam): void {
        console.info('[Rokurics] EntryAbility onCreate');
    }
    onDestroy(): void {
        console.info('[Rokurics] EntryAbility onDestroy');
    }
    onWindowStageCreate(windowStage: window.WindowStage): void {
        console.info('[Rokurics] EntryAbility onWindowStageCreate');
        windowStage.loadContent('pages/HomePage', (err) => {
            if (err.code) {
                console.error(`[Rokurics] Failed to load content: ${err.code}`);
                return;
            }
            console.info('[Rokurics] Content loaded successfully');
        });
    }
    onWindowStageDestroy(): void {
        console.info('[Rokurics] EntryAbility onWindowStageDestroy');
    }
    onForeground(): void {
        console.info('[Rokurics] EntryAbility onForeground');
    }
    onBackground(): void {
        console.info('[Rokurics] EntryAbility onBackground');
    }
}
