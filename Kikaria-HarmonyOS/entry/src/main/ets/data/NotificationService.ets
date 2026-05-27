/**
 * NotificationService - Degradable wrapper for @ohos.notificationManager.
 * All methods have try-catch fallbacks; failures are logged but never crash.
 */
import { notificationManager } from '@kit.NotificationKit'
import { wantAgent } from '@kit.AbilityKit'
import { appState } from '../data/AppState'

const NOTIFICATION_ID = 1001

export class NotificationService {
  /**
   * Request notification permission. Returns true if granted, false if denied/error.
   */
  async requestPermission(): Promise<boolean> {
    try {
      await notificationManager.requestEnableNotification()
      return true
    } catch (e) {
      console.warn(`Kikaria: Notification permission denied: ${e}`)
      return false
    }
  }

  /**
   * Check if notifications are currently enabled.
   */
  async isEnabled(): Promise<boolean> {
    try {
      return await notificationManager.isNotificationEnabled()
    } catch (e) {
      console.warn(`Kikaria: Notification check failed: ${e}`)
      return false
    }
  }

  /**
   * Schedule a study reminder notification at the configured time.
   * Silently degrades if permission not granted or API fails.
   */
  async scheduleReminder(): Promise<void> {
    try {
      const enabled = await this.isEnabled()
      if (!enabled) {
        return
      }
      if (appState.countdownEndDate === null) {
        return
      }

      const daysLeft = appState.countdownDayCount
      const progressRatio = appState.totalCount > 0
        ? appState.masteredCount / appState.totalCount
        : 0

      let message = '今日学习时间到了，继续巩固知识点吧。'
      if (daysLeft !== null && daysLeft <= 7) {
        message = `距离截止还有 ${daysLeft} 天，今日已掌握 ${appState.todayMarkedMasteredCount}/${appState.dailyGoal} 个知识点。`
      }
      if (progressRatio >= 0.8 && daysLeft !== null && daysLeft > 0) {
        message = `即将完成！已掌握 ${appState.masteredCount}/${appState.totalCount}，最后冲刺！`
      }

      const wantAgentInfo: wantAgent.WantAgentInfo = {
        wants: [{
          bundleName: 'com.vita0818.kikaria',
          abilityName: 'EntryAbility'
        }],
        requestCode: 0,
        operationType: wantAgent.OperationType.START_ABILITY,
        wantAgentFlags: [wantAgent.WantAgentFlags.UPDATE_PRESENT_FLAG]
      }

      const agent = await wantAgent.getWantAgent(wantAgentInfo)

      const notificationRequest: notificationManager.NotificationRequest = {
        id: NOTIFICATION_ID,
        content: {
          notificationContentType: notificationManager.ContentType.NOTIFICATION_CONTENT_BASIC_TEXT,
          normal: {
            title: 'Kikaria · 学习提醒',
            text: message
          }
        },
        wantAgent: agent
      }

      await notificationManager.publish(notificationRequest)
      console.info('Kikaria: Study reminder scheduled')
    } catch (e) {
      console.warn(`Kikaria: Notification schedule failed: ${e}`)
    }
  }

  /**
   * Cancel the scheduled study reminder.
   */
  async cancelReminder(): Promise<void> {
    try {
      await notificationManager.cancel(NOTIFICATION_ID)
      console.info('Kikaria: Study reminder cancelled')
    } catch (e) {
      console.warn(`Kikaria: Notification cancel failed: ${e}`)
    }
  }
}

export const notificationService = new NotificationService()
