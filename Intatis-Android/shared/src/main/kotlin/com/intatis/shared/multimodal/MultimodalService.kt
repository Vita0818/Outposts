package com.intatis.shared.multimodal

import com.intatis.shared.ConversationEventKinds
import com.intatis.shared.IConversationEventSink
import com.intatis.shared.WorkspaceSecurity
import com.intatis.shared.provider.GeneratedImage
import com.intatis.shared.provider.ImageGenerationProvider
import com.intatis.shared.provider.ImageRequest
import com.intatis.shared.provider.TranscriptionProvider
import com.intatis.shared.provider.TranscriptionRequest
import com.intatis.shared.provider.VideoGenerationProvider
import com.intatis.shared.provider.VideoJobState
import com.intatis.shared.provider.VideoRequest
import kotlinx.coroutines.delay
import java.io.File
import java.util.Locale
import java.util.UUID

class MultimodalService(
    private val eventSink: IConversationEventSink,
) : IMultimodalService {
    override suspend fun generateImage(
        provider: ImageGenerationProvider,
        model: String,
        prompt: String,
        workspaceRoot: String,
        size: String,
    ): MultimodalArtifactRef {
        val request = ImageRequest(
            model = model,
            prompt = prompt,
            size = size.ifBlank { "1024x1024" },
        )
        val images: List<GeneratedImage> = provider.generate(request)
        if (images.isEmpty()) {
            throw IllegalStateException("image provider returned no images")
        }

        var first: MultimodalArtifactRef? = null
        val imageRoot = File(WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/artifacts/images")).also {
            it.mkdirs()
        }

        images.forEach { image ->
            val artifactId = UUID.randomUUID().toString()
            val ext = mimeExtension(image.mime, "img")
            val output = File(imageRoot, "$artifactId.$ext")
            output.writeBytes(image.data)

            val ref = MultimodalArtifactRef(
                artifactId = artifactId,
                kind = "image",
                mime = image.mime,
                path = output.absolutePath,
                producedBy = model,
                prompt = prompt,
            )
            first = first ?: ref
            announce(ref)
        }

        return first ?: throw IllegalStateException("image provider returned no persisted artifacts")
    }

    override suspend fun transcribe(
        provider: TranscriptionProvider,
        model: String,
        audio: ByteArray,
        workspaceRoot: String,
        filename: String,
        mime: String,
    ): Pair<String, MultimodalArtifactRef> {
        val text = provider.transcribe(
            TranscriptionRequest(
                model = model,
                audio = audio,
                filename = filename.ifBlank { "audio.m4a" },
                mime = mime.ifBlank { "audio/m4a" },
            ),
        )
        val transcriptRoot = File(WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/artifacts/transcripts")).also {
            it.mkdirs()
        }
        val artifactId = UUID.randomUUID().toString()
        val output = File(transcriptRoot, "$artifactId.txt")
        output.writeText(text)

        val ref = MultimodalArtifactRef(
            artifactId = artifactId,
            kind = "transcript",
            mime = "text/plain",
            path = output.absolutePath,
            producedBy = model,
            prompt = null,
        )
        announce(ref)
        return text to ref
    }

    override suspend fun generateVideo(
        provider: VideoGenerationProvider,
        model: String,
        prompt: String,
        workspaceRoot: String,
        pollIntervalMs: Long,
        maxPolls: Int,
    ): MultimodalArtifactRef {
        val request = VideoRequest(model = model, prompt = prompt)
        val artifactId = UUID.randomUUID().toString()
        val jobId = provider.submit(request)

        announceProgress(artifactId, 0.0, VideoJobState.queued.name)

        val videoRoot = File(WorkspaceSecurity.resolveInWorkspace(workspaceRoot, ".intatis/artifacts/videos")).also {
            it.mkdirs()
        }

        repeat(maxPolls) {
            val status = provider.poll(jobId)
            announceProgress(artifactId, status.progress, status.state.name)
            when (status.state) {
                VideoJobState.completed -> {
                    val resultData = status.resultData ?: throw IllegalStateException("video generation completed without data")
                    val ext = mimeExtension(status.mime, "mp4")
                    val output = File(videoRoot, "$artifactId.$ext")
                    output.writeBytes(resultData)
                    val ref = MultimodalArtifactRef(
                        artifactId = artifactId,
                        kind = "video",
                        mime = status.mime,
                        path = output.absolutePath,
                        producedBy = model,
                        prompt = prompt,
                    )
                    announce(ref)
                    return ref
                }
                VideoJobState.failed -> throw IllegalStateException("video generation failed")
                VideoJobState.queued,
                VideoJobState.running -> {
                    delay((pollIntervalMs).coerceAtLeast(1))
                }
            }
        }

        throw IllegalStateException("video generation timed out")
    }

    private suspend fun announce(ref: MultimodalArtifactRef) {
        runCatching {
            eventSink.appendAsync(
                ConversationEventKinds.ArtifactAdded,
                mapOf(
                    "artifactId" to ref.artifactId,
                    "kind" to ref.kind,
                    "mime" to ref.mime,
                    "path" to ref.path,
                    "producedBy" to ref.producedBy,
                    "prompt" to ref.prompt,
                ).filterValues { it != null },
            )
        }
    }

    private suspend fun announceProgress(artifactId: String, progress: Double, state: String) {
        runCatching {
            eventSink.appendAsync(
                "artifact_progress",
                mapOf(
                    "artifactId" to artifactId,
                    "progress" to progress,
                    "state" to state.lowercase(Locale.getDefault()),
                ),
            )
        }
    }

    private fun mimeExtension(mime: String, fallback: String): String {
        val lowered = mime.lowercase(Locale.getDefault())
        return when {
            lowered.contains("png") -> "png"
            lowered.contains("jpeg") || lowered.contains("jpg") -> "jpg"
            lowered.contains("webp") -> "webp"
            lowered.contains("gif") -> "gif"
            else -> fallback
        }
    }
}
