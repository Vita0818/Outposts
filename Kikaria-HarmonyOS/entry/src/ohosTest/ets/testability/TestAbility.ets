/**
 * TestAbility - Minimal test ability for ohosTest skeleton.
 * Runs SmokeTest assertions on startup and logs results.
 */
import { UIAbility, Want, AbilityConstant } from '@kit.AbilityKit'
import { window } from '@kit.ArkUI'
import { runAllTests } from '../test/SmokeTest.test'

export default class TestAbility extends UIAbility {
  onCreate(want: Want, launchParam: AbilityConstant.LaunchParam): void {
    console.info('[Kikaria ohosTest] TestAbility.onCreate — running smoke tests')
    const result = runAllTests()
    const allPassed = result.failed === 0
    console.info(`[Kikaria ohosTest] Smoke tests ${allPassed ? 'PASSED' : 'FAILED'}`)
  }

  onWindowStageCreate(windowStage: window.WindowStage): void {
    windowStage.loadContent('pages/Index', (err) => {
      if (err.code) {
        console.error(`[Kikaria ohosTest] Failed to load content: ${err.code}`)
      }
    })
  }
}
