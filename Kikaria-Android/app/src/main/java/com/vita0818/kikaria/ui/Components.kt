package com.vita0818.kikaria.ui

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.defaultMinSize
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.ArrowForward
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Tag
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.AppModel
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.kikariaColors
import kotlinx.coroutines.delay

/** 页面通用背景:pageGradient 渐变铺满。 */
@Composable
fun KikariaPageBackground(content: @Composable () -> Unit) {
    val colors = kikariaColors()
    Box(
        Modifier
            .fillMaxSize()
            .background(Brush.linearGradient(colors.pageGradient)),
    ) { content() }
}

/** 玻璃拟态卡片:半透明底 + 渐变描边,参数对齐 liquidGlassCard(圆角 28、fill 0.48、stroke 0.42)。 */
@Composable
fun GlassCard(
    modifier: Modifier = Modifier,
    cornerRadius: Int = 28,
    fillAlpha: Float = 0.48f,
    strokeAlpha: Float = 0.42f,
    content: @Composable () -> Unit,
) {
    val colors = kikariaColors()
    val dark = androidx.compose.foundation.isSystemInDarkTheme()
    val effFill = if (dark) fillAlpha * 0.82f else fillAlpha
    val effStroke = if (dark) strokeAlpha * 0.86f else strokeAlpha
    Surface(
        modifier = modifier.clip(RoundedCornerShape(cornerRadius.dp)),
        shape = RoundedCornerShape(cornerRadius.dp),
        color = colors.glassSurface.copy(alpha = effFill),
        border = BorderStroke(
            1.dp,
            Brush.linearGradient(
                listOf(
                    Color.White.copy(alpha = effStroke),
                    Color.White.copy(alpha = effStroke * 0.24f),
                    colors.glassStrokeAccent.copy(alpha = if (dark) 0.22f else 0.13f),
                ),
            ),
        ),
    ) { content() }
}

/** 搜索栏:对齐 KikariaSearchBar(高 50、圆角 22)。 */
@Composable
fun KikariaSearchBar(
    value: String,
    onValueChange: (String) -> Unit,
    placeholder: String,
    modifier: Modifier = Modifier,
) {
    val colors = kikariaColors()
    GlassCard(modifier = modifier, cornerRadius = 22, fillAlpha = 0.44f) {
        Row(
            Modifier.padding(horizontal = 14.dp).height(50.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Icon(Icons.Filled.Search, contentDescription = null, tint = colors.blueGray, modifier = Modifier.size(18.dp))
            Spacer(Modifier.size(8.dp))
            TextField(
                value = value,
                onValueChange = onValueChange,
                placeholder = {
                    Text(placeholder, fontSize = 15.sp, color = colors.softText.copy(alpha = 0.7f))
                },
                singleLine = true,
                modifier = Modifier.weight(1f),
                colors = TextFieldDefaults.colors(
                    focusedTextColor = colors.deepText,
                    unfocusedTextColor = colors.deepText,
                    focusedContainerColor = Color.Transparent,
                    unfocusedContainerColor = Color.Transparent,
                    focusedIndicatorColor = Color.Transparent,
                    unfocusedIndicatorColor = Color.Transparent,
                    cursorColor = colors.sky,
                ),
                textStyle = MaterialTheme.typography.bodyLarge.copy(fontSize = 15.sp, fontWeight = FontWeight.Medium),
            )
            if (value.isNotEmpty()) {
                Icon(
                    Icons.Filled.Close,
                    contentDescription = "清空",
                    tint = colors.blueGray,
                    modifier = Modifier
                        .size(18.dp)
                        .clip(CircleShape)
                        .clickable { onValueChange("") }
                        .padding(2.dp),
                )
            }
        }
    }
}

/** 主渐变胶囊按钮(白字)。 */
@Composable
fun GradientCapsuleButton(
    text: String,
    icon: ImageVector? = null,
    gradient: List<Color>,
    enabled: Boolean = true,
    fontSize: Int = 17,
    onClick: () -> Unit,
) {
    val colors = kikariaColors()
    val alpha = if (enabled) 1f else 0.48f
    Box(
        Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(50))
            .background(brush = Brush.linearGradient(gradient), alpha = alpha)
            .clickable(enabled = enabled) { onClick() }
            .padding(horizontal = 18.dp, vertical = 14.dp),
        contentAlignment = Alignment.Center,
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            if (icon != null) {
                Icon(icon, contentDescription = null, tint = Color.White, modifier = Modifier.size(20.dp))
                Spacer(Modifier.size(8.dp))
            }
            Text(text, color = Color.White, fontSize = fontSize.sp, fontWeight = FontWeight.SemiBold)
        }
    }
}

