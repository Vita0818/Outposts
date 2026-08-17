package com.intatis.android.ui

import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Chat
import androidx.compose.material.icons.automirrored.filled.List
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.Icon
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.lifecycle.viewmodel.compose.viewModel
import com.intatis.android.ChatViewModel

private enum class Surface(val label: String) {
    CHAT("Chat"),
    SESSIONS("Sessions"),
    SETTINGS("Settings"),
}

@Composable
fun IntatisApp(viewModel: ChatViewModel = viewModel()) {
    var current by remember { mutableIntStateOf(0) }

    Scaffold(
        bottomBar = {
            NavigationBar {
                NavigationBarItem(
                    selected = current == 0,
                    onClick = { current = 0 },
                    icon = { Icon(Icons.AutoMirrored.Filled.Chat, contentDescription = null) },
                    label = { Text(Surface.CHAT.label) },
                )
                NavigationBarItem(
                    selected = current == 1,
                    onClick = { current = 1 },
                    icon = { Icon(Icons.AutoMirrored.Filled.List, contentDescription = null) },
                    label = { Text(Surface.SESSIONS.label) },
                )
                NavigationBarItem(
                    selected = current == 2,
                    onClick = { current = 2 },
                    icon = { Icon(Icons.Filled.Settings, contentDescription = null) },
                    label = { Text(Surface.SETTINGS.label) },
                )
            }
        },
    ) { padding ->
        val modifier = Modifier.padding(padding)
        when (current) {
            0 -> ChatScreen(viewModel, modifier)
            1 -> SessionsScreen(viewModel, modifier)
            else -> SettingsScreen(viewModel, modifier)
        }
    }
}
