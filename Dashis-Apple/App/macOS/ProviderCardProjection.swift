import Foundation

enum ProviderCardProjection {
  static func apply(
    snapshot: ProviderSnapshot,
    to base: DashisProvider,
    now: Date = Date()
  ) -> DashisProvider {
    var provider = base
    let freshness = FreshnessPolicy.freshness(of: snapshot, now: now)
    let urgent = snapshot.mostUrgentWindow
    let historical = historicalWarning(in: snapshot) != nil

    provider.sourceLabel = snapshot.sourceKind.label
    provider.freshnessLabel = historical ? "Historical" : freshness.label
    provider.statusLabel = statusLabel(snapshot: snapshot, freshness: freshness)
    provider.tone = tone(snapshot: snapshot, freshness: freshness)
    provider.progress = displayProgress(window: urgent, balance: snapshot.balance)
    provider.primary = primaryValue(snapshot: snapshot, window: urgent, balance: snapshot.balance)
    provider.caption = caption(snapshot: snapshot, freshness: freshness)
    provider.stats = stats(snapshot: snapshot)
    provider.lines = lines(snapshot: snapshot)
    return provider
  }

  private static func statusLabel(
    snapshot: ProviderSnapshot,
    freshness: SnapshotFreshness
  ) -> String {
    if freshness == .expired { return "expired" }
    if snapshot.sourceKind == .manualOnly && !snapshot.hasData { return "manual" }
    if !snapshot.hasData && !snapshot.partialFailures.isEmpty { return "failed" }
    if !snapshot.hasData { return "no data" }
    if !snapshot.partialFailures.isEmpty { return "partial" }
    if historicalWarning(in: snapshot) != nil { return "historical" }
    if snapshot.windows.contains(where: \.isExceeded) || snapshot.balance?.isExceeded == true {
      return "exceeded"
    }
    return snapshot.hasData ? "connected" : "no data"
  }

  private static func tone(
    snapshot: ProviderSnapshot,
    freshness: SnapshotFreshness
  ) -> DashisProviderTone {
    if !snapshot.hasData && !snapshot.partialFailures.isEmpty {
      return .incident
    }
    if snapshot.windows.contains(where: \.isExceeded) || snapshot.balance?.isExceeded == true {
      return .incident
    }
    if freshness != .fresh || !snapshot.partialFailures.isEmpty || !snapshot.warnings.isEmpty {
      return .watch
    }
    return .connected
  }

  private static func displayProgress(window: QuotaWindow?, balance: ProviderBalance?) -> Double {
    let used = window?.usedPercentage ?? balance?.usedPercentage ?? 0
    return ProviderJSON.clampForDisplay(used)
  }

  private static func primaryValue(
    snapshot: ProviderSnapshot,
    window: QuotaWindow?,
    balance: ProviderBalance?
  ) -> String {
    if snapshot.sourceKind == .manualOnly && !snapshot.hasData {
      return "Manual check required"
    }
    if let remaining = balance?.remaining {
      return format(remaining, unit: balance?.unit ?? "")
    }
    if let remaining = window?.remainingPercentage {
      let value = "\(formatNumber(remaining))% left"
      return historicalWarning(in: snapshot) == nil ? value : "\(value) · historical"
    }
    if let remaining = window?.remaining {
      let value = format(remaining, unit: window?.unit ?? "")
      return historicalWarning(in: snapshot) == nil ? value : "\(value) · historical"
    }
    let preferredMetric = snapshot.metrics.first(where: {
      ["turns", "requests", "total_tokens", "credits"].contains($0.key)
    }) ?? snapshot.metrics.first
    if let preferredMetric {
      return format(preferredMetric.value, unit: preferredMetric.unit)
    }
    return "No quota data"
  }

  private static func caption(
    snapshot: ProviderSnapshot,
    freshness: SnapshotFreshness
  ) -> String {
    let observed = ProviderProjectionFormatters.dateTime.string(from: snapshot.observedAt)
    var value = "\(snapshot.sourceKind.label) · \(snapshot.scope.label) · observed \(observed)."
    if let historical = historicalWarning(in: snapshot) {
      value += " Latest complete historical window only: \(historical.message)"
    }
    if freshness == .stale { value += " This snapshot is stale." }
    if freshness == .expired { value += " This snapshot is expired and is not current quota." }
    if !snapshot.partialFailures.isEmpty {
      value += " \(snapshot.partialFailures.count) subcheck(s) failed."
    }
    return value
  }

