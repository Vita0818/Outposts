package com.intatis.cli

import com.intatis.shared.SessionId
import com.intatis.shared.SessionKind
import com.intatis.shared.ChatLoop
import com.intatis.shared.protocol.Envelope
import com.intatis.shared.protocol.Jsonx
import com.intatis.shared.protocol.EventType
import com.intatis.shared.protocol.Jsonx.str
import kotlinx.serialization.json.JsonObject
import com.intatis.shared.providers.ChatChunk
import com.intatis.shared.providers.ChatMessage
import com.intatis.shared.providers.ChatProvider
import com.intatis.shared.providers.ChatRequest
import com.intatis.shared.providers.ConfigImport
import com.intatis.shared.providers.SecretSource
import com.intatis.shared.providers.SseParser
import com.intatis.shared.session.ConversationProjection
import com.intatis.shared.session.EventLog
import com.intatis.shared.session.EventLogException
import com.intatis.shared.session.SessionProjectionStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.runBlocking
import java.io.File
import java.nio.file.Files

/** Offline test suite: no network, no credentials, runs against temp dirs. */
object SelfTest {
    private var passed = 0
    private var failed = 0

    fun run(): Int {
        val root = Files.createTempDirectory("intatis-selftest").toFile()
        try {
            testJsoncStripping()
            testConfigImport()
            testSseParser()
            testEventLog(root)
            testProjectionFold(root)
            testSessionProjection(root)
            testChatLoop(root)
        } finally {
            root.deleteRecursively()
        }
        println()
        println("selftest: $passed passed, $failed failed")
        return if (failed == 0) 0 else 1
    }

    private fun check(name: String, condition: Boolean, detail: String = "") {
        if (condition) {
            passed++
            println("  ok   $name")
        } else {
            failed++
            println("  FAIL $name $detail")
        }
    }

    private fun testJsoncStripping() {
        val jsonc = """
        {
          // provider comment
          "model": "chat/gpt-4o-mini", /* block
          comment */
          "provider": {
            "chat": { "npm": "@ai-sdk/openai-compatible",
              "options": { "baseURL": "https://chat.example.com/v1", "apiKey": "{env:CHAT_API_KEY}", },
              "models": { "gpt-4o-mini": "Mini", },
            },
          },
        }
        """.trimIndent()
        val obj = com.intatis.shared.protocol.Jsonx.parseObject(
            com.intatis.shared.protocol.Jsonx.stripJsonc(jsonc))
        check("jsonc: comments and trailing commas stripped",
            obj.str("model") == "chat/gpt-4o-mini" &&
                (obj["provider"] as? JsonObject)?.let {
                    ((it["chat"] as? JsonObject)
                        ?.get("options") as? JsonObject)?.str("apiKey")
                } == "{env:CHAT_API_KEY}")
    }

    private fun testConfigImport() {
        val config = """
        {
          "enabled_providers": ["chat", "images"],
          "model": "chat/main-model",
          "permission_reviewer_model": "chat/reviewer-model",
          "image_model": "images/gpt-image-1",
          "provider": {
            "chat": {
              "npm": "@ai-sdk/openai-compatible",
              "options": { "baseURL": "https://chat.example.com/v1", "apiKey": "{env:CHAT_API_KEY}" },
              "models": {
                "main-model": { "name": "Main" },
                "reviewer-model": { "name": "Reviewer" }
              }
            },
            "images": {
              "options": { "baseURL": "https://images.example.com/v1", "apiKey": "{env:IMAGE_API_KEY}" },
              "models": { "gpt-image-1": { "name": "Images" } }
            },
            "disabled-provider": {
              "options": { "baseURL": "https://disabled.example.com/v1" },
              "models": {}
            }
          }
        }
        """.trimIndent()
        val imported = ConfigImport.parse(config, "test.json",
            mapOf("CHAT_API_KEY" to "sk-test-value-123456"))

        check("config: providers parsed and filtered",
            imported.providers.size == 2 && imported.providers.all { it.id in setOf("chat", "images") })
        check("config: chat role resolved",
            imported.chat?.providerId == "chat" && imported.chat?.modelId == "main-model")
        check("config: reviewer binding resolved",
            imported.reviewer?.modelId == "reviewer-model")
        check("config: api key ref is env reference",
            imported.provider("chat")?.apiKeyRef?.source == SecretSource.ENVIRONMENT &&
                imported.provider("chat")?.apiKeyRef?.value == "CHAT_API_KEY")
        check("config: image model hidden from inference menu",
            imported.inferenceModels().none { it.id == "gpt-image-1" } &&
                imported.inferenceModels().any { it.id == "main-model" })

        val failClosed = ConfigImport.parse(
            """{"model":"chat/main-model","permission_reviewer_model":"nope/missing",""" +
                """"provider":{"chat":{"options":{"baseURL":"https://x.example/v1"},"models":{"main-model":"Main"}}}}""",
            "t.json")
        check("config: unresolvable reviewer fails closed",
            failClosed.reviewer == null && failClosed.reviewerFailedClosed)

        val defaults = ConfigImport.parse(
            """{"model":"ollama/llama3","provider":{"ollama":{"models":{"llama3":"Llama"}}}}""", "t.json")
        check("config: built-in default base url for ollama",
            defaults.provider("ollama")?.baseUrl == "http://localhost:11434/v1")
    }

