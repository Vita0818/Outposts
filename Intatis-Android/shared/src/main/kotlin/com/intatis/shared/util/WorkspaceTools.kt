package com.intatis.shared.util

import com.intatis.shared.model.SearchHit
import java.io.File

object WorkspaceTools {
    fun resolveWorkspace(configured: String?, requested: String?): String {
        val source = requested?.ifBlank { null } ?: configured
        require(!source.isNullOrBlank()) { "No workspace path is configured." }
        val expanded = CommandParser.expandTilde(source)
        val file = File(expanded).canonicalFile
        require(file.exists() && file.isDirectory) { "Workspace not found: ${file.absolutePath}" }
        return file.absolutePath
    }

    fun list(workspace: String, relativePath: String = "."): List<String> {
        val dir = resolvePath(workspace, relativePath)
        return dir.list()?.toList()?.sorted() ?: emptyList()
    }

    fun readText(workspace: String, relativePath: String): String {
        val file = resolvePath(workspace, relativePath)
        require(file.isFile) { "File not found: ${file.absolutePath}" }
        return file.readText()
    }

    fun search(workspace: String, query: String, relativePath: String? = null): List<SearchHit> {
        val root = resolvePath(workspace, relativePath ?: ".")
        require(root.exists()) { "Path not found: ${root.absolutePath}" }

        val hits = mutableListOf<SearchHit>()
        val files = if (root.isFile) sequenceOf(root) else root.walkTopDown().filter { it.isFile }

        for (file in files) {
            if (hits.size >= 200) break
            val lines = runCatching { file.readLines() }.getOrNull() ?: continue
            for ((i, line) in lines.withIndex()) {
                if (line.contains(query, ignoreCase = true)) {
                    val rel = file.relativeTo(File(workspace)).path
                    hits.add(SearchHit(rel, i + 1, line))
                    if (hits.size >= 200) break
                }
            }
        }
        return hits
    }

    fun writeText(workspace: String, relativePath: String, content: String, overwrite: Boolean = true) {
        val file = resolvePath(workspace, relativePath)
        if (!overwrite && file.exists()) throw IllegalStateException("file already exists: ${file.absolutePath}")
        file.parentFile?.mkdirs()
        file.writeText(content)
    }

    private fun resolvePath(workspace: String, relativePath: String): File {
        val root = File(workspace).canonicalFile
        val target = if (File(relativePath).isAbsolute) File(relativePath) else File(root, relativePath)
        val canonical = target.canonicalFile
        require(canonical == root || canonical.toPath().startsWith(root.toPath())) { "Access denied: path escapes workspace" }
        return canonical
    }
}
