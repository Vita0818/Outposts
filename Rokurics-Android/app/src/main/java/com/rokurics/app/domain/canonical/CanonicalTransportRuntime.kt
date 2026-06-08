package com.rokurics.app.domain.canonical

data class CanonicalTransportRoute(
    val scheme: String,
    val host: String,
    val port: Int? = null,
    val pathTemplate: String? = null
)

data class CanonicalTransportRequest(
    val route: CanonicalTransportRoute,
    val bodyHash: CanonicalHash? = null,
    val headers: Map<String, String> = emptyMap()
)

data class CanonicalTransportResponse(
    val status: Int,
    val bodyHash: CanonicalHash? = null
)

data class CanonicalTransportCapability(
    val supportsResumable: Boolean = false,
    val supportsChunked: Boolean = false,
    val supportsFinalize: Boolean = false,
    val maxChunkSize: Int = 65536
)

object CanonicalTransportRuntime {
    private val capabilityCache = mutableMapOf<String, CanonicalTransportCapability>()

    fun evaluateRoute(route: CanonicalTransportRoute): CanonicalTransportCapability {
        val key = "${route.scheme}://${route.host}${route.port?.let { ":$it" } ?: ""}"
        return capabilityCache.getOrPut(key) {
            when (route.scheme) {
                "https" -> CanonicalTransportCapability(
                    supportsResumable = true,
                    supportsChunked = true,
                    supportsFinalize = true,
                    maxChunkSize = 131072
                )
                "http" -> CanonicalTransportCapability(
                    supportsResumable = false,
                    supportsChunked = true,
                    supportsFinalize = false,
                    maxChunkSize = 65536
                )
                "local" -> CanonicalTransportCapability(
                    supportsResumable = false,
                    supportsChunked = true,
                    supportsFinalize = true,
                    maxChunkSize = 262144
                )
                else -> CanonicalTransportCapability(
                    supportsResumable = false,
                    supportsChunked = false,
                    supportsFinalize = false,
                    maxChunkSize = 4096
                )
            }
        }
    }
}
