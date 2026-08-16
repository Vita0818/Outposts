package com.intatis.shared.architecture

/**
 * Migration entrypoint for the domain-oriented Android shared runtime layout.
 *
 * The architecture migration (P0-001) is organized around these canonical
 * domain groups so downstream entry points can consume a stable package map
 * while sessions and tools continue to be migrated incrementally.
 */
public object ArchitectureMigration {
    public const val phase: String = "P0-001"
    public val domainPackages: List<String> = listOf(
        "core",
        "protocol",
        "provider",
        "permission",
        "tools",
        "agentkernel",
        "conversation",
        "cowork",
        "multimodal",
        "artifacts",
        "sharedui",
    )
}
