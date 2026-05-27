package com.rokurics.app.domain.provider

import com.rokurics.app.domain.model.RecordingMetadata

class TranscriptionCoordinator(
    private val provider: TranscriptionProvider = MockTranscriptionProvider()
) {
    suspend fun transcribe(recording: RecordingMetadata, audioFilePath: String): Result<TranscriptionResult> {
        provider.validateConfiguration()
        val request = TranscriptionRequest(
            recordingID = recording.id,
            audioFilePath = audioFilePath
        )
        return provider.transcribe(request)
    }
}

class NoteGenerationCoordinator(
    private val provider: NoteGenerationProvider = MockNoteGenerationProvider()
) {
    suspend fun generateNote(
        recording: RecordingMetadata,
        transcript: String
    ): Result<NoteGenerationResult> {
        provider.validateConfiguration()
        val request = NoteGenerationRequest(
            recordingID = recording.id,
            transcript = transcript,
            title = recording.title,
            studyFilingPath = recording.studyFiling?.displaySummary
        )
        return provider.generateNote(request)
    }
}
