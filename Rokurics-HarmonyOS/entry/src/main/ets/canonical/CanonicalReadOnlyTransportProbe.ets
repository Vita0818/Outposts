/**
 * CanonicalReadOnlyTransportProbe — read-only transport probe for validation and audit.
 * Ported from Rokurics-Apple (CanonicalReadOnlyTransportProbe.swift).
 */

import {
  CanonicalHash, nilIfEmpty
} from './CanonicalCore'
import {
  CanonicalProductionRedaction,
  CanonicalProductionSignedRequest, CanonicalTransportRoute
} from './CanonicalProductionPorts'
import {
  CanonicalShadowMigrationRedaction
} from './CanonicalShadowMigration'

// =============================================================================
// PROBE ROUTE STATUS
// =============================================================================

export enum CanonicalReadOnlyTransportProbeRouteStatus {
  ALLOWED_READ_ONLY = 'allowedReadOnly',
  REJECTED_MUTATING = 'rejectedMutating',
  REJECTED_UNKNOWN = 'rejectedUnknown',
  SUPPRESSED_DISABLED = 'suppressedDisabled'
}

export enum CanonicalReadOnlyTransportProbeFailure {
  DISABLED = 'disabled',
  MUTATING_ROUTE_REJECTED = 'mutatingRouteRejected',
  UNKNOWN_ROUTE_REJECTED = 'unknownRouteRejected',
  ARTIFACT_FETCH_NOT_ALLOWED = 'artifactFetchNotAllowed',
  ARTIFACT_FETCH_TOO_LARGE = 'artifactFetchTooLarge',
  AUTH_BOUNDARY_MISSING = 'authBoundaryMissing',
  MANIFEST_HASH_USED_AS_AUTH = 'manifestHashUsedAsAuth',
  NETWORK_SUPPRESSED = 'networkSuppressed',
  SEND_FAILED = 'sendFailed'
}

// =============================================================================
// LIVE PROBE MODE
// =============================================================================

export enum CanonicalLiveReadOnlyTransportProbeMode {
  DISABLED = 'disabled',
  CLASSIFY_ONLY = 'classifyOnly',
  BUILD_SIGNED_ENVELOPE_ONLY = 'buildSignedEnvelopeOnly',
  SEND_READ_ONLY_PROBE = 'sendReadOnlyProbe',
  BLOCKED_MUTATING_ROUTE = 'blockedMutatingRoute'
}

export enum CanonicalLiveReadOnlyTransportProbeFailure {
  DISABLED = 'disabled',
  MUTATING_ROUTE_REJECTED = 'mutatingRouteRejected',
  UNKNOWN_ROUTE_REJECTED = 'unknownRouteRejected',
  ARTIFACT_FETCH_NOT_ALLOWED = 'artifactFetchNotAllowed',
  ARTIFACT_FETCH_TOO_LARGE = 'artifactFetchTooLarge',
  INTERNAL_CONFIGURATION_MISSING = 'internalConfigurationMissing',
  AUTH_BOUNDARY_MISSING = 'authBoundaryMissing',
  MANIFEST_HASH_USED_AS_AUTH = 'manifestHashUsedAsAuth',
  SIGNED_ENVELOPE_BUILD_FAILED = 'signedEnvelopeBuildFailed',
  NETWORK_SUPPRESSED = 'networkSuppressed',
  SEND_FAILED = 'sendFailed',
  RESPONSE_REJECTED = 'responseRejected'
}

