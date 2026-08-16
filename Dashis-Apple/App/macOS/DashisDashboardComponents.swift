import SwiftUI

struct DashisProviderCard: View {
  @Environment(\.colorScheme) private var colorScheme
  let provider: DashisProvider
  let isLoading: Bool
  let onPrimaryAction: () -> Void

  var body: some View {
    VStack(alignment: .leading, spacing: 0) {
      HStack(alignment: .top, spacing: 14) {
        Image(systemName: provider.symbolName)
          .font(.system(size: 18, weight: .semibold))
          .foregroundStyle(DashisTheme.accent)
          .frame(width: 40, height: 40)
          .background(DashisTheme.mutedSurface(colorScheme), in: RoundedRectangle(cornerRadius: 8, style: .continuous))
          .overlay {
            RoundedRectangle(cornerRadius: 8, style: .continuous)
              .stroke(DashisTheme.stroke(colorScheme), lineWidth: 1)
          }

        VStack(alignment: .leading, spacing: 3) {
          Text(provider.name)
            .font(DashisType.body(20, .semibold))
          Text(provider.kind)
            .font(DashisType.caption(13))
            .foregroundStyle(DashisTheme.secondaryText(colorScheme))
        }

        Spacer(minLength: 12)
        VStack(alignment: .trailing, spacing: 6) {
          Text(provider.statusLabel)
            .font(DashisType.caption(12, .semibold))
            .foregroundStyle(DashisTheme.statusColor(provider.tone))
            .padding(.horizontal, 9)
            .padding(.vertical, 5)
            .background(DashisTheme.statusColor(provider.tone).opacity(0.13), in: Capsule())

          HStack(spacing: 5) {
            providerBadge(provider.sourceLabel)
            providerBadge(provider.freshnessLabel)
          }
        }
      }
      .padding(.bottom, 34)

      Text(provider.primary)
        .font(DashisType.title(48))
        .lineLimit(1)
        .minimumScaleFactor(0.62)

      Text(provider.caption)
        .font(DashisType.caption(13))
        .foregroundStyle(DashisTheme.secondaryText(colorScheme))
        .lineLimit(3)
        .padding(.top, 8)
        .frame(minHeight: 58, alignment: .topLeading)

      HStack(spacing: 10) {
        ForEach(Array(provider.stats.enumerated()), id: \.offset) { _, stat in
          VStack(alignment: .leading, spacing: 9) {
            Text(stat.title)
              .font(DashisType.caption(12))
              .foregroundStyle(DashisTheme.secondaryText(colorScheme))
            Text(stat.value)
              .font(DashisType.body(24, .semibold))
              .lineLimit(1)
              .minimumScaleFactor(0.72)
          }
          .frame(maxWidth: .infinity, minHeight: 78, alignment: .topLeading)
          .padding(12)
          .background(DashisTheme.mutedSurface(colorScheme), in: RoundedRectangle(cornerRadius: 8, style: .continuous))
          .overlay {
            RoundedRectangle(cornerRadius: 8, style: .continuous)
              .stroke(DashisTheme.stroke(colorScheme), lineWidth: 1)
          }
        }
      }
      .padding(.top, 18)

      ProgressView(value: provider.progress, total: 100)
        .padding(.top, 18)

      VStack(spacing: 8) {
        ForEach(Array(provider.lines.prefix(6))) { line in
          HStack {
            Text(line.title)
              .foregroundStyle(DashisTheme.secondaryText(colorScheme))
            Spacer(minLength: 12)
            Text(line.value)
              .foregroundStyle(DashisTheme.primaryText(colorScheme))
              .multilineTextAlignment(.trailing)
          }
          .font(DashisType.caption(13, .medium))
          .padding(11)
          .background(DashisTheme.mutedSurface(colorScheme), in: RoundedRectangle(cornerRadius: 8, style: .continuous))
          .overlay {
            RoundedRectangle(cornerRadius: 8, style: .continuous)
              .stroke(DashisTheme.stroke(colorScheme), lineWidth: 1)
          }
        }

        if provider.lines.count > 6 {
          Text("\(provider.lines.count - 6) more row(s) in provider detail")
            .font(DashisType.caption(12, .medium))
            .foregroundStyle(DashisTheme.secondaryText(colorScheme))
            .frame(maxWidth: .infinity, alignment: .center)
            .padding(.top, 4)
        }
      }
      .padding(.top, 18)

      if let actionTitle = provider.actionTitle {
        Button {
          onPrimaryAction()
        } label: {
          HStack(spacing: 8) {
            if isLoading {
              ProgressView()
                .controlSize(.small)
            }
            Text(isLoading ? "Checking" : actionTitle)
          }
          .frame(maxWidth: .infinity)
        }
        .buttonStyle(.borderedProminent)
        .disabled(isLoading)
        .padding(.top, 18)
      }

      Spacer(minLength: 20)
    }
    .padding(24)
    .frame(minHeight: 500, alignment: .topLeading)
    .dashisGlassCard(cornerRadius: 8)
  }

