package com.rokurics.app.ui.library

import android.media.MediaPlayer
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

/**
 * Shared playback state holder for the persistent mini-player.
 *
 * Accessible from any screen via the companion object, so playback
 * continues uninterrupted across navigation.
 */
class PlaybackState private constructor() {
    private val _recordingId = MutableStateFlow<String?>(null)
    val recordingId: StateFlow<String?> = _recordingId.asStateFlow()

    private val _title = MutableStateFlow("")
    val title: StateFlow<String> = _title.asStateFlow()

    private val _isPlaying = MutableStateFlow(false)
    val isPlaying: StateFlow<Boolean> = _isPlaying.asStateFlow()

    private val _positionMs = MutableStateFlow(0)
    val positionMs: StateFlow<Int> = _positionMs.asStateFlow()

    private val _durationMs = MutableStateFlow(0)
    val durationMs: StateFlow<Int> = _durationMs.asStateFlow()

    private val _isSeeking = MutableStateFlow(false)
    val isSeeking: StateFlow<Boolean> = _isSeeking.asStateFlow()

    private val _seekFraction = MutableStateFlow(0f)
    val seekFraction: StateFlow<Float> = _seekFraction.asStateFlow()

    private val _errorMessage = MutableStateFlow<String?>(null)
    val errorMessage: StateFlow<String?> = _errorMessage.asStateFlow()

    var mediaPlayer: MediaPlayer? = null
        private set

    var isActive: Boolean = false
        private set

    fun startPlayback(
        id: String,
        title: String,
        filePath: String,
        onComplete: () -> Unit = {}
    ) {
        stopPlayback()
        try {
            val mp = MediaPlayer().apply {
                setDataSource(filePath)
                prepare()
                start()
                setOnCompletionListener {
                    _isPlaying.value = false
                    _positionMs.value = _durationMs.value
                    onComplete()
                }
            }
            mediaPlayer = mp
            _recordingId.value = id
            _title.value = title
            _durationMs.value = mp.duration
            _positionMs.value = 0
            _isPlaying.value = true
            _errorMessage.value = null
            isActive = true
        } catch (e: Exception) {
            _errorMessage.value = "播放失败: ${e.message}"
            stopPlayback()
        }
    }

    fun togglePlayPause() {
        val mp = mediaPlayer ?: return
        try {
            if (_isPlaying.value) {
                mp.pause()
                _isPlaying.value = false
            } else {
                mp.start()
                _isPlaying.value = true
            }
        } catch (e: Exception) {
            _errorMessage.value = "播放错误: ${e.message}"
        }
    }

    fun seekTo(positionMs: Int) {
        mediaPlayer?.seekTo(positionMs)
        _positionMs.value = positionMs
    }

    fun beginSeek() {
        _isSeeking.value = true
    }

    fun endSeek(fraction: Float) {
        val targetMs = (fraction * _durationMs.value).toInt()
        seekTo(targetMs)
        _isSeeking.value = false
    }

    fun updatePosition() {
        val mp = mediaPlayer ?: return
        if (!_isSeeking.value) {
            try {
                _positionMs.value = mp.currentPosition
                if (_durationMs.value > 0 && _positionMs.value >= _durationMs.value) {
                    _positionMs.value = _durationMs.value
                    _isPlaying.value = false
                }
            } catch (_: Exception) {}
        } else {
            try {
                val pos = mp.currentPosition
                if (_durationMs.value > 0) {
                    _seekFraction.value = pos.toFloat() / _durationMs.value
                }
            } catch (_: Exception) {}
        }
    }

    fun stopPlayback() {
        try {
            mediaPlayer?.apply {
                if (isPlaying) stop()
                release()
            }
        } catch (_: Exception) {}
        mediaPlayer = null
        _recordingId.value = null
        _title.value = ""
        _isPlaying.value = false
        _positionMs.value = 0
        _durationMs.value = 0
        _isSeeking.value = false
        _errorMessage.value = null
        isActive = false
    }

    companion object {
        val shared: PlaybackState by lazy { PlaybackState() }
    }
}
