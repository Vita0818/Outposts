package com.intatis.cli

import com.intatis.shared.SessionId
import com.intatis.shared.SessionKind
import com.intatis.shared.protocol.EventType
import com.intatis.shared.protocol.Jsonx.str
import com.intatis.shared.ChatLoop
import com.intatis.shared.providers.ConfigSecretResolver
import com.intatis.shared.providers.ConfigStore
import com.intatis.shared.providers.OpenAIWireProvider
import com.intatis.shared.providers.ReasoningEffort
import com.intatis.shared.session.EventLog
import com.intatis.shared.session.SessionHistoryStore
import kotlinx.coroutines.runBlocking
import java.io.File

fun main(args: Array<String>) {
    val command = args.firstOrNull() ?: "help"
    val rest = args.drop(1)

    when (command) {
        "help", "--help", "-h" -> printHelp()

        "config" -> printConfig()

        "settings" -> printSettings()

        "chat" -> runBlocking { runChat() }

        "selftest" -> exitProcess(SelfTest.run())

        else -> {
            println("unknown command: $command")
            printHelp()
        }
    }
}

private fun exitProcess(code: Int): Nothing {
    kotlin.system.exitProcess(code)
}

private fun printHelp() {
    println(
        """
        intatis — local AI workspace (Android-shared core, JVM CLI)

        usage:
          intatis help              show this help
          intatis config            print the resolved provider configuration (secrets masked)
          intatis settings          print application paths
          intatis chat              streaming chat REPL (no tools — mobile Chat subset contract)
          intatis selftest          offline test suite (no network)

        slash commands:
          /model [id]               show or set the session model
          /attach <image-path>      attach an image to the next message
          /exit                     quit
        """.trimIndent()
    )
}

private fun printConfig() {
    val (config, source) = ConfigStore.load()
    println("source: ${if (source.path.isNotEmpty()) source.path else "(defaults; no config file found)"}")
    config.warnings.forEach { println("warning: $it") }

    for (provider in config.providers) {
        val models = if (provider.models.isEmpty()) "-" else provider.models.joinToString(", ") { it.id }
        println("provider ${provider.id} (${provider.displayName})")
        println("  base url: ${provider.baseUrl}")
        println("  api key:  ${provider.apiKeyRef.describe()}")
        println("  models:   $models")
    }
    fun role(label: String, ref: com.intatis.shared.providers.ModelRef?) =
        println("$label: ${ref?.displayLabel ?: "-"}")
    role("model", config.chat)
    role("permission_reviewer_model", config.reviewer)
    role("image_model", config.image)
    role("transcription_model", config.transcription)
    role("embedding_model", config.embedding)
    role("reranker_model", config.reranker)
    if (config.reviewerFailedClosed) println("permission reviewer: FAIL CLOSED (field present but unresolvable)")
}

private fun printSettings() {
    println("user.home:    ${System.getProperty("user.home")}")
    println("config paths:")
    for (candidate in com.intatis.shared.providers.AppConfig.configCandidates()) {
        println("  ${if (candidate.exists()) "*" else " "} ${candidate.path}")
    }
}

private suspend fun runChat() {
    val (config, _) = ConfigStore.load()
    val chat = config.chat
        ?: com.intatis.shared.providers.ModelRef("openai", com.intatis.shared.providers.AppConfig.DEFAULT_MODEL)
    val entry = config.provider(chat.providerId)
    if (entry == null) {
        println("no provider '${chat.providerId}' configured; run 'intatis config'")
        return
    }
    val apiKey = ConfigSecretResolver(File(""), config.sourcePath.takeIf { it.isNotEmpty() }
        ?.let(::File) ?: File("")).resolveSecret(entry.apiKeyRef)
    if (apiKey.isEmpty()) {
        println("api key for '${chat.providerId}' is empty (${entry.apiKeyRef.describe()})")
        return
    }

    val session = SessionId.new(SessionKind.CHAT)
    val root = File(System.getProperty("user.home"), ".intatis/sessions")
    val log = EventLog.open(session.value, SessionHistoryStore.sessionFile(root, session.value))
    println("intatis chat · ${chat.displayLabel} · session ${session.value}")
    println("type /help for commands; /exit to quit")

    var currentModel = chat.modelId
    val pendingImages = mutableListOf<String>()

    log.onEnvelopeAppended = { envelope ->
        when (envelope.type) {
            EventType.MESSAGE_DELTA -> envelope.payload?.str("text_delta")?.let { print(it); System.out.flush() }
            EventType.MESSAGE_COMPLETED -> println()
            EventType.ERROR -> println("\n  error: ${envelope.payload?.str("message")}")
        }
    }

    while (true) {
        print("chat> ")
        val line = readLine() ?: break
        val text = line.trim()
        if (text.isEmpty()) continue

        if (text.startsWith("/")) {
            var exit = false
            when {
                text == "/help" -> println("/model [id]  /attach <path>  /config  /exit")
                text.startsWith("/model") -> {
                    val arg = text.removePrefix("/model").trim()
                    if (arg.isNotEmpty()) currentModel = arg
                    println("model: $currentModel")
                }
                text.startsWith("/attach") -> {
                    val path = text.removePrefix("/attach").trim()
                    val file = File(path)
                    if (!file.exists()) println("not found: $path")
                    else {
                        val mime = when (file.extension.lowercase()) {
                            "png" -> "image/png"; "webp" -> "image/webp"; "gif" -> "image/gif"
                            else -> "image/jpeg"
                        }
                        val dataUrl = "data:$mime;base64," +
                            java.util.Base64.getEncoder().encodeToString(file.readBytes())
                        pendingImages.add(dataUrl)
                        println("attached image (${file.length() / 1024} KiB)")
                    }
                }
                text == "/config" -> println(com.intatis.shared.providers.AppConfig.defaultConfigPath().path)
                text == "/exit" || text == "/quit" -> exit = true
                else -> println("unknown command $text; /help for help")
            }
            if (exit) break
            continue
        }

        val images = pendingImages.map { com.intatis.shared.providers.ImageAttachment(it) }
        pendingImages.clear()

        val provider = OpenAIWireProvider(
            OpenAIWireProvider.defaultClient(), entry.baseUrl, apiKey, entry.chatEndpoint)
        val effort = ReasoningEffort.fromWire(System.getenv("INTATIS_REASONING"))
        val loop = ChatLoop(
            log = log,
            provider = provider,
            model = currentModel,
            systemPrompt = "You are Intatis, a concise local AI assistant.",
            reasoningEffort = effort,
            includeUsage = true,
        )
        try {
            loop.send(text, images)
        } catch (e: kotlinx.coroutines.CancellationException) {
            // turn already settled as interrupted
        } catch (e: Exception) {
            println("error: ${e.message}")
        }
    }
    log.close()
}