  private func providerBadge(_ value: String) -> some View {
    Text(value)
      .font(DashisType.caption(10, .medium))
      .foregroundStyle(DashisTheme.secondaryText(colorScheme))
      .padding(.horizontal, 7)
      .padding(.vertical, 3)
      .background(DashisTheme.mutedSurface(colorScheme), in: Capsule())
      .overlay {
        Capsule().stroke(DashisTheme.stroke(colorScheme), lineWidth: 1)
      }
  }
}

struct DashisProviderDetail: View {
  @Environment(\.colorScheme) private var colorScheme
  let provider: DashisProvider
  @ObservedObject var store: DashisProviderStore

  var body: some View {
    VStack(alignment: .leading, spacing: 16) {
      Text(provider.detailTitle)
        .font(DashisType.body(20, .semibold))

      ForEach(provider.lines) { line in
        HStack {
          Text(line.title)
            .foregroundStyle(DashisTheme.secondaryText(colorScheme))
          Spacer(minLength: 12)
          Text(line.value)
            .multilineTextAlignment(.trailing)
        }
        .font(DashisType.caption(13, .medium))
        .padding(11)
        .background(DashisTheme.mutedSurface(colorScheme), in: RoundedRectangle(cornerRadius: 8, style: .continuous))
        .overlay {
          RoundedRectangle(cornerRadius: 8, style: .continuous)
            .stroke(DashisTheme.stroke(colorScheme), lineWidth: 1)
        }
      }

      if provider.id == "codex" {
        CodexNativeControls(store: store)
      } else if provider.id == "claude" {
        ClaudeNativeControls(store: store)
      } else if provider.id == "google" {
        GoogleNativeControls(store: store)
      } else if provider.id == "openrouter" {
        OpenRouterNativeControls(store: store)
      }

      Text(provider.detailNote)
        .font(DashisType.caption(13))
        .foregroundStyle(DashisTheme.secondaryText(colorScheme))
        .fixedSize(horizontal: false, vertical: true)
    }
    .padding(20)
    .frame(minHeight: 500, alignment: .topLeading)
    .dashisGlassCard(cornerRadius: 8)
  }
}

struct CodexNativeControls: View {
  @ObservedObject var store: DashisProviderStore

