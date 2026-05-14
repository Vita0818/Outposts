package com.vita0818.kikaria.ui.mastered

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.IconButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vita0818.kikaria.data.KnowledgePoint
import com.vita0818.kikaria.ui.theme.KikariaColors
import com.vita0818.kikaria.viewmodel.KikariaViewModel
import com.vita0818.kikaria.viewmodel.ReviewMode

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MasteredScreen(
    viewModel: KikariaViewModel,
    onBack: () -> Unit,
    onStartMasteredReview: () -> Unit
) {
    val points = viewModel.masteredPoints

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = "已掌握",
                        fontWeight = FontWeight.SemiBold,
                        color = KikariaColors.DeepText
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Text("←", fontSize = 22.sp, color = KikariaColors.DeepText)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = KikariaColors.GlassSurface.copy(alpha = 0f)
                )
            )
        },
        containerColor = KikariaColors.GlassSurface
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(horizontal = 24.dp)
        ) {
            if (points.isEmpty()) {
                Spacer(modifier = Modifier.height(60.dp))
                Text(
                    text = "已掌握列表为空",
                    modifier = Modifier.fillMaxWidth(),
                    textAlign = androidx.compose.ui.text.style.TextAlign.Center,
                    color = KikariaColors.TertiaryText,
                    fontSize = 18.sp
                )
                Spacer(modifier = Modifier.height(16.dp))
                Text(
                    text = "在复习中标记掌握后，知识点会出现在这里。",
                    modifier = Modifier.fillMaxWidth(),
                    textAlign = androidx.compose.ui.text.style.TextAlign.Center,
                    color = KikariaColors.SoftText,
                    fontSize = 14.sp
                )
            } else {
                Button(
                    onClick = onStartMasteredReview,
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 16.dp)
                        .height(50.dp),
                    shape = RoundedCornerShape(16.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = KikariaColors.MasteredGreen
                    )
                ) {
                    Text(
                        text = "复习已掌握 (${points.size})",
                        fontSize = 16.sp,
                        fontWeight = FontWeight.SemiBold
                    )
                }

                LazyColumn {
                    items(points, key = { it.id }) { point ->
                        MasteredItem(
                            point = point,
                            onRemove = { viewModel.toggleMastered(point) }
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun MasteredItem(
    point: KnowledgePoint,
    onRemove: () -> Unit
) {
    var expanded by remember { mutableStateOf(false) }

    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 6.dp)
            .clickable { expanded = !expanded },
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(
            containerColor = KikariaColors.MasteredCompletedGreen.copy(alpha = 0.4f)
        )
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = androidx.compose.foundation.layout.Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = point.title,
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 16.sp,
                    color = KikariaColors.MasteredDeepGreen,
                    modifier = Modifier.weight(1f)
                )
                Button(
                    onClick = onRemove,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = KikariaColors.RemoveCoral.copy(alpha = 0.12f)
                    ),
                    shape = RoundedCornerShape(10.dp)
                ) {
                    Text(
                        text = "移出",
                        fontSize = 13.sp,
                        color = KikariaColors.RemoveCoral
                    )
                }
            }

            if (expanded) {
                Spacer(modifier = Modifier.height(10.dp))
                Text(
                    text = "💡 ${point.hint}",
                    fontSize = 14.sp,
                    color = KikariaColors.SoftText
                )
                Spacer(modifier = Modifier.height(6.dp))
                Text(
                    text = "📖 ${point.content}",
                    fontSize = 14.sp,
                    color = KikariaColors.DeepText
                )
            }
        }
    }
}
