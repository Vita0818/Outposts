package com.vita0818.kikaria.ui.settings

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaScrollPageShell
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics

/**
 * Privacy Policy screen, translated from the iOS privacy alert in ContentView.swift.
 *
 * The iOS app shows a simple alert with the privacy statement.
 * On Android this becomes a full screen for better readability.
 */
@Composable
fun PrivacyPolicyScreen(
    onBack: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText

    val metrics = rememberKikariaPhoneMetrics()

    KikariaScrollPageShell(onBack = onBack, metrics = metrics) {
        Spacer(modifier = Modifier.height(metrics.pageTopPadding))
                Text(
                    text = KikariaTypography.mixedText(
                        "隐私政策",
                        size = 30,
                        weight = FontWeight.Bold
                    ),
                    color = deepText
                )

                Spacer(modifier = Modifier.height(16.dp))

                KikariaGlassCard(
                    modifier = Modifier.fillMaxWidth(),
                    cornerRadius = 28.dp,
                    fillOpacity = 0.44f
                ) {
                    Column(modifier = Modifier.padding(22.dp)) {
                        Text(
                            text = KikariaTypography.mixedText(
                                "Kikaria 当前仅在本机保存你的学习资料、预设、头像和学习进度。" +
                                        "学习进度通知使用本地通知，不会上传到服务器。",
                                size = 16,
                                weight = FontWeight.Normal
                            ),
                            color = deepText,
                            lineHeight = 26.sp
                        )

                        Spacer(modifier = Modifier.height(16.dp))

                        Text(
                            text = KikariaTypography.mixedText(
                                "本应用不收集任何个人信息，不使用任何第三方分析或广告服务。" +
                                        "所有数据仅存储在你的设备上，卸载应用时将被一并删除。",
                                size = 16,
                                weight = FontWeight.Normal
                            ),
                            color = deepText,
                            lineHeight = 26.sp
                        )

                        Spacer(modifier = Modifier.height(16.dp))

                        Text(
                            text = KikariaTypography.mixedText(
                                "如果你有任何关于隐私的问题，可以通过 GitHub Issues 联系我们。",
                                size = 16,
                                weight = FontWeight.Normal
                            ),
                            color = softText,
                            lineHeight = 26.sp
                        )
                    }
                }

                Spacer(modifier = Modifier.height(32.dp))
            }
}
