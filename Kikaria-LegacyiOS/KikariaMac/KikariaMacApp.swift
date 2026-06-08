//
//  KikariaMacApp.swift
//  KikariaMac
//
//  Created by Vita on 2026/5/11.
//

import SwiftUI

@main
struct KikariaMacApp: App {
    var body: some Scene {
        WindowGroup {
            KikariaMacRootView()
                .frame(minWidth: 1240, minHeight: 690)
        }
        .defaultSize(width: 1240, height: 780)
    }
}
