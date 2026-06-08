/**
 * CanonicalTransportRuntime — canonical transport layer with nodes and envelope exchange.
 * Ported from Rokurics-Apple (CanonicalTransportRuntime.swift).
 */

import {
  CanonicalHash, CanonicalTimestamp, CanonicalNode, CanonicalManifest,
  CanonicalCapability, nilIfEmpty, timestampString, bufferToHex
} from './CanonicalCore'
import { cryptoFramework } from '@kit.CryptoArchitectureKit'

// =============================================================================
// TRANSPORT ROUTE
// =============================================================================

export enum CanonicalTransportRoute {
  MANIFEST_EXCHANGE = 'manifestExchange',
  APPLY_PLAN = 'applyPlan',
  APPLY_METADATA = 'applyMetadata',
  FILE_READ = 'fileRead',
  UPLOAD_START = 'uploadStart',
  UPLOAD_STATUS = 'uploadStatus',
  UPLOAD_CHUNK = 'uploadChunk',
  UPLOAD_FINALIZE = 'uploadFinalize'
}

// =============================================================================
// TRANSPORT ENVELOPE
// =============================================================================

export class CanonicalTransportEnvelope {
  requestID: string
  sourceNodeID: string
  destinationNodeID: string
  route: CanonicalTransportRoute
  body: Uint8Array
  bodyHash: CanonicalHash
  idempotencyKey: string | null
  sentAt: CanonicalTimestamp

  constructor(
    requestID: string = CanonicalTransportEnvelope.generateUUID(),
    sourceNodeID: string,
    destinationNodeID: string,
    route: CanonicalTransportRoute,
    body: Uint8Array = new Uint8Array(0),
    idempotencyKey: string | null = null,
    sentAt: Date = new Date()
  ) {
    this.requestID = (nilIfEmpty(requestID.trim()) ?? CanonicalTransportEnvelope.generateUUID())
    this.sourceNodeID = (nilIfEmpty(sourceNodeID.trim()) ?? 'source:unknown')
    this.destinationNodeID = (nilIfEmpty(destinationNodeID.trim()) ?? 'destination:unknown')
    this.route = route
    this.body = body
    this.bodyHash = CanonicalTransportEnvelope.hash(body)
    this.idempotencyKey = idempotencyKey ? nilIfEmpty(idempotencyKey.trim()) : null
    this.sentAt = new CanonicalTimestamp(sentAt)
  }

  get id(): string {
    return this.requestID
  }

  get hasValidBodyHash(): boolean {
    return CanonicalTransportEnvelope.hash(this.body).equals(this.bodyHash)
  }

  static hash(data: Uint8Array): CanonicalHash {
    return CanonicalHash.sha256(data)
  }

  private static generateUUID(): string {
    const buf = new Uint8Array(16)
    for (let i = 0; i < 16; i++) {
      buf[i] = Math.floor(Math.random() * 256)
    }
    buf[6] = (buf[6] & 0x0f) | 0x40
    buf[8] = (buf[8] & 0x3f) | 0x80
    let uuid = ''
    for (let i = 0; i < 16; i++) {
      uuid += buf[i].toString(16).padStart(2, '0')
      if (i === 3 || i === 5 || i === 7 || i === 9) {
        uuid += '-'
      }
    }
    return uuid
  }
}

// =============================================================================
// TRANSPORT RESPONSE
// =============================================================================

export class CanonicalTransportResponse {
  ok: boolean
  status: string
  body: Uint8Array
  bodyHash: CanonicalHash
  error: string | null

  constructor(ok: boolean, status: string, body: Uint8Array = new Uint8Array(0), error: string | null = null) {
    this.ok = ok
    this.status = (nilIfEmpty(status.trim()) ?? (ok ? 'ok' : 'failed'))
    this.body = body
    this.bodyHash = CanonicalTransportEnvelope.hash(body)
    this.error = error ? nilIfEmpty(error.trim()) : null
  }

  get hasValidBodyHash(): boolean {
    return CanonicalTransportEnvelope.hash(this.body).equals(this.bodyHash)
  }
}

// =============================================================================
// TRANSPORT RUNTIME ERROR
// =============================================================================

export enum CanonicalTransportRuntimeError {
  NODE_NOT_REGISTERED = 'nodeNotRegistered',
  ROUTE_NOT_ALLOWED = 'routeNotAllowed',
  CAPABILITY_MISSING = 'capabilityMissing',
  INVALID_BODY_HASH = 'invalidBodyHash',
  INVALID_MANIFEST_HASH = 'invalidManifestHash',
  INCOMPATIBLE_SCHEMA = 'incompatibleSchema',
  HANDLER_MISSING = 'handlerMissing'
}

// =============================================================================
// TRANSPORT PORT INTERFACE
// =============================================================================

export interface CanonicalTransportPort {
  register(node: CanonicalNode, allowedRoutes: Set<CanonicalTransportRoute>): void
  send(envelope: CanonicalTransportEnvelope): CanonicalTransportResponse
}

// =============================================================================
// IN-MEMORY TRANSPORT
// =============================================================================

