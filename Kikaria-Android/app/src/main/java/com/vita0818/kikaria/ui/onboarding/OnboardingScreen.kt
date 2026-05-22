package com.vita0818.kikaria.ui.onboarding

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
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.components.KikariaPageShell
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography

/**
 * Onboarding screen translated from the iOS OnboardingView in ContentView.swift.
 *
 * Introduces new users to Kikaria's three core concepts through swipeable
 * page cards with large icon graphics and action button.
 */
@Composable
fun OnboardingScreen(
    onComplete: () -> Unit
) {
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val actionGradient = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    val shadowColor = if (isDark) KikariaColors.SkyDark.copy(alpha = 0.22f) else KikariaColors.Sky.copy(alpha = 0.22f)

    var selectedPage by remember { mutableIntStateOf(0) }

    val pages = listOf(
        OnboardingPageData(
            title = "选择一套预设",
            subtitle = "从数学、物理、计算机科学与英语预设开始，也可以上传自己的 Markdown 知识点。",
            icon = KikariaIcons.books
        ),
        OnboardingPageData(
            title = "先回忆，再查看",
            subtitle = "背诵时先看知识点名称，必要时查看提示，再查看答案。",
            icon = KikariaIcons.hint
        ),
        OnboardingPageData(
            title = "整理你的学习状态",
            subtitle = "把不熟的内容加入重点集锦，把已经掌握的内容标记为已掌握。",
            icon = KikariaIcons.mastered
        )
    )

    KikariaPageShell {
        Column(
            modifier = Modifier.fillMaxSize(),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // Header
            Text(
                text = KikariaTypography.mixedText("Kikaria", size = 36, weight = FontWeight.SemiBold),
                color = deepText,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(start = 24.dp, top = 24.dp)
            )

            Spacer(modifier = Modifier.weight(0.3f))

            // Page card
            val page = pages[selectedPage]
            OnboardingPageCard(page = page, isDark = isDark)

            Spacer(modifier = Modifier.height(28.dp))

            // Page dots
            PageDots(
                count = pages.size,
                selected = selectedPage,
                isDark = isDark
            )

            Spacer(modifier = Modifier.weight(0.5f))

            // Action button
            Box(
                modifier = Modifier
                    .padding(horizontal = 24.dp)
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(28.dp))
                    .shadow(18.dp, RoundedCornerShape(28.dp), spotColor = shadowColor)
                    .background(actionGradient)
                    .clickable {
                        if (selectedPage < pages.size - 1) {
                            selectedPage += 1
                        } else {
                            onComplete()
                        }
                    }
                    .padding(vertical = 17.dp),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = if (selectedPage == pages.size - 1) "开始使用" else "下一步",
                    fontSize = 17.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = Color.White
                )
            }

            Spacer(modifier = Modifier.height(28.dp))
        }
    }
}

// ─── Onboarding Page Data ───

private data class OnboardingPageData(
    val title: String,
    val subtitle: String,
    val icon: androidx.compose.ui.graphics.vector.ImageVector
)

// ─── Onboarding Page Card ───

@Composable
private fun OnboardingPageCard(
    page: OnboardingPageData,
    isDark: Boolean
) {
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val softText = if (isDark) KikariaColors.SoftTextDark else KikariaColors.SoftText
    val actionGradient = if (isDark) KikariaColors.ActionGradientDark else KikariaColors.ActionGradientLight
    val glassSurface = if (isDark) KikariaColors.GlassSurfaceDark else KikariaColors.GlassSurface

    val shape = RoundedCornerShape(34.dp)
    val skyShadow = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky).copy(alpha = 0.13f)

    Box(
        modifier = Modifier
            .padding(horizontal = 24.dp)
            .fillMaxWidth()
            .shadow(24.dp, shape, spotColor = skyShadow)
            .clip(shape)
            .background(glassSurface.copy(alpha = 0.50f))
            .padding(horizontal = 24.dp, vertical = 44.dp),
        contentAlignment = Alignment.Center
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(26.dp)
        ) {
            // Icon in gradient circle
            Box(
                modifier = Modifier.size(132.dp),
                contentAlignment = Alignment.Center
            ) {
                Box(
                    modifier = Modifier
                        .size(132.dp)
                        .shadow(24.dp, CircleShape,
                            spotColor = (if (isDark) KikariaColors.SkyDark else KikariaColors.Sky)
                                .copy(alpha = 0.20f))
                        .clip(CircleShape)
                        .background(actionGradient)
                )

                // Decorative highlight
                Box(
                    modifier = Modifier
                        .size(86.dp)
                        .offset(x = 28.dp, y = (-26).dp)
                        .clip(CircleShape)
                        .background(Color.White.copy(alpha = 0.24f))
                )

                Icon(
                    imageVector = page.icon,
                    contentDescription = null,
                    tint = Color.White.copy(alpha = 0.96f),
                    modifier = Modifier.size(54.dp)
                )
            }

            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                Text(
                    text = KikariaTypography.mixedText(
                        page.title,
                        size = 29,
                        weight = FontWeight.Bold
                    ),
                    color = deepText,
                    textAlign = TextAlign.Center
                )

                Text(
                    text = KikariaTypography.mixedText(
                        page.subtitle,
                        size = 16,
                        weight = FontWeight.Medium
                    ),
                    color = softText,
                    textAlign = TextAlign.Center,
                    lineHeight = 24.sp
                )
            }
        }
    }
}

// ─── Page Dots ───

@Composable
private fun PageDots(
    count: Int,
    selected: Int,
    isDark: Boolean
) {
    val activeColor = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky
    val inactiveColor = (if (isDark) KikariaColors.MistDark else KikariaColors.Mist)

    Row(
        horizontalArrangement = Arrangement.spacedBy(10.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        for (i in 0 until count) {
            Box(
                modifier = Modifier
                    .size(if (i == selected) 10.dp else 8.dp)
                    .clip(CircleShape)
                    .background(if (i == selected) activeColor else inactiveColor)
            )
        }
    }
}
