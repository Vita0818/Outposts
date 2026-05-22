package com.vita0818.kikaria.ui.navigation

import androidx.compose.runtime.Composable
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
import com.vita0818.kikaria.ui.preset.NewPresetScreen
import com.vita0818.kikaria.ui.preset.PresetSelectionScreen
import com.vita0818.kikaria.ui.reinforcement.ReinforcementScreen
import com.vita0818.kikaria.ui.review.ReviewScreen
import com.vita0818.kikaria.ui.scope.ScopeSelectionScreen
import com.vita0818.kikaria.ui.settings.EditProfileScreen
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
    const val ONBOARDING = "onboarding"
    const val MARKDOWN_GUIDE = "markdown_guide"
    const val NEW_PRESET = "new_preset"
}

@Composable
fun KikariaNavGraph(
    navController: NavHostController = rememberNavController(),
    viewModel: KikariaViewModel = viewModel()
) {
    NavHost(
        navController = navController,
        startDestination = Routes.HOME
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
                onSetDailyGoal = { viewModel.setDailyGoal(it) },
                onSetCountdownRange = { start, end ->
                    viewModel.setCountdownRange(start, end)
                },
                onSetDangerPercent = { viewModel.setDangerPercent(it) },
                onToggleNotifications = { enabled ->
                    viewModel.setNotificationsEnabled(enabled)
                },
                onSetNotificationTime = { viewModel.setNotificationTime(it) },
                onOpenOnboarding = {
                    navController.navigate(Routes.ONBOARDING)
                },
                onOpenMarkdownGuide = {
                    navController.navigate(Routes.MARKDOWN_GUIDE)
                },
                onOpenPrivacyPolicy = {
                    // TODO: show privacy policy
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
                onEditPreset = {
                    // TODO: navigate to edit preset / markdown editor
                },
                onDeletePreset = { preset ->
                    viewModel.deletePreset(preset.id)
                }
            )
        }

        composable(Routes.EDIT_PROFILE) {
            EditProfileScreen(
                initialDisplayName = viewModel.userDisplayName,
                initialHandle = viewModel.userHandle,
                onBack = { navController.popBackStack() },
                onSave = { displayName, handle ->
                    viewModel.updateProfile(displayName, handle)
                }
            )
        }

        composable(Routes.ONBOARDING) {
            OnboardingScreen(
                onComplete = {
                    navController.popBackStack()
                }
            )
        }

        composable(Routes.MARKDOWN_GUIDE) {
            MarkdownFormatGuideScreen(
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
    }
}
