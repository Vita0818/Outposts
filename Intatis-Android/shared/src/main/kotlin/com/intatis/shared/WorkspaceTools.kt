package com.intatis.shared

import java.io.File

object WorkspaceTools {
    fun resolveWorkspace(configured: String?, requested: String?): String {
        val source = requested?.ifBlank { null } ?: configured?.ifBlank { null }
        if (source.isNullOrBlank()) {
            throw IllegalStateException("No workspace path is configured.")
        }

        val expanded = CommandParser.expandTilde(source)
        val full = File(expanded).absoluteFile
        if (!full.exists()) {
            throw IllegalArgumentException("Workspace not found: ${full.absolutePath}")
        }
        return full.absolutePath
    }

    fun list(workspace: String, relativePath: String = ""): List<String> {
        val root = resolvePath(workspace, relativePath)
        ensureInsideWorkspace(workspace, root)
        return root.list()
            ?.sorted()
            ?.toList()
            ?: emptyList()
    }

    fun readText(workspace: String, relativePath: String): String {
        val path = resolvePath(workspace, relativePath)
        ensureInsideWorkspace(workspace, path)
        if (!path.exists()) throw IllegalArgumentException("File not found: ${path.path}")
        return path.readText()
    }

    fun search(workspace: String, needle: String, relativePath: String? = null): List<SearchHit> {
        val root = resolvePath(workspace, relativePath ?: ".")
        ensureInsideWorkspace(workspace, root)

        if (!root.exists()) throw IllegalArgumentException("Path not found: ${root.path}")

        val matches = mutableListOf<SearchHit>()
        if (root.isFile) {
            searchInFile(root, workspace, needle, matches)
            return matches
        }

        root.walkTopDown()
            .onEnter { path -> path == root || !isHiddenPath(path) }
            .filter { it.isFile && !isHiddenPath(it) }
            .forEachIndexed { index, file ->
            if (matches.size >= 200) return matches
            searchInFile(file, workspace, needle, matches)
        }
        return matches
    }

    fun writeText(workspace: String, relativePath: String, content: String, overwrite: Boolean = true) {
        val path = resolvePath(workspace, relativePath)
        ensureInsideWorkspace(workspace, path)
        if (!overwrite && path.exists()) throw IllegalStateException("File already exists: ${path.path}")
        path.parentFile?.mkdirs()
        path.writeText(content)
    }

    private fun searchInFile(file: File, workspace: String, needle: String, matches: MutableList<SearchHit>) {
        val rel = file.relativeTo(File(workspace)).path
        var idx = 1
        file.forEachLine { line ->
            if (line.contains(needle, ignoreCase = false)) {
                matches.add(SearchHit(rel, idx, line))
            }
            idx++
            if (matches.size >= 200) return
        }
    }

    private fun resolvePath(workspace: String, relativePath: String): File {
        val root = File(workspace).absoluteFile
        return if (relativePath.isBlank() || relativePath == ".") {
            root
        } else if (File(relativePath).isAbsolute) {
            File(relativePath).absoluteFile
        } else {
            File(root, relativePath).absoluteFile
        }
    }

    private fun ensureInsideWorkspace(workspace: String, candidate: File) {
        if (!WorkspaceSecurity.isWithinWorkspace(candidate.absolutePath, File(workspace).absolutePath)) {
            throw IllegalArgumentException("Access denied: path escapes workspace")
        }
    }

    private fun isHiddenPath(file: File): Boolean {
        return file.isHidden || file.name.startsWith(".")
    }
}
