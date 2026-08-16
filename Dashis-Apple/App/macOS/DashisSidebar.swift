import SwiftUI

struct DashisSidebar: View {
  @Environment(\.colorScheme) private var colorScheme
  let providers: [DashisProvider]
  @Binding var selectionID: String

  var body: some View {
    VStack(alignment: .leading, spacing: 18) {
      Text("Dashis")
        .font(DashisType.brand(29))
        .foregroundStyle(DashisTheme.primaryText(colorScheme))
        .padding(.horizontal, 12)
        .padding(.top, 22)

      VStack(spacing: 6) {
        sidebarButton(
          id: DashisSelection.dashboard,
          title: "Dashboard",
          symbolName: "rectangle.3.group"
        )
      }

      VStack(alignment: .leading, spacing: 6) {
        ForEach(providers) { provider in
          sidebarButton(
            id: provider.id,
            title: provider.name,
            symbolName: provider.symbolName
          )
        }
      }

      Spacer(minLength: 24)

      sidebarButton(
        id: DashisSelection.settings,
        title: "Settings",
        symbolName: "gearshape"
      )
      .padding(.bottom, 12)
    }
    .padding(.horizontal, 12)
    .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
    .background(DashisTheme.mutedSurface(colorScheme).opacity(colorScheme == .dark ? 0.55 : 0.74))
  }

  private func sidebarButton(id: String, title: String, symbolName: String) -> some View {
    Button {
      selectionID = id
    } label: {
      Label(title, systemImage: symbolName)
        .font(DashisType.caption(13, .semibold))
        .foregroundStyle(selectionID == id ? DashisTheme.primaryText(colorScheme) : DashisTheme.secondaryText(colorScheme))
        .frame(maxWidth: .infinity, minHeight: 38, alignment: .leading)
        .padding(.horizontal, 10)
        .background {
          if selectionID == id {
            RoundedRectangle(cornerRadius: 8, style: .continuous)
              .fill(DashisTheme.accent.opacity(0.13))
          }
        }
        .overlay {
          if selectionID == id {
            RoundedRectangle(cornerRadius: 8, style: .continuous)
              .stroke(DashisTheme.accent.opacity(0.34), lineWidth: 1)
          }
        }
    }
    .buttonStyle(.plain)
  }
}
