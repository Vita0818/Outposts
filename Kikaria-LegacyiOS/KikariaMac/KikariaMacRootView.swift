//
//  KikariaMacRootView.swift
//  KikariaMac
//
//  Created by Vita on 2026/5/11.
//

import SwiftUI
#if os(macOS)
import AppKit
#endif

// 基于 Kikaria iPad 横屏版入口适配：这里不新造 Mac UI，只包裹真实 ContentView 并设置桌面窗口下限。
struct KikariaMacRootView: View {
    var body: some View {
        ContentView()
            #if os(macOS)
            .background(KikariaMacWindowChromeConfigurator())
            #endif
            .frame(minWidth: 1240, minHeight: 690)
    }
}

#if os(macOS)
private struct KikariaMacWindowChromeConfigurator: NSViewRepresentable {
    func makeNSView(context: Context) -> NSView {
        let view = NSView()
        DispatchQueue.main.async {
            configureWindow(for: view)
        }
        return view
    }

    func updateNSView(_ nsView: NSView, context: Context) {
        DispatchQueue.main.async {
            configureWindow(for: nsView)
        }
    }

    private func configureWindow(for view: NSView) {
        guard let window = view.window else {
            return
        }

        window.titleVisibility = .hidden
        window.titlebarAppearsTransparent = true
        window.styleMask.insert(.fullSizeContentView)
        window.isMovableByWindowBackground = true
    }
}
#endif

#Preview {
    KikariaMacRootView()
}
