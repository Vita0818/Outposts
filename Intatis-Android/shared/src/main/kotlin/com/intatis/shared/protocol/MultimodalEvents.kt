package com.intatis.shared.protocol

data class ArtifactAddedPayload(
    val artifactId: ArtifactID,
    val kind: String,
    val mime: String,
    val path: String,
    val producedBy: String? = null,
    val prompt: String? = null
)

data class ArtifactProgressPayload(
    val artifactId: ArtifactID,
    val progress: Double,
    val state: String
)
