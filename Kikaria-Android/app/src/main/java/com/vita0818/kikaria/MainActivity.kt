package com.vita0818.kikaria

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import com.vita0818.kikaria.ui.navigation.KikariaNavGraph
import com.vita0818.kikaria.ui.theme.KikariaTheme

/**
 * Main entry point for the Kikaria Android app.
 *
 * Translated from the iOS [KikariaApp.swift] @main App struct:
 * - [MainActivity] replaces the @main App entry point.
 * - [KikariaApp] composable replaces the WindowGroup { ContentView() } scene.
 *
 * TODO: Android notification channel setup and study-progress scheduling.
 * The iOS app sets UNUserNotificationCenter delegate in KikariaApp.init().
 * On Android this requires creating a NotificationChannel, requesting
 * POST_NOTIFICATIONS permission (API 33+), and scheduling alarms/work
 * for daily study-progress warnings.
 */
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            KikariaApp()
        }
    }
}

/**
 * Root composable that wraps the navigation graph in the Kikaria theme.
 *
 * Each screen is responsible for its own page-gradient background,
 * so the top-level [Surface] uses [Color.Transparent].
 */
@Composable
fun KikariaApp() {
    KikariaTheme(
        darkTheme = isSystemInDarkTheme()
    ) {
        Surface(
            modifier = Modifier.fillMaxSize(),
            color = Color.Transparent
        ) {
            KikariaNavGraph()
        }
    }
}
