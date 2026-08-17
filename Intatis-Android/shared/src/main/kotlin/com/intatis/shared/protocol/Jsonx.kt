package com.intatis.shared.protocol

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.buildJsonArray
import kotlinx.serialization.json.buildJsonObject

/** JSONC stripping and canonical JSON helpers shared by config and protocol code. */
object Jsonx {
    val lenient: Json = Json {
        ignoreUnknownKeys = true
        isLenient = false
        allowComments = true
        allowTrailingComma = true
    }

    val pretty: Json = Json {
        prettyPrint = true
        ignoreUnknownKeys = true
    }

    /**
     * Strips // and /* */ comments plus trailing commas, string-aware, mirroring the
     * Apple importer's JSONC preprocessing.
     */
    fun stripJsonc(source: String): String {
        val noComments = StringBuilder(source.length)
        var i = 0
        while (i < source.length) {
            val c = source[i]
            if (c == '"') {
                val start = i
                i++
                while (i < source.length) {
                    val s = source[i]
                    if (s == '\\') { i += 2; continue }
                    if (s == '"') { i++; break }
                    i++
                }
                noComments.append(source, start, i)
                continue
            }
            if (c == '/' && i + 1 < source.length && source[i + 1] == '/') {
                while (i < source.length && source[i] != '\n') i++
                continue
            }
            if (c == '/' && i + 1 < source.length && source[i + 1] == '*') {
                i += 2
                while (i + 1 < source.length && !(source[i] == '*' && source[i + 1] == '/')) i++
                i = minOf(i + 2, source.length)
                continue
            }
            noComments.append(c)
            i++
        }

        val text = noComments.toString()
        val out = StringBuilder(text.length)
        i = 0
        while (i < text.length) {
            val c = text[i]
            if (c == '"') {
                val start = i
                i++
                while (i < text.length) {
                    val s = text[i]
                    if (s == '\\') { i += 2; continue }
                    if (s == '"') { i++; break }
                    i++
                }
                out.append(text, start, i)
                continue
            }
            if (c == ',') {
                var j = i + 1
                while (j < text.length && text[j].isWhitespace()) j++
                if (j < text.length && (text[j] == ']' || text[j] == '}')) {
                    i++
                    continue
                }
            }
            out.append(c)
            i++
        }
        return out.toString()
    }

    fun parseObject(json: String): JsonObject =
        lenient.parseToJsonElement(json).let { it as? JsonObject ?: throw IllegalArgumentException("not a JSON object") }

    fun parseObjectOrNull(json: String): JsonObject? = try {
        parseObject(json)
    } catch (_: Exception) {
        null
    }

    /** Deterministic serialization: keys sorted alphabetically at every level. */
    fun serializeSorted(element: JsonElement): String = serializeSortedNode(element).toString()

    private fun serializeSortedNode(element: JsonElement): JsonElement = when (element) {
        is JsonObject -> buildJsonObject {
            element.keys.sorted().forEach { key -> put(key, element[key] ?: JsonNull) }
        }
        is JsonArray -> buildJsonArray { element.forEach { add(serializeSortedNode(it)) } }
        else -> element
    }

    fun JsonObject.str(field: String): String? = (this[field] as? JsonPrimitive)?.content
    fun JsonObject.int(field: String): Int? = (this[field] as? JsonPrimitive)?.content?.toIntOrNull()
    fun JsonObject.bool(field: String): Boolean? = (this[field] as? JsonPrimitive)?.content?.toBooleanStrictOrNull()
}
