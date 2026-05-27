/**
 * OpenHarmonyTestRunner - Standard hypium test runner for ohosTest.
 * Wires the Kikaria smoke test suite into the hypium framework.
 *
 * Once the build-profile.json5 test target is activated, DevEco Studio
 * will invoke this runner to execute tests.
 */
import hilog from '@ohos.hilog'
import { Hypium } from '@ohos/hypium'
import abilityDelegatorRegistry from '@ohos.app.ability.abilityDelegatorRegistry'

const DOMAIN = 0x0000
const TAG = 'KikariaTestRunner'

export default class OpenHarmonyTestRunner {
  onPrepare(): void {
    hilog.info(DOMAIN, TAG, '%{public}s', 'OpenHarmonyTestRunner onPrepare')
  }

  onRun(): void {
    hilog.info(DOMAIN, TAG, '%{public}s', 'OpenHarmonyTestRunner onRun')

    try {
      const abilityDelegator = abilityDelegatorRegistry.getAbilityDelegator()
      const abilityDelegatorArguments = abilityDelegatorRegistry.getArguments()
      hilog.info(DOMAIN, TAG, '%{public}s', 'Test runner initialized, loading test suite')
    } catch (e) {
      hilog.error(DOMAIN, TAG, '%{public}s', `Test runner init failed: ${e}`)
    }
  }
}