export enum CanonicalLiveReadOnlyTransportProbeDiagnosticKind {
  CANONICAL_LIVE_READ_ONLY_PROBE_POLICY_EVALUATED = 'canonicalLiveReadOnlyProbePolicyEvaluated',
  CANONICAL_LIVE_READ_ONLY_PROBE_ROUTE_ALLOWED = 'canonicalLiveReadOnlyProbeRouteAllowed',
  CANONICAL_LIVE_READ_ONLY_PROBE_ROUTE_REJECTED = 'canonicalLiveReadOnlyProbeRouteRejected',
  CANONICAL_LIVE_READ_ONLY_PROBE_ENVELOPE_BUILT = 'canonicalLiveReadOnlyProbeEnvelopeBuilt',
  CANONICAL_LIVE_READ_ONLY_PROBE_SEND_SUPPRESSED = 'canonicalLiveReadOnlyProbeSendSuppressed',
  CANONICAL_LIVE_READ_ONLY_PROBE_SEND_STARTED = 'canonicalLiveReadOnlyProbeSendStarted',
  CANONICAL_LIVE_READ_ONLY_PROBE_SEND_COMPLETED = 'canonicalLiveReadOnlyProbeSendCompleted',
  CANONICAL_LIVE_READ_ONLY_PROBE_SEND_FAILED = 'canonicalLiveReadOnlyProbeSendFailed',
  CANONICAL_LIVE_READ_ONLY_PROBE_NO_MUTATION_VERIFIED = 'canonicalLiveReadOnlyProbeNoMutationVerified',
  CANONICAL_LIVE_READ_ONLY_PROBE_MUTATION_RISK_BLOCKED = 'canonicalLiveReadOnlyProbeMutationRiskBlocked'
}

// =============================================================================
// PROBE ROUTE
// =============================================================================

export class CanonicalReadOnlyTransportProbeRoute {
  method: string
  path: string

  constructor(method: string, path: string) {
    this.method = method.trim().toUpperCase()
    this.path = CanonicalShadowMigrationRedaction.safeText(path) ?? '/unknown'
  }

  static readonly HEALTH = new CanonicalReadOnlyTransportProbeRoute('GET', '/health')
  static readonly FINGERPRINT = new CanonicalReadOnlyTransportProbeRoute('GET', '/fingerprint')
  static readonly SYNC_STATUS = new CanonicalReadOnlyTransportProbeRoute('POST', '/sync/status')
  static readonly SYNC_INVENTORY = new CanonicalReadOnlyTransportProbeRoute('POST', '/sync/inventory')
  static readonly DEVICE_STATUS = new CanonicalReadOnlyTransportProbeRoute('POST', '/device/status')
  static readonly ARTIFACT_REQUEST = new CanonicalReadOnlyTransportProbeRoute('POST', '/sync/artifact-request')

  get isKnownReadOnlyRoute(): boolean {
    if (this.method === 'GET' && (this.path === '/health' || this.path === '/fingerprint')) {
      return true
    }
    if (this.method === 'POST' && (this.path === '/sync/inventory' || this.path === '/sync/artifact-request')) {
      return true
    }
    return false
  }

  get isKnownMutatingRoute(): boolean {
    const mutatingPaths = ['/pair', '/upload-secure-test', '/upload-recording-metadata',
      '/upload-recording-audio', '/upload-recording-audio-session/start',
      '/upload-recording-audio-session/status', '/upload-recording-audio-session/chunk',
      '/upload-recording-audio-session/finalize', '/device/status', '/sync/status',
      '/sync/apply', '/sync/apply-metadata', '/sync/manifest']
    if (this.method === 'POST' && mutatingPaths.indexOf(this.path) >= 0) {
      return true
    }
    return false
  }
}

// =============================================================================
// PROBE POLICY
// =============================================================================

export class CanonicalReadOnlyTransportProbePolicy {
  isEnabled: boolean
  allowBoundedArtifactFetch: boolean
  artifactFetchMaxBytes: number
  allowNetworkSend: boolean
  allowedRoutes: CanonicalReadOnlyTransportProbeRoute[]

  constructor(
    isEnabled: boolean = false,
    allowBoundedArtifactFetch: boolean = false,
    artifactFetchMaxBytes: number = 256 * 1024,
    allowNetworkSend: boolean = false,
    allowedRoutes: CanonicalReadOnlyTransportProbeRoute[] = [
      CanonicalReadOnlyTransportProbeRoute.HEALTH,
      CanonicalReadOnlyTransportProbeRoute.FINGERPRINT,
      CanonicalReadOnlyTransportProbeRoute.SYNC_INVENTORY
    ]
  ) {
    this.isEnabled = isEnabled
    this.allowBoundedArtifactFetch = allowBoundedArtifactFetch
    this.artifactFetchMaxBytes = Math.max(0, artifactFetchMaxBytes)
    this.allowNetworkSend = allowNetworkSend
    this.allowedRoutes = Array.from(new Set(allowedRoutes)).sort(
      (a, b) => (a.method + ' ' + a.path).localeCompare(b.method + ' ' + b.path)
    )
  }

