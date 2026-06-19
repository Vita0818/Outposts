package com.intatis.shared.session

import com.intatis.shared.util.WorkspaceTools
import java.nio.file.Files
import kotlin.random.Random

object SelfTest {
    suspend fun run(): Boolean {
        val tmp = Files.createTempDirectory("intatis-android-selftest-${Random.nextInt()}" ).toFile()
        return try {
            WorkspaceTools.writeText(tmp.absolutePath, "readme.txt", "hello intatis")
            val text = WorkspaceTools.readText(tmp.absolutePath, "readme.txt")
            val hits = WorkspaceTools.search(tmp.absolutePath, "intatis")
            text == "hello intatis" && hits.size == 1
        } finally {
            runCatching { tmp.deleteRecursively() }
        }
    }
}
