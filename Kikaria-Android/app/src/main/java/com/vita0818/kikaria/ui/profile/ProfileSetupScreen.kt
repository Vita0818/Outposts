package com.vita0818.kikaria.ui.profile

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.components.KikariaProfileAvatar
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics

/**
 * Profile setup screen matching iOS InitialProfileSetupView (ContentView.swift lines 6098-6338).
 *
 * Apple layout:
 * - "欢迎使用 Kikaria" title + "先设置你的个人资料" subtitle
 * - Large avatar with optional plus button
 * - 昵称 (Display Name) text field
 * - 用户名 (User Handle) text field
 * - "开始使用" action button (disabled until display name is non-empty)
 * - All in a liquid glass card centered vertically
 */
@Composable
fun ProfileSetupScreen(
    initialDisplayName: String,
    initialHandle: String,
    onComplete: (displayName: String, handle: String) -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val metrics = rememberKikariaPhoneMetrics()
    val isExpanded = metrics.isTablet
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val actionGrad = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    val sky = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface

    val avatarSize = if (metrics.isLargePadPortrait) 116.dp
        else if (isExpanded) 108.dp else 88.dp
    val cardMaxWidth = if (metrics.isLargePadPortrait) 500.dp
        else if (isExpanded) 480.dp else 370.dp
    val cardPadding = if (isExpanded) 32.dp else 24.dp

    var displayName by remember { mutableStateOf(
        if (initialDisplayName == "Vita" || initialDisplayName.isEmpty()) "" else initialDisplayName
    ) }
    var userHandle by remember { mutableStateOf(
        if (initialHandle == "vita_0818" || initialHandle.isEmpty()) "" else initialHandle
    ) }

    val canSave = displayName.trim().isNotEmpty()

    KikariaPageShell {
        Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = Alignment.Center
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = metrics.horizontalPadding),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
                // Glass card container — matches Apple liquidGlassCard
                val cardShape = RoundedCornerShape(if (isExpanded) 38.dp else 34.dp)
                Box(
                    modifier = Modifier
                        .widthIn(max = cardMaxWidth)
                        .fillMaxWidth()
                        .shadow(
                            if (isExpanded) 28.dp else 24.dp, cardShape,
                            ambientColor = sky.copy(alpha = 0.16f),
                            spotColor = sky.copy(alpha = 0.16f)
                        )
                        .clip(cardShape)
                        .background(glassSurface.copy(alpha = 0.46f))
                        .padding(cardPadding)
                ) {
                    Column(
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.spacedBy(if (isExpanded) 28.dp else 22.dp)
                    ) {
                        // Title + subtitle
                        Column(
                            horizontalAlignment = Alignment.CenterHorizontally,
                            verticalArrangement = Arrangement.spacedBy(if (isExpanded) 12.dp else 10.dp)
                        ) {
                            Text(
                                text = KikariaTypography.mixedText(
                                    "欢迎使用 Kikaria",
                                    size = if (isExpanded) 34 else 30,
                                    weight = FontWeight.Bold
                                ),
                                color = deepText,
                                textAlign = TextAlign.Center
                            )
                            Text(
                                text = KikariaTypography.mixedText(
                                    "先设置你的个人资料",
                                    size = if (isExpanded) 18 else 16,
                                    weight = FontWeight.Medium
                                ),
                                color = softText,
                                textAlign = TextAlign.Center
                            )
                        }

                        // Avatar
                        KikariaProfileAvatar(
                            size = avatarSize,
                            displayName = displayName.ifEmpty { "K" }
                        )

                        Spacer(modifier = Modifier.height(4.dp))

                        // Text fields
                        Column(
                            verticalArrangement = Arrangement.spacedBy(if (isExpanded) 16.dp else 14.dp)
                        ) {
                            ProfileTextField(
                                label = "昵称",
                                value = displayName,
                                onValueChange = { displayName = it },
                                placeholder = "输入你的昵称",
                                deepText = deepText,
                                softText = softText,
                                glassSurface = glassSurface
                            )
                            ProfileTextField(
                                label = "用户名",
                                value = userHandle,
                                onValueChange = { userHandle = it },
                                placeholder = "输入用户名",
                                deepText = deepText,
                                softText = softText,
                                glassSurface = glassSurface
                            )
                        }

                        // Action button — "开始使用"
                        Box(
                            modifier = Modifier
                                .padding(top = 4.dp)
                                .fillMaxWidth()
                                .shadow(16.dp, RoundedCornerShape(28.dp),
                                    spotColor = sky.copy(alpha = if (canSave) 0.22f else 0.04f))
                                .clip(RoundedCornerShape(28.dp))
                                .background(
                                    if (canSave) actionGrad
                                    else Brush.linearGradient(
                                        listOf(glassSurface.copy(alpha = 0.3f), glassSurface.copy(alpha = 0.3f))
                                    )
                                )
                                .clickable(enabled = canSave) {
                                    val trimmedName = displayName.trim()
                                    if (trimmedName.isNotEmpty()) {
                                        val trimmedHandle = userHandle.trim().trimStart('@')
                                        val finalHandle = trimmedHandle.ifEmpty {
                                            trimmedName.lowercase()
                                                .map { if (it in 'a'..'z' || it in '0'..'9') it else '_' }
                                                .joinToString("").trimEnd('_')
                                                .ifEmpty { "kikaria_user" }
                                        }
                                        onComplete(trimmedName, finalHandle)
                                    }
                                }
                                .padding(vertical = if (isExpanded) 18.dp else 16.dp),
                            contentAlignment = Alignment.Center
                        ) {
                            Text(
                                text = "开始使用",
                                fontSize = if (isExpanded) 18.sp else 17.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = if (canSave) Color.White
                                    else Color.White.copy(alpha = 0.48f)
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun ProfileTextField(
    label: String,
    value: String,
    onValueChange: (String) -> Unit,
    placeholder: String,
    deepText: Color,
    softText: Color,
    glassSurface: Color
) {
    val shape = RoundedCornerShape(14.dp)

    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
        Text(
            text = label,
            fontSize = 13.sp,
            fontWeight = FontWeight.SemiBold,
            color = softText
        )
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .clip(shape)
                .background(glassSurface.copy(alpha = 0.28f))
                .padding(horizontal = 16.dp, vertical = 14.dp)
        ) {
            androidx.compose.foundation.text.BasicTextField(
                value = value,
                onValueChange = onValueChange,
                textStyle = androidx.compose.ui.text.TextStyle(
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Medium,
                    color = deepText
                ),
                modifier = Modifier.fillMaxWidth(),
                singleLine = true,
                decorationBox = { innerTextField ->
                    Box {
                        if (value.isEmpty()) {
                            Text(
                                placeholder,
                                fontSize = 16.sp,
                                fontWeight = FontWeight.Medium,
                                color = softText.copy(alpha = 0.5f)
                            )
                        }
                        innerTextField()
                    }
                }
            )
        }
    }
}
