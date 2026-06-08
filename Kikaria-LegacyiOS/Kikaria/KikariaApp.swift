//
//  KikariaApp.swift
//  Kikaria
//
//  Created by Vita on 2026/5/1.
//

import SwiftUI
import UserNotifications

@main
struct KikariaApp: App {
    init() {
        UNUserNotificationCenter.current().delegate = KikariaNotificationDelegate.shared
    }

    var body: some Scene {
        WindowGroup {
            ContentView()
        }
    }
}