/** 复习动作按钮:primary=渐变底白字;secondary=玻璃底+渐变描边。 */
@Composable
fun ReviewActionButton(
    text: String,
    icon: ImageVector,
    gradient: List<Color>,
    toneColor: Color,
    primary: Boolean = true,
    enabled: Boolean = true,
    onClick: () -> Unit,
) {
    val colors = kikariaColors()
    val shape = RoundedCornerShape(26.dp)
    val content: @Composable () -> Unit = {
        Row(
            Modifier.padding(horizontal = 16.dp, vertical = 14.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.Center,
        ) {
            Icon(icon, contentDescription = null, tint = if (primary) Color.White else toneColor, modifier = Modifier.size(20.dp))
            Spacer(Modifier.size(8.dp))
            Text(
                text,
                color = if (primary) Color.White else colors.deepText,
                fontSize = 17.sp,
                fontWeight = FontWeight.SemiBold,
                textAlign = TextAlign.Center,
            )
        }
    }
    if (primary) {
        Surface(
            modifier = Modifier.fillMaxWidth().clip(shape).clickable(enabled = enabled) { onClick() },
            shape = shape,
            color = Color.Transparent,
        ) {
            Box(Modifier.background(Brush.linearGradient(gradient))) { content() }
        }
    } else {
        Surface(
            modifier = Modifier.fillMaxWidth().clip(shape).clickable(enabled = enabled) { onClick() },
            shape = shape,
            color = colors.glassSurface.copy(alpha = if (enabled) 0.36f else 0.2f),
            border = BorderStroke(1.dp, Brush.linearGradient(gradient)),
        ) { content() }
    }
}

/** 标签胶囊流(居中)。 */
@OptIn(ExperimentalLayoutApi::class)
@Composable
fun LightTagRow(tags: List<String>, modifier: Modifier = Modifier) {
    val colors = kikariaColors()
    FlowRow(
        modifier = modifier,
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        verticalArrangement = Arrangement.spacedBy(6.dp),
    ) {
        tags.forEach { tag ->
            GlassCard(cornerRadius = 14, fillAlpha = 0.4f, strokeAlpha = 0.3f) {
                Text(
                    tag,
                    color = colors.softText,
                    fontSize = 12.sp,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.padding(horizontal = 11.dp, vertical = 6.dp),
                )
            }
        }
    }
}

/** 顶部返回按钮 + 标题行。 */
@Composable
fun PageHeader(
    title: String,
    onBack: (() -> Unit)? = null,
    trailing: (@Composable () -> Unit)? = null,
) {
    val colors = kikariaColors()
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        if (onBack != null) {
            GlassCard(cornerRadius = 21, fillAlpha = 0.44f, strokeAlpha = 0.36f) {
                IconButton(onClick = onBack, modifier = Modifier.size(42.dp)) {
                    Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "返回", tint = colors.deepText, modifier = Modifier.size(20.dp))
                }
            }
            Spacer(Modifier.size(14.dp))
        }
        Text(
            title,
            color = colors.deepText,
            fontSize = 32.sp,
            fontWeight = FontWeight.Bold,
            fontFamily = FontFamily.Default,
            modifier = Modifier.weight(1f),
        )
        trailing?.invoke()
    }
}

