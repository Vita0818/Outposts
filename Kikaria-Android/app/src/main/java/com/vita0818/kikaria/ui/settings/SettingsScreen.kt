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
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaProfileAvatar
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography

/**
 * Settings screen translated from the iOS SettingsView in ContentView.swift.
 *
 * Includes: profile section, current preset, daily goal, countdown,
 * danger percent, notification settings, help, and about.
 */
@Composable
fun SettingsScreen(
    userDisplayName: String,
    userHandle: String,
    presetName: String,
    dailyGoal: Int,
    countdownDays: Int?,
    dangerPercent: Int,
    notificationsEnabled: Boolean,
    notificationTimeText: String,
    onBack: () -> Unit,
    onEditProfile: () -> Unit,
    onOpenDailyGoalPicker: () -> Unit = {},
    onOpenCountdownPicker: () -> Unit = {},
    onOpenDangerPicker: () -> Unit = {},
    onToggleNotifications: (Boolean) -> Unit = {},
    onOpenNotificationTimePicker: () -> Unit = {},
    onOpenOnboarding: () -> Unit = {},
    onOpenMarkdownGuide: () -> Unit = {},
    onOpenPrivacyPolicy: () -> Unit = {}
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val mist = if (isDark) KikariaColors.MistDark else KikariaColors.Mist

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
                Text(
                    text = KikariaTypography.mixedText("设置", size = 30, weight = FontWeight.Bold),
                    color = deepText
                )

                Spacer(modifier = Modifier.height(18.dp))

                // ── Profile Section ──
                KikariaGlassCard(
                    modifier = Modifier.fillMaxWidth(),
                    cornerRadius = 28.dp,
                    fillOpacity = 0.44f
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(24.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        KikariaProfileAvatar(
                            size = 86.dp,
                            displayName = userDisplayName
                        )

                        Spacer(modifier = Modifier.height(12.dp))

                        Text(
                            text = KikariaTypography.mixedText(
                                userDisplayName.ifEmpty { "Kikaria" },
                                size = 28,
                                weight = FontWeight.SemiBold
                            ),
                            color = deepText
                        )

                        Spacer(modifier = Modifier.height(4.dp))

                        Text(
                            text = "@${userHandle.ifEmpty { "user" }}",
                            fontSize = 15.sp,
                            fontWeight = FontWeight.Medium,
                            color = softText
                        )

                        Spacer(modifier = Modifier.height(14.dp))

                        SettingsButton(
                            text = "编辑个人资料",
                            isPrimary = false,
                            onClick = onEditProfile
                        )
                    }
                }

                Spacer(modifier = Modifier.height(12.dp))

                // ── Learning Settings ──
                SettingsSection(title = "当前预设") {
                    SettingsRow(
                        title = "当前预设",
                        value = presetName,
                        showChevron = false
                    ) {}
                    SettingsDivider()
                    SettingsRow(
                        title = "每日学习目标",
                        value = "$dailyGoal"
                    ) { onOpenDailyGoalPicker() }
                    SettingsDivider()
                    SettingsRow(
                        title = "倒数日",
                        value = countdownDays?.let { "${it}天" } ?: "未设置"
                    ) { onOpenCountdownPicker() }
                    SettingsDivider()
                    SettingsRow(
                        title = "进度安全线",
                        value = "${dangerPercent}%"
                    ) { onOpenDangerPicker() }
                }

                Spacer(modifier = Modifier.height(12.dp))

                // ── Notification Settings ──
                SettingsSection(title = "通知") {
                    SettingsToggleRow(
                        title = "学习进度通知",
                        isOn = notificationsEnabled
                    ) { onToggleNotifications(it) }

                    if (notificationsEnabled) {
                        SettingsDivider()
                        SettingsRow(
                            title = "通知时间",
                            value = notificationTimeText
                        ) { onOpenNotificationTimePicker() }
                    }
                }

                Spacer(modifier = Modifier.height(12.dp))

                // ── Help ──
                SettingsSection(title = "帮助") {
                    SettingsRow(title = "新手引导", value = "") { onOpenOnboarding() }
                    SettingsDivider()
                    SettingsRow(title = "Markdown 格式", value = "") { onOpenMarkdownGuide() }
                }

                Spacer(modifier = Modifier.height(12.dp))

                // ── About ──
                SettingsSection(title = "关于") {
                    SettingsRow(title = "隐私政策", value = "") { onOpenPrivacyPolicy() }
                    SettingsDivider()
                    SettingsRow(title = "版权声明", value = "© 2026 Vita", showChevron = false) {}
                    SettingsDivider()
                    SettingsRow(title = "版本", value = "0.1.0", showChevron = false) {}
                }

                Spacer(modifier = Modifier.height(32.dp))
            }
        }
    }
}

