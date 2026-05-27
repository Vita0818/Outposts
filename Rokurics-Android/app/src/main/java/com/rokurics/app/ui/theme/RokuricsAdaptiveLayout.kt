package com.rokurics.app.ui.theme

import androidx.compose.runtime.Composable
import androidx.compose.runtime.Immutable
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp

enum class RokuricsWidthCategory {
    COMPACT,
    REGULAR_PAD,
    WIDE_PAD
}

@Immutable
data class RokuricsAdaptiveMetrics(
    val widthDp: Float,
    val heightDp: Float
) {
    val widthCategory: RokuricsWidthCategory
        get() = when {
            widthDp < 600f -> RokuricsWidthCategory.COMPACT
            widthDp < 900f -> RokuricsWidthCategory.REGULAR_PAD
            else -> RokuricsWidthCategory.WIDE_PAD
        }

    val isPadWidth: Boolean get() = widthCategory != RokuricsWidthCategory.COMPACT
    val isWide: Boolean get() = widthDp >= 600f
    val isShort: Boolean get() = heightDp < 760f
    val isNarrow: Boolean get() = widthDp < 360f

    val horizontalPadding: Dp
        get() = when {
            isNarrow -> 20.dp
            widthCategory == RokuricsWidthCategory.COMPACT -> 24.dp
            widthCategory == RokuricsWidthCategory.REGULAR_PAD -> 32.dp
            else -> 40.dp
        }

    val homeMaxWidth: Dp
        get() = when (widthCategory) {
            RokuricsWidthCategory.COMPACT -> Dp.Unspecified
            RokuricsWidthCategory.REGULAR_PAD -> 680.dp
            RokuricsWidthCategory.WIDE_PAD -> 760.dp
        }

    val headerScale: Float
        get() = if (isPadWidth) 1.12f else 1f

    val orbScale: Float
        get() = when {
            isNarrow && isShort -> 0.78f
            isNarrow -> 0.84f
            isShort -> 0.84f
            heightDp < 820f -> 0.92f
            isPadWidth -> 1.16f
            else -> 1f
        }

    val dashboardScale: Float
        get() = when {
            isNarrow -> 0.90f
            isPadWidth -> 1.08f
            else -> 1f
        }

    val cardSpacing: Dp
        get() = if (isPadWidth) 16.dp else 13.dp

    val homeTopPadding: Dp
        get() = if (isPadWidth) 24.dp else 18.dp

    val homeBottomPadding: Dp
        get() = when {
            isShort -> 18.dp
            isPadWidth -> 34.dp
            else -> 26.dp
        }

    val contentMaxWidth: Dp
        get() = when {
            isWide && widthDp >= 900f -> 760.dp
            isWide -> 680.dp
            else -> Dp.Unspecified
        }

    companion object {
        fun from(widthDp: Dp, heightDp: Dp): RokuricsAdaptiveMetrics =
            RokuricsAdaptiveMetrics(widthDp.value, heightDp.value)
    }
}
