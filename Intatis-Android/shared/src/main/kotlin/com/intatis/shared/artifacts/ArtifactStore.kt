package com.intatis.shared.artifacts

import com.intatis.shared.newProtocolId
import com.intatis.shared.protocol.ArtifactID
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.buildJsonArray
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import java.io.File
import java.io.FileNotFoundException
import java.io.IOException
import java.time.Instant
import java.util.Locale

class ArtifactStore(root: String) {
    private val rootDir: File = File(root)
    private val blobsDir: File = File(rootDir, "blobs")
    private val indexFile: File = File(rootDir, "index.json")
    private val json = Json { ignoreUnknownKeys = true }
    private val mutex = Any()
    private val index: MutableMap<ArtifactID, ArtifactRef> = mutableMapOf()

    init {
        rootDir.mkdirs()
        blobsDir.mkdirs()
        loadIndex()
    }

    fun add(
        kind: ArtifactKind,
        mime: String,
        data: ByteArray,
        ext: String,
        producedBy: String? = null,
        prompt: String? = null,
    ): ArtifactRef = synchronized(mutex) {
        val id = newProtocolId("art")
        val safeExt = ext.trim().ifEmpty { "bin" }.trimStart('.')
        val path = "blobs/$id.$safeExt"
        val file = File(rootDir, path)

        try {
            file.writeBytes(data)
            val ref = ArtifactRef(
                id = id,
                kind = kind,
                mime = mime,
                path = path,
                producedBy = producedBy,
                prompt = prompt,
                createdAt = Instant.now(),
            )
            index[id] = ref
            persistIndex()
            return ref
        } catch (error: IOException) {
            throw IllegalStateException("Failed to add artifact $id", error)
        }
    }

    fun addAttachment(name: String, data: ByteArray, mime: String): ArtifactRef = synchronized(mutex) {
        val extension = fileExtension(of = name)
        return add(
            kind = ArtifactKind.fileAttachment,
            mime = mime,
            data = data,
            ext = extension,
            producedBy = null,
            prompt = null,
        )
    }

    fun refFor(id: ArtifactID): ArtifactRef? = synchronized(mutex) {
        index[id]
    }

    fun dataFor(id: ArtifactID): ByteArray = synchronized(mutex) {
        val ref = index[id] ?: throw FileNotFoundException("artifact not found: $id")
        val absolute = File(rootDir, ref.path)
        if (!absolute.exists()) {
            throw FileNotFoundException("artifact file not found: ${absolute.absolutePath}")
        }
        return absolute.readBytes()
    }

    fun list(): List<ArtifactRef> = synchronized(mutex) {
        index.values.sortedBy { it.createdAt }
    }

    fun absolutePathFor(ref: ArtifactRef): String = synchronized(mutex) {
        File(rootDir, ref.path).absolutePath
    }

    private fun loadIndex() = synchronized(mutex) {
        if (!indexFile.exists()) return@loadIndex

        val entries = runCatching { json.parseToJsonElement(indexFile.readText()) }
            .getOrNull() as? JsonArray ?: return

        val restored = mutableMapOf<ArtifactID, ArtifactRef>()
        for (entry in entries) {
            parseRef(entry)?.let { restored[it.id] = it }
        }
        index.clear()
        index.putAll(restored)
    }

    private fun persistIndex() {
        val serialized = buildJsonArray {
            index.values.sortedBy { it.createdAt }.forEach {
                add(
                    buildJsonObject {
                        put("id", it.id)
                        put("kind", it.kind.toWireName())
                        put("mime", it.mime)
                        put("path", it.path)
                        put("producedBy", it.producedBy ?: "")
                        put("prompt", it.prompt ?: "")
                        put("createdAt", it.createdAt.toString())
                    },
                )
            }
        }

        indexFile.writeText(json.encodeToString(JsonArray.serializer(), serialized))
    }

    private fun parseRef(element: JsonElement): ArtifactRef? {
        if (element !is JsonObject) return null

        val id = element["id"]?.jsonPrimitive?.contentOrNull ?: return null
        val kind = parseArtifactKind(element["kind"]?.jsonPrimitive?.contentOrNull)
        val mime = element["mime"]?.jsonPrimitive?.contentOrNull ?: return null
        val path = element["path"]?.jsonPrimitive?.contentOrNull ?: return null
        val producedBy = element["producedBy"]?.jsonPrimitive?.contentOrNull?.ifBlank { null }
        val prompt = element["prompt"]?.jsonPrimitive?.contentOrNull?.ifBlank { null }

        val createdAt = element["createdAt"]?.jsonPrimitive?.contentOrNull
            ?.let { runCatching { Instant.parse(it) }.getOrElse { Instant.now() } } ?: Instant.now()

        return ArtifactRef(
            id = id,
            kind = kind,
            mime = mime,
            path = path,
            producedBy = producedBy,
            prompt = prompt,
            createdAt = createdAt,
        )
    }

    private fun fileExtension(of: String): String {
        val name = of.trim()
        val dot = name.lastIndexOf('.')
        return if (dot in 1 until name.length) name.substring(dot + 1) else "bin"
    }

    private fun parseArtifactKind(value: String?): ArtifactKind = when (value?.trim()?.lowercase(Locale.getDefault()) ?: "") {
        "transcript" -> ArtifactKind.transcript
        "image" -> ArtifactKind.image
        "video" -> ArtifactKind.video
        "audio" -> ArtifactKind.audio
        "file_attachment", "fileattachment" -> ArtifactKind.fileAttachment
        "diff" -> ArtifactKind.diff
        "patch" -> ArtifactKind.patch
        "report" -> ArtifactKind.report
        else -> ArtifactKind.image
    }

    private fun ArtifactKind.toWireName(): String = when (this) {
        ArtifactKind.transcript -> "transcript"
        ArtifactKind.image -> "image"
        ArtifactKind.video -> "video"
        ArtifactKind.audio -> "audio"
        ArtifactKind.fileAttachment -> "file_attachment"
        ArtifactKind.diff -> "diff"
        ArtifactKind.patch -> "patch"
        ArtifactKind.report -> "report"
    }
}
