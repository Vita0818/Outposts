package com.vita0818.kikaria

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.vita0818.kikaria.ui.KikariaPageBackground
import com.vita0818.kikaria.ui.ToastHost
import com.vita0818.kikaria.ui.pages.CollectionsPage
import com.vita0818.kikaria.ui.pages.EditKnowledgePointPage
import com.vita0818.kikaria.ui.pages.EditPresetPage
import com.vita0818.kikaria.ui.pages.EditProfilePage
import com.vita0818.kikaria.ui.pages.HomePage
import com.vita0818.kikaria.ui.pages.MarkdownGuidePage
import com.vita0818.kikaria.ui.pages.NewPresetPage
import com.vita0818.kikaria.ui.pages.OnboardingOverlay
import com.vita0818.kikaria.ui.pages.PresetSelectionPage
import com.vita0818.kikaria.ui.pages.ProfileSetupOverlay
import com.vita0818.kikaria.ui.pages.ReviewPage
import com.vita0818.kikaria.ui.pages.ScopePage
import com.vita0818.kikaria.ui.pages.SettingsPage
import com.vita0818.kikaria.ui.pages.TodayOverviewPage
import com.vita0818.kikaria.ui.theme.KikariaTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        AppModel.init(this)
        setContent {
            KikariaTheme {
                KikariaRoot()
            }
        }
    }

    override fun onStop() {
        super.onStop()
        AppModel.persistNow()
    }
}

object Routes {
    const val HOME = "home"
    const val SCOPE = "scope"
    const val REVIEW = "review/{mode}"
    const val TODAY = "todayOverview"
    const val HISTORY = "reviewHistory"
    const val REINFORCEMENT = "reinforcement"
    const val MASTERED = "mastered"
    const val SETTINGS = "settings"
    const val EDIT_PROFILE = "editProfile"
    const val PRESET_SELECTION = "presetSelection"
    const val NEW_PRESET = "newPreset"
    const val MARKDOWN_GUIDE = "markdownGuide"
    const val EDIT_PRESET = "editPreset/{presetId}"
    const val EDIT_POINT = "editPoint/{presetId}/{pointId}"

    fun review(mode: String) = "review/$mode"
    fun editPreset(presetId: String) = "editPreset/$presetId"
    fun editPoint(presetId: String, pointId: String?) = "editPoint/$presetId/${pointId ?: "new"}"
}

@Composable
fun KikariaRoot() {
    val navController = rememberNavController()
    Box(Modifier.fillMaxSize()) {
        KikariaPageBackground {
            NavHost(
                navController = navController,
                startDestination = Routes.HOME,
            ) {
                composable(Routes.HOME) { HomePage(navController) }
                composable(Routes.SCOPE) { ScopePage(navController) }
                composable(
                    Routes.REVIEW,
                    arguments = listOf(navArgument("mode") { type = NavType.StringType }),
                ) { entry ->
                    ReviewPage(navController, entry.arguments?.getString("mode") ?: "normal")
                }
                composable(Routes.TODAY) { TodayOverviewPage(navController) }
                composable(Routes.HISTORY) {
                    com.vita0818.kikaria.ui.pages.ReviewHistoryPage(navController)
                }
                composable(Routes.REINFORCEMENT) { CollectionsPage(navController, mastered = false) }
                composable(Routes.MASTERED) { CollectionsPage(navController, mastered = true) }
                composable(Routes.SETTINGS) { SettingsPage(navController) }
                composable(Routes.EDIT_PROFILE) { EditProfilePage(navController) }
                composable(Routes.PRESET_SELECTION) { PresetSelectionPage(navController) }
                composable(Routes.NEW_PRESET) { NewPresetPage(navController) }
                composable(Routes.MARKDOWN_GUIDE) { MarkdownGuidePage(navController) }
                composable(
                    Routes.EDIT_PRESET,
                    arguments = listOf(navArgument("presetId") { type = NavType.StringType }),
                ) { entry ->
                    EditPresetPage(navController, entry.arguments?.getString("presetId") ?: "")
                }
                composable(
                    Routes.EDIT_POINT,
                    arguments = listOf(
                        navArgument("presetId") { type = NavType.StringType },
                        navArgument("pointId") { type = NavType.StringType },
                    ),
                ) { entry ->
                    EditKnowledgePointPage(
                        navController,
                        entry.arguments?.getString("presetId") ?: "",
                        entry.arguments?.getString("pointId") ?: "new",
                    )
                }
            }
        }
        ToastHost()
        ProfileSetupOverlay()
        OnboardingOverlay()
    }
}
