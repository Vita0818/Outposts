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
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import java.util.Calendar
import java.util.Date
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
    countdownDays: Int,
    countdownEndDate: Date?,
    dangerPercent: Int,
    notificationsEnabled: Boolean,
    notificationTimeText: String,
    onBack: () -> Unit,
    onEditProfile: () -> Unit,
    onSetDailyGoal: (Int) -> Unit = {},
    onSetCountdownRange: (Date?, Date?) -> Unit = {},
    onSetDangerPercent: (Int) -> Unit = {},
    onToggleNotifications: (Boolean) -> Unit = {},
    onSetNotificationTime: (String) -> Unit = {},
    onOpenOnboarding: () -> Unit = {},
    onOpenMarkdownGuide: () -> Unit = {},
    onOpenPrivacyPolicy: () -> Unit = {}
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText

    var showDailyGoalPicker by remember { mutableStateOf(false) }
    var showCountdownPicker by remember { mutableStateOf(false) }
    var showDangerPicker by remember { mutableStateOf(false) }
    var showNotificationTimePicker by remember { mutableStateOf(false) }
    var countdownDraftStart by remember { mutableStateOf(countdownEndDate ?: Date()) }
    var countdownDraftEnd by remember { mutableStateOf(countdownEndDate ?: Date()) }
    var draftGoal by remember { mutableStateOf(dailyGoal) }
    var draftDanger by remember { mutableStateOf(dangerPercent) }
    var draftHour by remember {
        mutableStateOf(
            notificationTimeText.split(":").firstOrNull()?.toIntOrNull() ?: 21
        )
    }
    var draftMinute by remember {
        mutableStateOf(
            notificationTimeText.split(":").getOrNull(1)?.toIntOrNull() ?: 0
        )
    }

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
                    ) { showDailyGoalPicker = true }
                    SettingsDivider()
                    SettingsRow(
                        title = "倒数日",
                        value = if (countdownDays > 0) "${countdownDays}天" else "未设置"
                    ) { showCountdownPicker = true }
                    SettingsDivider()
                    SettingsRow(
                        title = "进度安全线",
                        value = "${dangerPercent}%"
                    ) { showDangerPicker = true }
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
                        ) { showNotificationTimePicker = true }
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

            // ── Picker Dialogs ──
            if (showDailyGoalPicker) {
                PickerDialog(
                    title = "每日学习目标",
                    valueText = "$draftGoal",
                    isDark = isDark,
                    onDismiss = { showDailyGoalPicker = false },
                    onConfirm = {
                        onSetDailyGoal(draftGoal)
                        showDailyGoalPicker = false
                    }
                ) {
                    PickerWheel(
                        values = (1..100).toList(),
                        selected = draftGoal,
                        formatLabel = { "$it 个" },
                        onSelected = { draftGoal = it },
                        isDark = isDark
                    )
                }
            }

            if (showCountdownPicker) {
                PickerDialog(
                    title = "倒数日",
                    valueText = if (countdownDays > 0) "${countdownDays}天" else "未设置",
                    isDark = isDark,
                    onDismiss = { showCountdownPicker = false },
                    onConfirm = {
                        onSetCountdownRange(countdownDraftStart, countdownDraftEnd)
                        showCountdownPicker = false
                    },
                    onClear = {
                        onSetCountdownRange(null, null)
                        showCountdownPicker = false
                    }
                ) {
                    DateRangePicker(
                        startDate = countdownDraftStart,
                        endDate = countdownDraftEnd,
                        onStartDateChanged = { countdownDraftStart = it },
                        onEndDateChanged = { countdownDraftEnd = it },
                        isDark = isDark
                    )
                }
            }

            if (showDangerPicker) {
                PickerDialog(
                    title = "进度安全线",
                    valueText = "$draftDanger%",
                    isDark = isDark,
                    onDismiss = { showDangerPicker = false },
                    onConfirm = {
                        onSetDangerPercent(draftDanger)
                        showDangerPicker = false
                    }
                ) {
                    PickerWheel(
                        values = (1..100).toList(),
                        selected = draftDanger,
                        formatLabel = { "$it%" },
                        onSelected = { draftDanger = it },
                        isDark = isDark
                    )
                }
            }

            if (showNotificationTimePicker) {
                PickerDialog(
                    title = "通知时间",
                    valueText = notificationTimeText,
                    isDark = isDark,
                    onDismiss = { showNotificationTimePicker = false },
                    onConfirm = {
                        val formatted = String.format("%02d:%02d", draftHour, draftMinute)
                        onSetNotificationTime(formatted)
                        showNotificationTimePicker = false
                    }
                ) {
                    TimePicker(
                        hour = draftHour,
                        minute = draftMinute,
                        onHourChanged = { draftHour = it },
                        onMinuteChanged = { draftMinute = it },
                        isDark = isDark
                    )
                }
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

// ─── Picker Dialog Overlay ───

@Composable
private fun PickerDialog(
    title: String,
    valueText: String,
    isDark: Boolean,
    onDismiss: () -> Unit,
    onConfirm: () -> Unit,
    onClear: (() -> Unit)? = null,
    content: @Composable () -> Unit
) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Black.copy(alpha = 0.001f))
            .clickable { onDismiss() },
        contentAlignment = Alignment.Center
    ) {
        KikariaGlassCard(
            modifier = Modifier.padding(horizontal = 34.dp).fillMaxWidth(),
            cornerRadius = 28.dp,
            fillOpacity = 0.50f
        ) {
            Column(
                modifier = Modifier.padding(18.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = title,
                        fontSize = 17.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = deepText
                    )
                    Text(
                        text = valueText,
                        fontSize = 17.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
                    )
                }
                Spacer(modifier = Modifier.height(12.dp))
                content()
                Spacer(modifier = Modifier.height(12.dp))
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    if (onClear != null) {
                        Box(
                            modifier = Modifier
                                .weight(1f)
                                .clip(RoundedCornerShape(20.dp))
                                .background(
                                    (if (isDark) KikariaColors.GlassSurfaceDark
                                    else KikariaColors.GlassSurface).copy(alpha = 0.44f)
                                )
                                .clickable { onClear() }
                                .padding(vertical = 12.dp),
                            contentAlignment = Alignment.Center
                        ) {
                            Text("清除", fontSize = 16.sp, fontWeight = FontWeight.SemiBold, color = deepText)
                        }
                    }
                    Box(
                        modifier = Modifier
                            .weight(1f)
                            .clip(RoundedCornerShape(20.dp))
                            .background(actionGrad)
                            .clickable { onConfirm() }
                            .padding(vertical = 12.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Text("完成", fontSize = 16.sp, fontWeight = FontWeight.SemiBold, color = Color.White)
                    }
                }
            }
        }
    }
}

