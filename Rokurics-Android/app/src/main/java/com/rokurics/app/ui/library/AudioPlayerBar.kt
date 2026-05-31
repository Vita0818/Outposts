package com.rokurics.app.ui.library

import android.media.MediaPlayer
import androidx.compose.animation.animateColorAsState
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Pause
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.rokurics.app.ui.theme.RokuricsColors
import com.rokurics.app.ui.theme.rokuricsGlassCard
import kotlinx.coroutines.delay
import java.io.File

enum class AudioPlayerState { IDLE, PLAYING, PAUSED, COMPLETED, ERROR }

@Composable
fun AudioPlayerBar(
    audioFilePath: String,
    modifier: Modifier = Modifier
) {
    var playerState by remember { mutableStateOf(AudioPlayerState.IDLE) }
    var currentPositionMs by remember { mutableIntStateOf(0) }
    var durationMs by remember { mutableIntStateOf(0) }
    var errorMessage by remember { mutableStateOf<String?>(null) }
    val mediaPlayer = remember { MediaPlayer() }
    var isSeeking by remember { mutableStateOf(false) }
    var seekProgress by remember { mutableFloatStateOf(0f) }

    val playPauseColor by animateColorAsState(
        targetValue = if (playerState == AudioPlayerState.PLAYING) RokuricsColors.coral else RokuricsColors.aqua,
        label = "playPauseColor"
    )

    // Prepare player when file path changes
    LaunchedEffect(audioFilePath) {
        try {
            mediaPlayer.reset()
            val file = File(audioFilePath)
            if (!file.exists()) {
                errorMessage = "音频文件不存在"
                playerState = AudioPlayerState.ERROR
                return@LaunchedEffect
            }
            mediaPlayer.setDataSource(audioFilePath)
            mediaPlayer.prepare()
            durationMs = mediaPlayer.duration
            currentPositionMs = 0
            playerState = AudioPlayerState.IDLE
            errorMessage = null
        } catch (e: Exception) {
            errorMessage = "播放器初始化失败: ${e.message}"
            playerState = AudioPlayerState.ERROR
        }
    }

    // Seek position while seeking
    LaunchedEffect(isSeeking) {
        if (isSeeking) {
            while (isSeeking) {
                delay(100)
                if (playerState == AudioPlayerState.PLAYING) {
                    currentPositionMs = mediaPlayer.currentPosition
                    val fraction = if (durationMs > 0) currentPositionMs.toFloat() / durationMs else 0f
                    seekProgress = fraction
                }
            }
        }
    }

    // Position tracker while playing
    LaunchedEffect(playerState) {
        if (playerState == AudioPlayerState.PLAYING) {
            while (playerState == AudioPlayerState.PLAYING) {
                delay(250)
                if (!isSeeking) {
                    currentPositionMs = mediaPlayer.currentPosition
                    if (durationMs > 0 && currentPositionMs >= durationMs) {
                        playerState = AudioPlayerState.COMPLETED
                        currentPositionMs = durationMs
                    }
                }
            }
        }
    }

    DisposableEffect(Unit) {
        onDispose {
            try { mediaPlayer.release() } catch (_: Exception) {}
        }
    }

    val isDark = isSystemInDarkTheme()
    val barSurface = if (isDark) Color(0xFF0D2424).copy(alpha = 0.80f) else Color.White.copy(alpha = 0.7f)
    val barTextColor = if (isDark) RokuricsColors.deepTextDark else RokuricsColors.deepText
    val barSubTextColor = if (isDark) RokuricsColors.softTextDark else RokuricsColors.softText

    Card(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = barSurface),
        border = androidx.compose.foundation.BorderStroke(0.5.dp, Color.White.copy(alpha = if (isDark) 0.08f else 0.18f))
    ) {
        Column(
            modifier = Modifier.padding(12.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            // Title + time row
            Row(
                verticalAlignment = Alignment.CenterVertically,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(
                    text = "播放录音",
                    fontSize = 13.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = barTextColor
                )
                Spacer(Modifier.weight(1f))
                Text(
                    text = "${formatPosition(currentPositionMs)} / ${formatPosition(durationMs)}",
                    fontSize = 12.sp,
                    color = barSubTextColor
                )
            }

            // Seek + controls row
            Row(
                verticalAlignment = Alignment.CenterVertically,
                modifier = Modifier.fillMaxWidth()
            ) {
                // Play/Pause button
                Box(
                    modifier = Modifier
                        .size(38.dp)
                        .clip(CircleShape)
                        .background(playPauseColor.copy(alpha = 0.15f))
                        .clickable(enabled = playerState != AudioPlayerState.ERROR) {
                            try {
                                when (playerState) {
                                    AudioPlayerState.IDLE, AudioPlayerState.PAUSED, AudioPlayerState.COMPLETED -> {
                                        if (playerState == AudioPlayerState.COMPLETED) {
                                            mediaPlayer.seekTo(0)
                                            currentPositionMs = 0
                                        }
                                        mediaPlayer.start()
                                        playerState = AudioPlayerState.PLAYING
                                    }
                                    AudioPlayerState.PLAYING -> {
                                        mediaPlayer.pause()
                                        playerState = AudioPlayerState.PAUSED
                                    }
                                    else -> {}
                                }
                            } catch (e: Exception) {
                                errorMessage = "播放错误: ${e.message}"
                                playerState = AudioPlayerState.ERROR
                            }
                        },
                    contentAlignment = Alignment.Center
                ) {
                    Icon(
                        imageVector = if (playerState == AudioPlayerState.PLAYING) Icons.Default.Pause else Icons.Default.PlayArrow,
                        contentDescription = if (playerState == AudioPlayerState.PLAYING) "暂停" else "播放",
                        tint = playPauseColor,
                        modifier = Modifier.size(20.dp)
                    )
                }

                Spacer(Modifier.width(10.dp))

                // Seek bar (compact)
                Slider(
                    value = if (isSeeking) seekProgress
                    else if (durationMs > 0) currentPositionMs.toFloat() / durationMs else 0f,
                    onValueChange = { fraction ->
                        seekProgress = fraction
                        isSeeking = true
                    },
                    onValueChangeFinished = {
                        val targetMs = (seekProgress * durationMs).toInt()
                        mediaPlayer.seekTo(targetMs)
                        currentPositionMs = targetMs
                        isSeeking = false
                    },
                    modifier = Modifier.weight(1f).height(20.dp),
                    colors = SliderDefaults.colors(
                        thumbColor = RokuricsColors.aqua,
                        activeTrackColor = RokuricsColors.aqua,
                        inactiveTrackColor = RokuricsColors.aqua.copy(alpha = 0.18f)
                    ),
                    enabled = playerState != AudioPlayerState.ERROR && durationMs > 0
                )
            }

            if (errorMessage != null) {
                Text(
                    text = errorMessage!!,
                    fontSize = 12.sp,
                    color = RokuricsColors.coral,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        }
    }
}

private fun formatPosition(ms: Int): String {
    val totalSecs = (ms / 1000).coerceAtLeast(0)
    val minutes = totalSecs / 60
    val seconds = totalSecs % 60
    return "%02d:%02d".format(minutes, seconds)
}
