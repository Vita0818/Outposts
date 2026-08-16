package com.vita0818.kikaria.ui.settings

import android.Manifest
import android.content.pm.PackageManager
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
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
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.animation.core.animateDpAsState
import androidx.compose.animation.core.tween
import androidx.compose.material3.Icon
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
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.content.ContextCompat
import com.vita0818.kikaria.ui.components.KikariaFormPageShell
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.components.KikariaProfileAvatar
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics

@Composable
fun SettingsScreen(
    userDisplayName: String, userHandle: String, presetName: String,
    avatarUri: String? = null,
    dailyGoal: Int, countdownDays: Int, countdownEndDate: Date?, dangerPercent: Int,
    notificationsEnabled: Boolean, notificationTimeText: String,
    onBack: () -> Unit, onEditProfile: () -> Unit,
    onSetDailyGoal: (Int) -> Unit = {}, onSetCountdownRange: (Date?, Date?) -> Unit = { _, _ -> },
    onSetDangerPercent: (Int) -> Unit = {}, onToggleNotifications: (Boolean) -> Unit = {},
    onNotificationPermissionDenied: () -> Unit = {},
    onSetNotificationTime: (String) -> Unit = {}, onOpenOnboarding: () -> Unit = {},
    onOpenMarkdownGuide: () -> Unit = {}, onOpenPrivacyPolicy: () -> Unit = {},
    onOpenPresetSelection: () -> Unit = {}
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val context = LocalContext.current

    var showDailyGoalPicker by remember { mutableStateOf(false) }
    var showCountdownPicker by remember { mutableStateOf(false) }
    var showDangerPicker by remember { mutableStateOf(false) }
    var showNotificationTimePicker by remember { mutableStateOf(false) }
    var countdownDraftStart by remember { mutableStateOf(countdownEndDate ?: Date()) }
    var countdownDraftEnd by remember { mutableStateOf(countdownEndDate ?: Date()) }
    var draftGoal by remember { mutableStateOf(dailyGoal) }
    var draftDanger by remember { mutableStateOf(dangerPercent) }
    var draftHour by remember { mutableStateOf(notificationTimeText.split(":").firstOrNull()?.toIntOrNull() ?: 21) }
    var draftMinute by remember { mutableStateOf(notificationTimeText.split(":").getOrNull(1)?.toIntOrNull() ?: 0) }
    val notificationPermissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted ->
        if (isGranted) {
            onToggleNotifications(true)
        } else {
            onNotificationPermissionDenied()
        }
    }
    val handleNotificationToggle: (Boolean) -> Unit = { enabled ->
        if (!enabled) {
            onToggleNotifications(false)
        } else if (
            Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) !=
            PackageManager.PERMISSION_GRANTED
        ) {
            notificationPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
        } else {
            onToggleNotifications(true)
        }
    }

    val metrics = rememberKikariaPhoneMetrics()
    val sScale = maxOf(metrics.settingsScale, 1f)
    val srScale = maxOf(metrics.settingsRowScale, 1f)

    KikariaFormPageShell(title = "设置", onBack = onBack, metrics = metrics, closeIcon = KikariaIcons.close) {
        Spacer(modifier = Modifier.height(12.dp))

        // Profile Section
        KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 28.dp, fillOpacity = 0.44f) {
            Column(Modifier.fillMaxWidth().padding((24 * sScale).dp), horizontalAlignment = Alignment.CenterHorizontally) {
                KikariaProfileAvatar(
                    size = (86 * sScale).dp,
                    displayName = userDisplayName,
                    avatarUri = avatarUri
                )
                Spacer(Modifier.height((8 * sScale).dp))
                Text(KikariaTypography.mixedText(userDisplayName.ifEmpty { "Kikaria" }, size = (28 * sScale).toInt(), weight = FontWeight.SemiBold), color = deepText)
                Spacer(Modifier.height((4 * sScale).dp))
                Text("@${userHandle.ifEmpty { "user" }}", fontSize = (15 * sScale).sp, fontWeight = FontWeight.Medium, color = softText)
                Spacer(Modifier.height((14 * sScale).dp))
                SettingsButton("编辑个人资料", false, buttonScale = sScale) { onEditProfile() }
            }
        }

        Spacer(Modifier.height(8.dp))

        // Current Preset (matches iOS currentPresetOnlySection)
        SettingsSection("当前预设", scale = sScale) {
            SettingsRow("当前预设", presetName, false, scale = srScale) { onOpenPresetSelection() }
        }

        Spacer(Modifier.height(8.dp))

        // Learning Settings (matches iOS learningSettingsSection)
        SettingsSection("学习", scale = sScale) {
            SettingsRow("每日学习目标", "$dailyGoal", scale = srScale) { showDailyGoalPicker = true }
            SettingsDivider()
            SettingsRow("倒数日", if (countdownDays > 0) "${countdownDays}天" else "未设置", scale = srScale) { showCountdownPicker = true }
            SettingsDivider()
            SettingsRow("进度安全线", "${dangerPercent}%", scale = srScale) { showDangerPicker = true }
        }

        Spacer(Modifier.height(8.dp))

        // Notification
        SettingsSection("通知", scale = sScale) {
            SettingsToggleRow("学习进度通知", notificationsEnabled, scale = srScale, onToggle = handleNotificationToggle)
            if (notificationsEnabled) { SettingsDivider(); SettingsRow("通知时间", notificationTimeText, scale = srScale) { showNotificationTimePicker = true } }
        }

        Spacer(Modifier.height(8.dp))

        // Help
        SettingsSection("帮助", scale = sScale) {
            SettingsRow("新手引导", "", scale = srScale) { onOpenOnboarding() }
            SettingsDivider()
            SettingsRow("Markdown 格式", "", scale = srScale) { onOpenMarkdownGuide() }
        }

        Spacer(Modifier.height(8.dp))

        // About
        SettingsSection("关于", scale = sScale) {
            SettingsRow("隐私政策", "", scale = srScale) { onOpenPrivacyPolicy() }
            SettingsDivider()
            SettingsRow("版权声明", "© 2026 Vita", false, scale = srScale) {}
            SettingsDivider()
            SettingsRow("版本", "0.1.0", false, scale = srScale) {}
        }

        Spacer(Modifier.height(32.dp))

        // Picker dialogs
        if (showDailyGoalPicker) PickerDialog("每日学习目标", "$draftGoal", isDark, { showDailyGoalPicker = false }, { onSetDailyGoal(draftGoal); showDailyGoalPicker = false }) {
            PickerWheel((1..100).toList(), draftGoal, { "$it 个" }, { draftGoal = it }, isDark)
        }
        if (showCountdownPicker) PickerDialog("倒数日", if (countdownDays > 0) "${countdownDays}天" else "未设置", isDark, { showCountdownPicker = false }, { onSetCountdownRange(countdownDraftStart, countdownDraftEnd); showCountdownPicker = false }, { onSetCountdownRange(null, null); showCountdownPicker = false }) {
            DateRangePicker(countdownDraftStart, countdownDraftEnd, { countdownDraftStart = it }, { countdownDraftEnd = it }, isDark)
        }
        if (showDangerPicker) PickerDialog("进度安全线", "$draftDanger%", isDark, { showDangerPicker = false }, { onSetDangerPercent(draftDanger); showDangerPicker = false }) {
            PickerWheel((1..100).toList(), draftDanger, { "$it%" }, { draftDanger = it }, isDark)
        }
        if (showNotificationTimePicker) PickerDialog("通知时间", notificationTimeText, isDark, { showNotificationTimePicker = false }, {
            onSetNotificationTime(String.format("%02d:%02d", draftHour, draftMinute)); showNotificationTimePicker = false
        }) {
            TimePicker(draftHour, draftMinute, { draftHour = it }, { draftMinute = it }, isDark)
        }
    }
}

