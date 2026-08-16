package com.vita0818.kikaria.ui.pages

import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.graphics.Matrix
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.border
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
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Lightbulb
import androidx.compose.material.icons.automirrored.filled.MenuBook
import androidx.compose.material3.Icon
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.AppModel
import com.vita0818.kikaria.ui.GradientCapsuleButton
import com.vita0818.kikaria.ui.GlassCard
import com.vita0818.kikaria.ui.KikariaPageBackground
import com.vita0818.kikaria.ui.theme.kikariaColors
import kotlinx.coroutines.launch
import java.io.ByteArrayOutputStream
import android.util.Base64

private data class OnboardingData(
    val title: String,
    val subtitle: String,
    val icon: androidx.compose.ui.graphics.vector.ImageVector,
)

/** 新手引导浮层:3 页 Pager,完成后写入 hasCompletedOnboarding。 */
@OptIn(androidx.compose.foundation.ExperimentalFoundationApi::class)
@Composable
fun OnboardingOverlay() {
    if (!AppModel.isShowingOnboarding) return
    val colors = kikariaColors()
    val pages = listOf(
        OnboardingData("选择一套预设", "从数学、物理、计算机科学与英语预设开始，也可以上传自己的 Markdown 知识点。", Icons.AutoMirrored.Filled.MenuBook),
        OnboardingData("先回忆，再查看", "背诵时先看知识点名称，必要时查看提示，再查看答案。", Icons.Filled.Lightbulb),
        OnboardingData("整理你的学习状态", "把不熟的内容加入重点集锦，把已经掌握的内容标记为已掌握。", Icons.Filled.CheckCircle),
    )
    val pagerState = androidx.compose.foundation.pager.rememberPagerState(pageCount = { pages.size })
    val scope = androidx.compose.runtime.rememberCoroutineScope()

    KikariaPageBackground {
        Column(
            Modifier
                .fillMaxSize()
                .padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center,
        ) {
            androidx.compose.foundation.pager.HorizontalPager(
                state = pagerState,
                modifier = Modifier.fillMaxWidth(),
            ) { page ->
                OnboardingPageCard(pages[page])
            }
            Spacer(Modifier.height(20.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                repeat(pages.size) { i ->
                    Box(
                        Modifier
                            .size(if (pagerState.currentPage == i) 9.dp else 7.dp)
                            .clip(CircleShape)
                            .background(if (pagerState.currentPage == i) colors.sky else colors.blueGray.copy(alpha = 0.4f)),
                    )
                }
            }
            Spacer(Modifier.height(24.dp))
            GradientCapsuleButton(
                text = if (pagerState.currentPage == pages.size - 1) "开始使用" else "下一步",
                gradient = colors.actionGradient,
            ) {
                if (pagerState.currentPage == pages.size - 1) {
                    AppModel.hasCompletedOnboarding = true
                    AppModel.isShowingOnboarding = false
                    AppModel.persistNow()
                } else {
                    scope.launch {
                        pagerState.animateScrollToPage(pagerState.currentPage + 1)
                    }
                }
            }
        }
    }
}

@Composable
private fun OnboardingPageCard(data: OnboardingData) {
    val colors = kikariaColors()
    GlassCard(cornerRadius = 34, fillAlpha = 0.50f, modifier = Modifier.fillMaxWidth()) {
        Column(
            Modifier.fillMaxWidth().padding(28.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Box(
                Modifier
                    .size(132.dp)
                    .clip(CircleShape)
                    .background(Brush.linearGradient(colors.actionGradient)),
                contentAlignment = Alignment.Center,
            ) {
                Icon(data.icon, contentDescription = null, tint = Color.White, modifier = Modifier.size(54.dp))
            }
            Spacer(Modifier.height(24.dp))
            Text(data.title, color = colors.deepText, fontSize = 29.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.Center)
            Spacer(Modifier.height(10.dp))
            Text(
                data.subtitle,
                color = colors.softText,
                fontSize = 16.sp,
                fontWeight = FontWeight.Medium,
                textAlign = TextAlign.Center,
                lineHeight = 24.sp,
            )
        }
    }
}

/** 首次资料设置浮层:头像 + 昵称 + 用户名。 */
@Composable
fun ProfileSetupOverlay() {
    if (!AppModel.isShowingProfileSetup) return
    val colors = kikariaColors()
    var nickname by remember { mutableStateOf("") }
    var handle by remember { mutableStateOf("") }
    var avatarBase64 by remember { mutableStateOf<String?>(null) }
    val context = LocalContext.current

    val photoLauncher = rememberLauncherForActivityResult(ActivityResultContracts.PickVisualMedia()) { uri ->
        if (uri != null) {
            avatarBase64 = runCatching {
                val source = context.contentResolver.openInputStream(uri)?.use { BitmapFactory.decodeStream(it) }
                source?.let { compressAvatar(it) }
            }.getOrNull()
        }
    }

    KikariaPageBackground {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            GlassCard(cornerRadius = 34, fillAlpha = 0.55f, modifier = Modifier.fillMaxWidth().padding(horizontal = 24.dp)) {
                Column(Modifier.fillMaxWidth().padding(28.dp), horizontalAlignment = Alignment.CenterHorizontally) {
                    Text("欢迎使用 Kikaria", color = colors.deepText, fontSize = 30.sp, fontWeight = FontWeight.Bold)
                    Spacer(Modifier.height(8.dp))
                    Text("先设置你的个人资料", color = colors.softText, fontSize = 16.sp, fontWeight = FontWeight.Medium)
                    Spacer(Modifier.height(24.dp))

                    Box(contentAlignment = Alignment.BottomEnd) {
                        Box(
                            Modifier
                                .size(88.dp)
                                .clip(CircleShape)
                                .background(Brush.linearGradient(colors.actionGradient)),
                            contentAlignment = Alignment.Center,
                        ) {
                            val bmp = avatarBase64?.let { decodeAvatar(it) }
                            if (bmp != null) {
                                androidx.compose.foundation.Image(
                                    bitmap = bmp.asImageBitmap(),
                                    contentDescription = "选择头像",
                                    contentScale = androidx.compose.ui.layout.ContentScale.Crop,
                                    modifier = Modifier.fillMaxSize(),
                                )
                            } else {
                                Text(
                                    nickname.trim().take(1).ifEmpty { "K" },
                                    color = Color.White,
                                    fontSize = 36.sp,
                                    fontWeight = FontWeight.SemiBold,
                                )
                            }
                        }
                        Box(
                            Modifier
                                .size(30.dp)
                                .clip(CircleShape)
                                .background(Brush.linearGradient(colors.actionGradient))
                                .border(2.dp, colors.glassSurface, CircleShape)
                                .clickable {
                                    photoLauncher.launch(PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly))
                                },
                            contentAlignment = Alignment.Center,
                        ) {
                            Icon(Icons.Filled.Add, contentDescription = "选择头像", tint = Color.White, modifier = Modifier.size(16.dp))
                        }
                    }

                    Spacer(Modifier.height(24.dp))
                    ProfileField("昵称", nickname) { nickname = it }
                    Spacer(Modifier.height(12.dp))
                    ProfileField("用户名", handle) { handle = it }
                    Spacer(Modifier.height(24.dp))
                    GradientCapsuleButton(
                        text = "开始使用",
                        gradient = colors.actionGradient,
                        enabled = nickname.isNotBlank(),
                    ) {
                        val trimmedHandle = handle.trim().trimStart('@')
                        val derived = trimmedHandle.ifEmpty {
                            nickname.trim().lowercase().replace(Regex("[^a-z0-9]"), "_").ifEmpty { "kikaria_user" }
                        }
                        AppModel.userProfile = AppModel.userProfile.copy(
                            displayName = nickname.trim(),
                            userHandle = derived,
                            avatarBase64 = avatarBase64,
                        )
                        AppModel.hasCompletedProfileSetup = true
                        AppModel.isShowingProfileSetup = false
                        if (!AppModel.hasCompletedOnboarding) {
                            AppModel.isShowingOnboarding = true
                        }
                        AppModel.persistNow()
                    }
                }
            }
        }
    }
}

