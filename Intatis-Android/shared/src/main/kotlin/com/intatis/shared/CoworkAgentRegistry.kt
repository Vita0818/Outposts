package com.intatis.shared

import java.util.Locale

class CoworkAgentRegistry {
    companion object {
        const val PermissionReviewerIdentity = "permission_reviewer"
    }

    private val agents = linkedMapOf<String, CoworkEngine.CoworkAgentState>(String.CASE_INSENSITIVE_ORDER)

    val names: List<String>
        get() = agents.keys.toList()

    val count: Int
        get() = agents.size

    val all: List<CoworkEngine.CoworkAgentState>
        get() = agents.values.toList()

    fun isEmpty(): Boolean = agents.isEmpty()

    fun contains(name: String): Boolean = agents.containsKey(normalize(name))

    fun resolve(name: String?): CoworkEngine.CoworkAgentState? {
        if (name == null) return agents.values.firstOrNull()
        return agents[normalize(name)]
    }

    fun register(state: CoworkEngine.CoworkAgentState): Boolean {
        val key = normalize(state.name)
        if (agents.containsKey(key)) return false
        agents[key] = state
        return true
    }

    fun unregister(name: String): CoworkEngine.CoworkAgentState? = agents.remove(normalize(name))

    fun isReservedPermissionReviewer(name: String): Boolean = name.equals(PermissionReviewerIdentity, ignoreCase = true)

    private fun normalize(name: String): String = name.lowercase(Locale.getDefault())
}