  static readonly DISABLED = new CanonicalReadOnlyTransportProbePolicy()

  routeStatus(route: CanonicalReadOnlyTransportProbeRoute, bodyByteCount: number):
    [CanonicalReadOnlyTransportProbeRouteStatus, CanonicalReadOnlyTransportProbeFailure | null] {
    if (!this.isEnabled) {
      return [CanonicalReadOnlyTransportProbeRouteStatus.SUPPRESSED_DISABLED, CanonicalReadOnlyTransportProbeFailure.DISABLED]
    }
    if (!route.isKnownReadOnlyRoute) {
      return [
        route.isKnownMutatingRoute ? CanonicalReadOnlyTransportProbeRouteStatus.REJECTED_MUTATING : CanonicalReadOnlyTransportProbeRouteStatus.REJECTED_UNKNOWN,
        route.isKnownMutatingRoute ? CanonicalReadOnlyTransportProbeFailure.MUTATING_ROUTE_REJECTED : CanonicalReadOnlyTransportProbeFailure.UNKNOWN_ROUTE_REJECTED
      ]
    }
    if (route.method === 'POST' && route.path === '/sync/artifact-request') {
      if (!this.allowBoundedArtifactFetch) {
        return [CanonicalReadOnlyTransportProbeRouteStatus.REJECTED_MUTATING, CanonicalReadOnlyTransportProbeFailure.ARTIFACT_FETCH_NOT_ALLOWED]
      }
      if (bodyByteCount > this.artifactFetchMaxBytes) {
        return [CanonicalReadOnlyTransportProbeRouteStatus.REJECTED_MUTATING, CanonicalReadOnlyTransportProbeFailure.ARTIFACT_FETCH_TOO_LARGE]
      }
    }
    if (!this.allowedRoutes.some(r => r.method === route.method && r.path === route.path)) {
      return [CanonicalReadOnlyTransportProbeRouteStatus.REJECTED_UNKNOWN, CanonicalReadOnlyTransportProbeFailure.UNKNOWN_ROUTE_REJECTED]
    }
    return [CanonicalReadOnlyTransportProbeRouteStatus.ALLOWED_READ_ONLY, null]
  }
}

// =============================================================================
// LIVE PROBE POLICY
// =============================================================================

export class CanonicalLiveReadOnlyTransportProbePolicy {
  mode: CanonicalLiveReadOnlyTransportProbeMode
  route: CanonicalReadOnlyTransportProbeRoute
  allowBoundedArtifactFetch: boolean
  artifactFetchMaxBytes: number
  internalConfigurationEnabled: boolean
  requestTimeoutSeconds: number
  resourceTimeoutSeconds: number

  constructor(
    mode: CanonicalLiveReadOnlyTransportProbeMode = CanonicalLiveReadOnlyTransportProbeMode.DISABLED,
    route: CanonicalReadOnlyTransportProbeRoute = CanonicalReadOnlyTransportProbeRoute.SYNC_INVENTORY,
    allowBoundedArtifactFetch: boolean = false,
    artifactFetchMaxBytes: number = 256 * 1024,
    internalConfigurationEnabled: boolean = false,
    requestTimeoutSeconds: number = 5,
    resourceTimeoutSeconds: number = 8
  ) {
    this.mode = mode
    this.route = route
    this.allowBoundedArtifactFetch = allowBoundedArtifactFetch
    this.artifactFetchMaxBytes = Math.max(0, artifactFetchMaxBytes)
    this.internalConfigurationEnabled = internalConfigurationEnabled
    this.requestTimeoutSeconds = Math.max(1, requestTimeoutSeconds)
    this.resourceTimeoutSeconds = Math.max(this.requestTimeoutSeconds, resourceTimeoutSeconds)
  }

  static readonly DISABLED = new CanonicalLiveReadOnlyTransportProbePolicy()
}

// =============================================================================
// LIVE PROBE GATE
// =============================================================================

