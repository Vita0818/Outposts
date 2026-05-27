package com.rokurics.app.service

import org.junit.Assert.*
import org.junit.Test

class RecordingManagerStateTest {

    @Test
    fun testAllStatesExist() {
        assertEquals(12, RokuricsRecordingState.values().size)
    }

    @Test
    fun testNotificationPermissionDeniedStateExists() {
        val state = RokuricsRecordingState.valueOf("NOTIFICATION_PERMISSION_DENIED")
        assertNotNull(state)
    }

    @Test
    fun testNotificationDeniedIsNotRecording() {
        assertFalse(RokuricsRecordingState.NOTIFICATION_PERMISSION_DENIED.isRecording)
    }

    @Test
    fun testNotificationDeniedIsNotPaused() {
        assertFalse(RokuricsRecordingState.NOTIFICATION_PERMISSION_DENIED.isPaused)
    }

    @Test
    fun testNotificationDeniedIsNotBusy() {
        assertFalse(RokuricsRecordingState.NOTIFICATION_PERMISSION_DENIED.isBusy)
    }

    @Test
    fun testIdleStateIsNotBusy() {
        assertFalse(RokuricsRecordingState.IDLE.isBusy)
    }

    @Test
    fun testRecordingStateIsRecording() {
        assertTrue(RokuricsRecordingState.RECORDING.isRecording)
    }

    @Test
    fun testPausedStateIsPaused() {
        assertTrue(RokuricsRecordingState.PAUSED.isPaused)
    }

    @Test
    fun testPermissionDeniedIsNotBusy() {
        assertFalse(RokuricsRecordingState.PERMISSION_DENIED.isBusy)
    }

    @Test
    fun testFailedIsNotBusy() {
        assertFalse(RokuricsRecordingState.FAILED.isBusy)
    }

    @Test
    fun testSavedIsNotBusy() {
        assertFalse(RokuricsRecordingState.SAVED.isBusy)
    }

    @Test
    fun testBusyStates() {
        assertTrue(RokuricsRecordingState.REQUESTING_PERMISSION.isBusy)
        assertTrue(RokuricsRecordingState.CONFIGURING_SESSION.isBusy)
        assertTrue(RokuricsRecordingState.STOPPING.isBusy)
        assertTrue(RokuricsRecordingState.FILING.isBusy)
        assertTrue(RokuricsRecordingState.SAVING.isBusy)
    }
}