// ─── Settings Section ───

@Composable
private fun SettingsSection(
    title: String,
    content: @Composable () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText

    Column {
        Text(
            text = title,
            fontSize = 13.sp,
            fontWeight = FontWeight.SemiBold,
            color = softText,
            modifier = Modifier.padding(start = 4.dp, bottom = 8.dp)
        )
        KikariaGlassCard(
            modifier = Modifier.fillMaxWidth(),
            cornerRadius = 22.dp,
            fillOpacity = 0.40f
        ) {
            Column(modifier = Modifier.padding(4.dp)) {
                content()
            }
        }
    }
}

// ─── Settings Row ───

@Composable
private fun SettingsRow(
    title: String,
    value: String,
    showChevron: Boolean = true,
    onClick: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clickable { onClick() }
            .padding(horizontal = 18.dp, vertical = 16.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = KikariaTypography.mixedText(title, size = 17, weight = FontWeight.Medium),
            color = deepText,
            modifier = Modifier.weight(1f)
        )
        if (value.isNotEmpty()) {
            Text(
                text = value,
                fontSize = 15.sp,
                fontWeight = FontWeight.Medium,
                color = softText,
                modifier = Modifier.padding(end = 8.dp)
            )
        }
        if (showChevron) {
            Text(
                "›",
                fontSize = 18.sp,
                fontWeight = FontWeight.SemiBold,
                color = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
                    .copy(alpha = 0.52f)
            )
        }
    }
}

// ─── Settings Toggle Row ───

@Composable
private fun SettingsToggleRow(
    title: String,
    isOn: Boolean,
    onToggle: (Boolean) -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 18.dp, vertical = 16.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = KikariaTypography.mixedText(title, size = 17, weight = FontWeight.Medium),
            color = deepText,
            modifier = Modifier.weight(1f)
        )
        Box(
            modifier = Modifier
                .size(51.dp, 31.dp)
                .clip(RoundedCornerShape(16.dp))
                .background(if (isOn) sky else (if (isDark) KikariaColors.MistDark else KikariaColors.Mist))
                .clickable { onToggle(!isOn) },
            contentAlignment = if (isOn) Alignment.CenterEnd else Alignment.CenterStart
        ) {
            Box(
                modifier = Modifier
                    .padding(4.dp)
                    .size(23.dp)
                    .clip(CircleShape)
                    .background(Color.White)
                    .shadow(2.dp, CircleShape)
            )
        }
    }
}

// ─── Settings Divider ───

@Composable
private fun SettingsDivider() {
    val isDark = isSystemInDarkTheme()
    Box(
        Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp)
            .height(0.5.dp)
            .background(
                (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray)
                    .copy(alpha = 0.14f)
            )
    )
}

// ─── Settings Button ───

@Composable
private fun SettingsButton(
    text: String,
    isPrimary: Boolean = true,
    onClick: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    Box(
        modifier = Modifier
            .clip(RoundedCornerShape(20.dp))
            .shadow(
                12.dp, RoundedCornerShape(20.dp),
                spotColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.10f)
            )
            .background(
                if (isPrimary) {
                    if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
                } else {
                    Brush.linearGradient(
                        listOf(
                            (if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface)
                                .copy(alpha = 0.44f),
                            (if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface)
                                .copy(alpha = 0.44f)
                        )
                    )
                }
            )
            .clickable { onClick() }
            .padding(horizontal = 24.dp, vertical = 13.dp),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = text,
            fontSize = 16.sp,
            fontWeight = FontWeight.SemiBold,
            color = if (isPrimary) Color.White
            else if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
        )
    }
}
