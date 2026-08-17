package com.intatis.android.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.Card
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.intatis.android.ChatViewModel
import java.time.ZoneId
import java.time.format.DateTimeFormatter

private val SessionTimeFormat =
    DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm").withZone(ZoneId.systemDefault())

@Composable
fun SessionsScreen(
    viewModel: ChatViewModel,
    modifier: Modifier = Modifier,
    onOpenSession: () -> Unit = {},
) {
    val state by viewModel.state.collectAsState()
    var sessions by remember { mutableStateOf(viewModel.recentSessions()) }
    var refreshKey by remember { mutableStateOf(0) }

    // Refresh when the screen becomes visible again.
    androidx.compose.runtime.LaunchedEffect(refreshKey) {
        sessions = viewModel.recentSessions()
    }

    Column(
        modifier = modifier
            .fillMaxSize()
            .padding(horizontal = 16.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(top = 12.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Text(
                text = "Sessions",
                style = MaterialTheme.typography.titleLarge,
                modifier = Modifier.weight(1f),
            )
            TextButton(onClick = {
                viewModel.startNewSession()
                refreshKey++
                onOpenSession()
            }) {
                Icon(Icons.Filled.Add, contentDescription = null)
                Text("New Chat")
            }
        }

        if (sessions.isEmpty()) {
            Text(
                text = "No chat sessions yet.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.outline,
            )
        }

        LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            items(sessions, key = { it.id }) { summary ->
                Card(modifier = Modifier.fillMaxWidth()) {
                    Row(
                        modifier = Modifier.padding(start = 16.dp, top = 12.dp, bottom = 12.dp, end = 4.dp),
                        verticalAlignment = Alignment.CenterVertically,
                    ) {
                        Column(Modifier.weight(1f)) {
                            Text(
                                text = summary.displayName ?: summary.id,
                                style = MaterialTheme.typography.titleMedium,
                            )
                            Text(
                                text = "${summary.eventCount} events · ${SessionTimeFormat.format(summary.updatedAt)}",
                                style = MaterialTheme.typography.labelSmall,
                                color = MaterialTheme.colorScheme.outline,
                            )
                        }
                        IconButton(onClick = {
                            viewModel.deleteSession(summary)
                            sessions = viewModel.recentSessions()
                        }) {
                            Icon(
                                Icons.Filled.Delete,
                                contentDescription = "Delete session",
                                tint = MaterialTheme.colorScheme.error,
                            )
                        }
                    }
                }
            }
        }

        if (state.error.isNotEmpty()) {
            Text(
                text = state.error,
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.error,
            )
        }
    }
}
