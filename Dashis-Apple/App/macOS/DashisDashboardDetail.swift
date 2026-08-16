import SwiftUI

struct DashisDashboardDetail: View {
  let selectedID: String
  @ObservedObject var store: DashisProviderStore

  var body: some View {
    ScrollView {
      VStack(alignment: .leading, spacing: 18) {
        DashisPageHeader(title: store.title(for: selectedID))
        content
      }
      .padding(.horizontal, 34)
      .padding(.top, 32)
      .padding(.bottom, 34)
      .frame(maxWidth: 1220)
      .frame(maxWidth: .infinity, alignment: .center)
    }
    .scrollContentBackground(.hidden)
  }

  @ViewBuilder private var content: some View {
    if selectedID == DashisSelection.dashboard {
      LazyVGrid(columns: [GridItem(.adaptive(minimum: 360), spacing: 18)], spacing: 18) {
        ForEach(store.providers) { provider in
          DashisProviderCard(
            provider: provider,
            isLoading: store.isLoading(provider.id)
          ) {
            Task { await store.runPrimaryCheck(for: provider.id) }
          }
        }
      }
    } else if selectedID == DashisSelection.settings {
      DashisSettingsPanel(store: store)
    } else if let provider = store.provider(id: selectedID) {
      LazyVGrid(columns: [GridItem(.adaptive(minimum: 360), spacing: 18)], spacing: 18) {
        DashisProviderCard(
          provider: provider,
          isLoading: store.isLoading(provider.id)
        ) {
          Task { await store.runPrimaryCheck(for: provider.id) }
        }
        DashisProviderDetail(provider: provider, store: store)
      }
    } else {
      LazyVGrid(columns: [GridItem(.adaptive(minimum: 360), spacing: 18)], spacing: 18) {
        ForEach(store.providers) { provider in
          DashisProviderCard(
            provider: provider,
            isLoading: store.isLoading(provider.id)
          ) {
            Task { await store.runPrimaryCheck(for: provider.id) }
          }
        }
      }
    }
  }
}
