package com.intatis.shared.multimodal

import com.intatis.shared.provider.ImageGenerationProvider
import com.intatis.shared.provider.TranscriptionProvider
import com.intatis.shared.provider.VideoGenerationProvider

data class MultimodalArtifactRef(
    val artifactId: String,
    val kind: String,
    val mime: String,
    val path: String,
    val producedBy: String? = null,
    val prompt: String? = null,
)

interface IMultimodalService {
    suspend fun generateImage(
        provider: ImageGenerationProvider,
        model: String,
        prompt: String,
        workspaceRoot: String,
        size: String = "1024x1024",
    ): MultimodalArtifactRef

    suspend fun transcribe(
        provider: TranscriptionProvider,
        model: String,
        audio: ByteArray,
        workspaceRoot: String,
        filename: String = "audio.m4a",
        mime: String = "audio/m4a",
    ): Pair<String, MultimodalArtifactRef>

    suspend fun generateVideo(
        provider: VideoGenerationProvider,
        model: String,
        prompt: String,
        workspaceRoot: String,
        pollIntervalMs: Long = 500,
        maxPolls: Int = 240,
    ): MultimodalArtifactRef
}

typealias ImageAttachment = com.intatis.shared.ImageAttachment