  var body: some View {
    VStack(alignment: .leading, spacing: 12) {
      Text("Personal usage uses an experimental, non-public Codex Desktop endpoint and runs only after this button is clicked.")
        .font(DashisType.caption(12))
        .foregroundStyle(.secondary)

      HStack(spacing: 10) {
        Button {
          Task { await store.checkCodexDesktop() }
        } label: {
          Label("Check desktop usage", systemImage: "person.crop.circle.badge.checkmark")
        }
        .buttonStyle(.borderedProminent)
        .disabled(store.isLoading("codex"))

        Button {
          store.clearCodexSession()
        } label: {
          Label("Clear", systemImage: "xmark.circle")
        }
        .buttonStyle(.bordered)
      }

      Divider()

      TextField("workspace id", text: $store.codexWorkspaceID)
        .textFieldStyle(.roundedBorder)
        .disabled(store.isLoading("codex"))
      SecureField("analytics API key", text: $store.codexAnalyticsAPIKey)
        .textFieldStyle(.roundedBorder)
        .disabled(store.isLoading("codex"))
      Stepper("Analytics window: \(store.codexAnalyticsDays) days", value: $store.codexAnalyticsDays, in: 1...90)
        .font(DashisType.caption(13))
        .disabled(store.isLoading("codex"))
      Button {
        Task { await store.checkCodexAnalytics() }
      } label: {
        Label("Check workspace analytics", systemImage: "chart.bar.xaxis")
      }
      .buttonStyle(.bordered)
      .disabled(store.isLoading("codex"))
    }
  }
}

struct ClaudeNativeControls: View {
  @ObservedObject var store: DashisProviderStore

  var body: some View {
    VStack(alignment: .leading, spacing: 12) {
      Text(store.claudeConnectionMessage)
        .font(DashisType.caption(12))
        .foregroundStyle(.secondary)

      Text("Preview reads only ~/.claude/settings.json and makes no persistent change. Apply installs the bundled helper and writes the reviewed patch. Dashis never reads Claude auth, cookies, or transcripts, and never sends a request to refresh quota.")
        .font(DashisType.caption(12))
        .foregroundStyle(.secondary)

      HStack(spacing: 10) {
        Button {
          store.prepareClaudeConnect()
        } label: {
          Label("Preview connect", systemImage: "link.badge.plus")
        }
        .buttonStyle(.borderedProminent)

        Button {
          store.prepareClaudeDisconnect()
        } label: {
          Label("Preview disconnect", systemImage: "link.badge.minus")
        }
        .buttonStyle(.bordered)
      }

      if let summary = store.claudePatchSummary {
        VStack(alignment: .leading, spacing: 10) {
          Text("Pending settings change")
            .font(DashisType.caption(12, .semibold))
          Text(summary)
            .font(DashisType.caption(12))
          HStack(spacing: 10) {
            Button("Apply change") {
              store.applyClaudePendingPatch()
            }
            .buttonStyle(.borderedProminent)
            Button("Cancel") {
              store.cancelClaudePendingPatch()
            }
            .buttonStyle(.bordered)
          }
        }
        .padding(12)
        .background(.secondary.opacity(0.08), in: RoundedRectangle(cornerRadius: 8))
      }

      Divider()

      HStack(spacing: 10) {
        Button {
          Task { await store.reloadClaudeSnapshot() }
        } label: {
          Label("Reload snapshot", systemImage: "arrow.clockwise")
        }
        .buttonStyle(.borderedProminent)
        .disabled(store.isLoading("claude"))

        Button {
          store.clearClaudeLoadedSnapshot()
        } label: {
          Label("Clear loaded data", systemImage: "xmark.circle")
        }
        .buttonStyle(.bordered)
      }
    }
  }
}

struct GoogleNativeControls: View {
  @ObservedObject var store: DashisProviderStore

