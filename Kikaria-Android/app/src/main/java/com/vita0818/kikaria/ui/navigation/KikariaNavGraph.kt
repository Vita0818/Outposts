package com.vita0818.kikaria.ui.navigation

import androidx.compose.runtime.Composable
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.vita0818.kikaria.ui.home.HomeScreen
import com.vita0818.kikaria.ui.mastered.MasteredScreen
import com.vita0818.kikaria.ui.presets.PresetSelectionScreen
import com.vita0818.kikaria.ui.reinforcement.ReinforcementScreen
import com.vita0818.kikaria.ui.review.ReviewScreen
import com.vita0818.kikaria.ui.scope.ScopeSelectionScreen
import com.vita0818.kikaria.viewmodel.KikariaViewModel
import com.vita0818.kikaria.viewmodel.ReviewMode

/**
 * Navigation route constants. Each screen has a unique string route.
 * Translated from the `AppRoute` enum in ContentView.swift.
 */
object Routes {
    const val HOME = "home"
    const val REVIEW = "review"
    const val SCOPE = "scope"
    const val REINFORCEMENT = "reinforcement"
    const val MASTERED = "mastered"
    const val PRESETS = "presets"
}

/**
 * Root navigation graph for the Kikaria app.
 * Uses Jetpack Navigation Compose with string-based routes.
 * Translated from the NavigationStack(path:) + AppRoute system in ContentView.swift.
 */
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
                    navController.navigate(Routes.PRESETS)
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

        composable(Routes.PRESETS) {
            PresetSelectionScreen(
                viewModel = viewModel,
                onBack = {
                    navController.popBackStack()
                }
            )
        }
    }
}
