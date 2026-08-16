package com.vita0818.kikaria.ui.navigation

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.ui.Alignment
import androidx.compose.ui.graphics.Color
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Snackbar
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.vita0818.kikaria.ui.guide.MarkdownFormatGuideScreen
import com.vita0818.kikaria.ui.home.HomeScreen
import com.vita0818.kikaria.ui.mastered.MasteredScreen
import com.vita0818.kikaria.ui.onboarding.OnboardingScreen
import com.vita0818.kikaria.ui.overview.ReviewHistoryScreen
import com.vita0818.kikaria.ui.overview.TodayOverviewScreen
import com.vita0818.kikaria.ui.preset.EditKnowledgePointScreen
import com.vita0818.kikaria.ui.preset.EditPresetScreen
import com.vita0818.kikaria.ui.preset.NewPresetScreen
import com.vita0818.kikaria.ui.preset.PresetSelectionScreen
import com.vita0818.kikaria.ui.profile.ProfileSetupScreen
import com.vita0818.kikaria.ui.reinforcement.ReinforcementScreen
import com.vita0818.kikaria.ui.review.ReviewScreen
import com.vita0818.kikaria.ui.scope.ScopeSelectionScreen
import com.vita0818.kikaria.ui.settings.EditProfileScreen
import com.vita0818.kikaria.ui.settings.PrivacyPolicyScreen
import com.vita0818.kikaria.ui.settings.SettingsScreen
import com.vita0818.kikaria.viewmodel.KikariaViewModel
import com.vita0818.kikaria.viewmodel.ReviewMode

object Routes {
    const val HOME = "home"
    const val REVIEW = "review"
    const val SCOPE = "scope"
    const val REINFORCEMENT = "reinforcement"
    const val MASTERED = "mastered"
    const val SETTINGS = "settings"
    const val TODAY_OVERVIEW = "today_overview"
    const val REVIEW_HISTORY = "review_history"
    const val PRESET_SELECTION = "preset_selection"
    const val EDIT_PROFILE = "edit_profile"
    const val PROFILE_SETUP = "profile_setup"
    const val ONBOARDING = "onboarding"
    const val MARKDOWN_GUIDE = "markdown_guide"
    const val NEW_PRESET = "new_preset"
    const val PRIVACY_POLICY = "privacy_policy"
    const val EDIT_PRESET = "edit_preset/{presetId}"
    const val EDIT_KNOWLEDGE_POINT = "edit_knowledge_point/{presetId}/{pointId}"

    fun editPresetRoute(presetId: String): String = "edit_preset/$presetId"

    fun editKnowledgePointRoute(presetId: String, pointId: String? = null): String {
        return "edit_knowledge_point/$presetId/${pointId ?: "new"}"
    }
}

