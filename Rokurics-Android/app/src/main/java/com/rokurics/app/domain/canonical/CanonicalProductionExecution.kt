package com.rokurics.app.domain.canonical

// ═══════════════════════════════════════════
// CanonicalProductionExecutionPolicy
// ═══════════════════════════════════════════

data class CanonicalProductionExecutionPolicy(
    val allowProductionRootWrites: Boolean = false,
    val requireOwnerApproval: Boolean = true,
    val requireRollbackPlan: Boolean = true,
    val requirePostcondition: Boolean = true,
    val maxSideEffects: Int = 0
) {
    init {
        require(maxSideEffects >= 0) { "maxSideEffects must be >= 0" }
    }

    companion object {
        val DEFAULT_DISABLED = CanonicalProductionExecutionPolicy()
        val PERMISSIVE = CanonicalProductionExecutionPolicy(
            allowProductionRootWrites = true,
            requireOwnerApproval = false,
            requireRollbackPlan = false,
            requirePostcondition = false,
            maxSideEffects = Int.MAX_VALUE
        )
    }
}

// ═══════════════════════════════════════════
// CanonicalProductionExecutionRejectionReason
// ═══════════════════════════════════════════

enum class CanonicalProductionExecutionRejectionReason(val rawValue: String) {
    PRODUCTION_ROOT_BLOCKED("productionRootBlocked"),
    OWNER_APPROVAL_MISSING("ownerApprovalMissing"),
    ROLLBACK_PLAN_MISSING("rollbackPlanMissing"),
    POSTCONDITION_MISSING("postconditionMissing"),
    SIDE_EFFECT_LIMIT_EXCEEDED("sideEffectLimitExceeded");

    companion object {
        val allCases: List<CanonicalProductionExecutionRejectionReason> = entries.toList()
    }
}

// ═══════════════════════════════════════════
// CanonicalProductionSideEffect
// ═══════════════════════════════════════════

enum class CanonicalProductionSideEffect(val rawValue: String) {
    NONE("none"),
    DIAGNOSTICS_WRITE("diagnosticsWrite"),
    SHADOW_ROOT_WRITE("shadowRootWrite"),
    STAGING_ROOT_WRITE("stagingRootWrite"),
    PRODUCTION_COMMIT("productionCommit"),
    READ_ONLY_NETWORK_PROBE("readOnlyNetworkProbe");

    companion object {
        val allCases: List<CanonicalProductionSideEffect> = entries.toList()
    }
}

// ═══════════════════════════════════════════
// CanonicalProductionDiagnosticsEvent
// ═══════════════════════════════════════════

data class CanonicalProductionDiagnosticsEvent
private constructor(
    val kind: String,
    val domain: String,
    val action: String,
    val objectID: String,
    val hashPrefix: String,
    val detail: String?
) {
    val id: String
        get() = listOfNotNull(kind, domain, objectID, hashPrefix).joinToString("|")

    companion object {
        operator fun invoke(
            kind: String,
            domain: String,
            action: String,
            objectID: String,
            hashPrefix: String,
            detail: String? = null
        ): CanonicalProductionDiagnosticsEvent {
            return CanonicalProductionDiagnosticsEvent(
                kind = CanonicalProductionRedaction.safeDiagnosticText(kind) ?: "unknown",
                domain = CanonicalProductionRedaction.safeDiagnosticText(domain) ?: "unknown",
                action = CanonicalProductionRedaction.safeDiagnosticText(action) ?: "unknown",
                objectID = CanonicalProductionRedaction.safeIdentifier(objectID, "unknown-object"),
                hashPrefix = CanonicalProductionRedaction.safeHashPrefix(hashPrefix) ?: "",
                detail = CanonicalProductionRedaction.safeDiagnosticText(detail ?: "")
            )
        }
    }
}

// ═══════════════════════════════════════════
// CanonicalProductionExecutionAudit
// ═══════════════════════════════════════════

data class CanonicalProductionExecutionAudit
private constructor(
    val domain: String,
    val action: String,
    val result: String,
    val sideEffects: List<CanonicalProductionSideEffect>
) {
    val id: String
        get() = listOf(domain, action, result).joinToString("|")

    val totalSideEffects: Int
        get() = sideEffects.size

    val hasProductionCommit: Boolean
        get() = sideEffects.contains(CanonicalProductionSideEffect.PRODUCTION_COMMIT)

    companion object {
        operator fun invoke(
            domain: String,
            action: String,
            result: String,
            sideEffects: List<CanonicalProductionSideEffect> = emptyList()
        ): CanonicalProductionExecutionAudit {
            return CanonicalProductionExecutionAudit(
                domain = CanonicalProductionRedaction.safeDiagnosticText(domain) ?: "unknown",
                action = CanonicalProductionRedaction.safeDiagnosticText(action) ?: "unknown",
                result = CanonicalProductionRedaction.safeDiagnosticText(result) ?: "unknown",
                sideEffects = sideEffects.distinct().sortedBy { it.rawValue }
            )
        }
    }
}

// ═══════════════════════════════════════════
// CanonicalProductionExecutionResult
// ═══════════════════════════════════════════

data class CanonicalProductionExecutionResult(
    val allowed: Boolean,
    val audit: List<CanonicalProductionExecutionAudit>,
    val diagnostics: List<CanonicalProductionDiagnosticsEvent>
) {
    val totalSideEffects: Int
        get() = audit.sumOf { it.totalSideEffects }

    val rejectionReasons: List<CanonicalProductionExecutionRejectionReason>
        get() = if (allowed) emptyList() else listOf(
            CanonicalProductionExecutionRejectionReason.PRODUCTION_ROOT_BLOCKED
        )
}

// ═══════════════════════════════════════════
// CanonicalProductionRedaction
// ═══════════════════════════════════════════

object CanonicalProductionRedaction {
    fun safeIdentifier(value: String, fallback: String): String {
        val trimmed = value.trim()
        if (trimmed.isEmpty()) return fallback
        if (trimmed.length > 128) return trimmed.take(128)
        return trimmed.replace(Regex("[^a-zA-Z0-9._\\-]"), "")
            .let { it.ifEmpty { fallback } }
    }

    fun safeDiagnosticText(value: String): String? {
        val trimmed = value.trim()
        if (trimmed.isEmpty()) return null
        return trimmed.take(256)
    }

    fun safeHashPrefix(value: String?, length: Int = 8): String? {
        if (value == null) return null
        val trimmed = value.trim().lowercase()
        if (trimmed.isEmpty()) return null
        val bounded = maxOf(4, minOf(length, 64))
        if (trimmed.all { it in '0'..'9' || it in 'a'..'f' }) {
            return trimmed.take(bounded)
        }
        return trimmed.take(bounded).replace(Regex("[^a-f0-9]"), "")
            .let { it.ifEmpty { null } }
    }
}
