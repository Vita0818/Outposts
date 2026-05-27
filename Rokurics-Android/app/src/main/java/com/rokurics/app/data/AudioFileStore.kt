package com.rokurics.app.data

import android.content.Context
import com.rokurics.app.RokuricsApp
import com.rokurics.app.domain.model.RecordingMetadata
import org.json.JSONObject
import java.io.File
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class AudioFileStore(
    private val context: Context = RokuricsApp.instance
) {
    private val baseDir: File
        get() = File(context.filesDir, "Rokurics").also { it.mkdirs() }

    private val recordingsDir: File
        get() = File(baseDir, "Recordings").also { it.mkdirs() }

    private val metadataDir: File
        get() = File(baseDir, "Metadata").also { it.mkdirs() }

    fun ensureStorageDirectories() {
        recordingsDir.mkdirs()
        metadataDir.mkdirs()
    }

    fun makeRecordingFileUrl(date: Date = Date(), fallback: Boolean = false): File {
        val formatter = SimpleDateFormat("yyyy-MM-dd_HH-mm-ss", Locale.US)
        val suffix = if (fallback) "_fallback" else ""
        val baseName = "rokurics_${formatter.format(date)}$suffix"
        return File(recordingsDir, "$baseName.m4a")
    }

    fun makeMetadataFile(id: String): File {
        return File(metadataDir, "$id.json")
    }

    fun fileExists(file: File): Boolean = file.exists()

    fun directoryExists(file: File): Boolean = file.isDirectory

    fun fileSize(file: File): Long = if (file.exists()) file.length() else 0L

    fun removeFileIfExists(file: File) {
        if (file.exists()) file.delete()
    }

    fun relativePath(file: File): String {
        val basePath = baseDir.absolutePath.let {
            if (it.endsWith("/")) it else "$it/"
        }
        val filePath = file.absolutePath
        check(filePath.startsWith(basePath)) { "File outside Rokurics directory" }
        return filePath.removePrefix(basePath)
    }

    fun saveMetadata(metadata: RecordingMetadata) {
        val file = makeMetadataFile(metadata.id)
        file.writeText(metadata.toJson().toString(2))
    }

    fun loadMetadata(id: String): RecordingMetadata? {
        val file = makeMetadataFile(id)
        return if (file.exists()) {
            try {
                RecordingMetadata.fromJson(JSONObject(file.readText()))
            } catch (e: Exception) {
                null
            }
        } else null
    }

    fun loadAllMetadata(includeDeleted: Boolean = false): List<RecordingMetadata> {
        return metadataDir.listFiles()
            ?.filter { it.extension == "json" }
            ?.mapNotNull { file ->
                try {
                    RecordingMetadata.fromJson(JSONObject(file.readText()))
                } catch (e: Exception) {
                    null
                }
            }
            ?.filter { includeDeleted || !it.isDeleted }
            ?.sortedByDescending { it.createdAt }
            ?: emptyList()
    }

    fun loadTrashedMetadata(): List<RecordingMetadata> {
        return loadAllMetadata(includeDeleted = true)
            .filter { it.isDeleted }
            .sortedByDescending { it.deletedAt ?: it.createdAt }
    }

    fun updateMetadata(metadata: RecordingMetadata) {
        saveMetadata(metadata)
    }

    fun updateTitle(recordingID: String, rawTitle: String): RecordingMetadata? {
        val metadata = loadMetadata(recordingID) ?: return null
        val updated = metadata.copy(title = rawTitle.trim().ifEmpty { metadata.title })
        saveMetadata(updated)
        return updated
    }

    fun moveToTrash(metadata: RecordingMetadata): RecordingMetadata {
        val updated = metadata.copy(isDeleted = true, deletedAt = Date())
        saveMetadata(updated)
        return updated
    }

    fun restoreRecording(id: String): RecordingMetadata? {
        val metadata = loadMetadata(id) ?: return null
        val updated = metadata.copy(isDeleted = false, deletedAt = null)
        saveMetadata(updated)
        return updated
    }

    fun permanentlyDeleteRecording(id: String) {
        val metadata = loadMetadata(id) ?: return
        val audioFile = File(baseDir, metadata.relativeAudioPath)
        val metadataFile = makeMetadataFile(id)
        audioFile.delete()
        metadataFile.delete()
    }

    fun audioFileFor(metadata: RecordingMetadata): File {
        return File(baseDir, metadata.relativeAudioPath)
    }

    fun moveRecordingToTrash(metadata: RecordingMetadata): RecordingMetadata {
        return moveToTrash(metadata)
    }
}
