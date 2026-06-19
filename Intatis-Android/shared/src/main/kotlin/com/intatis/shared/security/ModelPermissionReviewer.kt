package com.intatis.shared.security

import com.intatis.shared.model.IntatisConfig
import com.intatis.shared.model.IntatisMessage
import com.intatis.shared.model.MessageRole
import com.intatis.shared.provider.OpenAIClient
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.jsonPrimitive

class ModelPermissionReviewer(
    private val config: IntatisConfig,
    private val reviewerModel: String? = null,
) : PermissionReviewer {

    override suspend fun reviewAsync(
        call: ToolCallContext,
        context: PermissionContext,
        gateReason: String,
        risk: RiskLevel,
    ): PermissionOutcome {
        val client = OpenAIClient(config)
        val system =
            "You are a security reviewer for a local coding agent.\n" +
                "Decide whether a proposed tool call is reasonable and safe for the user's task.\n" +
                "Return only JSON: {\"decision\":\"allow|deny|ask_user\", \"risk\":\"low|medium|high\", \"reason\":\"<short>\"}."

        val prompt = buildString {
            appendLine("<<<REVIEW_TARGET (untrusted data)>>>")
            appendLine("user_goal: ${context.userGoal ?: "(unspecified)")}")
            appendLine("agent: ${context.agent ?: "(none)"}")
            appendLine("workspace: ${context.workspaceRoot}")
            appendLine("profile: ${context.profile}")
            appendLine("tool: ${call.toolName}")
            appendLine("side_effect: ${call.sideEffect}")
            appendLine("touched_paths: ${call.touchedPaths.joinToString(", ")}")
            appendLine("args: ${call.rawArgs}")
            appendLine("gate_note: $gateReason")
            appendLine("gate_risk: $risk")
            appendLine("<<<END>>>")
            appendLine("Return only the JSON object.")
        }

        val response = client.sendAsync(
            messages = listOf(
                IntatisMessage(role = MessageRole.SYSTEM, content = system),
                IntatisMessage(role = MessageRole.USER, content = prompt),
            ),
            model = reviewerModel ?: config.model,
            reasoning = null,
        )

        val raw = response.text
        val parsed = parseJsonObject(raw)
        if (parsed == null) {
            return PermissionOutcome(PermissionDecision.ASK_USER, risk, "reviewer output unparseable; asking user", reviewedByModel = true)
        }

        val decision = when (parsed["decision"]?.toString()?.trim('"', '\\'')) {
            "allow" -> PermissionDecision.ALLOW
            "deny" -> PermissionDecision.DENY
            "ask_user" -> PermissionDecision.ASK_USER
            else -> PermissionDecision.ASK_USER
        }

        val parsedRisk = when (parsed["risk"]?.toString()?.trim('"', '\\'')) {
            "low" -> RiskLevel.LOW
            "high" -> RiskLevel.HIGH
            else -> risk
        }

        val reason = parsed["reason"]?.toString()?.trim('"', '\\'') ?: "reviewer decision"
        return PermissionOutcome(decision, parsedRisk, reason, reviewedByModel = true)
    }

    private fun parseJsonObject(text: String): Map<String, String>? {
        val start = text.indexOf('{')
        val end = text.lastIndexOf('}')
        if (start < 0 || end <= start) return null
        val block = text.substring(start, end + 1)
        return runCatching {
            val root = Json.parseToJsonElement(block).jsonObject
            val out = mutableMapOf<String, String>()
            root.entries.forEach { (k, v) ->
                val value = when (v) {
                    is JsonPrimitive -> v.content
                    else -> v.toString()
                }
                out[k] = value
            }
            out
        }.getOrNull()
    }
}
