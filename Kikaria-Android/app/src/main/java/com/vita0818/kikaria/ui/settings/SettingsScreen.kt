package com.vita0818.kikaria.ui.settings

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaPageTitle
import com.vita0818.kikaria.ui.components.KikariaSectionDivider
import com.vita0818.kikaria.ui.components.KikariaSectionHeader
import com.vita0818.kikaria.ui.components.KikariaSettingsRow
import com.vita0818.kikaria.ui.components.KikariaSettingsSection
import com.vita0818.kikaria.ui.components.KikariaSettingsToggleRow
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.viewmodel.KikariaViewModel

@Composable
fun SettingsScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit,
    onEditProfile: () -> Unit = {}
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val presetName = viewModel.activePreset?.name ?: "无"

    KikariaPageShell {
        Box(modifier = Modifier.fillMaxSize()) {
            KikariaBackButton(onClick = onBack)

            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = 24.dp)
                    .padding(top = 70.dp)
            ) {
                KikariaPageTitle(title = "设置")

                Spacer(modifier = Modifier.height(20.dp))

                // ── Profile section ──
                Column(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    // Avatar
                    ProfileAvatarLarge(
                        displayName = viewModel.userDisplayName,
                        size = 86.dp,
                        isDark = isDark
                    )

                    Spacer(Modifier.height(12.dp))

                    // Name
                    Text(
                        text = KikariaTypography.mixedText(
                            viewModel.userDisplayName.ifEmpty { "Kikaria" },
                            size = 28,
                            weight = FontWeight.SemiBold
                        ),
                        color = deepText
                    )

                    Spacer(Modifier.height(4.dp))

                    // Edit profile button
                    Text(
                        text = "编辑个人资料",
                        fontSize = 16.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = deepText,
                        modifier = Modifier
                            .clip(androidx.compose.foundation.shape.RoundedCornerShape(20.dp))
                            .background(
                                if (isDark) KikariaColors.GlassSurfaceDark.copy(alpha = 0.36f)
                                else KikariaColors.GlassSurface.copy(alpha = 0.36f)
                            )
                            .clickable { onEditProfile() }
                            .padding(horizontal = 24.dp, vertical = 13.dp)
                    )
                }

                Spacer(Modifier.height(24.dp))

                // ── Current Preset Section ──
                KikariaSettingsSection(title = "当前预设") {
                    KikariaSettingsRow(
                        title = "当前预设",
                        valueText = presetName,
                        showsChevron = false
                    )
                }

                Spacer(Modifier.height(16.dp))

                // ── Learning Section ──
                KikariaSettingsSection(title = "学习") {
                    KikariaSettingsRow(
                        title = "每日学习目标",
                        valueText = "${viewModel.dailyGoal}",
                        onClick = {
                            // Toggle daily goal would open a picker
                            // For now, cycle through common values
                            val goals = listOf(5, 10, 15, 20, 25, 30, 40, 50, 75, 100)
                            val idx = goals.indexOf(viewModel.dailyGoal)
                            viewModel.updateDailyGoal(goals[(idx + 1) % goals.size])
                        }
                    )
                    KikariaSectionDivider()
                    KikariaSettingsRow(
                        title = "倒数日",
                        valueText = if (viewModel.countdownEndDate != null && viewModel.countdownDays > 0)
                            "${viewModel.countdownDays}天" else "未设置"
                    )
                    KikariaSectionDivider()
                    KikariaSettingsRow(
                        title = "进度安全线",
                        valueText = "${viewModel.dangerPercent}%",
                        onClick = {
                            val percents = listOf(50, 60, 70, 80, 90)
                            val idx = percents.indexOf(viewModel.dangerPercent)
                            viewModel.updateDangerPercent(percents[(idx + 1) % percents.size])
                        }
                    )
                }

                Spacer(Modifier.height(16.dp))

                // ── Notification Section ──
                KikariaSettingsSection(title = "通知") {
                    KikariaSettingsToggleRow(
                        title = "学习进度通知",
                        isChecked = viewModel.notificationsEnabled,
                        onCheckedChange = { viewModel.updateNotificationsEnabled(it) }
                    )
                }

                Spacer(Modifier.height(16.dp))

                // ── About Section ──
                KikariaSettingsSection(title = "关于") {
                    KikariaSettingsRow(
                        title = "版权声明",
                        valueText = "© 2026 Kikaria",
                        showsChevron = false
                    )
                    KikariaSectionDivider()
                    KikariaSettingsRow(
                        title = "版本",
                        valueText = "0.1.0",
                        showsChevron = false
                    )
                }

                Spacer(modifier = Modifier.height(34.dp))
            }
        }
    }
}

@Composable
private fun ProfileAvatarLarge(
    displayName: String,
    size: androidx.compose.ui.unit.Dp,
    isDark: Boolean
) {
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface
    val shadowColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.16f)
    val displayChar = if (displayName.isNotEmpty()) displayName.first().uppercase() else "K"
    val gradientColors = if (isDark)
        listOf(KikariaColors.SkyDark, KikariaColors.CyanDark)
    else listOf(KikariaColors.Sky, KikariaColors.Cyan)

    Box(
        modifier = Modifier
            .size(size)
            .shadow(12.dp, CircleShape, ambientColor = shadowColor, spotColor = shadowColor)
            .clip(CircleShape)
            .background(glassSurface.copy(alpha = 0.36f))
            .padding(5.dp),
        contentAlignment = Alignment.Center
    ) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .clip(CircleShape)
                .background(Brush.linearGradient(gradientColors)),
            contentAlignment = Alignment.Center
        ) {
            Text(
                text = displayChar,
                color = Color.White,
                fontWeight = FontWeight.Bold,
                fontSize = (size.value * 0.38f).sp,
                fontFamily = FontFamily.Serif
            )
        }
    }
}
