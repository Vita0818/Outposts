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
import com.vita0818.kikaria.ui.navigation.KikariaNavGraph
import com.vita0818.kikaria.ui.theme.KikariaTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            KikariaApp()
        }
    }
}

@Composable
fun KikariaApp() {
    KikariaTheme(
        darkTheme = isSystemInDarkTheme()
    ) {
        Surface(modifier = Modifier.fillMaxSize()) {
            KikariaNavGraph()
        }
    }
}
