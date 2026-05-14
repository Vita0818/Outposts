/**
 * EntryAbility.ets - Main entry point for the Kikaria HarmonyOS app.
 * Stage model UIAbility that loads the Index page.
 *
 * Translated from the @main KikariaApp struct in source/Kikaria/KikariaApp.swift
 * and the Stage model EntryAbility pattern for HarmonyOS.
 */

import { UIAbility, Want, AbilityConstant } from '@kit.AbilityKit'
import { window } from '@kit.ArkUI'

export default class EntryAbility extends UIAbility {
  onCreate(want: Want, launchParam: AbilityConstant.LaunchParam): void {
    // App initialization - no network, no accounts, local only.
    // The app state (presets, knowledge points) is initialized
    // lazily when the Index page first appears.
  }

  onDestroy(): void {
    // Cleanup if needed
  }

  onWindowStageCreate(windowStage: window.WindowStage): void {
    // Main window - load the Index page
    windowStage.loadContent('pages/Index', (err) => {
      if (err.code) {
        console.error(`Kikaria: Failed to load content: ${JSON.stringify(err)}`)
        return
      }
      console.info('Kikaria: Window content loaded successfully')
    })

    // Set the window background to the Kikaria page background color
    windowStage.getMainWindow().then((mainWindow: window.Window) => {
      mainWindow.setWindowBackgroundColor('#EDF9FF')
    })
  }

  onWindowStageDestroy(): void {
    // Release resources
  }

  onForeground(): void {
    // App comes to foreground
  }

  onBackground(): void {
    // App goes to background
  }
}