    private fun testSseParser() {
        val parser = SseParser()
        val first = parser.consume("data: {\"a\":")
        check("sse: partial data accumulates", first.isEmpty())
        val second = parser.consume("1}\n\n")
        check("sse: dispatch on blank line", second == listOf("""{"a":1}"""))
        val multi = parser.consume("data: line1\ndata: line2\n\n")
        check("sse: multi-line data joined", multi == listOf("line1\nline2"))
        val ignored = parser.consume(": comment\nevent: x\nid: 1\nretry: 10\n\ndata: tail\n\n")
        check("sse: comments and meta ignored", ignored == listOf("tail"))
    }

    private fun testEventLog(root: File) {
        val file = File(root, "sess_test1/events.jsonl")
        EventLog.open("sess_test1", file).use { log ->
            val e1 = log.append(EventType.USER_MESSAGE,
                com.intatis.shared.protocol.UserMessagePayload(text = "hi").toJson())
            val e2 = log.append(EventType.MESSAGE_COMPLETED,
                com.intatis.shared.protocol.MessageCompletedPayload(
                    messageId = "msg_1", text = "hello", role = "assistant").toJson())
            check("eventlog: monotonic seq from zero", e1.seq == 0L && e2.seq == 1L)

            val replay = log.replay()
            check("eventlog: replay matches appends",
                replay.size == 2 && replay[0].session == "sess_test1" &&
                    replay[1].type == EventType.MESSAGE_COMPLETED)

            val unknown = log.append("future_event_type",
                Jsonx.lenient.parseToJsonElement("""{"x":1}""")
                    as JsonObject)
            check("eventlog: unknown future type reserves seq",
                unknown.seq == 2L && log.replay().size == 3)
        }

        EventLog.open("sess_test1", file).use { reopened ->
            check("eventlog: reopen rescans tail",
                reopened.lastSequence() == 2L && reopened.replay().size == 3)
        }

        EventLog.open("sess_test1", file).use {
            val second = tryOpenAgain(file)
            check("eventlog: writer lease is exclusive across runtimes", second == null)
            second?.close()
        }
    }

    private fun tryOpenAgain(file: File): EventLog? = try {
        EventLog.open("sess_test1", file)
    } catch (_: EventLogException) {
        null
    }

    private class FakeChatProvider : ChatProvider {
        val received = mutableListOf<ChatMessage>()
        override fun streamChat(request: ChatRequest): Flow<ChatChunk> = flow {
            request.messages.forEach { received.add(it) }
            emit(ChatChunk.Delta("hel"))
            emit(ChatChunk.Delta("lo"))
            emit(ChatChunk.UsageReport(
                com.intatis.shared.providers.Usage(promptTokens = 10, completionTokens = 2, totalTokens = 12)))
            emit(ChatChunk.Done)
        }
    }

    private fun testChatLoop(root: File) {
        val file = File(root, "sess_test2/events.jsonl")
        EventLog.open("sess_test2", file).use { log ->
            val provider = FakeChatProvider()
            val loop = ChatLoop(log, provider, "test-model", systemPrompt = "be brief", includeUsage = true)
            runBlocking { loop.send("hi there") }

            val projection = ConversationProjection.build(log.replay())
            check("chatloop: user + assistant messages folded",
                projection.size == 2 &&
                    projection[0].role == com.intatis.shared.protocol.MessageRole.USER &&
                    projection[0].text == "hi there" &&
                    projection[1].text == "hello" && projection[1].isComplete)
            check("chatloop: history includes system + current turn",
                provider.received.size == 2 &&
                    provider.received[0].let { it.role == "system" && it.content == "be brief" } &&
                    provider.received.last().content == "hi there")
            val outcome = log.replay().last { it.type == EventType.TURN_OUTCOME }
            check("chatloop: turn outcome completed", outcome.payload?.str("outcome") == "completed")
        }
    }

    private fun testProjectionFold(root: File) {
        val file = File(root, "sess_test3/events.jsonl")
        EventLog.open("sess_test3", file).use { log ->
            log.append(EventType.USER_MESSAGE,
                com.intatis.shared.protocol.UserMessagePayload(text = "q").toJson())
            log.append(EventType.MESSAGE_DELTA,
                com.intatis.shared.protocol.MessageDeltaPayload(messageId = "msg_a", textDelta = "one ").toJson())
            log.append(EventType.MESSAGE_DELTA,
                com.intatis.shared.protocol.MessageDeltaPayload(messageId = "msg_a", textDelta = "two").toJson())
            log.append(EventType.ERROR,
                com.intatis.shared.protocol.ErrorPayload(code = "provider", message = "boom").toJson())

            val projection = ConversationProjection.build(log.replay())
            check("projection: open delta flushed as incomplete message",
                projection.size == 3 && projection[1].text == "one two" && !projection[1].isComplete)
            check("projection: error rendered as system row",
                projection[2].role == com.intatis.shared.protocol.MessageRole.SYSTEM &&
                    projection[2].text.contains("boom"))
        }
    }

    private fun testSessionProjection(root: File) {
        val file = File(root, "sess_test4/events.jsonl")
        EventLog.open("sess_test4", file).use { log ->
            SessionProjectionStore.updateDisplayName(log, SessionKind.CHAT, "My Chat", changeKind = "created")
            SessionProjectionStore.updateDisplayName(log, SessionKind.CHAT, "Renamed Chat")

            val document = SessionProjectionStore.rebuild(log)
            check("session.json: display name rebuilt from log",
                document.displayName == "Renamed Chat" && document.settingsRevision == 2)
            check("session.json: derived cache written",
                SessionProjectionStore.load(file)?.displayName == "Renamed Chat")
        }
    }
}
