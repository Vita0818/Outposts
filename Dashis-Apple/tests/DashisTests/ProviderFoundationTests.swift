import Foundation
import XCTest
@testable import Dashis

final class ProviderEndpointPolicyTests: XCTestCase {
  func testOpenRouterKeyAllowsExactGET() throws {
    var request = URLRequest(url: try XCTUnwrap(URL(string: "https://openrouter.ai/api/v1/key")))
    request.httpMethod = "GET"
    XCTAssertTrue(ProviderEndpointPolicy.allows(request))
  }

  func testOpenRouterRejectsWrongMethodAndUnexpectedQuery() throws {
    var wrongMethod = URLRequest(url: try XCTUnwrap(URL(string: "https://openrouter.ai/api/v1/key")))
    wrongMethod.httpMethod = "POST"
    XCTAssertFalse(ProviderEndpointPolicy.allows(wrongMethod))

    var query = URLRequest(url: try XCTUnwrap(URL(string: "https://openrouter.ai/api/v1/key?token=secret")))
    query.httpMethod = "GET"
    XCTAssertFalse(ProviderEndpointPolicy.allows(query))
  }

  func testPolicyRejectsLookalikeHostAndEmbeddedCredentials() throws {
    var lookalike = URLRequest(url: try XCTUnwrap(URL(string: "https://openrouter.ai.example.com/api/v1/key")))
    lookalike.httpMethod = "GET"
    XCTAssertFalse(ProviderEndpointPolicy.allows(lookalike))

    var credentials = URLRequest(url: try XCTUnwrap(URL(string: "https://user:password@openrouter.ai/api/v1/key")))
    credentials.httpMethod = "GET"
    XCTAssertFalse(ProviderEndpointPolicy.allows(credentials))
  }

  func testGoogleProjectPathsAndExactMetricFilter() throws {
    var quota = URLRequest(url: try XCTUnwrap(URL(string:
      "https://cloudquotas.googleapis.com/v1/projects/demo-project/locations/global/services/generativelanguage.googleapis.com/quotaInfos?pageSize=1000"
    )))
    quota.httpMethod = "GET"
    XCTAssertTrue(ProviderEndpointPolicy.allows(quota))

    var components = URLComponents(string: "https://monitoring.googleapis.com/v3/projects/demo-project/timeSeries")!
    components.queryItems = [
      URLQueryItem(name: "filter", value: "metric.type = \"generativelanguage.googleapis.com/quota/generate_content_requests_per_minute/usage\""),
      URLQueryItem(name: "interval.startTime", value: "2026-07-11T00:00:00Z"),
      URLQueryItem(name: "interval.endTime", value: "2026-07-11T01:00:00Z"),
      URLQueryItem(name: "view", value: "FULL")
    ]
    var monitoring = URLRequest(url: try XCTUnwrap(components.url))
    monitoring.httpMethod = "GET"
    XCTAssertTrue(ProviderEndpointPolicy.allows(monitoring))
  }

  func testPOSTRequiresExpectedContentTypeAndBody() throws {
    var request = URLRequest(url: try XCTUnwrap(URL(string: "https://openrouter.ai/api/v1/analytics/query")))
    request.httpMethod = "POST"
    request.httpBody = try JSONSerialization.data(withJSONObject: [
      "metrics": ["request_count"],
      "limit": 100,
      "time_range": [
        "start": "2026-07-10T00:00:00Z",
        "end": "2026-07-11T00:00:00Z"
      ]
    ])
    XCTAssertFalse(ProviderEndpointPolicy.allows(request))
    request.setValue("application/json", forHTTPHeaderField: "Content-Type")
    XCTAssertTrue(ProviderEndpointPolicy.allows(request))

    request.httpBody = try JSONSerialization.data(withJSONObject: [
      "metrics": ["request_count"],
      "limit": 100,
      "time_range": [
        "start": "2026-07-10T00:00:00Z",
        "end": "2026-07-11T00:00:00Z"
      ],
      "unexpected": "value"
    ])
    XCTAssertFalse(ProviderEndpointPolicy.allows(request))
  }
}

final class ProviderSnapshotTests: XCTestCase {
  func testFreshStaleExpiredAndFutureTimestamps() {
    let now = Date(timeIntervalSince1970: 1_800_000_000)
    XCTAssertEqual(FreshnessPolicy.freshness(of: snapshot(at: now.addingTimeInterval(-60)), now: now), .fresh)
    XCTAssertEqual(FreshnessPolicy.freshness(of: snapshot(at: now.addingTimeInterval(-16 * 60)), now: now), .stale)
    XCTAssertEqual(FreshnessPolicy.freshness(of: snapshot(at: now.addingTimeInterval(-25 * 60 * 60)), now: now), .expired)
    XCTAssertEqual(FreshnessPolicy.freshness(of: snapshot(at: now.addingTimeInterval(61)), now: now), .missing)
  }

  func testProjectionPreservesNegativeBalanceWhileClampingOnlyProgress() {
    let snapshot = ProviderSnapshot(
      providerID: .openRouter,
      scope: ProviderScope(kind: .apiKey, label: "OAuth key"),
      sourceKind: .officialDirect,
      observedAt: Date(),
      windows: [],
      balance: ProviderBalance(
        label: "Key limit",
        used: 12,
        limit: 10,
        remaining: -2,
        unit: "USD",
        resetDescription: nil
      ),
      metrics: [],
      warnings: [],
      partialFailures: []
    )
    let projected = ProviderCardProjection.apply(snapshot: snapshot, to: .openRouter)
    XCTAssertEqual(projected.primary, "$-2.00")
    XCTAssertEqual(projected.statusLabel, "exceeded")
    XCTAssertEqual(projected.progress, 100)
    XCTAssertEqual(snapshot.balance?.remaining, -2)
  }

  func testISODateParsesWithAndWithoutFractionalSeconds() {
    XCTAssertNotNil(ProviderJSON.date("2026-07-11T01:02:03Z"))
    XCTAssertNotNil(ProviderJSON.date("2026-07-11T01:02:03.456Z"))
  }

  private func snapshot(at date: Date) -> ProviderSnapshot {
    ProviderSnapshot(
      providerID: .claude,
      scope: .personal("Claude Code"),
      sourceKind: .officialLocalBridge,
      observedAt: date,
      windows: [],
      balance: nil,
      metrics: [ProviderMetric(key: "fixture", label: "Fixture", value: 1, unit: "value")],
      warnings: [],
      partialFailures: []
    )
  }
}

final class ProviderPKCETests: XCTestCase {
  func testGeneratedPKCEUsesURLSafeS256Values() throws {
    let pair = try ProviderPKCE.generate()
    XCTAssertGreaterThanOrEqual(pair.verifier.count, 43)
    XCTAssertEqual(pair.challenge.count, 43)
    XCTAssertNil(pair.verifier.range(of: #"[^A-Za-z0-9_-]"#, options: .regularExpression))
    XCTAssertNil(pair.challenge.range(of: #"[^A-Za-z0-9_-]"#, options: .regularExpression))
    XCTAssertFalse(pair.verifier.contains("="))
    XCTAssertFalse(pair.challenge.contains("="))
  }
}
