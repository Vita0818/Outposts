package com.intatis.shared.agentkernel

import com.intatis.shared.OpenAIClient
import com.intatis.shared.ToolDescriptor
import com.intatis.shared.IntatisConfig
import com.intatis.shared.provider.AgentToolProvider
import com.intatis.shared.provider.ProviderRegistry
import java.time.Duration

data class ToolCallResult(
    val text: String,
    val toolCalls: List<OpenAIClient.ToolCall>,
    val latency: Duration,
    val usage: String?,
)

class ToolCallingProvider(config: IntatisConfig) {
    private val toolProvider: AgentToolProvider = ProviderRegistry(config).agentToolProvider(config.agentToolProviderId)

    suspend fun sendAsync(
        messages: List<OpenAIClient.OpenAIChatMessage>,
        toolDescriptors: List<ToolDescriptor>,
        model: String? = null,
        reasoning: String? = null,
        includeUsage: Boolean = false,
    ): ToolCallResult {
        val result = toolProvider.sendWithToolsAsync(messages, toolDescriptors, model, reasoning, includeUsage)

        return ToolCallResult(
            text = result.text,
            toolCalls = result.toolCalls,
            latency = result.latency,
            usage = result.usage,
        )
    }
}
