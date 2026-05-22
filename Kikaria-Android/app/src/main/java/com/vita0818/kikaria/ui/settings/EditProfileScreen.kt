package com.vita0818.kikaria.ui.settings

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
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
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardCapitalization
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaBackButton
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaProfileAvatar
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography

/**
 * Edit profile screen translated from the iOS EditProfileView in ContentView.swift.
 *
 * Allows editing the user's display name and handle, with avatar display.
 */
@Composable
fun EditProfileScreen(
    initialDisplayName: String,
    initialHandle: String,
    onBack: () -> Unit,
    onSave: (displayName: String, handle: String) -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky

    var displayName by remember { mutableStateOf(initialDisplayName) }
    var userHandle by remember { mutableStateOf(initialHandle) }

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
                // Title row with save button
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = KikariaTypography.mixedText(
                            "编辑个人资料",
                            size = 24,
                            weight = FontWeight.Bold
                        ),
                        color = deepText,
                        modifier = Modifier.weight(1f)
                    )

                    Box(
                        modifier = Modifier
                            .clip(RoundedCornerShape(14.dp))
                            .clickable {
                                val trimmedName = displayName.trim()
                                val trimmedHandle = userHandle.trim().trimStart('@')
                                onSave(
                                    trimmedName.ifEmpty { "Kikaria" },
                                    trimmedHandle.ifEmpty { "user" }
                                )
                                onBack()
                            }
                            .background(sky.copy(alpha = 0.18f))
                            .padding(horizontal = 16.dp, vertical = 10.dp)
                    ) {
                        Text(
                            "保存",
                            fontSize = 15.sp,
                            fontWeight = FontWeight.SemiBold,
                            color = sky
                        )
                    }
                }

                Spacer(modifier = Modifier.height(24.dp))

                // Avatar section
                Column(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    KikariaProfileAvatar(
                        size = 92.dp,
                        displayName = displayName
                    )

                    Spacer(modifier = Modifier.height(14.dp))

                    Text(
                        text = "更换头像",
                        fontSize = 14.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = deepText,
                        modifier = Modifier
                            .clip(RoundedCornerShape(20.dp))
                            .background(
                                (if (isDark) KikariaColors.GlassSurfaceDark
                                else KikariaColors.GlassSurface).copy(alpha = 0.38f)
                            )
                            .padding(horizontal = 18.dp, vertical = 11.dp)
                    )
                }

                Spacer(modifier = Modifier.height(24.dp))

                // Display name field
                ProfileTextField(
                    title = "显示名称",
                    value = displayName,
                    onValueChange = { displayName = it },
                    isDark = isDark
                )

                Spacer(modifier = Modifier.height(14.dp))

                // User handle field
                ProfileTextField(
                    title = "用户 ID",
                    value = userHandle,
                    onValueChange = { userHandle = it },
                    isDark = isDark
                )

                Spacer(modifier = Modifier.height(32.dp))
            }
        }
    }
}

// ─── Profile Text Field ───

@Composable
private fun ProfileTextField(
    title: String,
    value: String,
    onValueChange: (String) -> Unit,
    isDark: Boolean
) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface

    Column {
        Text(
            text = KikariaTypography.mixedText(
                title,
                size = 14,
                weight = FontWeight.SemiBold
            ),
            color = softText,
            modifier = Modifier.padding(start = 4.dp, bottom = 8.dp)
        )

        val shape = RoundedCornerShape(20.dp)
        KikariaGlassCard(
            modifier = Modifier.fillMaxWidth(),
            cornerRadius = 20.dp,
            fillOpacity = 0.50f,
            shadowElevation = 12.dp,
            shadowOpacity = 0.08f
        ) {
            TextField(
                value = value,
                onValueChange = onValueChange,
                textStyle = TextStyle(
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Normal,
                    color = deepText
                ),
                colors = TextFieldDefaults.colors(
                    focusedContainerColor = Color.Transparent,
                    unfocusedContainerColor = Color.Transparent,
                    focusedIndicatorColor = Color.Transparent,
                    unfocusedIndicatorColor = Color.Transparent,
                    cursorColor = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
                ),
                keyboardOptions = KeyboardOptions(
                    capitalization = KeyboardCapitalization.None
                ),
                modifier = Modifier.fillMaxWidth(),
                singleLine = true,
                placeholder = {
                    Text(
                        text = title,
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Normal,
                        color = softText.copy(alpha = 0.5f)
                    )
                }
            )
        }
    }
}