export class CanonicalLiveReadOnlyTransportProbeGate {
  mode: CanonicalLiveReadOnlyTransportProbeMode
  route: CanonicalReadOnlyTransportProbeRoute
  routeStatus: CanonicalReadOnlyTransportProbeRouteStatus
  shouldClassify: boolean
  shouldBuildEnvelope: boolean
  shouldSend: boolean
  blocked: boolean
  suppressed: boolean
  failure: CanonicalLiveReadOnlyTransportProbeFailure | null
  reason: string

  constructor(
    mode: CanonicalLiveReadOnlyTransportProbeMode,
    route: CanonicalReadOnlyTransportProbeRoute,
    routeStatus: CanonicalReadOnlyTransportProbeRouteStatus,
    shouldClassify: boolean,
    shouldBuildEnvelope: boolean,
    shouldSend: boolean,
    blocked: boolean,
    suppressed: boolean,
    failure: CanonicalLiveReadOnlyTransportProbeFailure | null,
    reason: string
  ) {
    this.mode = mode
    this.route = route
    this.routeStatus = routeStatus
    this.shouldClassify = shouldClassify
    this.shouldBuildEnvelope = shouldBuildEnvelope
    this.shouldSend = shouldSend
    this.blocked = blocked
    this.suppressed = suppressed
    this.failure = failure
    this.reason = CanonicalShadowMigrationRedaction.safeText(reason) ?? failure ?? mode
  }

  static evaluate(
    policy: CanonicalLiveReadOnlyTransportProbePolicy,
    bodyByteCount: number
  ): CanonicalLiveReadOnlyTransportProbeGate {
    if (policy.mode === CanonicalLiveReadOnlyTransportProbeMode.DISABLED) {
      return new CanonicalLiveReadOnlyTransportProbeGate(
        CanonicalLiveReadOnlyTransportProbeMode.DISABLED, policy.route,
        CanonicalReadOnlyTransportProbeRouteStatus.SUPPRESSED_DISABLED,
        false, false, false, false, true,
        CanonicalLiveReadOnlyTransportProbeFailure.DISABLED,
        'liveReadOnlyProbeDisabled'
      )
    }

    const routeDecision = CanonicalReadOnlyTransportProbePolicy.DISABLED.routeStatus(policy.route, bodyByteCount)
    if (routeDecision[0] === CanonicalReadOnlyTransportProbeRouteStatus.REJECTED_MUTATING ||
      policy.mode === CanonicalLiveReadOnlyTransportProbeMode.BLOCKED_MUTATING_ROUTE) {
      return new CanonicalLiveReadOnlyTransportProbeGate(
        CanonicalLiveReadOnlyTransportProbeMode.BLOCKED_MUTATING_ROUTE, policy.route,
        CanonicalReadOnlyTransportProbeRouteStatus.REJECTED_MUTATING,
        true, false, false, true, true,
        CanonicalLiveReadOnlyTransportProbeFailure.MUTATING_ROUTE_REJECTED,
        'mutatingRouteProbeBlocked'
      )
    }

    if (routeDecision[1]) {
      return new CanonicalLiveReadOnlyTransportProbeGate(
        policy.mode, policy.route, routeDecision[0],
        true, false, false, true, true,
        CanonicalLiveReadOnlyTransportProbeFailure.ARTIFACT_FETCH_NOT_ALLOWED,
        (routeDecision[1] as string)
      )
    }

    if (policy.mode === CanonicalLiveReadOnlyTransportProbeMode.SEND_READ_ONLY_PROBE &&
      !policy.internalConfigurationEnabled) {
      return new CanonicalLiveReadOnlyTransportProbeGate(
        policy.mode, policy.route, routeDecision[0],
        true, false, false, true, true,
        CanonicalLiveReadOnlyTransportProbeFailure.INTERNAL_CONFIGURATION_MISSING,
        'internalConfigurationMissing'
      )
    }

    switch (policy.mode) {
      case CanonicalLiveReadOnlyTransportProbeMode.CLASSIFY_ONLY:
        return new CanonicalLiveReadOnlyTransportProbeGate(
          policy.mode, policy.route, routeDecision[0],
          true, false, false, false, true,
          CanonicalLiveReadOnlyTransportProbeFailure.NETWORK_SUPPRESSED, 'classifyOnly'
        )
      case CanonicalLiveReadOnlyTransportProbeMode.BUILD_SIGNED_ENVELOPE_ONLY:
        return new CanonicalLiveReadOnlyTransportProbeGate(
          policy.mode, policy.route, routeDecision[0],
          true, true, false, false, true,
          CanonicalLiveReadOnlyTransportProbeFailure.NETWORK_SUPPRESSED, 'buildSignedEnvelopeOnly'
        )
      case CanonicalLiveReadOnlyTransportProbeMode.SEND_READ_ONLY_PROBE:
        return new CanonicalLiveReadOnlyTransportProbeGate(
          policy.mode, policy.route, routeDecision[0],
          true, true, true, false, false,
          null, 'sendReadOnlyProbe'
        )
      default:
        return new CanonicalLiveReadOnlyTransportProbeGate(
          CanonicalLiveReadOnlyTransportProbeMode.DISABLED, policy.route,
          CanonicalReadOnlyTransportProbeRouteStatus.SUPPRESSED_DISABLED,
          false, false, false, false, true,
          CanonicalLiveReadOnlyTransportProbeFailure.DISABLED, 'liveReadOnlyProbeDisabled'
        )
    }
  }
}

