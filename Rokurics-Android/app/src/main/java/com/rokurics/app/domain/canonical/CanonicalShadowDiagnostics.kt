package com.rokurics.app.domain.canonical

import java.util.Date

// ── CanonicalShadowDiagnosticsMode ──

enum class CanonicalShadowDiagnosticsMode(val rawValue: String) {
    DISABLED("disabled"),
    REPORT_ONLY("reportOnly"),
    REPORT_WITH_PREFIX("reportWithPrefix"),
    REDACTED("redacted");

    companion object {
        val allCases: List<CanonicalShadowDiagnosticsMode> = entries.toList()
    }
}

// ── CanonicalShadowDiagnosticsCategory ──

enum class CanonicalShadowDiagnosticsCategory(val rawValue: String) {
    METADATA_CONVERGED("metadataConverged"),
    METADATA_DIVERGED("metadataDiverged"),
    AUDIO_SAME("audioSame"),
    AUDIO_UNKNOWN("audioUnknown"),
    AUDIO_CONFLICT("audioConflict"),
    GENERATED_ARTIFACT("generatedArtifact"),
    CREATED_AT_IGNORED("createdAtIgnored"),
    PROCESSING_IGNORED("processingIgnored");

    companion object {
        val allCases: List<CanonicalShadowDiagnosticsCategory> = entries.toList()
    }
}

// ── CanonicalShadowDiagnosticsEvent ──

data class CanonicalShadowDiagnosticsEvent(
    val category: CanonicalShadowDiagnosticsCategory,
    val objectID: String,
    val hashPrefix: String?,
    val detail: String?
) {
    val id: String get() = objectID
}

// ── CanonicalShadowDiagnosticsReport ──

data class CanonicalShadowDiagnosticsReport(
    val mode: CanonicalShadowDiagnosticsMode,
    val events: List<CanonicalShadowDiagnosticsEvent>,
    val convergedCount: Int,
    val divergedCount: Int,
    val inputUnchanged: Boolean
) {
    val totalEventCount: Int get() = events.size

    val summary: String
        get() = listOf(
            "mode=${mode.rawValue}",
            "total=${totalEventCount}",
            "converged=$convergedCount",
            "diverged=$divergedCount",
            "inputUnchanged=$inputUnchanged"
        ).joinToString(",")

    companion object {
        fun fromEvents(
            mode: CanonicalShadowDiagnosticsMode,
            events: List<CanonicalShadowDiagnosticsEvent>,
            inputUnchanged: Boolean = false
        ): CanonicalShadowDiagnosticsReport {
            val converged = events.count { it.isConverged() }
            val diverged = events.count { it.isDiverged() }
            return CanonicalShadowDiagnosticsReport(
                mode = mode,
                events = events,
                convergedCount = converged,
                divergedCount = diverged,
                inputUnchanged = inputUnchanged
            )
        }
    }
}

// ── CanonicalShadowDiagnostics ──

object CanonicalShadowDiagnostics {

    private val convergedCategories = setOf(
        CanonicalShadowDiagnosticsCategory.METADATA_CONVERGED,
        CanonicalShadowDiagnosticsCategory.AUDIO_SAME,
        CanonicalShadowDiagnosticsCategory.CREATED_AT_IGNORED,
        CanonicalShadowDiagnosticsCategory.PROCESSING_IGNORED
    )

    private val divergedCategories = setOf(
        CanonicalShadowDiagnosticsCategory.METADATA_DIVERGED,
        CanonicalShadowDiagnosticsCategory.AUDIO_CONFLICT,
        CanonicalShadowDiagnosticsCategory.AUDIO_UNKNOWN,
        CanonicalShadowDiagnosticsCategory.GENERATED_ARTIFACT
    )

