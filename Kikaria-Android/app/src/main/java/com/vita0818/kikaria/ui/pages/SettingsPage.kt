package com.vita0818.kikaria.ui.pages

import android.Manifest
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
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
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.PhotoCamera
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Icon
import androidx.compose.material3.Switch
import androidx.compose.material3.SwitchDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.navigation.NavController
import com.vita0818.kikaria.AppModel
import com.vita0818.kikaria.Routes
import com.vita0818.kikaria.data.AppStore
import com.vita0818.kikaria.notif.Notifications
import com.vita0818.kikaria.ui.GlassCard
import com.vita0818.kikaria.ui.GlassIconButton
import com.vita0818.kikaria.ui.PageHeader
import com.vita0818.kikaria.ui.ProfileAvatar
import com.vita0818.kikaria.ui.theme.kikariaColors
import java.util.Calendar

/** 设置页:预设/学习目标/倒数日/安全线/通知/帮助/关于。 */
@Composable
fun SettingsPage(navController: NavController) {
    val colors = kikariaColors()
    val context = LocalContext.current
    var showPrivacy by remember { mutableStateOf(false) }
    var picker by remember { mutableStateOf<String?>(null) } // goal/danger/countdown/time

    val permissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission(),
    ) { granted ->
        AppModel.notificationsEnabled = granted
        if (!granted) AppModel.showToast("请在系统设置中允许通知")
        AppModel.scheduleStudyStatePersistence()
        AppModel.rescheduleWarningNotification()
    }

    Column(
        Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 24.dp),
    ) {
        PageHeader(
            title = "设置",
            onBack = { navController.popBackStack() },
            trailing = { GlassIconButton(icon = Icons.Filled.Close, onClick = { navController.popBackStack() }) },
        )

        // 资料区
        GlassCard(cornerRadius = 28, fillAlpha = 0.44f, modifier = Modifier.fillMaxWidth()) {
            Column(Modifier.fillMaxWidth().padding(20.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                ProfileAvatar(size = 86) { navController.navigate(Routes.EDIT_PROFILE) }
                Spacer(Modifier.height(12.dp))
                Text(AppModel.userProfile.displayName, color = colors.deepText, fontSize = 28.sp, fontWeight = FontWeight.SemiBold)
                Spacer(Modifier.height(2.dp))
                Text("@${AppModel.userProfile.userHandle}", color = colors.softText, fontSize = 15.sp, fontWeight = FontWeight.Medium)
                Spacer(Modifier.height(14.dp))
                GlassCard(cornerRadius = 50, fillAlpha = 0.36f, strokeAlpha = 0.3f) {
                    Text(
                        "编辑个人资料",
                        color = colors.deepText,
                        fontSize = 15.sp,
                        fontWeight = FontWeight.SemiBold,
                        modifier = Modifier
                            .clickable { navController.navigate(Routes.EDIT_PROFILE) }
                            .padding(horizontal = 18.dp, vertical = 9.dp),
                    )
                }
            }
        }
        Spacer(Modifier.height(16.dp))

        SettingsSectionCard("当前预设") {
            SettingsListRow(title = "预设", value = AppModel.currentPreset?.name ?: "", onClick = null)
        }
        Spacer(Modifier.height(14.dp))

        SettingsSectionCard("学习") {
            SettingsListRow(title = "每日学习目标", value = "${AppModel.dailyGoal} 个") { picker = "goal" }
            SettingsDivider()
            val countdown = AppModel.countdownDayCount
            SettingsListRow(
                title = "倒数日",
                value = countdown?.let { "$it 天" } ?: "未设置",
            ) { picker = "countdown" }
            SettingsDivider()
            SettingsListRow(title = "进度安全线", value = "${AppModel.dangerPercent}%") { picker = "danger" }
        }
        Spacer(Modifier.height(14.dp))

        SettingsSectionCard("通知") {
            Row(
                Modifier.fillMaxWidth().padding(horizontal = 18.dp, vertical = 14.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text("学习进度通知", color = colors.deepText, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                Switch(
                    checked = AppModel.notificationsEnabled,
                    onCheckedChange = { enabled ->
                        if (enabled) {
                            if (Build.VERSION.SDK_INT >= 33 && !Notifications.hasPermission(context)) {
                                permissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
                            } else {
                                AppModel.notificationsEnabled = true
                                AppModel.scheduleStudyStatePersistence()
                                AppModel.rescheduleWarningNotification()
                            }
                        } else {
                            AppModel.notificationsEnabled = false
                            AppModel.scheduleStudyStatePersistence()
                            AppModel.rescheduleWarningNotification()
                        }
                    },
                    colors = SwitchDefaults.colors(checkedTrackColor = colors.sky),
                )
            }
            if (AppModel.notificationsEnabled) {
                SettingsDivider()
                SettingsListRow(
                    title = "通知时间",
                    value = String.format("%02d:%02d", AppModel.notificationTimeHour, AppModel.notificationTimeMinute),
                ) { picker = "time" }
                if (AppModel.countdownStartDate == null || AppModel.countdownEndDate == null) {
                    SettingsDivider()
                    Text(
                        "需设置倒数日",
                        color = colors.softText,
                        fontSize = 13.sp,
                        modifier = Modifier.padding(horizontal = 18.dp, vertical = 10.dp),
                    )
                }
            }
        }
        Spacer(Modifier.height(14.dp))

        SettingsSectionCard("帮助") {
            SettingsListRow(title = "新手引导", value = "", onClick = {
                AppModel.isShowingOnboarding = true
            })
            SettingsDivider()
            SettingsListRow(title = "Markdown 格式", value = "") { navController.navigate(Routes.MARKDOWN_GUIDE) }
        }
        Spacer(Modifier.height(14.dp))

        SettingsSectionCard("关于") {
            SettingsListRow(title = "隐私政策", value = "") { showPrivacy = true }
            SettingsDivider()
            SettingsListRow(title = "版权声明", value = "© 2026 Vita", onClick = null)
            SettingsDivider()
            SettingsListRow(title = "版本", value = "1.0.0 (1)", onClick = null)
            SettingsDivider()
            SettingsListRow(title = "备案号", value = "浙ICP备2026034004号", onClick = null)
        }
        Spacer(Modifier.height(30.dp))
    }

    when (picker) {
        "goal" -> NumberPickerDialog(
            title = "每日学习目标",
            range = 1..100,
            initial = AppModel.dailyGoal,
            suffix = " 个",
        ) { value ->
            AppModel.dailyGoal = value
            AppModel.scheduleStudyStatePersistence()
            AppModel.rescheduleWarningNotification()
            picker = null
        }
        "danger" -> NumberPickerDialog(
            title = "进度安全线",
            range = 1..100,
            initial = AppModel.dangerPercent,
            suffix = "%",
        ) { value ->
            AppModel.dangerPercent = value
            AppModel.scheduleStudyStatePersistence()
            AppModel.rescheduleWarningNotification()
            picker = null
        }
        "time" -> TimePickerDialog(
            initialHour = AppModel.notificationTimeHour,
            initialMinute = AppModel.notificationTimeMinute,
        ) { hour, minute ->
            AppModel.notificationTimeHour = hour
            AppModel.notificationTimeMinute = minute
            AppModel.scheduleStudyStatePersistence()
            AppModel.rescheduleWarningNotification()
            picker = null
        }
        "countdown" -> CountdownDialog(
            startDate = AppModel.countdownStartDate,
            endDate = AppModel.countdownEndDate,
        ) { start, end ->
            AppModel.countdownStartDate = start
            AppModel.countdownEndDate = end
            AppModel.scheduleStudyStatePersistence()
            AppModel.rescheduleWarningNotification()
            picker = null
        }
    }

    if (showPrivacy) {
        AlertDialog(
            onDismissRequest = { showPrivacy = false },
            title = { Text("隐私政策") },
            text = {
                Text("Kikaria 当前仅在本机保存你的学习资料、预设、头像和学习进度。学习进度通知使用本地通知，不会上传到服务器。")
            },
            confirmButton = {
                TextButton(onClick = { showPrivacy = false }) { Text("知道了") }
            },
        )
    }
}

@Composable
fun SettingsSectionCard(title: String, content: @Composable () -> Unit) {
    val colors = kikariaColors()
    GlassCard(cornerRadius = 28, fillAlpha = 0.44f, modifier = Modifier.fillMaxWidth()) {
        Column(Modifier.fillMaxWidth().padding(vertical = 12.dp)) {
            Text(
                title,
                color = colors.softText,
                fontSize = 13.sp,
                fontWeight = FontWeight.SemiBold,
                modifier = Modifier.padding(horizontal = 18.dp),
            )
            Spacer(Modifier.height(4.dp))
            content()
        }
    }
}

@Composable
fun SettingsListRow(title: String, value: String, onClick: (() -> Unit)?) {
    val colors = kikariaColors()
    Row(
        Modifier
            .fillMaxWidth()
            .then(if (onClick != null) Modifier.clickable { onClick() } else Modifier)
            .padding(horizontal = 18.dp, vertical = 15.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(title, color = colors.deepText, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
        if (value.isNotEmpty()) {
            Text(value, color = colors.sky, fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
        }
        if (onClick != null) {
            Spacer(Modifier.width(4.dp))
            Icon(Icons.AutoMirrored.Filled.KeyboardArrowRight, contentDescription = null, tint = colors.blueGray)
        }
    }
}

@Composable
fun SettingsDivider() {
    val colors = kikariaColors()
    Box(
        Modifier
            .fillMaxWidth()
            .padding(start = 18.dp)
            .height(1.dp)
            .background(colors.blueGray.copy(alpha = 0.10f)),
    )
}

/** 数字滚轮对话框(1-100)。 */
@Composable
fun NumberPickerDialog(
    title: String,
    range: IntRange,
    initial: Int,
    suffix: String,
    onConfirm: (Int) -> Unit,
) {
    var selected by remember { mutableStateOf(initial.coerceIn(range.first, range.last)) }
    AlertDialog(
        onDismissRequest = { onConfirm(selected) },
        title = { Text(title) },
        text = {
            LazyColumn(
                modifier = Modifier.fillMaxWidth().height(220.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
            ) {
                items(count = range.last - range.first + 1) { i ->
                    val value = range.first + i
                    val colors = kikariaColors()
                    Text(
                        "$value$suffix",
                        color = if (value == selected) colors.sky else colors.softText,
                        fontSize = if (value == selected) 22.sp else 16.sp,
                        fontWeight = if (value == selected) FontWeight.Bold else FontWeight.Normal,
                        modifier = Modifier
                            .fillMaxWidth()
                            .clickable { selected = value }
                            .padding(vertical = 8.dp),
                        textAlign = androidx.compose.ui.text.style.TextAlign.Center,
                    )
                }
            }
        },
        confirmButton = {
            TextButton(onClick = { onConfirm(selected) }) { Text("完成") }
        },
    )
}

/** 时:分 对话框。 */
@Composable
fun TimePickerDialog(initialHour: Int, initialMinute: Int, onConfirm: (Int, Int) -> Unit) {
    var hour by remember { mutableStateOf(initialHour) }
    var minute by remember { mutableStateOf(initialMinute) }
    val colors = kikariaColors()
    AlertDialog(
        onDismissRequest = { onConfirm(hour, minute) },
        title = { Text("通知时间") },
        text = {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.Center) {
                WheelColumn((0..23).toList(), hour, pad = true) { hour = it }
                Text(":", color = colors.deepText, fontSize = 20.sp, modifier = Modifier.padding(horizontal = 12.dp))
                WheelColumn((0..59).toList(), minute, pad = true) { minute = it }
            }
        },
        confirmButton = {
            TextButton(onClick = { onConfirm(hour, minute) }) { Text("完成") }
        },
    )
}

@Composable
private fun WheelColumn(values: List<Int>, selected: Int, pad: Boolean, onSelect: (Int) -> Unit) {
    val colors = kikariaColors()
    LazyColumn(
        modifier = Modifier.width(68.dp).height(220.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        items(count = values.size) { i ->
            val value = values[i]
            val text = if (pad) String.format("%02d", value) else "$value"
            Text(
                text,
                color = if (value == selected) colors.sky else colors.softText,
                fontSize = if (value == selected) 20.sp else 15.sp,
                fontWeight = if (value == selected) FontWeight.Bold else FontWeight.Normal,
                modifier = Modifier
                    .fillMaxWidth()
                    .clickable { onSelect(value) }
                    .padding(vertical = 6.dp),
                textAlign = androidx.compose.ui.text.style.TextAlign.Center,
            )
        }
    }
}

/** 倒数日设置:开始/结束 年月日 + 清除。 */
@Composable
fun CountdownDialog(
    startDate: Long?,
    endDate: Long?,
    onConfirm: (Long?, Long?) -> Unit,
) {
    val colors = kikariaColors()
    val startCal = Calendar.getInstance().apply { startDate?.let { timeInMillis = it } }
    val endCal = Calendar.getInstance().apply { endDate?.let { timeInMillis = it } }
    var startYear by remember { mutableStateOf(startCal.get(Calendar.YEAR)) }
    var startMonth by remember { mutableStateOf(startCal.get(Calendar.MONTH) + 1) }
    var startDay by remember { mutableStateOf(startCal.get(Calendar.DAY_OF_MONTH)) }
    var endYear by remember { mutableStateOf(endCal.get(Calendar.YEAR)) }
    var endMonth by remember { mutableStateOf(endCal.get(Calendar.MONTH) + 1) }
    var endDay by remember { mutableStateOf(endCal.get(Calendar.DAY_OF_MONTH)) }
    var error by remember { mutableStateOf(false) }

    fun dateOf(y: Int, m: Int, d: Int): Long = Calendar.getInstance().apply {
        set(y, m - 1, d, 0, 0, 0)
        set(Calendar.MILLISECOND, 0)
    }.timeInMillis

    AlertDialog(
        onDismissRequest = { onConfirm(dateOf(startYear, startMonth, startDay), dateOf(endYear, endMonth, endDay)) },
        title = { Text("倒数日") },
        text = {
            Column {
                Text("开始日期", color = colors.softText, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
                Spacer(Modifier.height(6.dp))
                DateWheelRow(startYear, startMonth, startDay) { y, m, d -> startYear = y; startMonth = m; startDay = d }
                Spacer(Modifier.height(14.dp))
                Text("结束日期", color = colors.softText, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
                Spacer(Modifier.height(6.dp))
                DateWheelRow(endYear, endMonth, endDay) { y, m, d -> endYear = y; endMonth = m; endDay = d }
                if (error) {
                    Spacer(Modifier.height(10.dp))
                    Text("结束日期不能早于开始日期。", color = colors.removeCoral, fontSize = 14.sp)
                }
            }
        },
        confirmButton = {
            Row {
                TextButton(onClick = { onConfirm(null, null) }) { Text("清除") }
                TextButton(onClick = {
                    val start = dateOf(startYear, startMonth, startDay)
                    val end = dateOf(endYear, endMonth, endDay)
                    if (end < start) {
                        error = true
                    } else {
                        onConfirm(start, end)
                    }
                }) { Text("完成") }
            }
        },
    )
}

@Composable
private fun DateWheelRow(year: Int, month: Int, day: Int, onChange: (Int, Int, Int) -> Unit) {
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.Center) {
        WheelColumnInt((2020..2035).toList(), year) { onChange(it, month, day) }
        Spacer(Modifier.width(8.dp))
        WheelColumnInt((1..12).toList(), month) { onChange(year, it, day) }
        Spacer(Modifier.width(8.dp))
        WheelColumnInt((1..31).toList(), day) { onChange(year, month, it) }
    }
}

@Composable
private fun WheelColumnInt(values: List<Int>, selected: Int, onSelect: (Int) -> Unit) {
    val colors = kikariaColors()
    LazyColumn(
        modifier = Modifier.width(if (selected > 99) 80.dp else 54.dp).height(160.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        items(count = values.size) { i ->
            val value = values[i]
            Text(
                "$value",
                color = if (value == selected) colors.sky else colors.softText,
                fontSize = if (value == selected) 18.sp else 14.sp,
                fontWeight = if (value == selected) FontWeight.Bold else FontWeight.Normal,
                modifier = Modifier
                    .fillMaxWidth()
                    .clickable { onSelect(value) }
                    .padding(vertical = 4.dp),
                textAlign = androidx.compose.ui.text.style.TextAlign.Center,
            )
        }
    }
}

/** 编辑资料:头像 + 显示名称 + 用户 ID。 */
@Composable
fun EditProfilePage(navController: NavController) {
    val colors = kikariaColors()
    val context = LocalContext.current
    var displayName by remember { mutableStateOf(AppModel.userProfile.displayName) }
    var handle by remember { mutableStateOf(AppModel.userProfile.userHandle) }
    var avatarBase64 by remember { mutableStateOf(AppModel.userProfile.avatarBase64) }

    val photoLauncher = rememberLauncherForActivityResult(ActivityResultContracts.PickVisualMedia()) { uri ->
        if (uri != null) {
            avatarBase64 = runCatching {
                val source = context.contentResolver.openInputStream(uri)?.use {
                    android.graphics.BitmapFactory.decodeStream(it)
                }
                source?.let { compressAvatar(it) }
            }.getOrNull()
        }
    }

    Column(
        Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        PageHeader(
            title = "编辑资料",
            onBack = { navController.popBackStack() },
        )
        Spacer(Modifier.height(10.dp))
        ProfileAvatar(size = 92)
        Spacer(Modifier.height(12.dp))
        GlassCard(cornerRadius = 50, fillAlpha = 0.36f, strokeAlpha = 0.3f) {
            Row(
                Modifier
                    .clickable {
                        photoLauncher.launch(PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly))
                    }
                    .padding(horizontal = 16.dp, vertical = 9.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Icon(Icons.Filled.PhotoCamera, contentDescription = null, tint = colors.sky, modifier = Modifier.size(16.dp))
                Spacer(Modifier.width(6.dp))
                Text("更换头像", color = colors.deepText, fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
            }
        }
        Spacer(Modifier.height(20.dp))
        ProfileField("显示名称", displayName) { displayName = it }
        Spacer(Modifier.height(12.dp))
        ProfileField("用户 ID", handle) { handle = it }
        Spacer(Modifier.height(24.dp))
        GlassCard(cornerRadius = 26, fillAlpha = 0.5f, modifier = Modifier.fillMaxWidth()) {
            Box(
                Modifier
                    .fillMaxWidth()
                    .clickable {
                        AppModel.userProfile = AppModel.userProfile.copy(
                            displayName = displayName.trim().ifEmpty { "Vita" },
                            userHandle = handle.trim().ifEmpty { "vita_0818" },
                            avatarBase64 = avatarBase64,
                        )
                        AppModel.persistNow()
                        navController.popBackStack()
                    }
                    .padding(vertical = 14.dp),
                contentAlignment = Alignment.Center,
            ) {
                Text("保存", color = colors.sky, fontSize = 17.sp, fontWeight = FontWeight.SemiBold)
            }
        }
        Spacer(Modifier.height(30.dp))
    }
}
