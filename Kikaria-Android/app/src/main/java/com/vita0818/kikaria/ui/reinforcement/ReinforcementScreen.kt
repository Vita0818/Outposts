package com.vita0818.kikaria.ui.reinforcement

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.ui.components.KikariaEmptyState
import com.vita0818.kikaria.ui.components.KikariaGlassCard
import com.vita0818.kikaria.ui.components.KikariaIcons
import com.vita0818.kikaria.ui.components.KikariaMathText
import com.vita0818.kikaria.ui.components.KikariaPageTitle
import com.vita0818.kikaria.ui.components.KikariaScrollPageShell
import com.vita0818.kikaria.ui.components.KikariaTagChip
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.ui.theme.KikariaTypography
import com.vita0818.kikaria.ui.theme.rememberKikariaPhoneMetrics
import com.vita0818.kikaria.viewmodel.KikariaViewModel

@Composable
fun ReinforcementScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit,
    onStartReinforcementReview: () -> Unit
) {
    val points = viewModel.reinforcedPoints

    val metrics = rememberKikariaPhoneMetrics()

    KikariaScrollPageShell(onBack = onBack, metrics = metrics) {
        Spacer(modifier = Modifier.height(metrics.pageTopPadding))
        KikariaPageTitle(title = "重点集锦")
        Spacer(modifier = Modifier.height(18.dp))

        if (points.isEmpty()) {
            KikariaGlassCard(Modifier.fillMaxWidth(), cornerRadius = 30.dp, fillOpacity = 0.54f) {
                KikariaEmptyState(title = "还没有重点", subtitle = "在背诵时查看答案后，可以把知识点加入这里。")
            }
        } else {
            ReinforcementStartButton(count = points.size, onClick = onStartReinforcementReview)
            Spacer(modifier = Modifier.height(12.dp))
            points.forEach { point -> ReinforcementItem(point = point, onRemove = { viewModel.toggleReinforcement(point) }) }
            Spacer(modifier = Modifier.height(32.dp))
        }
    }
}

@Composable
private fun ReinforcementStartButton(count: Int, onClick: () -> Unit) {
    val isDark = isSystemInDarkTheme()
    val nextGrad = if (isDark) KikariaColors.NextGradientDark else KikariaColors.NextGradientLight
    val shape = RoundedCornerShape(28.dp)
    Box(Modifier.fillMaxWidth().padding(vertical = 8.dp).shadow(20.dp, shape, ambientColor = (if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber).copy(alpha = 0.16f), spotColor = (if (isDark) KikariaColors.NextAmberDark else KikariaColors.NextAmber).copy(alpha = 0.16f)).clip(shape).background(nextGrad).clickable { onClick() }.padding(horizontal = 22.dp, vertical = 22.dp), contentAlignment = Alignment.Center) {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
            Text("开始重点背诵", fontSize = 20.sp, fontWeight = FontWeight.SemiBold, color = Color.White)
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(KikariaTypography.mixedText("$count", size = 20, weight = FontWeight.Bold), color = Color.White)
                Spacer(Modifier.padding(start = 12.dp))
                Icon(
                    imageVector = KikariaIcons.forward,
                    contentDescription = "开始重点背诵",
                    modifier = Modifier.size(18.dp),
                    tint = Color.White.copy(alpha = 0.72f)
                )
            }
        }
    }
}

@Composable
private fun ReinforcementItem(point: KnowledgePoint, onRemove: () -> Unit) {
    var expanded by remember { mutableStateOf(false) }
    val isDark = isSystemInDarkTheme()
    val deepText = if (isDark) KikariaColors.DeepTextDark else KikariaColors.DeepText
    val removeCoral = if (isDark) KikariaColors.RemoveCoralDark else KikariaColors.RemoveCoral
    KikariaGlassCard(Modifier.fillMaxWidth().padding(vertical = 6.dp).clickable { expanded = !expanded }, cornerRadius = 30.dp, fillOpacity = 0.42f, shadowElevation = 20.dp, shadowOpacity = 0.12f) {
        Column(modifier = Modifier.padding(18.dp)) {
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
                Column(Modifier.weight(1f)) {
                    Text(KikariaTypography.mixedText(point.title, size = 22, weight = FontWeight.SemiBold), color = deepText)
                if (point.reinforcementCount > 0) Text(KikariaTypography.mixedText("${point.reinforcementCount}次", size = 14, weight = FontWeight.Bold), color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky)
                }
                if (point.tags.isNotEmpty()) Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) { point.tags.take(3).forEach { tag -> KikariaTagChip(tag = tag, fontSize = 11) } }
            }
            if (expanded) {
                Spacer(Modifier.height(10.dp))
                Text("提示", fontSize = 13.sp, fontWeight = FontWeight.Bold, color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky)
                KikariaMathText(text = point.hint, fontSize = 15, fontWeight = FontWeight.Medium)
                Spacer(Modifier.height(8.dp))
                Text("答案", fontSize = 13.sp, fontWeight = FontWeight.Bold, color = if (isDark) KikariaColors.SkyDark else KikariaColors.Sky)
                KikariaMathText(text = point.content, fontSize = 15, fontWeight = FontWeight.Medium)
            }
            Spacer(Modifier.height(14.dp))
            Box(Modifier.fillMaxWidth().clip(RoundedCornerShape(16.dp)).shadow(14.dp, RoundedCornerShape(16.dp), ambientColor = removeCoral.copy(alpha = 0.18f), spotColor = removeCoral.copy(alpha = 0.18f)).background(if (isDark) KikariaColors.RemoveGradientDark else KikariaColors.RemoveGradientLight).clickable { onRemove() }.padding(vertical = 14.dp), contentAlignment = Alignment.Center) {
                Text("移出重点集锦", fontSize = 14.sp, fontWeight = FontWeight.SemiBold, color = Color.White)
            }
        }
    }
}