export type TransportHandler = (envelope: CanonicalTransportEnvelope) => CanonicalTransportResponse

export class InMemoryCanonicalTransportRuntime implements CanonicalTransportPort {
  private nodes: Map<string, CanonicalNode> = new Map()
  private routesByNodeID: Map<string, Set<CanonicalTransportRoute>> = new Map()
  private handlers: Map<string, TransportHandler> = new Map()
  private idempotencyResponses: Map<string, CanonicalTransportResponse> = new Map()

  register(node: CanonicalNode, allowedRoutes: Set<CanonicalTransportRoute> = new Set(Object.values(CanonicalTransportRoute))) {
    this.nodes.set(node.nodeID, node)
    this.routesByNodeID.set(node.nodeID, allowedRoutes)
  }

  registerHandler(nodeID: string, route: CanonicalTransportRoute, handler: TransportHandler) {
    this.handlers.set(InMemoryCanonicalTransportRuntime.handlerKey(nodeID, route), handler)
  }

  send(envelope: CanonicalTransportEnvelope): CanonicalTransportResponse {
    if (!envelope.hasValidBodyHash) {
      throw CanonicalTransportRuntimeError.INVALID_BODY_HASH
    }
    if (!this.nodes.has(envelope.sourceNodeID)) {
      throw CanonicalTransportRuntimeError.NODE_NOT_REGISTERED
    }
    if (!this.nodes.has(envelope.destinationNodeID)) {
      throw CanonicalTransportRuntimeError.NODE_NOT_REGISTERED
    }
    if (!this.routesByNodeID.get(envelope.destinationNodeID)?.has(envelope.route)) {
      throw CanonicalTransportRuntimeError.ROUTE_NOT_ALLOWED
    }
    const source = this.nodes.get(envelope.sourceNodeID)!
    const destination = this.nodes.get(envelope.destinationNodeID)!
    InMemoryCanonicalTransportRuntime.validateCapabilities(source, destination, envelope.route)

    if (envelope.idempotencyKey) {
      const replayKey = InMemoryCanonicalTransportRuntime.idempotencyResponseKey(envelope, envelope.idempotencyKey)
      const existing = this.idempotencyResponses.get(replayKey)
      if (existing) {
        return existing
      }
      const response = this.dispatch(envelope)
      this.idempotencyResponses.set(replayKey, response)
      return response
    }
    return this.dispatch(envelope)
  }

  validateManifest(manifest: CanonicalManifest) {
    if (manifest.schemaVersion !== CanonicalManifest.CURRENT_SCHEMA_VERSION) {
      throw CanonicalTransportRuntimeError.INCOMPATIBLE_SCHEMA
    }
    if (!manifest.hasValidManifestHash) {
      throw CanonicalTransportRuntimeError.INVALID_MANIFEST_HASH
    }
  }

  private dispatch(envelope: CanonicalTransportEnvelope): CanonicalTransportResponse {
    const key = InMemoryCanonicalTransportRuntime.handlerKey(envelope.destinationNodeID, envelope.route)
    const handler = this.handlers.get(key)
    if (!handler) {
      throw CanonicalTransportRuntimeError.HANDLER_MISSING
    }
    const response = handler(envelope)
    if (!response.hasValidBodyHash) {
      throw CanonicalTransportRuntimeError.INVALID_BODY_HASH
    }
    return response
  }

  private static validateCapabilities(source: CanonicalNode, destination: CanonicalNode, route: CanonicalTransportRoute) {
    const required = InMemoryCanonicalTransportRuntime.requiredCapabilities(route)
    for (const capability of required) {
      if (source.capabilities.indexOf(capability) < 0) {
        throw CanonicalTransportRuntimeError.CAPABILITY_MISSING
      }
      if (destination.capabilities.indexOf(capability) < 0) {
        throw CanonicalTransportRuntimeError.CAPABILITY_MISSING
      }
    }
  }

  private static requiredCapabilities(route: CanonicalTransportRoute): CanonicalCapability[] {
    switch (route) {
      case CanonicalTransportRoute.MANIFEST_EXCHANGE:
        return [CanonicalCapability.RECORDING_METADATA]
      case CanonicalTransportRoute.APPLY_PLAN:
      case CanonicalTransportRoute.APPLY_METADATA:
        return [CanonicalCapability.RECORDING_METADATA]
      case CanonicalTransportRoute.FILE_READ:
        return [CanonicalCapability.OBJECT_PROJECTION]
      case CanonicalTransportRoute.UPLOAD_START:
      case CanonicalTransportRoute.UPLOAD_STATUS:
      case CanonicalTransportRoute.UPLOAD_CHUNK:
      case CanonicalTransportRoute.UPLOAD_FINALIZE:
        return [CanonicalCapability.AUDIO_ARTIFACT]
    }
  }

  private static handlerKey(nodeID: string, route: CanonicalTransportRoute): string {
    return nodeID + '|' + route
  }

  private static idempotencyResponseKey(envelope: CanonicalTransportEnvelope, idempotencyKey: string): string {
    return [envelope.sourceNodeID, envelope.destinationNodeID, envelope.route, idempotencyKey].join('|')
  }
}