// =============================================================================
// LIVE PROBE RESULT
// =============================================================================

export class CanonicalLiveReadOnlyTransportProbeResult {
  mode: CanonicalLiveReadOnlyTransportProbeMode
  route: CanonicalReadOnlyTransportProbeRoute
  routeStatus: CanonicalReadOnlyTransportProbeRouteStatus
  envelopeBuilt: boolean
  sentNetwork: boolean
  completed: boolean
  blocked: boolean
  suppressed: boolean
  authBoundaryPreserved: boolean
  failure: CanonicalLiveReadOnlyTransportProbeFailure | null
  httpStatusCode: number | null
  responseByteCount: number | null
  diagnostics: CanonicalLiveReadOnlyTransportProbeDiagnosticKind[]
  reason: string

  constructor(
    mode: CanonicalLiveReadOnlyTransportProbeMode,
    route: CanonicalReadOnlyTransportProbeRoute,
    routeStatus: CanonicalReadOnlyTransportProbeRouteStatus,
    envelopeBuilt: boolean = false,
    sentNetwork: boolean = false,
    completed: boolean = false,
    blocked: boolean = false,
    suppressed: boolean = true,
    authBoundaryPreserved: boolean = false,
    failure: CanonicalLiveReadOnlyTransportProbeFailure | null = null,
    httpStatusCode: number | null = null,
    responseByteCount: number | null = null,
    diagnostics: CanonicalLiveReadOnlyTransportProbeDiagnosticKind[] = [],
    reason: string
  ) {
    this.mode = mode
    this.route = route
    this.routeStatus = routeStatus
    this.envelopeBuilt = envelopeBuilt
    this.sentNetwork = sentNetwork
    this.completed = completed
    this.blocked = blocked
    this.suppressed = suppressed
    this.authBoundaryPreserved = authBoundaryPreserved
    this.failure = failure
    this.httpStatusCode = httpStatusCode
    this.responseByteCount = responseByteCount
    this.diagnostics = Array.from(new Set(diagnostics)).sort((a, b) => a.localeCompare(b))
    this.reason = CanonicalShadowMigrationRedaction.safeText(reason) ?? failure ?? mode
  }

  get diagnosticsSummary(): string {
    return [
      'mode=' + this.mode, 'route=' + this.route.method + ' ' + this.route.path,
      'routeStatus=' + this.routeStatus, 'envelopeBuilt=' + this.envelopeBuilt,
      'sent=' + this.sentNetwork, 'completed=' + this.completed,
      'blocked=' + this.blocked, 'suppressed=' + this.suppressed,
      'authBoundaryPreserved=' + this.authBoundaryPreserved,
      'httpStatus=' + (this.httpStatusCode ? String(this.httpStatusCode) : 'none'),
      'responseBytes=' + (this.responseByteCount ? String(this.responseByteCount) : 'none'),
      'failure=' + (this.failure ?? 'none'), 'reason=' + this.reason
    ].join(',')
  }
}

// =============================================================================
// PROBE REQUEST
// =============================================================================

