package com.intatis.shared.agentkernel

import com.intatis.shared.OpenAIClient

class ContextBuilder(
    private val systemPrompt: String,
    private val assistantRules: String,
) {
    fun createBaseContext(): MutableList<OpenAIClient.OpenAIChatMessage> = mutableListOf(
        OpenAIClient.OpenAIChatMessage("system", systemPrompt),
        OpenAIClient.OpenAIChatMessage("system", assistantRules),
    )
}