  var body: some View {
    VStack(alignment: .leading, spacing: 12) {
      Picker("Mode", selection: $store.googleMode) {
        ForEach(DashisGoogleMode.allCases) { mode in
          Text(mode.rawValue).tag(mode)
        }
      }
      .pickerStyle(.segmented)
      .disabled(store.isLoading("google"))

      if store.googleMode == .consumer {
        Text("Google publishes no supported third-party API for Gemini consumer subscription balance. Dashis can open the official UI or display a reading you enter manually.")
          .font(DashisType.caption(12))
          .foregroundStyle(.secondary)

        Text("If you use Antigravity, run /credits in its official interface and copy only the numbers you want Dashis to display.")
          .font(DashisType.caption(12))
          .foregroundStyle(.secondary)

        Button {
          store.openGoogleConsumerQuotaPage()
        } label: {
          Label("Open Gemini official page", systemImage: "arrow.up.right.square")
        }
        .buttonStyle(.borderedProminent)

        TextField("manual used (optional)", text: $store.googleManualUsed)
          .textFieldStyle(.roundedBorder)
        TextField("manual limit (optional)", text: $store.googleManualLimit)
          .textFieldStyle(.roundedBorder)
        TextField("manual remaining (optional)", text: $store.googleManualRemaining)
          .textFieldStyle(.roundedBorder)
        TextField("unit", text: $store.googleManualUnit)
          .textFieldStyle(.roundedBorder)

        HStack(spacing: 10) {
          Button {
            Task { await store.recordGoogleManualReading() }
          } label: {
            Label("Record manual reading", systemImage: "square.and.pencil")
          }
          .buttonStyle(.bordered)

          Button {
            store.clearGoogleSession()
          } label: {
            Label("Clear", systemImage: "xmark.circle")
          }
          .buttonStyle(.bordered)
        }
      } else {
        Text("Project mode requests the cloud-platform OAuth scope, keeps the access token only in memory, and derives quota from Cloud Quotas plus Cloud Monitoring. The authorized principal needs cloudquotas.quotas.get and monitoring.timeSeries.list.")
          .font(DashisType.caption(12))
          .foregroundStyle(.secondary)

        TextField("Google Desktop OAuth client ID", text: $store.googleOAuthClientID)
          .textFieldStyle(.roundedBorder)
          .disabled(store.isLoading("google"))
        TextField("Google Cloud project ID or number", text: $store.googleProjectID)
          .textFieldStyle(.roundedBorder)
          .disabled(store.isLoading("google"))
        TextField("optional quota IDs, comma-separated", text: $store.googleQuotaIDs)
          .textFieldStyle(.roundedBorder)
          .disabled(store.isLoading("google"))
        Text("Leave quota IDs blank for a bounded automatic selection; paste exact Cloud Quotas quotaId values to narrow the Monitoring work further.")
          .font(DashisType.caption(12))
          .foregroundStyle(.secondary)
        Text(store.googleConnectionMessage)
          .font(DashisType.caption(12))
          .foregroundStyle(.secondary)

        HStack(spacing: 10) {
          Button {
            Task { await store.connectGoogleProject() }
          } label: {
            Label("Connect Google", systemImage: "person.crop.circle.badge.checkmark")
          }
          .buttonStyle(.borderedProminent)
          .disabled(store.isLoading("google"))

          Button {
            Task { await store.checkGoogleProject() }
          } label: {
            Label("Check quotas", systemImage: "chart.bar")
          }
          .buttonStyle(.bordered)
          .disabled(store.isLoading("google") || !store.isGoogleProjectConnected)

          Button {
            store.clearGoogleSession()
          } label: {
            Label("Clear", systemImage: "xmark.circle")
          }
          .buttonStyle(.bordered)
        }
      }
    }
  }
}

struct OpenRouterNativeControls: View {
  @ObservedObject var store: DashisProviderStore

