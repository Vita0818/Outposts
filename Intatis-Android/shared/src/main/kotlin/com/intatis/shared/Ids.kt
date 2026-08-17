package com.intatis.shared

import java.security.SecureRandom

enum class SessionKind(val wire: String) {
    CHAT("chat"),
    CODE("code"),
    COWORK("cowork");

    val idPrefix: String
        get() = when (this) {
            CHAT -> "sess_"
            CODE -> "code_"
            COWORK -> "cowork_"
        }

    companion object {
        fun fromWire(value: String): SessionKind = when (value) {
            "code" -> CODE
            "cowork" -> COWORK
            else -> CHAT
        }
    }
}

/**
 * Typed identifiers mirror the Apple project's TypedID scheme: random lowercase
 * alphanumerics of length 8 behind a short prefix such as "sess_" or "msg_".
 */
@JvmInline
value class SessionId(val value: String) {
    val kind: SessionKind
        get() = when {
            value.startsWith("code_") -> SessionKind.CODE
            value.startsWith("cowork_") -> SessionKind.COWORK
            else -> SessionKind.CHAT
        }

    companion object {
        fun new(kind: SessionKind) = SessionId(IdGen.random(kind.idPrefix))
    }
}

@JvmInline value class MessageId(val value: String) {
    companion object { fun new() = MessageId(IdGen.random("msg_")) }
}

@JvmInline value class SubmissionId(val value: String) {
    companion object { fun new() = SubmissionId(IdGen.random("sub_")) }
}

@JvmInline value class TurnId(val value: String) {
    companion object { fun new() = TurnId(IdGen.random("turn_")) }
}

@JvmInline value class ArtifactId(val value: String) {
    companion object { fun new() = ArtifactId(IdGen.random("art_")) }
}

object IdGen {
    private const val Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789"
    private val random = SecureRandom()

    fun random(prefix: String, length: Int = 8): String {
        val bytes = ByteArray(length)
        random.nextBytes(bytes)
        return prefix + bytes.joinToString("") { Alphabet[it.toInt().mod(Alphabet.length)].toString() }
    }
}