// ─── Picker Wheel ───

@Composable
private fun PickerWheel(
    values: List<Int>,
    selected: Int,
    formatLabel: (Int) -> String,
    onSelected: (Int) -> Unit,
    isDark: Boolean
) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .height(180.dp)
            .clip(RoundedCornerShape(16.dp))
            .background(
                (if (isDark) KikariaColors.MistDark else KikariaColors.Mist).copy(alpha = 0.4f)
            ),
        contentAlignment = Alignment.Center
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.Center,
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Previous
            Box(
                modifier = Modifier
                    .clip(RoundedCornerShape(12.dp))
                    .clickable {
                        val idx = values.indexOf(selected)
                        if (idx > 0) onSelected(values[idx - 1])
                    }
                    .padding(12.dp)
            ) {
                Text("‹", fontSize = 22.sp, fontWeight = FontWeight.Bold, color = deepText)
            }

            Spacer(modifier = Modifier.width(16.dp))

            Text(
                text = KikariaTypography.mixedText(formatLabel(selected), size = 20, weight = FontWeight.SemiBold),
                color = deepText
            )

            Spacer(modifier = Modifier.width(16.dp))

            // Next
            Box(
                modifier = Modifier
                    .clip(RoundedCornerShape(12.dp))
                    .clickable {
                        val idx = values.indexOf(selected)
                        if (idx < values.size - 1) onSelected(values[idx + 1])
                    }
                    .padding(12.dp)
            ) {
                Text("›", fontSize = 22.sp, fontWeight = FontWeight.Bold, color = deepText)
            }
        }
    }
}