/** 空态卡。 */
@Composable
fun SoftEmptyState(icon: ImageVector, title: String, subtitle: String, modifier: Modifier = Modifier) {
    val colors = kikariaColors()
    GlassCard(modifier = modifier.fillMaxWidth(), cornerRadius = 30, fillAlpha = 0.5f) {
        Column(
            Modifier.fillMaxWidth().padding(28.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Icon(icon, contentDescription = null, tint = colors.sky, modifier = Modifier.size(42.dp))
            Spacer(Modifier.height(14.dp))
            Text(title, color = colors.deepText, fontSize = 20.sp, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(6.dp))
            Text(subtitle, color = colors.softText, fontSize = 15.sp, textAlign = TextAlign.Center)
        }
    }
}

/** Toast 层:读 AppModel.toastMessage,2 秒后消失。 */
@Composable
fun ToastHost() {
    val toast = AppModel.toastMessage
    val colors = kikariaColors()
    LaunchedEffect(toast?.first) {
        if (toast != null) {
            delay(2000)
            AppModel.dismissToast()
        }
    }
    if (toast != null) {
        Box(Modifier.fillMaxWidth().padding(top = 66.dp), contentAlignment = Alignment.TopCenter) {
            GlassCard(cornerRadius = 22, fillAlpha = 0.52f, strokeAlpha = 0.4f) {
                Text(
                    toast.second,
                    color = colors.deepText,
                    fontSize = 14.sp,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.padding(horizontal = 18.dp, vertical = 13.dp),
                )
            }
        }
    }
}

/** 圆形玻璃图标按钮。 */
@Composable
fun GlassIconButton(icon: ImageVector, size: Int = 42, contentDescription: String? = null, onClick: () -> Unit) {
    val colors = kikariaColors()
    GlassCard(cornerRadius = size / 2, fillAlpha = 0.44f, strokeAlpha = 0.36f) {
        IconButton(onClick = onClick, modifier = Modifier.size(size.dp)) {
            Icon(icon, contentDescription = contentDescription, tint = colors.deepText, modifier = Modifier.size(20.dp))
        }
    }
}

/** 资料头像:有图片显示图片,否则首字母圆。 */
@Composable
fun ProfileAvatar(size: Int = 44, onClick: (() -> Unit)? = null) {
    val colors = kikariaColors()
    val profile = AppModel.userProfile
    val bitmap = rememberAvatarBitmap(profile.avatarBase64)
    val shape = CircleShape
    Box(
        Modifier
            .size(size.dp)
            .clip(shape)
            .then(if (onClick != null) Modifier.clickable { onClick() } else Modifier)
            .background(Brush.linearGradient(colors.actionGradient)),
        contentAlignment = Alignment.Center,
    ) {
        if (bitmap != null) {
            androidx.compose.foundation.Image(
                bitmap = bitmap,
                contentDescription = "头像",
                contentScale = androidx.compose.ui.layout.ContentScale.Crop,
                modifier = Modifier.fillMaxSize(),
            )
        } else {
            Text(
                profile.displayName.trim().take(1).ifEmpty { "K" },
                color = Color.White,
                fontSize = (size * 0.42f).sp,
                fontWeight = FontWeight.SemiBold,
            )
        }
    }
}

@Composable
fun rememberAvatarBitmap(base64: String?): androidx.compose.ui.graphics.ImageBitmap? {
    return androidx.compose.runtime.remember(base64) {
        if (base64.isNullOrBlank()) null
        else runCatching {
            val bytes = android.util.Base64.decode(base64, android.util.Base64.NO_WRAP)
            android.graphics.BitmapFactory.decodeByteArray(bytes, 0, bytes.size)
        }.getOrNull()?.asImageBitmap()
    }
}
