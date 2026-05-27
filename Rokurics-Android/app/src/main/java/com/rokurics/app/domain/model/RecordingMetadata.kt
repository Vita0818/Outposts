package com.rokurics.app.domain.model

import org.json.JSONObject
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.UUID

data class RecordingMetadata(
    val id: String = UUID.randomUUID().toString(),
    val title: String,
    val fileName: String,
    val relativeAudioPath: String,
    val relativeMetadataPath: String,
    val createdAt: Date = Date(),
    val endedAt: Date = Date(),
    val duration: Double = 0.0,
    val format: String = "m4a",
    val codec: String = "AAC",
    val sampleRate: Double = 16000.0,
    val channels: Int = 1,
    val bitrate: Int = 64000,
    val fileSize: Long = 0L,
    val uploadStatus: String = "localOnly",
    val transcriptionStatus: String = "notStarted",
    val noteStatus: String = "notStarted",
    val tags: List<String> = emptyList(),
    val studyFiling: StudyFilingPath? = null,
    val uploadProgressFraction: Double? = null,
    val uploadProgressConfirmedBytes: Long? = null,
    val uploadProgressTotalBytes: Long? = null,
    val uploadPhase: String? = null,
    val uploadProgressDescription: String? = null,
    val isDeleted: Boolean = false,
    val deletedAt: Date? = null
) {
    fun toJson(): JSONObject = JSONObject().apply {
        put("id", id)
        put("title", title)
        put("fileName", fileName)
        put("relativeAudioPath", relativeAudioPath)
        put("relativeMetadataPath", relativeMetadataPath)
        put("createdAt", createdAt.time)
        put("endedAt", endedAt.time)
        put("duration", duration)
        put("format", format)
        put("codec", codec)
        put("sampleRate", sampleRate)
        put("channels", channels)
        put("bitrate", bitrate)
        put("fileSize", fileSize)
        put("uploadStatus", uploadStatus)
        put("transcriptionStatus", transcriptionStatus)
        put("noteStatus", noteStatus)
        put("isDeleted", isDeleted)
    }

    companion object {
        fun defaultTitle(createdAt: Date = Date()): String {
            val formatter = SimpleDateFormat("yyyy-MM-dd HH:mm", Locale.CHINA)
            return "录音 ${formatter.format(createdAt)}"
        }

        fun fromJson(json: JSONObject): RecordingMetadata {
            return RecordingMetadata(
                id = json.optString("id", UUID.randomUUID().toString()),
                title = json.optString("title", "未命名录音"),
                fileName = json.optString("fileName", ""),
                relativeAudioPath = json.optString("relativeAudioPath", ""),
                relativeMetadataPath = json.optString("relativeMetadataPath", ""),
                createdAt = Date(json.optLong("createdAt", System.currentTimeMillis())),
                endedAt = Date(json.optLong("endedAt", System.currentTimeMillis())),
                duration = json.optDouble("duration", 0.0),
                format = json.optString("format", "m4a"),
                codec = json.optString("codec", "AAC"),
                sampleRate = json.optDouble("sampleRate", 16000.0),
                channels = json.optInt("channels", 1),
                bitrate = json.optInt("bitrate", 64000),
                fileSize = json.optLong("fileSize", 0L),
                uploadStatus = json.optString("uploadStatus", "localOnly"),
                transcriptionStatus = json.optString("transcriptionStatus", "notStarted"),
                noteStatus = json.optString("noteStatus", "notStarted"),
                isDeleted = json.optBoolean("isDeleted", false),
                deletedAt = if (json.has("deletedAt")) Date(json.optLong("deletedAt")) else null
            )
        }
    }
}

enum class RecordingUploadStatus(val rawValue: String) {
    LOCAL_ONLY("localOnly"),
    PENDING("pending"),
    UPLOADING("uploading"),
    UPLOADED("uploaded"),
    FAILED("failed");

    companion object {
        fun fromRawValue(value: String): RecordingUploadStatus =
            entries.find { it.rawValue == value } ?: LOCAL_ONLY
    }
}
