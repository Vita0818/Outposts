/**
 * Entry ability - app entry point
 */
import { UIAbility, Want, AbilityConstant } from '@kit.AbilityKit'
import { window } from '@kit.ArkUI'

export default class EntryAbility extends UIAbility {
  onCreate(want: Want, launchParam: AbilityConstant.LaunchParam): void {
    console.info('[Rokurics] EntryAbility onCreate')
  }

  onDestroy(): void {
    console.info('[Rokurics] EntryAbility onDestroy')
  }

  onWindowStageCreate(windowStage: window.WindowStage): void {
    console.info('[Rokurics] EntryAbility onWindowStageCreate')

    windowStage.loadContent('pages/HomePage', (err) => {
      if (err.code) {
        console.error(`[Rokurics] Failed to load content: ${err.code}`)
        return
      }
      console.info('[Rokurics] Content loaded successfully')
    })
  }

  onWindowStageDestroy(): void {
    console.info('[Rokurics] EntryAbility onWindowStageDestroy')
  }

  onForeground(): void {
    console.info('[Rokurics] EntryAbility onForeground')
  }

  onBackground(): void {
    console.info('[Rokurics] EntryAbility onBackground')
  }
}
