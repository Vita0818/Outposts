package com.intatis.shared.protocol

data class TurnStatsPayload(
    val promptTokens: Int? = null,
    val completionTokens: Int? = null,
    val totalTokens: Int? = null,
    val ttftMillis: Int? = null,
    val totalMillis: Int? = null,
    val model: String? = null
)
