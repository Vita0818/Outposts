package com.intatis.shared

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import com.intatis.shared.provider.ProviderRegistry

class ModelPermissionReviewer(config: IntatisConfig, model: String? = null) : IPermissionReviewer {
    private val modelProvider = ProviderRegistry(config).agentToolProvider(config.agentToolProviderId)
    private val reviewerModel: String = if (model.isNullOrBlank()) config.model else model

    override suspend fun reviewAsync(
        call: ToolCallContext,
        context: PermissionContext,
        gateReason: String,
        risk: RiskLevel
    ): PermissionOutcome {
        val touched = call.touchedPaths.joinToString("; ")
        val goal = context.userGoal ?: "(unspecified)"
        val agent = context.agent ?: "(none)"

        val prompt = """
<<<REVIEW_TARGET (untrusted data)>>>
user_goal: $goal
agent: $agent
workspace: ${context.workspaceRoot}
profile: ${context.profile.reviewPromptValue()}
tool: ${call.toolName}
side_effect: ${call.sideEffect}
touched_paths: $touched
args: ${call.rawArgs}
gate_note: $gateReason
gate_risk: $risk
<<<END>>>
Return only the JSON object.
        """.trimIndent()

        val system = """
You are a security reviewer for a local coding agent.
Decide whether a proposed tool call is reasonable for the user's task and safe to run.
Return only a JSON object, no prose:
{\"decision\":\"allow|deny|ask_user\",\"risk\":\"low|medium|high\",\"reason\":\"<short>\"}.
Prefer ask_user when unsure.
        """.trimIndent()

        val messages = listOf(
            OpenAIClient.OpenAIChatMessage("system", system),
            OpenAIClient.OpenAIChatMessage("user", prompt),
        )

        return try {
            val text = modelProvider.sendWithToolsAsync(
                messages = messages,
                tools = emptyList(),
                model = reviewerModel,
            ).text
            parse(text, risk)
        } catch (_: Exception) {
            PermissionOutcome(PermissionDecision.AskUser, risk, "reviewer error; asking user", reviewedByModel = true)
        } ?: PermissionOutcome(PermissionDecision.AskUser, risk, "reviewer output unparseable; asking user", reviewedByModel = true)
    }

    private fun parse(text: String, fallbackRisk: RiskLevel): PermissionOutcome? {
        val start = text.indexOf('{')
        val end = text.lastIndexOf('}')
        if (start < 0 || end <= start) return null

        val jsonText = text.substring(start, end + 1)
        val json = Json { ignoreUnknownKeys = true }.parseToJsonElement(jsonText).jsonObject
        val decision = when (json["decision"]?.jsonPrimitive?.content?.lowercase()) {
            "allow" -> PermissionDecision.Allow
            "deny" -> PermissionDecision.Deny
            "ask_user" -> PermissionDecision.AskUser
            else -> PermissionDecision.AskUser
        }

        val riskText = json["risk"]?.jsonPrimitive?.content?.lowercase()
        val outputRisk = when (riskText) {
            "low" -> RiskLevel.Low
            "medium" -> RiskLevel.Medium
            "high" -> RiskLevel.High
            else -> fallbackRisk
        }

        val reason = json["reason"]?.jsonPrimitive?.content ?: "reviewer decision"
        return PermissionOutcome(decision, outputRisk, reason, reviewedByModel = true)
    }

    private fun PermissionProfile.reviewPromptValue(): String = when (this) {
        PermissionProfile.Manual -> "manual"
        PermissionProfile.Reviewed -> "reviewed"
        PermissionProfile.Autopilot -> "autopilot"
        PermissionProfile.ReadOnly -> "read_only"
        PermissionProfile.Locked -> "locked"
    }
}