@Composable
fun ProfileField(label: String, value: String, modifier: Modifier = Modifier, onValueChange: (String) -> Unit) {
    val colors = kikariaColors()
    Column(modifier.fillMaxWidth()) {
        Text(label, color = colors.softText, fontSize = 13.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        OutlinedTextField(
            value = value,
            onValueChange = onValueChange,
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(16.dp),
            singleLine = true,
            colors = OutlinedTextFieldDefaults.colors(
                focusedTextColor = colors.deepText,
                unfocusedTextColor = colors.deepText,
                focusedBorderColor = colors.sky,
                unfocusedBorderColor = colors.blueGray.copy(alpha = 0.4f),
                focusedContainerColor = colors.glassSurface.copy(alpha = 0.4f),
                unfocusedContainerColor = colors.glassSurface.copy(alpha = 0.4f),
            ),
        )
    }
}

/** 头像压缩:最长边 512、JPEG 82,对齐 Apple 版。 */
fun compressAvatar(source: Bitmap): String {
    val maxSide = 512
    val scale = if (source.width > source.height) {
        maxSide.toFloat() / source.width
    } else {
        maxSide.toFloat() / source.height
    }
    val bitmap = if (scale < 1f) {
        Bitmap.createScaledBitmap(source, (source.width * scale).toInt(), (source.height * scale).toInt(), true)
    } else {
        Bitmap.createBitmap(source, 0, 0, source.width, source.height, Matrix(), false)
    }
    val out = ByteArrayOutputStream()
    bitmap.compress(Bitmap.CompressFormat.JPEG, 82, out)
    return Base64.encodeToString(out.toByteArray(), Base64.NO_WRAP)
}

fun decodeAvatar(base64: String): Bitmap? = runCatching {
    val bytes = Base64.decode(base64, Base64.NO_WRAP)
    BitmapFactory.decodeByteArray(bytes, 0, bytes.size)
}.getOrNull()
