package com.rokurics.app.domain.provider

import kotlinx.coroutines.runBlocking
import org.junit.Assert.*
import org.junit.Test

class LocalTranscriptionEngineTest {

    @Test
    fun testFakeEngineIsAvailable() {
        val engine = FakeLocalTranscriptionEngine(simulateAvailable = true)
        assertTrue(engine.isAvailable())
        assertEquals("fake_local_transcription", engine.id)
    }

    @Test
    fun testFakeEngineNotAvailable() {
        val engine = FakeLocalTranscriptionEngine(simulateAvailable = false)
        assertFalse(engine.isAvailable())
    }

    @Test
    fun testFakeEngineSucceeds() = runBlocking {
        val engine = FakeLocalTranscriptionEngine(shouldSucceed = true)
        val result = engine.transcribe(TranscriptionRequest(
            recordingID = "rec-1",
            audioFilePath = "/fake/path.m4a"
        ))
        assertTrue(result.isSuccess)
        assertEquals("rec-1", result.getOrThrow().recordingID)
        assertTrue(result.getOrThrow().transcript.contains("模拟"))
    }

    @Test
    fun testFakeEngineFails() = runBlocking {
        val engine = FakeLocalTranscriptionEngine(shouldSucceed = false)
        val result = engine.transcribe(TranscriptionRequest(
            recordingID = "rec-1",
            audioFilePath = "/fake/path.m4a"
        ))
        assertTrue(result.isFailure)
        assertTrue(result.exceptionOrNull()?.message?.contains("Fake") == true)
    }

    @Test
    fun testFakeEngineCallCount() = runBlocking {
        val engine = FakeLocalTranscriptionEngine()
        assertEquals(0, engine.callCount)
        engine.transcribe(TranscriptionRequest("r1", "/fake"))
        assertEquals(1, engine.callCount)
        engine.transcribe(TranscriptionRequest("r2", "/fake"))
        assertEquals(2, engine.callCount)
    }

    @Test
    fun testTranscriptionStatusEnumValues() {
        assertEquals(6, LocalTranscriptionStatus.values().size)
        assertEquals("不可用", LocalTranscriptionStatus.UNAVAILABLE.label)
        assertEquals("可用", LocalTranscriptionStatus.AVAILABLE.label)
        assertEquals("转写中", LocalTranscriptionStatus.RUNNING.label)
        assertEquals("已完成", LocalTranscriptionStatus.SUCCEEDED.label)
        assertEquals("失败", LocalTranscriptionStatus.FAILED.label)
        assertEquals("排队中", LocalTranscriptionStatus.QUEUED.label)
    }

    @Test
    fun testTranscriptionRequestFields() {
        val req = TranscriptionRequest(
            recordingID = "rec-abc",
            audioFilePath = "/path/to/audio.m4a",
            language = "zh-CN"
        )
        assertEquals("rec-abc", req.recordingID)
        assertEquals("/path/to/audio.m4a", req.audioFilePath)
        assertEquals("zh-CN", req.language)
    }

    @Test
    fun testTranscriptionResultFields() {
        val result = TranscriptionResult(
            recordingID = "rec-1",
            transcript = "这是一段测试转写文本",
            providerID = "test_provider",
            modelName = "test-model",
            segments = listOf(
                TranscriptionSegment(0.0, 2.5, "这是"),
                TranscriptionSegment(2.5, 5.0, "一段测试")
            )
        )
        assertEquals("rec-1", result.recordingID)
        assertEquals("这是一段测试转写文本", result.transcript)
        assertEquals(2, result.segments.size)
        assertEquals(0.0, result.segments[0].startSeconds, 0.001)
        assertEquals("一段测试", result.segments[1].text)
    }

    @Test
    fun testTranscriptionError() {
        val error = TranscriptionError("测试错误消息")
        assertEquals("测试错误消息", error.message)
        assertTrue(error is Exception)
    }

    @Test
    fun testFakeEngineCustomTranscript() = runBlocking {
        val custom = "自定义模拟转写内容"
        val engine = FakeLocalTranscriptionEngine(simulatedTranscript = custom)
        val result = engine.transcribe(TranscriptionRequest("r1", "/f"))
        assertEquals(custom, result.getOrThrow().transcript)
    }
}
