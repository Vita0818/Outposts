package com.intatis.shared

import java.io.File
import java.nio.file.Paths
import java.util.Base64

abstract class ChatAttachment(open val name: String)

class TextAttachment(override val name: String, val content: String) : ChatAttachment(name)

class ImageAttachment(override val name: String, val mimeType: String, val url: String) : ChatAttachment(name)

class AttachmentLoadResult private constructor(
    val attachment: ChatAttachment?,
    val failure: String?
) {
    val isSuccess: Boolean
        get() = attachment != null

    companion object {
        fun success(attachment: ChatAttachment): AttachmentLoadResult =
            AttachmentLoadResult(attachment, null)

        fun failure(message: String): AttachmentLoadResult =
            AttachmentLoadResult(null, message)
    }
}

object AttachmentLoader {
    private val imageExtensions = setOf("png", "jpg", "jpeg", "gif", "webp", "bmp", "svg")

    fun load(path: String): AttachmentLoadResult {
        val normalized = CommandParser.expandTilde(path)
        val full = Paths.get(normalized).toFile().absoluteFile
        if (!full.exists()) {
            return AttachmentLoadResult.failure("file not found: ${full.absolutePath}")
        }

        val bytes = try {
            full.readBytes()
        } catch (ex: Exception) {
            return AttachmentLoadResult.failure("cannot read ${full.absolutePath}: ${ex.message}")
        }

        val ext = full.extension.lowercase()
        return if (ext in imageExtensions) {
            val mime = if (ext == "jpg") "image/jpeg" else "image/$ext"
            val dataUri = "data:$mime;base64,${Base64.getEncoder().encodeToString(bytes)}"
            AttachmentLoadResult.success(ImageAttachment(full.name, mime, dataUri))
        } else {
            val text = try {
                String(bytes)
            } catch (ex: Exception) {
                return AttachmentLoadResult.failure("unsupported file type for ${full.name}")
            }
            AttachmentLoadResult.success(TextAttachment(full.name, text))
        }
    }
}
