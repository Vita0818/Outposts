import SwiftUI

struct DashboardView: View {
  @Environment(\.colorScheme) private var colorScheme
  @SceneStorage("dashis.selectedView") private var selectedViewID = DashisSelection.dashboard
  @StateObject private var store = DashisProviderStore()

  var body: some View {
    NavigationSplitView {
      DashisSidebar(
        providers: store.providers,
        selectionID: $selectedViewID
      )
      .navigationSplitViewColumnWidth(min: 190, ideal: 236)
    } detail: {
      DashisDashboardDetail(
        selectedID: selectedViewID,
        store: store
      )
      .background(DashisTheme.page(colorScheme).ignoresSafeArea())
      .navigationTitle("")
    }
  }
}

#Preview {
  DashboardView()
    .frame(width: 1280, height: 860)
}
