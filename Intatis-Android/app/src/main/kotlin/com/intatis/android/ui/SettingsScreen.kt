package com.intatis.android.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.unit.dp
import com.intatis.android.ChatViewModel

@Composable
fun SettingsScreen(viewModel: ChatViewModel, modifier: Modifier = Modifier) {
    val state by viewModel.state.collectAsState()

    Column(
        modifier = modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 16.dp),
        verticalArrangement = Arrangement.spacedBy(14.dp),
    ) {
        Text(
            text = "Settings",
            style = MaterialTheme.typography.titleLarge,
            modifier = Modifier.padding(top = 12.dp),
        )

        if (state.needsApiKey) {
            Card {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                    Text("API key missing", style = MaterialTheme.typography.titleMedium)
                    Text(
                        text = "Place intatis.json in the app's files/intatis directory or set " +
                            "INTATIS_CONFIG, then reload. Keys are referenced as {env:VAR} or " +
                            "{file:path} and never copied into sessions.",
                        style = MaterialTheme.typography.bodyMedium,
                    )
                }
            }
        }

        Card {
            Column(
                Modifier.padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                Row {
                    Text("Providers", style = MaterialTheme.typography.titleMedium)
                    Text(
                        text = state.configSource.ifEmpty { "no config file" },
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.outline,
                    )
                }
                state.providers.forEach { provider ->
                    Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                        Text(
                            text = "${provider.displayName} (${provider.id})",
                            style = MaterialTheme.typography.bodyLarge,
                        )
                        Text(
                            text = provider.baseUrl,
                            style = MaterialTheme.typography.labelSmall,
                            fontFamily = FontFamily.Monospace,
                        )
                        Text(
                            text = "key: ${provider.keyDescription} · models: " +
                                provider.models.joinToString(", ").ifEmpty { "-" },
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.outline,
                        )
                    }
                }
            }
        }

        Card {
            Column(
                Modifier.padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(4.dp),
            ) {
                Text("Role routes", style = MaterialTheme.typography.titleMedium)
                state.roles.forEach { (label, value) ->
                    Text(
                        text = "$label: $value",
                        style = MaterialTheme.typography.labelMedium,
                    )
                }
                Text(
                    text = "Role routes are managed in the Intatis JSON config; the mobile app " +
                        "is a strict Chat subset per the Apple contract.",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.outline,
                )
            }
        }

        Card {
            Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text("Model", style = MaterialTheme.typography.titleMedium)
                Text(
                    text = "Active: ${state.selectedModel}",
                    style = MaterialTheme.typography.bodyMedium,
                )
                Text(
                    text = "Pick the model from the Chat header menu.",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.outline,
                )
            }
        }

        TextButton(onClick = viewModel::reloadConfig) {
            Text("Reload configuration")
        }
        androidx.compose.foundation.layout.Spacer(Modifier.padding(bottom = 20.dp))
    }
}