// ─── Date Range Picker ───

@Composable
private fun DateRangePicker(
    startDate: Date,
    endDate: Date,
    onStartDateChanged: (Date) -> Unit,
    onEndDateChanged: (Date) -> Unit,
    isDark: Boolean
) {
    Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
        DateField(
            label = "开始日期",
            date = startDate,
            onDateChanged = onStartDateChanged,
            isDark = isDark
        )
        DateField(
            label = "结束日期",
            date = endDate,
            onDateChanged = onEndDateChanged,
            isDark = isDark
        )
    }
}

@Composable
private fun DateField(
    label: String,
    date: Date,
    onDateChanged: (Date) -> Unit,
    isDark: Boolean
) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val cal = Calendar.getInstance().apply { time = date }
    val year = cal.get(Calendar.YEAR)
    val month = cal.get(Calendar.MONTH) + 1
    val day = cal.get(Calendar.DAY_OF_MONTH)

    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(
            text = label,
            fontSize = 13.sp,
            fontWeight = FontWeight.SemiBold,
            color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
        )
        Row(
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Month
            Box(
                modifier = Modifier
                    .clip(RoundedCornerShape(8.dp))
                    .clickable {
                        cal.add(Calendar.MONTH, -1)
                        onDateChanged(cal.time)
                    }
                    .padding(8.dp)
            ) { Text("‹", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = deepText) }
            Text(
                text = KikariaTypography.mixedText("${year}年${month}月${day}日", size = 14, weight = FontWeight.Medium),
                color = deepText
            )
            Box(
                modifier = Modifier
                    .clip(RoundedCornerShape(8.dp))
                    .clickable {
                        cal.add(Calendar.MONTH, 1)
                        onDateChanged(cal.time)
                    }
                    .padding(8.dp)
            ) { Text("›", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = deepText) }
        }
    }
}

// ─── Time Picker ───

@Composable
private fun TimePicker(
    hour: Int,
    minute: Int,
    onHourChanged: (Int) -> Unit,
    onMinuteChanged: (Int) -> Unit,
    isDark: Boolean
) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText

    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.Center,
        verticalAlignment = Alignment.CenterVertically
    ) {
        // Hours
        Box(
            modifier = Modifier
                .clip(RoundedCornerShape(8.dp))
                .clickable { onHourChanged(if (hour > 0) hour - 1 else 23) }
                .padding(8.dp)
        ) { Text("‹", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = deepText) }
        Text(
            text = KikariaTypography.mixedText(String.format("%02d", hour), size = 28, weight = FontWeight.Bold),
            color = deepText,
            modifier = Modifier.padding(horizontal = 12.dp)
        )
        Box(
            modifier = Modifier
                .clip(RoundedCornerShape(8.dp))
                .clickable { onHourChanged(if (hour < 23) hour + 1 else 0) }
                .padding(8.dp)
        ) { Text("›", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = deepText) }

        Text(":", fontSize = 28.sp, fontWeight = FontWeight.Bold,
            color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText)

        // Minutes
        Box(
            modifier = Modifier
                .clip(RoundedCornerShape(8.dp))
                .clickable { onMinuteChanged(if (minute > 0) minute - 1 else 59) }
                .padding(8.dp)
        ) { Text("‹", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = deepText) }
        Text(
            text = KikariaTypography.mixedText(String.format("%02d", minute), size = 28, weight = FontWeight.Bold),
            color = deepText,
            modifier = Modifier.padding(horizontal = 12.dp)
        )
        Box(
            modifier = Modifier
                .clip(RoundedCornerShape(8.dp))
                .clickable { onMinuteChanged(if (minute < 59) minute + 1 else 0) }
                .padding(8.dp)
        ) { Text("›", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = deepText) }
    }
}
