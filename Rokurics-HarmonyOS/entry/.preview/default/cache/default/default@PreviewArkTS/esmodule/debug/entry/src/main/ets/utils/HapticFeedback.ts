import vibrator from "@ohos:vibrator";
const HAPTIC_DURATION_MS = 10;
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
        });
    }
    catch (_e) {
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
        });
    }
    catch (_e) {
        // silent fallback
    }
}
