package com.rokurics.app.domain.provider

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL

data class NoteGenerationRequest(
    val recordingID: String,
    val transcript: String,
    val title: String,
    val studyFilingPath: String? = null,
    val instructions: String? = null
)

data class NoteGenerationResult(
    val recordingID: String,
    val noteMarkdown: String,
    val shortSummary: String? = null,
    val keyPoints: List<String> = emptyList(),
    val providerID: String,
    val modelName: String? = null
)

interface NoteGenerationProvider {
    val id: String
    val displayName: String
    suspend fun validateConfiguration()
    suspend fun generateNote(request: NoteGenerationRequest): Result<NoteGenerationResult>
}

class MockNoteGenerationProvider : NoteGenerationProvider {
    override val id = "mock_note_generation"
    override val displayName = "Mock Note Generation"
    override suspend fun validateConfiguration() {}
    override suspend fun generateNote(request: NoteGenerationRequest): Result<NoteGenerationResult> =
        Result.success(NoteGenerationResult(
            recordingID = request.recordingID,
            noteMarkdown = "# ${request.title}\n\n## 摘要\n\n[Mock] AI-generated note summary for: ${request.title}\n\n## 重点\n\n- [Mock] Key point 1\n- [Mock] Key point 2\n- [Mock] Key point 3",
            shortSummary = "[Mock] Summary for ${request.title}",
            keyPoints = listOf("[Mock] Key point 1", "[Mock] Key point 2", "[Mock] Key point 3"),
            providerID = id,
            modelName = "mock-v1"
        ))
}