@Composable
private fun SettingsSection(title: String, scale: Float = 1f, content: @Composable () -> Unit) {
    val isDark = isSystemInDarkTheme()
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    Column {
        Text(title, fontSize = (13 * scale).sp, fontWeight = FontWeight.SemiBold, color = softText, modifier = Modifier.padding(start = 4.dp, bottom = (8 * scale).dp))
        KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 22.dp, fillOpacity = 0.40f) { Column(Modifier.padding(4.dp)) { content() } }
    }
}
@Composable
private fun SettingsRow(title: String, value: String, showChevron: Boolean = true, scale: Float = 1f, onClick: () -> Unit) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    Row(Modifier.fillMaxWidth().clickable { onClick() }.padding(horizontal = (18 * scale).dp, vertical = (16 * scale).dp), verticalAlignment = Alignment.CenterVertically) {
        Text(KikariaTypography.mixedText(title, size = (17 * scale).toInt(), weight = FontWeight.Medium), color = deepText, modifier = Modifier.weight(1f))
        if (value.isNotEmpty()) Text(value, fontSize = (15 * scale).sp, fontWeight = FontWeight.Medium, color = softText, modifier = Modifier.padding(end = (8 * scale).dp))
        if (showChevron) Icon(
            imageVector = KikariaIcons.forward,
            contentDescription = "打开",
            modifier = Modifier.size((18 * scale).dp),
            tint = (if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray).copy(alpha = 0.52f)
        )
    }
}
@Composable
private fun SettingsToggleRow(title: String, isOn: Boolean, scale: Float = 1f, onToggle: (Boolean) -> Unit) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val thumbOffset by animateDpAsState(
        targetValue = if (isOn) 20.dp else 0.dp,
        animationSpec = tween(200)
    )
    Row(Modifier.fillMaxWidth().padding(horizontal = (18 * scale).dp, vertical = (16 * scale).dp), verticalAlignment = Alignment.CenterVertically) {
        Text(KikariaTypography.mixedText(title, size = (17 * scale).toInt(), weight = FontWeight.Medium), color = deepText, modifier = Modifier.weight(1f))
        Box(Modifier.size(51.dp, 31.dp).clip(RoundedCornerShape(16.dp)).background(if (isOn) sky else (if (isDark) KikariaColors.MistDark else KikariaColors.Mist)).clickable { onToggle(!isOn) }, contentAlignment = Alignment.CenterStart) {
            Box(Modifier.offset(x = thumbOffset).padding(4.dp).size(23.dp).clip(CircleShape).background(Color.White).shadow(2.dp, CircleShape))
        }
    }
}
@Composable
private fun SettingsDivider() {
    val isDark = isSystemInDarkTheme()
    Box(Modifier.fillMaxWidth().padding(horizontal = 16.dp).height(0.5.dp).background((if (isDark) KikariaColors.BlueGrayDark else KikariaColors.BlueGray).copy(alpha = 0.14f)))
}
@Composable
private fun SettingsButton(text: String, isPrimary: Boolean = true, buttonScale: Float = 1f, onClick: () -> Unit) {
    val isDark = isSystemInDarkTheme()
    val scale = maxOf(buttonScale, 1f)
    Box(Modifier.clip(RoundedCornerShape(20.dp)).shadow(12.dp, RoundedCornerShape(20.dp), spotColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.10f)).background(if (isPrimary) { if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight } else { Brush.linearGradient(listOf((if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface).copy(alpha = 0.44f), (if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface).copy(alpha = 0.44f))) }).clickable { onClick() }.padding(horizontal = (24 * scale).dp, vertical = (13 * scale).dp), contentAlignment = Alignment.Center) {
        Text(text, fontSize = (16 * scale).sp, fontWeight = FontWeight.SemiBold, color = if (isPrimary) Color.White else if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText)
    }
}
@Composable
private fun PickerDialog(title: String, valueText: String, isDark: Boolean, onDismiss: () -> Unit, onConfirm: () -> Unit, onClear: (() -> Unit)? = null, content: @Composable () -> Unit) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    Box(Modifier.fillMaxSize().background(Color.Black.copy(alpha = 0.35f)).clickable { onDismiss() }, contentAlignment = Alignment.Center) {
        KikariaGlassCard(Modifier.padding(horizontal = 34.dp).fillMaxWidth(), cornerRadius = 28.dp, fillOpacity = 0.50f) {
            Column(Modifier.padding(18.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                    Text(title, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, color = deepText)
                    Text(valueText, fontSize = 17.sp, fontWeight = FontWeight.SemiBold, color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky)
                }
                Spacer(Modifier.height(8.dp)); content(); Spacer(Modifier.height(8.dp))
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                    if (onClear != null) Box(Modifier.weight(1f).clip(RoundedCornerShape(20.dp)).background((if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface).copy(alpha = 0.44f)).clickable { onClear() }.padding(vertical = 12.dp), contentAlignment = Alignment.Center) { Text("清除", fontSize = 16.sp, fontWeight = FontWeight.SemiBold, color = deepText) }
                    Box(Modifier.weight(1f).clip(RoundedCornerShape(20.dp)).background(actionGrad).clickable { onConfirm() }.padding(vertical = 12.dp), contentAlignment = Alignment.Center) { Text("完成", fontSize = 16.sp, fontWeight = FontWeight.SemiBold, color = Color.White) }
                }
            }
        }
    }
}
@Composable
private fun PickerWheel(values: List<Int>, selected: Int, formatLabel: (Int) -> String, onSelected: (Int) -> Unit, isDark: Boolean) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    var textValue by remember(selected) { mutableStateOf(formatLabel(selected).replace(Regex("[^0-9]"), "")) }
    Box(Modifier.fillMaxWidth().height(180.dp).clip(RoundedCornerShape(16.dp)).background((if (isDark) KikariaColors.MistDark else KikariaColors.Mist).copy(alpha = 0.4f)), contentAlignment = Alignment.Center) {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.Center, verticalAlignment = Alignment.CenterVertically) {
            Box(Modifier.clip(RoundedCornerShape(12.dp)).clickable { val idx = values.indexOf(selected); if (idx > 0) onSelected(values[idx - 1]) }.padding(12.dp)) {
                Icon(
                    imageVector = KikariaIcons.back,
                    contentDescription = "减1",
                    modifier = Modifier.size(22.dp),
                    tint = deepText
                )
            }
            Spacer(Modifier.width(16.dp))
            androidx.compose.foundation.text.BasicTextField(
                value = textValue,
                onValueChange = { newText ->
                    val digits = newText.filter { it.isDigit() }
                    textValue = digits
                    val parsed = digits.toIntOrNull()
                    if (parsed != null) {
                        val clamped = values.minOf { it }.let { min ->
                            values.maxOf { it }.let { max -> parsed.coerceIn(min, max) }
                        }
                        onSelected(clamped)
                    }
                },
                textStyle = TextStyle(
                    fontSize = 20.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = deepText,
                    textAlign = TextAlign.Center
                ),
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                singleLine = true,
                modifier = Modifier.widthIn(min = 60.dp, max = 100.dp)
            )
            Spacer(Modifier.width(16.dp))
            Box(Modifier.clip(RoundedCornerShape(12.dp)).clickable { val idx = values.indexOf(selected); if (idx < values.size - 1) onSelected(values[idx + 1]) }.padding(12.dp)) {
                Icon(
                    imageVector = KikariaIcons.forward,
                    contentDescription = "加1",
                    modifier = Modifier.size(22.dp),
                    tint = deepText
                )
            }
        }
    }
}
@Composable
private fun DateRangePicker(startDate: Date, endDate: Date, onStartDateChanged: (Date) -> Unit, onEndDateChanged: (Date) -> Unit, isDark: Boolean) {
    Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
        DateField("开始日期", startDate, onStartDateChanged, isDark)
        DateField("结束日期", endDate, onEndDateChanged, isDark)
    }
}
@Composable
private fun DateField(label: String, date: Date, onDateChanged: (Date) -> Unit, isDark: Boolean) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val cal = Calendar.getInstance().apply { time = date }
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(label, fontSize = 13.sp, fontWeight = FontWeight.SemiBold, color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText)
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
            Box(Modifier.clip(RoundedCornerShape(8.dp)).clickable { cal.add(Calendar.MONTH, -1); onDateChanged(cal.time) }.padding(8.dp)) {
                Icon(
                    imageVector = KikariaIcons.back,
                    contentDescription = "上月",
                    modifier = Modifier.size(16.dp),
                    tint = deepText
                )
            }
            Text(KikariaTypography.mixedText("${cal.get(Calendar.YEAR)}年${cal.get(Calendar.MONTH)+1}月${cal.get(Calendar.DAY_OF_MONTH)}日", size = 14, weight = FontWeight.Medium), color = deepText)
            Box(Modifier.clip(RoundedCornerShape(8.dp)).clickable { cal.add(Calendar.MONTH, 1); onDateChanged(cal.time) }.padding(8.dp)) {
                Icon(
                    imageVector = KikariaIcons.forward,
                    contentDescription = "下月",
                    modifier = Modifier.size(16.dp),
                    tint = deepText
                )
            }
        }
    }
}
@Composable
private fun TimePicker(hour: Int, minute: Int, onHourChanged: (Int) -> Unit, onMinuteChanged: (Int) -> Unit, isDark: Boolean) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.Center, verticalAlignment = Alignment.CenterVertically) {
        Box(Modifier.clip(RoundedCornerShape(8.dp)).clickable { onHourChanged(if (hour > 0) hour - 1 else 23) }.padding(8.dp)) {
            Icon(
                imageVector = KikariaIcons.back,
                contentDescription = "小时减1",
                modifier = Modifier.size(16.dp),
                tint = deepText
            )
        }
        Text(KikariaTypography.mixedText(String.format("%02d", hour), size = 28, weight = FontWeight.Bold), color = deepText, modifier = Modifier.padding(horizontal = 12.dp))
        Box(Modifier.clip(RoundedCornerShape(8.dp)).clickable { onHourChanged(if (hour < 23) hour + 1 else 0) }.padding(8.dp)) {
            Icon(
                imageVector = KikariaIcons.forward,
                contentDescription = "小时加1",
                modifier = Modifier.size(16.dp),
                tint = deepText
            )
        }
        Text(":", fontSize = 28.sp, fontWeight = FontWeight.Bold, color = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText)
        Box(Modifier.clip(RoundedCornerShape(8.dp)).clickable { onMinuteChanged(if (minute > 0) minute - 1 else 59) }.padding(8.dp)) {
            Icon(
                imageVector = KikariaIcons.back,
                contentDescription = "分钟减1",
                modifier = Modifier.size(16.dp),
                tint = deepText
            )
        }
        Text(KikariaTypography.mixedText(String.format("%02d", minute), size = 28, weight = FontWeight.Bold), color = deepText, modifier = Modifier.padding(horizontal = 12.dp))
        Box(Modifier.clip(RoundedCornerShape(8.dp)).clickable { onMinuteChanged(if (minute < 59) minute + 1 else 0) }.padding(8.dp)) {
            Icon(
                imageVector = KikariaIcons.forward,
                contentDescription = "分钟加1",
                modifier = Modifier.size(16.dp),
                tint = deepText
            )
        }
    }
}