export class CanonicalReadOnlyTransportProbeRequest {
  route: CanonicalReadOnlyTransportProbeRoute
  bodyByteCount: number
  bodyHashPrefix: string | null
  timestampPresent: boolean
  noncePresent: boolean
  signaturePresent: boolean
  tlsPinningPreserved: boolean
  hmacPreserved: boolean
  bodyHashPreserved: boolean
  manifestHashPresent: boolean
  manifestHashUsedAsAuth: boolean

  constructor(
    route: CanonicalReadOnlyTransportProbeRoute,
    bodyByteCount: number = 0,
    bodyHash: CanonicalHash | null = null,
    timestampPresent: boolean = true,
    noncePresent: boolean = true,
    signaturePresent: boolean = true,
    tlsPinningPreserved: boolean = true,
    hmacPreserved: boolean = true,
    bodyHashPreserved: boolean = true,
    manifestHashPresent: boolean = false,
    manifestHashUsedAsAuth: boolean = false
  ) {
    this.route = route
    this.bodyByteCount = Math.max(0, bodyByteCount)
    this.bodyHashPrefix = bodyHash ? CanonicalProductionRedaction.hashPrefix(bodyHash.value) : null
    this.timestampPresent = timestampPresent
    this.noncePresent = noncePresent
    this.signaturePresent = signaturePresent
    this.tlsPinningPreserved = tlsPinningPreserved
    this.hmacPreserved = hmacPreserved
    this.bodyHashPreserved = bodyHashPreserved
    this.manifestHashPresent = manifestHashPresent
    this.manifestHashUsedAsAuth = manifestHashUsedAsAuth
  }

  get authBoundaryPreserved(): boolean {
    return this.tlsPinningPreserved && this.hmacPreserved && this.bodyHashPreserved &&
      this.timestampPresent && this.noncePresent && this.signaturePresent && !this.manifestHashUsedAsAuth
  }
}

// =============================================================================
// PROBE AUDIT
// =============================================================================

export class CanonicalReadOnlyTransportProbeAudit {
  route: CanonicalReadOnlyTransportProbeRoute
  routeStatus: CanonicalReadOnlyTransportProbeRouteStatus
  requestSent: boolean
  requestSuppressed: boolean
  authBoundaryPreserved: boolean
  manifestHashUsedAsAuth: boolean
  reason: string

  constructor(
    route: CanonicalReadOnlyTransportProbeRoute,
    routeStatus: CanonicalReadOnlyTransportProbeRouteStatus,
    requestSent: boolean,
    requestSuppressed: boolean,
    authBoundaryPreserved: boolean,
    manifestHashUsedAsAuth: boolean,
    reason: string
  ) {
    this.route = route
    this.routeStatus = routeStatus
    this.requestSent = requestSent
    this.requestSuppressed = requestSuppressed
    this.authBoundaryPreserved = authBoundaryPreserved
    this.manifestHashUsedAsAuth = manifestHashUsedAsAuth
    this.reason = CanonicalShadowMigrationRedaction.safeText(reason) ?? routeStatus
  }
}

// =============================================================================
// PROBE RESULT
// =============================================================================

export class CanonicalReadOnlyTransportProbeResult {
  status: string
  routeStatus: CanonicalReadOnlyTransportProbeRouteStatus
  sentNetwork: boolean
  blocked: boolean
  suppressed: boolean
  authBoundaryPreserved: boolean
  failure: CanonicalReadOnlyTransportProbeFailure | null
  audit: CanonicalReadOnlyTransportProbeAudit

  constructor(
    status: string,
    routeStatus: CanonicalReadOnlyTransportProbeRouteStatus,
    sentNetwork: boolean,
    blocked: boolean,
    suppressed: boolean,
    authBoundaryPreserved: boolean,
    failure: CanonicalReadOnlyTransportProbeFailure | null,
    audit: CanonicalReadOnlyTransportProbeAudit
  ) {
    this.status = CanonicalShadowMigrationRedaction.safeText(status) ?? routeStatus
    this.routeStatus = routeStatus
    this.sentNetwork = sentNetwork
    this.blocked = blocked
    this.suppressed = suppressed
    this.authBoundaryPreserved = authBoundaryPreserved
    this.failure = failure
    this.audit = audit
  }