class OpenAICompatibleNoteGenerationProvider(
    val baseURLString: String,
    val modelName: String,
    val apiKey: String,
    val temperature: Double = 0.3,
    val maxTokens: Int = 2000
) : NoteGenerationProvider {
    override val id = "openai_compatible_note"
    override val displayName = "OpenAI-compatible Note Generation"

    override suspend fun validateConfiguration() {
        if (baseURLString.isBlank()) throw NoteGenerationError("Base URL 未配置")
    }

    override suspend fun generateNote(request: NoteGenerationRequest): Result<NoteGenerationResult> =
        withContext(Dispatchers.IO) {
            try {
                val messages = JSONArray()
                messages.put(JSONObject().apply {
                    put("role", "system")
                    put("content", noteSystemPrompt(request))
                })
                messages.put(JSONObject().apply {
                    put("role", "user")
                    put("content", buildTranscriptWithContext(request))
                })

                val body = JSONObject().apply {
                    put("model", modelName)
                    put("messages", messages)
                    put("temperature", temperature)
                    put("max_tokens", maxTokens)
                    put("stream", false)
                }

                val responseText = sendJSON(baseURLString, "chat/completions", apiKey, body.toString())
                val response = JSONObject(responseText)
                val content = response.getJSONArray("choices")
                    .getJSONObject(0)
                    .getJSONObject("message")
                    .optString("content", "").trim()

                if (content.isEmpty()) return@withContext Result.failure(NoteGenerationError("AI 没有返回内容"))

                val summary = extractSection(content, "摘要")
                val keyPoints = extractBulletPoints(extractSection(content, "重点"))

                Result.success(NoteGenerationResult(
                    recordingID = request.recordingID,
                    noteMarkdown = content,
                    shortSummary = summary,
                    keyPoints = keyPoints,
                    providerID = id,
                    modelName = modelName
                ))
            } catch (e: NoteGenerationError) {
                Result.failure(e)
            } catch (e: Exception) {
                Result.failure(NoteGenerationError("笔记生成失败: ${e.message}"))
            }
        }

    private fun noteSystemPrompt(request: NoteGenerationRequest): String = buildString {
        append("你是 Rokurics 的课堂笔记整理助手。请根据提供的课堂录音转录文本，生成结构化的学习笔记。\n\n")
        append("笔记必须使用以下 Markdown 结构：\n")
        append("# 录音笔记\n\n")
        append("## 摘要\n用一段话概括本次录音的核心内容。\n\n")
        append("## 大纲\n列出录音内容的层次结构，使用嵌套列表。\n\n")
        append("## 重点\n列出 3-5 个最重要的知识点，每个知识点用一两句话解释。\n\n")
        append("## 待复习问题\n基于录音内容提出 3-5 个问题，供后续复习使用。\n\n")
        append("## 可整理为 Kikaria 知识卡的候选内容\n列出 2-4 个适合制作成知识卡片的独立知识点。\n\n")
        append("要求：\n")
        append("- 笔记内容应准确反映录音内容，不要凭空添加信息。\n")
        append("- 如果转录文本不完整或无法理解，请明确指出。\n")
        append("- 使用中文撰写。\n")
        append("- 录音标题：${request.title}\n")
    }

    private fun buildTranscriptWithContext(request: NoteGenerationRequest): String = buildString {
        append("录音标题：${request.title}\n")
        if (!request.studyFilingPath.isNullOrBlank()) {
            append("归档路径：${request.studyFilingPath}\n")
        }
        if (!request.instructions.isNullOrBlank()) {
            append("额外指示：${request.instructions}\n")
        }
        append("\n--- 转录文本 ---\n\n")
        append(request.transcript)
    }

    private fun sendJSON(baseURLString: String, path: String, apiKey: String, body: String): String {
        val normalizedBase = baseURLString.trimEnd('/')
        val url = URL("$normalizedBase/$path")
        val connection = url.openConnection() as HttpURLConnection
        connection.apply {
            requestMethod = "POST"
            doOutput = true
            connectTimeout = 30000
            readTimeout = 120000
            setRequestProperty("Content-Type", "application/json")
            if (apiKey.isNotEmpty()) {
                setRequestProperty("Authorization", "Bearer $apiKey")
            }
        }
        try {
            connection.outputStream.use { it.write(body.toByteArray(Charsets.UTF_8)) }
            val code = connection.responseCode
            if (code !in 200..299) {
                throw NoteGenerationError("AI 请求失败: HTTP $code")
            }
            return connection.inputStream.bufferedReader().readText()
        } finally {
            connection.disconnect()
        }
    }

    private fun extractSection(markdown: String, sectionName: String): String? {
        val pattern = Regex("""^##\s*$sectionName\s*\n+(.+?)(?=\n##\s|\z)""",
            setOf(RegexOption.MULTILINE, RegexOption.DOT_MATCHES_ALL))
        return pattern.find(markdown)?.groupValues?.get(1)?.trim()
    }

    private fun extractBulletPoints(text: String?): List<String> {
        if (text.isNullOrBlank()) return emptyList()
        return text.lines()
            .filter { it.trimStart().startsWith("-") || it.trimStart().startsWith("*") }
            .map { it.trimStart().removePrefix("- ").removePrefix("* ").trim() }
    }
}