  var body: some View {
    VStack(alignment: .leading, spacing: 12) {
      Picker("Mode", selection: $store.openRouterMode) {
        ForEach(DashisOpenRouterMode.allCases) { mode in
          Text(mode.rawValue).tag(mode)
        }
      }
      .pickerStyle(.segmented)
      .disabled(store.isLoading("openrouter"))

      if store.openRouterMode == .oauthKey {
        Text("OAuth PKCE creates a user-controlled OpenRouter key. The key and verifier stay only in this app session.")
          .font(DashisType.caption(12))
          .foregroundStyle(.secondary)
        Text("Clear cancels local work and forgets the key. If authorization may already have created a server-side key, revoke it in OpenRouter as well.")
          .font(DashisType.caption(12))
          .foregroundStyle(.secondary)
        Text(store.openRouterConnectionMessage)
          .font(DashisType.caption(12))
          .foregroundStyle(.secondary)

        HStack(spacing: 10) {
          if store.isOpenRouterOAuthConnected {
            Button {
              Task { await store.checkOpenRouterOAuthKey() }
            } label: {
              Label("Check key limit", systemImage: "gauge")
            }
            .buttonStyle(.borderedProminent)
            .disabled(store.isLoading("openrouter"))
          } else {
            Button {
              Task { await store.connectOpenRouterOAuth() }
            } label: {
              Label("Connect OpenRouter", systemImage: "link")
            }
            .buttonStyle(.borderedProminent)
            .disabled(store.isLoading("openrouter"))
          }

          Button {
            store.clearOpenRouterSession()
          } label: {
            Label("Clear", systemImage: "xmark.circle")
          }
          .buttonStyle(.bordered)
        }
      } else {
        Text("Advanced mode requires a management key and can query account credits, activity, beta analytics metadata/query, and optional generation detail.")
          .font(DashisType.caption(12))
          .foregroundStyle(.secondary)
        SecureField("management API key", text: $store.openRouterManagementAPIKey)
          .textFieldStyle(.roundedBorder)
          .disabled(store.isLoading("openrouter"))
        TextField("optional generation id", text: $store.openRouterGenerationID)
          .textFieldStyle(.roundedBorder)
          .disabled(store.isLoading("openrouter"))
        Stepper(
          "Analytics window: \(store.openRouterAnalyticsDays) days",
          value: $store.openRouterAnalyticsDays,
          in: 1...90
        )
        .font(DashisType.caption(13))
        .disabled(store.isLoading("openrouter"))

        HStack(spacing: 10) {
          Button {
            Task { await store.checkOpenRouterManagement() }
          } label: {
            Label("Check management data", systemImage: "network")
          }
          .buttonStyle(.borderedProminent)
          .disabled(store.isLoading("openrouter"))

          Button {
            store.clearOpenRouterSession()
          } label: {
            Label("Clear", systemImage: "xmark.circle")
          }
          .buttonStyle(.bordered)
        }
      }
    }
  }
}

struct DashisSettingsPanel: View {
  @Environment(\.colorScheme) private var colorScheme
  @ObservedObject var store: DashisProviderStore

  var body: some View {
    LazyVGrid(columns: [GridItem(.adaptive(minimum: 320), spacing: 14)], spacing: 14) {
      runtimeCard
      providersCard
    }
  }

  private var runtimeCard: some View {
    settingsCard(title: "Native runtime", rows: [
      ("UI", "SwiftUI"),
      ("WebView", "Not linked"),
      ("Network", "Ephemeral URLSession"),
      ("OAuth", "127.0.0.1 loopback"),
      ("Claude", "Opt-in local bridge")
    ])
  }

  private var providersCard: some View {
    settingsCard(
      title: "Providers",
      rows: store.providers.map { ($0.name, $0.kind) }
    )
  }

  private func settingsCard(title: String, rows: [(String, String)]) -> some View {
    VStack(alignment: .leading, spacing: 14) {
      Text(title)
        .font(DashisType.body(20, .semibold))
      ForEach(rows, id: \.0) { row in
        HStack {
          Text(row.0)
            .foregroundStyle(DashisTheme.secondaryText(colorScheme))
          Spacer(minLength: 12)
          Text(row.1)
            .multilineTextAlignment(.trailing)
        }
        .font(DashisType.caption(13, .medium))
        .padding(11)
        .background(DashisTheme.mutedSurface(colorScheme), in: RoundedRectangle(cornerRadius: 8, style: .continuous))
        .overlay {
          RoundedRectangle(cornerRadius: 8, style: .continuous)
            .stroke(DashisTheme.stroke(colorScheme), lineWidth: 1)
        }
      }
    }
    .padding(20)
    .dashisGlassCard(cornerRadius: 8)
  }
}