    fun compareLegacyVsCanonical(
        mode: CanonicalShadowDiagnosticsMode,
        legacyEntries: List<CanonicalShadowDiagnosticInputEntry>,
        canonicalEntries: List<CanonicalShadowDiagnosticInputEntry>
    ): CanonicalShadowDiagnosticsReport {
        if (mode == CanonicalShadowDiagnosticsMode.DISABLED) {
            return CanonicalShadowDiagnosticsReport(
                mode = mode,
                events = emptyList(),
                convergedCount = 0,
                divergedCount = 0,
                inputUnchanged = true
            )
        }

        val legacyByID = legacyEntries.associateBy { it.objectID }
        val canonicalByID = canonicalEntries.associateBy { it.objectID }
        val allIDs = (legacyByID.keys + canonicalByID.keys).toSortedSet()

        val events = mutableListOf<CanonicalShadowDiagnosticsEvent>()

        for (objectID in allIDs) {
            val legacy = legacyByID[objectID]
            val canonical = canonicalByID[objectID]

            when {
                legacy == null && canonical != null -> {
                    events.add(
                        CanonicalShadowDiagnosticsEvent(
                            category = CanonicalShadowDiagnosticsCategory.GENERATED_ARTIFACT,
                            objectID = objectID,
                            hashPrefix = canonical.hashPrefix,
                            detail = "canonical_only"
                        )
                    )
                }

                canonical == null && legacy != null -> {
                    events.add(
                        CanonicalShadowDiagnosticsEvent(
                            category = CanonicalShadowDiagnosticsCategory.METADATA_DIVERGED,
                            objectID = objectID,
                            hashPrefix = legacy.hashPrefix,
                            detail = "legacy_only"
                        )
                    )
                }

                legacy != null && canonical != null -> {
                    val category = compareEntries(legacy, canonical)
                    val hashPrefix = when (mode) {
                        CanonicalShadowDiagnosticsMode.REDACTED -> null
                        CanonicalShadowDiagnosticsMode.REPORT_WITH_PREFIX -> canonical.hashPrefix
                        else -> null
                    }

                    events.add(
                        CanonicalShadowDiagnosticsEvent(
                            category = category,
                            objectID = objectID,
                            hashPrefix = hashPrefix,
                            detail = buildDetail(legacy, canonical, category)
                        )
                    )
                }
            }
        }

        val inputUnchanged = legacyEntries.size == canonicalEntries.size &&
            legacyEntries.zip(canonicalEntries).all { (l, c) ->
                l.objectID == c.objectID && l.hashPrefix == c.hashPrefix
            }

        return CanonicalShadowDiagnosticsReport.fromEvents(
            mode = mode,
            events = events,
            inputUnchanged = inputUnchanged
        )
    }

    fun isEventConverged(category: CanonicalShadowDiagnosticsCategory): Boolean {
        return category in convergedCategories
    }

    fun isEventDiverged(category: CanonicalShadowDiagnosticsCategory): Boolean {
        return category in divergedCategories
    }

    private fun compareEntries(
        legacy: CanonicalShadowDiagnosticInputEntry,
        canonical: CanonicalShadowDiagnosticInputEntry
    ): CanonicalShadowDiagnosticsCategory {
        return when {
            legacy.metadataHash == canonical.metadataHash -> {
                if (legacy.audioHash == canonical.audioHash &&
                    legacy.audioHash != null
                ) {
                    CanonicalShadowDiagnosticsCategory.METADATA_CONVERGED
                } else if (legacy.audioHash != canonical.audioHash) {
                    CanonicalShadowDiagnosticsCategory.AUDIO_CONFLICT
                } else {
                    CanonicalShadowDiagnosticsCategory.AUDIO_UNKNOWN
                }
            }
            legacy.metadataHash != canonical.metadataHash -> {
                if (legacy.createdAt == canonical.createdAt &&
                    legacy.modifiedAt == canonical.modifiedAt
                ) {
                    CanonicalShadowDiagnosticsCategory.PROCESSING_IGNORED
                } else {
                    CanonicalShadowDiagnosticsCategory.METADATA_DIVERGED
                }
            }
            legacy.createdAt != canonical.createdAt -> {
                CanonicalShadowDiagnosticsCategory.CREATED_AT_IGNORED
            }
            else -> CanonicalShadowDiagnosticsCategory.METADATA_DIVERGED
        }
    }

    private fun buildDetail(
        legacy: CanonicalShadowDiagnosticInputEntry,
        canonical: CanonicalShadowDiagnosticInputEntry,
        category: CanonicalShadowDiagnosticsCategory
    ): String {
        return when (category) {
            CanonicalShadowDiagnosticsCategory.METADATA_CONVERGED ->
                "legacy=eq,canonical=eq"
            CanonicalShadowDiagnosticsCategory.METADATA_DIVERGED ->
                "legacy=${legacy.metadataHash?.take(8) ?: "null"}," +
                    "canonical=${canonical.metadataHash?.take(8) ?: "null"}"
            CanonicalShadowDiagnosticsCategory.AUDIO_SAME ->
                "audio=eq"
            CanonicalShadowDiagnosticsCategory.AUDIO_CONFLICT ->
                "legacy_audio=${legacy.audioHash?.take(8) ?: "null"}," +
                    "canonical_audio=${canonical.audioHash?.take(8) ?: "null"}"
            else -> category.rawValue
        }
    }
}

// ── CanonicalShadowDiagnosticInputEntry ──

data class CanonicalShadowDiagnosticInputEntry(
    val objectID: String,
    val metadataHash: String?,
    val audioHash: String?,
    val createdAt: Date?,
    val modifiedAt: Date?,
    val hashPrefix: String? = metadataHash?.take(8),
    val processingState: String? = null,
    val generatedArtifactKinds: List<String> = emptyList()
) {
    val id: String get() = objectID
}

// ── Extension helpers for diagnostics event classification ──

private fun CanonicalShadowDiagnosticsEvent.isConverged(): Boolean {
    return CanonicalShadowDiagnostics.isEventConverged(category)
}

private fun CanonicalShadowDiagnosticsEvent.isDiverged(): Boolean {
    return CanonicalShadowDiagnostics.isEventDiverged(category)
}