  private static func historicalWarning(in snapshot: ProviderSnapshot) -> ProviderWarning? {
    snapshot.warnings.first { $0.id.hasPrefix("google-historical-window") }
  }

  private static func stats(snapshot: ProviderSnapshot) -> [DashisProviderStat] {
    var result = snapshot.windows.prefix(3).map { window in
      DashisProviderStat(
        title: window.label,
        value: window.remainingPercentage.map { "\(formatNumber($0))% left" }
          ?? window.remaining.map { format($0, unit: window.unit) }
          ?? "-"
      )
    }
    if result.count < 3, let balance = snapshot.balance {
      result.append(DashisProviderStat(
        title: balance.label,
        value: balance.remaining.map { format($0, unit: balance.unit) } ?? "-"
      ))
    }
    for metric in snapshot.metrics where result.count < 3 {
      result.append(DashisProviderStat(title: metric.label, value: format(metric.value, unit: metric.unit)))
    }
    while result.count < 3 {
      result.append(DashisProviderStat(title: result.isEmpty ? "Quota" : "Metric", value: "-"))
    }
    return Array(result.prefix(3))
  }

  private static func lines(snapshot: ProviderSnapshot) -> [DashisProviderLine] {
    var result = [
      DashisProviderLine(title: "Source", value: snapshot.sourceKind.label),
      DashisProviderLine(title: "Scope", value: snapshot.scope.label),
      DashisProviderLine(
        title: "Observed",
        value: ProviderProjectionFormatters.dateTime.string(from: snapshot.observedAt)
      )
    ]

    for window in snapshot.windows {
      let value = window.remainingPercentage.map { "\(formatNumber($0))% remaining" }
        ?? window.remaining.map { format($0, unit: window.unit) }
        ?? window.usedPercentage.map { "\(formatNumber($0))% used" }
        ?? "Unavailable"
      result.append(DashisProviderLine(title: window.label, value: value))
      if let used = window.used {
        result.append(DashisProviderLine(title: "\(window.label) used", value: format(used, unit: window.unit)))
      }
      if let limit = window.limit {
        result.append(DashisProviderLine(title: "\(window.label) limit", value: format(limit, unit: window.unit)))
      }
      if let reset = window.resetsAt, reset > Date(timeIntervalSince1970: 0) {
        result.append(DashisProviderLine(
          title: "\(window.label) reset",
          value: ProviderProjectionFormatters.dateTime.string(from: reset)
        ))
      }
    }

    if let balance = snapshot.balance {
      result.append(DashisProviderLine(
        title: balance.label,
        value: balance.remaining.map { format($0, unit: balance.unit) } ?? "Unavailable"
      ))
      if let used = balance.used {
        result.append(DashisProviderLine(title: "\(balance.label) used", value: format(used, unit: balance.unit)))
      }
      if let limit = balance.limit {
        result.append(DashisProviderLine(title: "\(balance.label) limit", value: format(limit, unit: balance.unit)))
      }
      if let reset = balance.resetDescription, !reset.isEmpty {
        result.append(DashisProviderLine(title: "\(balance.label) reset", value: reset))
      }
    }
    result += snapshot.metrics.map { metric in
      DashisProviderLine(title: metric.label, value: format(metric.value, unit: metric.unit))
    }
    result += snapshot.warnings.map { DashisProviderLine(title: "Warning", value: $0.message) }
    result += snapshot.partialFailures.map {
      DashisProviderLine(title: "Partial failure · \($0.operation)", value: $0.message)
    }
    return result
  }

  private static func format(_ value: Double, unit: String) -> String {
    if unit == "USD" || unit == "$" { return String(format: "$%.2f", value) }
    if unit == "%" { return "\(formatNumber(value))%" }
    return unit.isEmpty ? formatNumber(value) : "\(formatNumber(value)) \(unit)"
  }

  private static func formatNumber(_ value: Double) -> String {
    if value.rounded() == value {
      return NumberFormatter.localizedString(from: NSNumber(value: value), number: .decimal)
    }
    return String(format: "%.2f", value)
  }
}

private enum ProviderProjectionFormatters {
  static let dateTime: DateFormatter = {
    let formatter = DateFormatter()
    formatter.dateStyle = .short
    formatter.timeStyle = .short
    return formatter
  }()
}