@Composable
fun KikariaNavGraph(
    navController: NavHostController = rememberNavController(),
    viewModel: KikariaViewModel = viewModel()
) {
    val snackbarHostState = remember { SnackbarHostState() }

    // Show toast messages from ViewModel
    LaunchedEffect(viewModel.toastMessage) {
        viewModel.toastMessage?.let { message ->
            snackbarHostState.showSnackbar(message)
            viewModel.clearToast()
        }
    }

    val startDestination = when {
        !viewModel.hasCompletedProfileSetup -> Routes.PROFILE_SETUP
        !viewModel.hasCompletedOnboarding -> Routes.ONBOARDING
        else -> Routes.HOME
    }

    Scaffold(
        snackbarHost = {
            SnackbarHost(snackbarHostState) { data ->
                Snackbar(
                    snackbarData = data,
                    containerColor = androidx.compose.ui.graphics.Color(0xFF214054),
                    contentColor = androidx.compose.ui.graphics.Color.White
                )
            }
        }
    ) { paddingValues ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
        NavHost(
            navController = navController,
            startDestination = startDestination,
            modifier = Modifier.fillMaxSize()
        ) {
        composable(Routes.HOME) {
            HomeScreen(
                viewModel = viewModel,
                onStartReview = {
                    viewModel.startReview(ReviewMode.NORMAL)
                    navController.navigate(Routes.REVIEW)
                },
                onOpenScope = {
                    navController.navigate(Routes.SCOPE)
                },
                onOpenReinforcement = {
                    navController.navigate(Routes.REINFORCEMENT)
                },
                onOpenMastered = {
                    navController.navigate(Routes.MASTERED)
                },
                onOpenPresetSelection = {
                    navController.navigate(Routes.PRESET_SELECTION)
                },
                onOpenSettings = {
                    navController.navigate(Routes.SETTINGS)
                },
                onOpenTodayOverview = {
                    navController.navigate(Routes.TODAY_OVERVIEW)
                }
            )
        }

        composable(Routes.REVIEW) {
            ReviewScreen(
                viewModel = viewModel,
                onBack = {
                    navController.popBackStack()
                }
            )
        }

        composable(Routes.SCOPE) {
            ScopeSelectionScreen(
                viewModel = viewModel,
                onBack = {
                    navController.popBackStack()
                }
            )
        }

        composable(Routes.REINFORCEMENT) {
            ReinforcementScreen(
                viewModel = viewModel,
                onBack = {
                    navController.popBackStack()
                },
                onStartReinforcementReview = {
                    viewModel.startReview(ReviewMode.REINFORCEMENT)
                    navController.navigate(Routes.REVIEW)
                }
            )
        }

        composable(Routes.MASTERED) {
            MasteredScreen(
                viewModel = viewModel,
                onBack = {
                    navController.popBackStack()
                },
                onStartMasteredReview = {
                    viewModel.startReview(ReviewMode.MASTERED)
                    navController.navigate(Routes.REVIEW)
                }
            )
        }

        composable(Routes.SETTINGS) {
            SettingsScreen(
                userDisplayName = viewModel.userDisplayName.ifEmpty { "K" },
                userHandle = viewModel.userHandle,
                presetName = viewModel.activePreset?.name ?: "无",
                avatarUri = viewModel.avatarUri,
                dailyGoal = viewModel.dailyGoal,
                countdownDays = viewModel.countdownDays,
                countdownEndDate = viewModel.countdownEndDate,
                dangerPercent = viewModel.dangerPercent,
                notificationsEnabled = viewModel.notificationsEnabled,
                notificationTimeText = viewModel.notificationTimeText,
                onBack = { navController.popBackStack() },
                onEditProfile = {
                    navController.navigate(Routes.EDIT_PROFILE)
                },
                onSetDailyGoal = { viewModel.updateDailyGoal(it) },
                onSetCountdownRange = { start, end ->
                    viewModel.setCountdownRange(start, end)
                },
                onSetDangerPercent = { viewModel.updateDangerPercent(it) },
                onToggleNotifications = { enabled ->
                    viewModel.updateNotificationsEnabled(enabled)
                },
                onNotificationPermissionDenied = {
                    viewModel.showToast("请在系统设置中允许通知")
                },
                onSetNotificationTime = { viewModel.setNotificationTime(it) },
                onOpenOnboarding = {
                    navController.navigate(Routes.ONBOARDING)
                },
                onOpenMarkdownGuide = {
                    navController.navigate(Routes.MARKDOWN_GUIDE)
                },
                onOpenPrivacyPolicy = {
                    navController.navigate(Routes.PRIVACY_POLICY)
                },
                onOpenPresetSelection = {
                    navController.navigate(Routes.PRESET_SELECTION)
                }
            )
        }

        composable(Routes.TODAY_OVERVIEW) {
            TodayOverviewScreen(
                presetName = viewModel.activePreset?.name ?: "无",
                todayMasteredCount = viewModel.todayMasteredCount,
                todayHintCount = viewModel.todayHintCount,
                todayReviewCount = viewModel.todayReviewCount,
                totalMasteredCount = viewModel.masteredPoints.size,
                dailyGoal = viewModel.dailyGoal,
                countdownDays = viewModel.countdownDays,
                onBack = { navController.popBackStack() },
                onOpenHistory = {
                    navController.navigate(Routes.REVIEW_HISTORY)
                }
            )
        }

        composable(Routes.REVIEW_HISTORY) {
            ReviewHistoryScreen(
                activityRecords = viewModel.activityRecords.toList(),
                onBack = { navController.popBackStack() }
            )
        }

        composable(Routes.PRESET_SELECTION) {
            PresetSelectionScreen(
                presets = viewModel.presets.toList(),
                currentPresetId = viewModel.activePresetId,
                onBack = { navController.popBackStack() },
                onSwitchPreset = { preset ->
                    viewModel.switchPreset(preset.id)
                    navController.popBackStack()
                },
                onNewPreset = {
                    navController.navigate(Routes.NEW_PRESET)
                },
                onEditPreset = { preset ->
                    navController.navigate(Routes.editPresetRoute(preset.id))
                },
                onDeletePreset = { preset ->
                    viewModel.deletePreset(preset.id)
                },
                onImportPreset = { name, markdownText ->
                    val preset = viewModel.importPreset(name, markdownText)
                    navController.popBackStack()
                    navController.navigate(Routes.editPresetRoute(preset.id))
                }
            )
        }

        composable(Routes.EDIT_PROFILE) {
            EditProfileScreen(
                initialDisplayName = viewModel.userDisplayName,
                initialHandle = viewModel.userHandle,
                initialAvatarUri = viewModel.avatarUri,
                onBack = { navController.popBackStack() },
                onSave = { displayName, handle ->
                    viewModel.updateProfile(displayName, handle)
                },
                onAvatarChanged = { uri ->
                    viewModel.avatarUri = uri
                    viewModel.saveState()
                }
            )
        }

        composable(Routes.PROFILE_SETUP) {
            ProfileSetupScreen(
                initialDisplayName = viewModel.userDisplayName,
                initialHandle = viewModel.userHandle,
                initialAvatarUri = viewModel.avatarUri,
                onComplete = { displayName, handle, avatarUri ->
                    viewModel.updateProfile(displayName, handle)
                    viewModel.avatarUri = avatarUri
                    viewModel.hasCompletedProfileSetup = true
                    viewModel.saveState()
                    if (!viewModel.hasCompletedOnboarding) {
                        navController.navigate(Routes.ONBOARDING) {
                            popUpTo(Routes.PROFILE_SETUP) { inclusive = true }
                        }
                    } else {
                        navController.navigate(Routes.HOME) {
                            popUpTo(Routes.PROFILE_SETUP) { inclusive = true }
                        }
                    }
                }
            )
        }

        composable(Routes.ONBOARDING) {
            OnboardingScreen(
                onComplete = {
                    viewModel.completeOnboarding()
                    navController.navigate(Routes.HOME) {
                        popUpTo(Routes.HOME) { inclusive = true }
                    }
                }
            )
        }

        composable(Routes.MARKDOWN_GUIDE) {
            MarkdownFormatGuideScreen(
                onBack = { navController.popBackStack() }
            )
        }

        composable(Routes.PRIVACY_POLICY) {
            PrivacyPolicyScreen(
                onBack = { navController.popBackStack() }
            )
        }

        composable(Routes.NEW_PRESET) {
            NewPresetScreen(
                onBack = { navController.popBackStack() },
                onCreatePreset = { name, category, markdownText ->
                    viewModel.createPreset(name, category, markdownText)
                    navController.popBackStack()
                }
            )
        }

        composable(Routes.EDIT_PRESET) { backStackEntry ->
            val presetId = backStackEntry.arguments?.getString("presetId") ?: ""
            val preset = viewModel.presets.find { it.id == presetId }
            if (preset != null) {
                EditPresetScreen(
                    preset = preset,
                    knowledgePoints = viewModel.knowledgePointsForPreset(presetId),
                    onBack = { navController.popBackStack() },
                    onSavePreset = { name, category, markdownText ->
                        viewModel.updatePreset(presetId, name, category, markdownText)
                        navController.popBackStack()
                    },
                    onAddPoint = {
                        navController.navigate(Routes.editKnowledgePointRoute(presetId))
                    },
                    onEditPoint = { point ->
                        navController.navigate(Routes.editKnowledgePointRoute(presetId, point.id))
                    },
                    onDeletePoint = { point ->
                        viewModel.deleteKnowledgePoint(presetId, point.id)
                    },
                    onDeletePreset = {
                        viewModel.deletePreset(presetId)
                        navController.popBackStack()
                    }
                )
            } else {
                // Preset not found — navigate back
                androidx.compose.runtime.LaunchedEffect(Unit) {
                    navController.popBackStack()
                }
            }
        }

        composable(Routes.EDIT_KNOWLEDGE_POINT) { backStackEntry ->
            val presetId = backStackEntry.arguments?.getString("presetId") ?: ""
            val pointId = backStackEntry.arguments?.getString("pointId") ?: "new"
            val preset = viewModel.presets.find { it.id == presetId }
            if (preset != null) {
                val point = if (pointId == "new") {
                    null
                } else {
                    viewModel.knowledgePointsForPreset(presetId).find { it.id == pointId }
                }
                EditKnowledgePointScreen(
                    presetName = preset.name,
                    point = point,
                    onBack = { navController.popBackStack() },
                    onSave = { savedPoint ->
                        viewModel.upsertKnowledgePoint(presetId, savedPoint)
                        navController.popBackStack()
                    }
                )
            } else {
                androidx.compose.runtime.LaunchedEffect(Unit) {
                    navController.popBackStack()
                }
            }
        }
        }

    }
    }
}
