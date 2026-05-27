package com.rokurics.app.data

import android.content.Context
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.google.gson.reflect.TypeToken
import com.rokurics.app.RokuricsApp
import org.json.JSONObject
import java.io.File
import java.util.UUID

data class PersistedConversation(
    val id: String = UUID.randomUUID().toString(),
    var title: String = "新对话",
    val messages: MutableList<PersistedMessage> = mutableListOf(),
    var contextID: String? = null,
    var contextPathDisplay: String? = null,
    var contextFormattedContext: String? = null,
    val createdAt: Long = System.currentTimeMillis(),
    var updatedAt: Long = System.currentTimeMillis()
)

data class PersistedMessage(
    val id: String = UUID.randomUUID().toString(),
    val role: String,
    val content: String,
    val timestamp: Long = System.currentTimeMillis()
)

class ChatStore(
    private val context: Context = RokuricsApp.instance
) {
    private val gson: Gson = GsonBuilder().setPrettyPrinting().create()

    private val chatsDir: File
        get() = File(context.filesDir, "Rokurics/chats/conversations").also { it.mkdirs() }

    private val indexFile: File
        get() = File(chatsDir.parentFile, "conversation-index.json")

    private val maxConversations = 12

    init {
        chatsDir.mkdirs()
    }

    fun loadAll(): List<PersistedConversation> {
        val index = loadIndex()
        val conversations = mutableListOf<PersistedConversation>()
        for ((id, filename) in index) {
            val file = File(chatsDir, filename)
            if (!file.exists()) continue
            try {
                conversations.add(gson.fromJson(file.readText(), PersistedConversation::class.java))
            } catch (_: Exception) {}
        }
        return conversations.sortedByDescending { it.updatedAt }
    }

    fun save(conversation: PersistedConversation) {
        conversation.updatedAt = System.currentTimeMillis()
        val file = File(chatsDir, "${conversation.id}.json")
        file.writeText(gson.toJson(conversation))
        updateIndex(conversation.id, file.name)
    }

    fun delete(conversationID: String) {
        val index = loadIndex()
        val filename = index[conversationID] ?: return
        val file = File(chatsDir, filename)
        if (file.exists()) file.delete()
        val updated = index.toMutableMap()
        updated.remove(conversationID)
        saveIndex(updated)
    }

    fun load(id: String): PersistedConversation? {
        val index = loadIndex()
        val filename = index[id] ?: return null
        val file = File(chatsDir, filename)
        if (!file.exists()) return null
        return try {
            gson.fromJson(file.readText(), PersistedConversation::class.java)
        } catch (_: Exception) { null }
    }

    fun pruneOldest() {
        val all = loadAll()
        if (all.size > maxConversations) {
            all.drop(maxConversations).forEach { delete(it.id) }
        }
    }

    private fun loadIndex(): Map<String, String> {
        if (!indexFile.exists()) return emptyMap()
        return try {
            val json = JSONObject(indexFile.readText())
            val map = mutableMapOf<String, String>()
            val keys = json.keys()
            while (keys.hasNext()) {
                val key = keys.next()
                map[key] = json.getString(key)
            }
            map
        } catch (_: Exception) { emptyMap() }
    }

    private fun saveIndex(map: Map<String, String>) {
        val json = JSONObject()
        map.forEach { (k, v) -> json.put(k, v) }
        indexFile.writeText(json.toString(2))
    }

    private fun updateIndex(id: String, filename: String) {
        val updated = loadIndex().toMutableMap()
        updated[id] = filename
        saveIndex(updated)
    }
}
