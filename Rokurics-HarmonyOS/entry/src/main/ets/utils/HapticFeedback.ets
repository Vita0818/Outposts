/**
 * Haptic feedback utility for HarmonyOS
 * Provides light haptic tap for orb interaction (mirrors Apple's haptic feedback)
 * Falls back silently if vibrator is unavailable (unsigned HAP, no permission, etc.)
 */
import { vibrator } from '@kit.SensorServiceKit'

const HAPTIC_DURATION_MS = 10

/**
 * Light haptic tap - safe to call anywhere, silently no-ops if unavailable.
 * Use for: orb press, button interactions, navigation transitions.
 */
export function hapticLight(): void {
  try {
    vibrator.startVibration({
      type: 'time',
      duration: HAPTIC_DURATION_MS
    }, {
      id: 0,
      usage: 'touch'
    })
  } catch (_e) {
    // Haptic not available on this device/build — silent fallback
  }
}

/**
 * Medium haptic for confirmation actions (save, delete)
 */
export function hapticMedium(): void {
  try {
    vibrator.startVibration({
      type: 'time',
      duration: 20
    }, {
      id: 0,
      usage: 'touch'
    })
  } catch (_e) {
    // silent fallback
  }
}
