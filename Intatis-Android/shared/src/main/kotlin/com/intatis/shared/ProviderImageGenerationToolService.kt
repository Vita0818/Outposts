package com.intatis.shared

import com.intatis.shared.provider.GeneratedImage
import com.intatis.shared.provider.ImageRequest
import com.intatis.shared.provider.ProviderRegistry
import java.io.File
import java.nio.file.Paths

class ProviderImageGenerationToolService(
    private val providerRegistry: ProviderRegistry,
) : ImageGenerationToolService {
    override suspend fun generateImage(
        prompt: String,
        size: String,
        count: Int,
        outputPath: String,
        workspaceRoot: String,
    ): ToolObservation {
        val provider = providerRegistry.imageProvider()
        val model = providerRegistry.imageModel()
        val request = ImageRequest(
            model = model,
            prompt = prompt,
            size = size,
            count = count,
        )

        val images: List<GeneratedImage> = provider.generate(request)
        if (images.isEmpty()) {
            throw IllegalStateException("image provider returned no images")
        }

        val workspace = File(workspaceRoot).canonicalFile
        val resolvedOutput = File(WorkspaceSecurity.resolveInWorkspace(workspace.absolutePath, outputPath))
        resolvedOutput.parentFile?.mkdirs()

        val written = mutableListOf<String>()
        images.forEachIndexed { index, image ->
            val name = if (images.size == 1) {
                resolvedOutput.name
            } else {
                val stem = resolvedOutput.nameWithoutExtension.ifBlank { "image" }
                val ext = resolvedOutput.extension.ifBlank { "png" }
                "$stem-${String.format("%02d", index + 1)}.$ext"
            }
            val imageFile = if (images.size == 1) resolvedOutput else File(resolvedOutput.parentFile, name)
            imageFile.writeBytes(image.data)
            val relative = runCatching { Paths.get(imageFile.path).toAbsolutePath().normalize() }
                .getOrElse { File(imageFile.path).absoluteFile.toPath() }
                .toString().removePrefix(workspace.absolutePath).trimStart('/', '\\')
            written.add(relative)
        }

        return ToolObservation(
            text = "generated ${images.size} image(s): ${written.joinToString(",")}",
            changedFiles = written,
        )
    }
}
