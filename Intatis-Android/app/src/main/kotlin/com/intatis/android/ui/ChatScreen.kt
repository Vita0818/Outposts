package com.intatis.android.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Send
import androidx.compose.material.icons.filled.ArrowDropDown
import androidx.compose.material.icons.filled.Stop
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TextField
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.intatis.android.ChatViewModel
import com.intatis.shared.protocol.MessageRole
import java.time.ZoneId
import java.time.format.DateTimeFormatter

private val TimeFormat = DateTimeFormatter.ofPattern("HH:mm").withZone(ZoneId.systemDefault())

@Composable
fun ChatScreen(viewModel: ChatViewModel, modifier: Modifier = Modifier) {
    val state by viewModel.state.collectAsState()
    var input by remember { mutableStateOf("") }
    var modelMenuOpen by remember { mutableStateOf(false) }
    val listState = rememberLazyListState()

    LaunchedEffect(state.messages.size, state.messages.lastOrNull()?.text?.length) {
        if (state.messages.isNotEmpty()) {
            listState.animateScrollToItem(state.messages.lastIndex)
        }
    }

    Column(
        modifier = modifier
            .fillMaxSize()
            .imePadding(),
    ) {
        // Header: serif session title + model pill (composer control height 40).
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 20.dp, vertical = 12.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Text(
                text = state.title,
                style = MaterialTheme.typography.titleLarge,
                modifier = Modifier.weight(1f),
            )
            Box {
                TextButton(onClick = { modelMenuOpen = true }) {
                    Text(
                        text = state.selectedModel,
                        style = MaterialTheme.typography.labelMedium,
                        modifier = Modifier.widthIn(max = 180.dp),
                        maxLines = 1,
                    )
                    Icon(Icons.Filled.ArrowDropDown, contentDescription = "Select model")
                }
                DropdownMenu(expanded = modelMenuOpen, onDismissRequest = { modelMenuOpen = false }) {
                    state.modelOptions.forEach { option ->
                        DropdownMenuItem(
                            text = { Text(option) },
                            onClick = {
                                viewModel.selectModel(option)
                                modelMenuOpen = false
                            },
                        )
                    }
                }
            }
        }

        // Message list: user trailing capsule (radius 16), assistant leading plain.
        LazyColumn(
            state = listState,
            modifier = Modifier
                .weight(1f)
                .fillMaxWidth()
                .padding(horizontal = 16.dp),
            verticalArrangement = Arrangement.spacedBy(14.dp),
        ) {
            items(state.messages.size) { index ->
                val message = state.messages[index]
                MessageBubble(message)
            }
        }

        if (state.error.isNotEmpty()) {
            Text(
                text = state.error,
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.error,
                modifier = Modifier.padding(horizontal = 20.dp, vertical = 4.dp),
            )
        }

        // Composer: pill input (radius 20) + send/stop.
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 12.dp, vertical = 8.dp),
            verticalAlignment = Alignment.Bottom,
        ) {
            TextField(
                value = input,
                onValueChange = { input = it },
                placeholder = { Text("Message Intatis…") },
                shape = RoundedCornerShape(20.dp),
                colors = TextFieldDefaults.colors(),
                maxLines = 6,
                modifier = Modifier
                    .weight(1f)
                    .heightIn(min = 52.dp, max = 160.dp),
            )
            Spacer(Modifier.widthIn(min = 4.dp))
            if (state.isStreaming) {
                IconButton(
                    onClick = viewModel::stop,
                    modifier = Modifier.padding(bottom = 4.dp),
                ) {
                    Icon(Icons.Filled.Stop, contentDescription = "Stop")
                }
            } else {
                IconButton(
                    onClick = {
                        val text = input
                        input = ""
                        viewModel.send(text)
                    },
                    modifier = Modifier.padding(bottom = 4.dp),
                    enabled = input.isNotBlank(),
                ) {
                    Icon(Icons.AutoMirrored.Filled.Send, contentDescription = "Send")
                }
            }
        }
        if (state.usageText.isNotEmpty()) {
            Text(
                text = state.usageText,
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.outline,
                modifier = Modifier
                    .align(Alignment.End)
                    .padding(end = 20.dp, bottom = 6.dp),
            )
        }
    }
}

@Composable
private fun MessageBubble(message: com.intatis.shared.session.ChatMessageView) {
    if (message.role == MessageRole.USER) {
        Box(
            modifier = Modifier.fillMaxWidth(),
            contentAlignment = Alignment.CenterEnd,
        ) {
            Surface(
                color = MaterialTheme.colorScheme.primary,
                shape = RoundedCornerShape(16.dp),
            ) {
                Column(Modifier.padding(horizontal = 15.dp, vertical = 11.dp)) {
                    Text(
                        text = message.text,
                        style = MaterialTheme.typography.bodyLarge,
                        color = MaterialTheme.colorScheme.onPrimary,
                    )
                    if (message.attachmentCount > 0) {
                        Text(
                            text = "📎 ${message.attachmentCount}",
                            style = MaterialTheme.typography.labelSmall,
                            color = MaterialTheme.colorScheme.onPrimary,
                        )
                    }
                }
            }
        }
    } else {
        Column(
            modifier = Modifier.fillMaxWidth(),
            horizontalAlignment = Alignment.Start,
        ) {
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                val caption = when (message.role) {
                    MessageRole.AGENT -> message.agent ?: "Agent"
                    MessageRole.SYSTEM -> "System"
                    else -> "Intatis"
                }
                Text(caption, style = MaterialTheme.typography.labelSmall)
                Text(TimeFormat.format(message.timestamp), style = MaterialTheme.typography.labelSmall)
            }
            Text(
                text = message.text + if (message.isComplete) "" else " …",
                style = MaterialTheme.typography.bodyLarge,
                color = if (message.role == MessageRole.SYSTEM) {
                    MaterialTheme.colorScheme.error
                } else {
                    MaterialTheme.colorScheme.onSurface
                },
            )
        }
    }
}
