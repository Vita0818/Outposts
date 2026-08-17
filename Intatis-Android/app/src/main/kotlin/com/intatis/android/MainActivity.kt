package com.intatis.android

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import com.intatis.android.ui.IntatisApp
import com.intatis.android.ui.IntatisTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            IntatisTheme {
                IntatisApp()
            }
        }
    }
}
