package com.vita0818.kikaria.ui.settings

import android.content.Intent
import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaFormPageShell
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.components.KikariaProfileAvatar
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics

@Composable
fun EditProfileScreen(
    initialDisplayName: String,
    initialHandle: String,
    initialAvatarUri: String? = null,
    onBack: () -> Unit,
    onSave: (displayName: String, handle: String) -> Unit,
    onAvatarChanged: ((String?) -> Unit)? = null
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val context = LocalContext.current

    var displayName by remember { mutableStateOf(initialDisplayName) }
    var userHandle by remember { mutableStateOf(initialHandle) }
    var avatarUri by remember { mutableStateOf(initialAvatarUri) }

    val imagePicker = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.OpenDocument()
    ) { uri: Uri? ->
        if (uri != null) {
            try {
                context.contentResolver.takePersistableUriPermission(
                    uri,
                    Intent.FLAG_GRANT_READ_URI_PERMISSION
                )
            } catch (_: SecurityException) {
            }
            val uriStr = uri.toString()
            avatarUri = uriStr
            onAvatarChanged?.invoke(uriStr)
        }
    }

    val metrics = rememberKikariaPhoneMetrics()

    KikariaFormPageShell(
        title = "编辑个人资料",
        onBack = onBack,
        metrics = metrics,
        closeIcon = KikariaIcons.back,
        actionLabel = "保存",
        onAction = {
            val tn = displayName.trim()
            val th = userHandle.trim().trimStart('@')
            onSave(tn.ifEmpty { "Kikaria" }, th.ifEmpty { "user" })
            onBack()
        }
    ) {
        Spacer(modifier = Modifier.height(12.dp))

        Column(Modifier.fillMaxWidth(), horizontalAlignment = Alignment.CenterHorizontally) {
            KikariaProfileAvatar(size = 92.dp, displayName = displayName, avatarUri = avatarUri)

            Spacer(modifier = Modifier.height(10.dp))

            Box(
                modifier = Modifier
                    .clip(RoundedCornerShape(16.dp))
                    .background(
                        if (isDark) KikariaColors.GlassSurfaceDark.copy(alpha = 0.44f)
                        else KikariaColors.GlassSurface.copy(alpha = 0.44f)
                    )
                    .clickable { imagePicker.launch(arrayOf("image/*")) }
                    .padding(horizontal = 18.dp, vertical = 11.dp)
            ) {
                Text(
                    "更换头像",
                    fontSize = 14.sp, fontWeight = FontWeight.SemiBold, color = deepText
                )
            }
        }

        Spacer(modifier = Modifier.height(18.dp))

        ProfileTextField("显示名称", displayName, { displayName = it }, isDark)
        Spacer(modifier = Modifier.height(10.dp))
        ProfileTextField("用户 ID", userHandle, { userHandle = it }, isDark)
        Spacer(modifier = Modifier.height(24.dp))
    }
}

@Composable
private fun ProfileTextField(title: String, value: String, onValueChange: (String) -> Unit, isDark: Boolean) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText

    Column {
        Text(
            KikariaTypography.mixedText(title, size = 14, weight = FontWeight.SemiBold),
            color = softText, modifier = Modifier.padding(start = 4.dp, bottom = 8.dp)
        )
        KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 20.dp, fillOpacity = 0.50f, shadowElevation = 12.dp, shadowOpacity = 0.08f) {
            TextField(
                value = value, onValueChange = onValueChange,
                textStyle = TextStyle(fontSize = 16.sp, fontWeight = FontWeight.Normal, color = deepText),
                colors = TextFieldDefaults.colors(
                    focusedContainerColor = Color.Transparent, unfocusedContainerColor = Color.Transparent,
                    focusedIndicatorColor = Color.Transparent, unfocusedIndicatorColor = Color.Transparent,
                    cursorColor = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
                ),
                keyboardOptions = KeyboardOptions(capitalization = KeyboardCapitalization.None),
                modifier = Modifier.fillMaxWidth(), singleLine = true,
                placeholder = { Text(title, fontSize = 16.sp, fontWeight = FontWeight.Normal, color = softText.copy(alpha = 0.5f)) }
            )
        }
    }
}