class AnthropicNoteGenerationProvider(
    val baseURLString: String,
    val modelName: String,
    val apiKey: String,
    val anthropicVersion: String = "2023-06-01",
    val temperature: Double = 0.3,
    val maxTokens: Int = 2000
) : NoteGenerationProvider {
    override val id = "anthropic_note"
    override val displayName = "Claude / Anthropic Note Generation"

    override suspend fun validateConfiguration() {
        if (apiKey.isBlank()) throw NoteGenerationError("API Key 未配置")
    }

    override suspend fun generateNote(request: NoteGenerationRequest): Result<NoteGenerationResult> =
        withContext(Dispatchers.IO) {
            try {
                val messages = JSONArray()
                messages.put(JSONObject().apply {
                    put("role", "user")
                    put("content", buildTranscriptWithContext(request))
                })

                val body = JSONObject().apply {
                    put("model", modelName)
                    put("max_tokens", maxTokens)
                    put("temperature", temperature)
                    put("system", noteSystemPrompt(request))
                    put("messages", messages)
                }

                val responseText = sendJSON(body.toString())
                val response = JSONObject(responseText)
                val contentBlocks = response.getJSONArray("content")
                val texts = mutableListOf<String>()
                for (i in 0 until contentBlocks.length()) {
                    val block = contentBlocks.getJSONObject(i)
                    if (block.optString("type") == "text") {
                        block.optString("text")?.let { texts.add(it) }
                    }
                }
                val content = texts.joinToString("\n").trim()
                if (content.isEmpty()) return@withContext Result.failure(NoteGenerationError("AI 没有返回内容"))

                val summary = extractSection(content, "摘要")
                val keyPoints = extractBulletPoints(extractSection(content, "重点"))

                Result.success(NoteGenerationResult(
                    recordingID = request.recordingID,
                    noteMarkdown = content,
                    shortSummary = summary,
                    keyPoints = keyPoints,
                    providerID = id,
                    modelName = modelName
                ))
            } catch (e: NoteGenerationError) {
                Result.failure(e)
            } catch (e: Exception) {
                Result.failure(NoteGenerationError("笔记生成失败: ${e.message}"))
            }
        }

    private fun noteSystemPrompt(request: NoteGenerationRequest): String = buildString {
        append("你是 Rokurics 的课堂笔记整理助手。请根据提供的课堂录音转录文本，生成结构化的学习笔记。\n\n")
        append("笔记必须使用以下 Markdown 结构：\n")
        append("# 录音笔记\n\n")
        append("## 摘要\n用一段话概括本次录音的核心内容。\n\n")
        append("## 大纲\n列出录音内容的层次结构，使用嵌套列表。\n\n")
        append("## 重点\n列出 3-5 个最重要的知识点，每个知识点用一两句话解释。\n\n")
        append("## 待复习问题\n基于录音内容提出 3-5 个问题，供后续复习使用。\n\n")
        append("## 可整理为 Kikaria 知识卡的候选内容\n列出 2-4 个适合制作成知识卡片的独立知识点。\n\n")
        append("要求：\n")
        append("- 笔记内容应准确反映录音内容，不要凭空添加信息。\n")
        append("- 如果转录文本不完整或无法理解，请明确指出。\n")
        append("- 使用中文撰写。\n")
        append("- 录音标题：${request.title}\n")
    }

    private fun buildTranscriptWithContext(request: NoteGenerationRequest): String = buildString {
        append("录音标题：${request.title}\n")
        if (!request.studyFilingPath.isNullOrBlank()) {
            append("归档路径：${request.studyFilingPath}\n")
        }
        if (!request.instructions.isNullOrBlank()) {
            append("额外指示：${request.instructions}\n")
        }
        append("\n--- 转录文本 ---\n\n")
        append(request.transcript)
    }

    private fun sendJSON(body: String): String {
        val normalizedBase = baseURLString.trimEnd('/')
        val url = URL("$normalizedBase/v1/messages")
        val connection = url.openConnection() as HttpURLConnection
        connection.apply {
            requestMethod = "POST"
            doOutput = true
            connectTimeout = 30000
            readTimeout = 120000
            setRequestProperty("Content-Type", "application/json")
            setRequestProperty("x-api-key", apiKey)
            setRequestProperty("anthropic-version", anthropicVersion)
        }
        try {
            connection.outputStream.use { it.write(body.toByteArray(Charsets.UTF_8)) }
            val code = connection.responseCode
            if (code !in 200..299) {
                throw NoteGenerationError("AI 请求失败: HTTP $code")
            }
            return connection.inputStream.bufferedReader().readText()
        } finally {
            connection.disconnect()
        }
    }

    private fun extractSection(markdown: String, sectionName: String): String? {
        val pattern = Regex("""^##\s*$sectionName\s*\n+(.+?)(?=\n##\s|\z)""",
            setOf(RegexOption.MULTILINE, RegexOption.DOT_MATCHES_ALL))
        return pattern.find(markdown)?.groupValues?.get(1)?.trim()
    }

    private fun extractBulletPoints(text: String?): List<String> {
        if (text.isNullOrBlank()) return emptyList()
        return text.lines()
            .filter { it.trimStart().startsWith("-") || it.trimStart().startsWith("*") }
            .map { it.trimStart().removePrefix("- ").removePrefix("* ").trim() }
    }
}

class NoteGenerationError(message: String) : Exception(message)
