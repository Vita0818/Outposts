package com.intatis.shared.attachments

import com.intatis.shared.util.CommandParser
import java.io.File
import java.nio.charset.Charset
import java.util.Base64

sealed class ChatAttachment(open val name: String)

class TextAttachment(override val name: String, val content: String) : ChatAttachment(name)

class ImageAttachment(
    override val name: String,
    val mimeType: String,
    val url: String,
) : ChatAttachment(name)

data class AttachmentLoadResult(val attachment: ChatAttachment? = null, val failure: String? = null) {
    val isSuccess: Boolean = attachment != null
}

object AttachmentLoader {
    private val imageExtensions = setOf("png", "jpg", "jpeg", "gif", "webp", "bmp")

    fun load(path: String): AttachmentLoadResult {
        return try {
            val full = CommandParser.expandTilde(path)
            val file = File(full)
            if (!file.exists()) return AttachmentLoadResult(failure = "file not found: ${file.absolutePath}")

            val bytes = file.readBytes()
            val ext = file.extension.lowercase()
            if (ext in imageExtensions) {
                val mime = if (ext == "jpg") "image/jpeg" else "image/$ext"
                val data = Base64.getEncoder().encodeToString(bytes)
                return AttachmentLoadResult(ImageAttachment(file.name, mime, "data:$mime;base64,$data"))
            }

            val text = try {
                bytes.toString(Charset.forName("UTF-8"))
            } catch (_: Exception) {
                return AttachmentLoadResult(failure = "unsupported file type '.${ext}' (only utf-8 text or images)")
            }
            AttachmentLoadResult(TextAttachment(file.name, text))
        } catch (ex: Exception) {
            AttachmentLoadResult(failure = ex.message ?: "unknown error")
        }
    }
}