  get diagnosticsSummary(): string {
    return [
      'probe=' + this.status,
      'route=' + this.audit.route.method + ' ' + this.audit.route.path,
      'routeStatus=' + this.routeStatus, 'sent=' + this.sentNetwork,
      'suppressed=' + this.suppressed, 'blocked=' + this.blocked,
      'authBoundaryPreserved=' + this.authBoundaryPreserved,
      'manifestHashAuth=' + this.audit.manifestHashUsedAsAuth,
      'failure=' + (this.failure ?? 'none')
    ].join(',')
  }
}

// =============================================================================
// READ-ONLY TRANSPORT PROBE
// =============================================================================

export class CanonicalReadOnlyTransportProbe {
  evaluate(
    request: CanonicalReadOnlyTransportProbeRequest,
    policy: CanonicalReadOnlyTransportProbePolicy = CanonicalReadOnlyTransportProbePolicy.DISABLED
  ): CanonicalReadOnlyTransportProbeResult {
    const routeDecision = policy.routeStatus(request.route, request.bodyByteCount)

    if (request.manifestHashUsedAsAuth) {
      return this.result(
        request, routeDecision[0], false, true, true,
        CanonicalReadOnlyTransportProbeFailure.MANIFEST_HASH_USED_AS_AUTH, 'manifestHashNotAuth'
      )
    }
    if (!request.authBoundaryPreserved) {
      return this.result(
        request, routeDecision[0], false, true, true,
        CanonicalReadOnlyTransportProbeFailure.AUTH_BOUNDARY_MISSING, 'authBoundaryMissing'
      )
    }
    if (routeDecision[1]) {
      return this.result(
        request, routeDecision[0], false,
        routeDecision[0] !== CanonicalReadOnlyTransportProbeRouteStatus.SUPPRESSED_DISABLED,
        true, routeDecision[1], routeDecision[1]
      )
    }
    const shouldSend = policy.allowNetworkSend
    return this.result(
      request, routeDecision[0], shouldSend, false, !shouldSend,
      shouldSend ? null : CanonicalReadOnlyTransportProbeFailure.NETWORK_SUPPRESSED,
      shouldSend ? 'readOnlyProbeSent' : 'readOnlyProbeSuppressed'
    )
  }

  request(
    signedRequest: CanonicalProductionSignedRequest,
    route: CanonicalReadOnlyTransportProbeRoute,
    tlsPinningPreserved: boolean = true,
    hmacPreserved: boolean = true,
    manifestHashPresent: boolean = false,
    manifestHashUsedAsAuth: boolean = false
  ): CanonicalReadOnlyTransportProbeRequest {
    return new CanonicalReadOnlyTransportProbeRequest(
      route, signedRequest.buildRequest.body.length, signedRequest.bodyHash,
      true, signedRequest.buildRequest.nonce.length > 0,
      signedRequest.signaturePrefix !== null || signedRequest.signerDescription !== null,
      tlsPinningPreserved, hmacPreserved,
      true, manifestHashPresent, manifestHashUsedAsAuth
    )
  }

  private result(
    request: CanonicalReadOnlyTransportProbeRequest,
    routeStatus: CanonicalReadOnlyTransportProbeRouteStatus,
    sentNetwork: boolean,
    blocked: boolean,
    suppressed: boolean,
    failure: CanonicalReadOnlyTransportProbeFailure | null,
    reason: string
  ): CanonicalReadOnlyTransportProbeResult {
    let status: string
    if (blocked) {
      status = 'blocked'
    } else if (sentNetwork) {
      status = 'completed'
    } else if (suppressed) {
      status = routeStatus === CanonicalReadOnlyTransportProbeRouteStatus.SUPPRESSED_DISABLED ? 'disabled' : 'suppressed'
    } else {
      status = 'completed'
    }
    const audit = new CanonicalReadOnlyTransportProbeAudit(
      request.route, routeStatus, sentNetwork, suppressed,
      request.authBoundaryPreserved, request.manifestHashUsedAsAuth, reason
    )
    return new CanonicalReadOnlyTransportProbeResult(
      status, routeStatus, sentNetwork, blocked, suppressed,
      request.authBoundaryPreserved, failure, audit
    )
  }
}
