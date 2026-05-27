package com.rokurics.app.ui

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.platform.LocalContext
import androidx.lifecycle.viewmodel.compose.viewModel
import com.rokurics.app.domain.provider.AndroidSpeechRecognizerEngine
import com.rokurics.app.service.RecordingManager
import com.rokurics.app.ui.home.HomeScreen

@Composable
fun ContentView() {
    val recordingManager: RecordingManager = viewModel()
    val context = LocalContext.current

    LaunchedEffect(Unit) {
        val engine = AndroidSpeechRecognizerEngine(context)
        if (engine.isAvailable()) {
            recordingManager.localTranscriptionEngine = engine
        }
    }

    HomeScreen(recordingManager = recordingManager)
}
