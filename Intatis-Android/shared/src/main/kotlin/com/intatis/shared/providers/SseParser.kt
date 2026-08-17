package com.intatis.shared.providers

/**
 * Incremental SSE parser: joins multi-line data fields, ignores event/id/retry and
 * comments, dispatches on blank line. A single SSE line may be split across network
 * chunks — the trailing partial line is carried into the next consume() call.
 */
class SseParser {
    private val pending = StringBuilder()
    private var pendingHasData = false
    private val carry = StringBuilder()

    fun consume(chunk: String): List<String> {
        val events = mutableListOf<String>()
        carry.append(chunk)
        var text = carry.toString()
        carry.clear()

        // Hold back a trailing partial line (no newline yet).
        val lastNewline = text.lastIndexOf('\n')
        if (lastNewline < text.length - 1) {
            carry.append(text.substring(lastNewline + 1))
            text = text.substring(0, lastNewline + 1)
        }

        for (rawLine in text.split('\n')) {
            val line = rawLine.trimEnd('\r')
            if (line.isEmpty()) {
                dispatch(events)
                continue
            }
            if (line[0] == ':') continue // comment
            if (line.startsWith("data:")) {
                var data = line.substring(5)
                if (data.isNotEmpty() && data[0] == ' ') data = data.substring(1)
                if (pendingHasData) pending.append('\n')
                pending.append(data)
                pendingHasData = true
            }
            // event:/id:/retry: intentionally ignored
        }
        return events
    }

    fun flush(): List<String> {
        val events = mutableListOf<String>()
        if (carry.isNotEmpty()) {
            val rest = carry.toString()
            carry.clear()
            consume(rest + "\n").forEach { events.add(it) }
        }
        dispatch(events)
        return events
    }

    private fun dispatch(events: MutableList<String>) {
        if (pendingHasData) {
            events.add(pending.toString())
            pending.clear()
            pendingHasData = false
        }
    }
}
