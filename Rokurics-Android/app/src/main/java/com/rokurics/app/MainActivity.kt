package com.rokurics.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import com.rokurics.app.ui.theme.RokuricsTheme
import com.rokurics.app.ui.ContentView

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            RokuricsTheme {
                Surface(modifier = Modifier.fillMaxSize()) {
                    ContentView()
                }
            }
        }
    }
}
