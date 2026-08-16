package com.intatis.shared.artifacts

import com.intatis.shared.protocol.ArtifactID
import java.time.Instant

enum class ArtifactKind {
    transcript,
    image,
    video,
    audio,
    fileAttachment,
    diff,
    patch,
    report
}

data class ArtifactRef(
    val id: ArtifactID,
    val kind: ArtifactKind,
    val mime: String,
    val path: String,
    val producedBy: String? = null,
    val prompt: String? = null,
    val createdAt: Instant = Instant.now(),
)
