/**
 * SettingsPage.ets - App settings with section cards, pickers, toggles.
 * Mirrors SettingsView in source/Kikaria/ContentView.swift (line 3441).
 *
 * Sections: Profile, Daily Goal, Danger Threshold, Countdown,
 *           Notifications, Appearance, About.
 */
import { appState } from '../data/AppState'
import { KikariaColors } from '../components/KikariaTheme'
import {
  SettingsSectionCard, SettingsSectionDivider, SettingsListRow,
  SettingsToggleRow, SettingsInfoTextRow, SettingsStepperRow
} from '../components/KikariaComponents'
import { isPadLandscape, isTwoColumnCapable, pageHorizontalPadding, ipadPortraitSettingsTopInset,
  settingsLandscapeLeftWidth, settingsLandscapeRightWidth, settingsLandscapeColumnSpacing,
  settingsLandscapeMaxWidth } from '../components/KikariaAdaptiveLayout'
import { notificationService } from '../data/NotificationService'
import { navPathStack } from '../data/NavigationService'

@Builder
export function SettingsPageBuilder(name: string, param: Object) {
  SettingsPageContent()
}

@Component
export struct SettingsPageContent {
  @StorageLink('kikaria_isDarkMode') isDark: boolean = false
  @State displayName: string = ''
  @State userHandle: string = ''
  @State presetName: string = ''
  @State dailyGoal: number = 20
  @State dangerPercent: number = 80
  @State notificationsEnabled: boolean = false
  @State masteredCount: number = 0
  @State totalCount: number = 0
  @State countdownEndDateStr: string = '未设置'
  @State notificationTimeStr: string = '21:00'
  @State showCountdownPicker: boolean = false
  @State showNotificationPicker: boolean = false
  @State showDangerPicker: boolean = false
  @State showGoalPicker: boolean = false
  @State pickerEndYear: number = 2026
  @State pickerEndMonth: number = 6
  @State pickerEndDay: number = 15
  @State pickerHour: number = 21
  @State pickerMinute: number = 0
  @State contentPadH: number = 24
  @State dangerValue: number = 80
  @State useTwoColumn: boolean = false

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    this.useTwoColumn = isTwoColumnCapable()
    this.refreshState()
  }

  refreshState(): void {
    this.displayName = appState.userProfile.displayName
    this.userHandle = appState.userProfile.userHandle
    this.presetName = appState.currentPreset.name
    this.dailyGoal = appState.dailyGoal
    this.dangerPercent = appState.dangerPercent
    this.dangerValue = appState.dangerPercent
    this.notificationsEnabled = appState.notificationsEnabled
    this.masteredCount = appState.masteredCount
    this.totalCount = appState.totalCount

    if (appState.countdownEndDate !== null) {
      const d = new Date(appState.countdownEndDate)
      this.countdownEndDateStr = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
      this.pickerEndYear = d.getFullYear()
      this.pickerEndMonth = d.getMonth() + 1
      this.pickerEndDay = d.getDate()
    } else {
      this.countdownEndDateStr = '未设置'
    }

    const nt = new Date(appState.notificationTime)
    this.notificationTimeStr = `${String(nt.getHours()).padStart(2, '0')}:${String(nt.getMinutes()).padStart(2, '0')}`
    this.pickerHour = nt.getHours()
    this.pickerMinute = nt.getMinutes()
  }

  handleNotificationToggle(value: boolean): void {
    if (value) {
      notificationService.requestPermission().then((granted: boolean) => {
        if (granted) {
          this.notificationsEnabled = true
          appState.updateNotificationsEnabled(true)
          notificationService.scheduleReminder()
        } else {
          this.notificationsEnabled = false
          appState.updateNotificationsEnabled(false)
        }
      })
    } else {
      this.notificationsEnabled = false
      appState.updateNotificationsEnabled(false)
      notificationService.cancelReminder()
    }
  }

  /* ── Section header label ── */
  @Builder
  sectionHeader(label: string) {
    Text(label)
      .fontSize(13)
      .fontWeight(FontWeight.Medium)
      .fontColor(KikariaColors.SOFT_TEXT)
      .width('100%')
      .padding({ left: 4, bottom: 2 })
  }

  /* ── Profile row ── */
  @Builder
  profileRow() {
    Row({ space: 12 }) {
      Row() {
        Text(this.displayName.charAt(0).toUpperCase())
          .fontSize(28)
          .fontWeight(FontWeight.Bold)
          .fontColor('#FFFFFF')
      }
      .width(56)
      .height(56)
      .borderRadius(28)
      .backgroundColor(KikariaColors.SKY)
      .justifyContent(FlexAlign.Center)

      Column({ space: 4 }) {
        Text(this.displayName)
          .fontSize(17)
          .fontWeight(FontWeight.Medium)
          .fontColor(KikariaColors.DEEP_TEXT)

        Text(`@${this.userHandle}`)
          .fontSize(13)
          .fontColor(KikariaColors.SOFT_TEXT)
      }
      .alignItems(HorizontalAlign.Start)

      Blank()

      Text('编辑 ›')
        .fontSize(14)
        .fontColor(KikariaColors.SKY)
    }
    .width('100%')
    .padding(16)
    .onClick(() => {
      navPathStack.pushPathByName('EditProfilePage', undefined)
    })
  }

  /* ── Landscape profile summary (left column) ── */
  @Builder
  landscapeProfileSummary() {
    Column({ space: 16 }) {
      Row() {
        Text(this.displayName.charAt(0).toUpperCase())
          .fontSize(42)
          .fontWeight(FontWeight.Bold)
          .fontColor('#FFFFFF')
      }
      .width(72)
      .height(72)
      .borderRadius(36)
      .backgroundColor(KikariaColors.SKY)
      .justifyContent(FlexAlign.Center)

      Column({ space: 4 }) {
        Text(this.displayName)
          .fontSize(24)
          .fontWeight(FontWeight.Bold)
          .fontColor(KikariaColors.DEEP_TEXT)
          .fontFamily('serif')

        Text(`@${this.userHandle}`)
          .fontSize(15)
          .fontColor(KikariaColors.SOFT_TEXT)
      }
      .alignItems(HorizontalAlign.Center)

      Button('编辑个人资料')
        .fontSize(16)
        .fontWeight(FontWeight.Medium)
        .fontColor(KikariaColors.DEEP_TEXT)
        .borderRadius(999)
        .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
        .backdropBlur(14)
        .height(44)
        .padding({ left: 24, right: 24 })
        .onClick(() => {
          navPathStack.pushPathByName('EditProfilePage', undefined)
        })
    }
    .width('100%')
    .alignItems(HorizontalAlign.Center)
  }

  /* ── Shared settings sections (Daily Goal, Danger, Countdown, Notifications, Appearance, About) ── */
  @Builder
  settingsContent() {
    Column({ space: 4 }) {
      this.sectionHeader('每日目标')
      SettingsSectionCard() {
        SettingsStepperRow({
          label: '每日掌握数量', value: this.dailyGoal, min: 1, max: 200,
          valueColor: KikariaColors.DEEP_TEXT,
          onDecrement: () => { if (this.dailyGoal > 1) { this.dailyGoal--; appState.updateDailyGoal(this.dailyGoal) } },
          onIncrement: () => { if (this.dailyGoal < 200) { this.dailyGoal++; appState.updateDailyGoal(this.dailyGoal) } }
        })
        SettingsSectionDivider()
        SettingsInfoTextRow({ text: `当前: ${this.masteredCount}/${this.totalCount} 已掌握 · 预设: ${this.presetName}` })
      }
    }.width('100%')

    Column({ space: 4 }) {
      this.sectionHeader('预警阈值')
      SettingsSectionCard() {
        SettingsStepperRow({
          label: '进度低于预期时提醒', value: this.dangerPercent, min: 5, max: 100,
          valueColor: KikariaColors.DEEP_TEXT,
          onDecrement: () => { if (this.dangerPercent > 5) { this.dangerPercent -= 5; appState.updateDangerPercent(this.dangerPercent) } },
          onIncrement: () => { if (this.dangerPercent < 100) { this.dangerPercent += 5; appState.updateDangerPercent(this.dangerPercent) } }
        })
        SettingsSectionDivider()
        SettingsInfoTextRow({ text: '当实际掌握进度低于预设的预期百分比时触发提醒。' })
      }
    }.width('100%')

    Column({ space: 4 }) {
      this.sectionHeader('倒计时')
      SettingsSectionCard() {
        SettingsListRow({ label: '截止日期', value: this.countdownEndDateStr, onTap: () => { this.showCountdownPicker = true } })
        SettingsSectionDivider()
        Row({ space: 8 }) {
          Button('设置日期').fontSize(14).fontWeight(FontWeight.Medium).fontColor('#FFFFFF').backgroundColor(KikariaColors.SKY).borderRadius(12).height(36).padding({ left: 16, right: 16 }).onClick(() => { this.showCountdownPicker = true })
          Button('清除').fontSize(14).fontWeight(FontWeight.Medium).fontColor(KikariaColors.SOFT_TEXT).backgroundColor(KikariaColors.MIST).borderRadius(12).height(36).padding({ left: 16, right: 16 }).onClick(() => { appState.updateCountdownRange(null, null); this.countdownEndDateStr = '未设置'; this.refreshState() })
        }.width('100%').padding({ left: 16, right: 16, top: 10, bottom: 14 })
      }
    }.width('100%')

    Column({ space: 4 }) {
      this.sectionHeader('通知')
      SettingsSectionCard() {
        SettingsToggleRow({ label: '学习进度提醒', isOn: this.notificationsEnabled, onChange: (value: boolean) => { this.handleNotificationToggle(value) } })
        SettingsSectionDivider()
        SettingsListRow({ label: '提醒时间', value: this.notificationTimeStr, onTap: () => { this.showNotificationPicker = true } })
        SettingsSectionDivider()
        SettingsInfoTextRow({ text: '需要先设置倒计时截止日期才能启用通知提醒。' })
      }
    }.width('100%')

    Column({ space: 4 }) {
      this.sectionHeader('外观')
      SettingsSectionCard() {
        SettingsToggleRow({ label: '暗色模式', isOn: this.isDark, onChange: (value: boolean) => { appState.setDarkMode(value) } })
      }
    }.width('100%')

    Column({ space: 4 }) {
      this.sectionHeader('关于')
      SettingsSectionCard() {
        SettingsListRow({ label: 'Markdown 格式指南', onTap: () => { navPathStack.pushPathByName('MarkdownFormatGuide', undefined) } })
        SettingsSectionDivider()
        SettingsListRow({ label: '新手指引', onTap: () => { navPathStack.pushPathByName('OnboardingPage', undefined) } })
        SettingsSectionDivider()
        SettingsListRow({ label: '预设管理', onTap: () => { navPathStack.pushPathByName('PresetSelectionPage', undefined) } })
      }
    }.width('100%')

    Text('Kikaria HarmonyOS · v1.0.0')
      .fontSize(12).fontColor(KikariaColors.TERTIARY_TEXT)
      .width('100%').textAlign(TextAlign.Center).padding({ top: 4 })

    Row().height(40)
  }

  /* ── Overlay backdrop ── */
  @Builder
  overlayBackdrop(onDismiss: () => void) {
    Column()
      .width('100%')
      .height('100%')
      .backgroundColor(KikariaColors.OVERLAY_BG)
      .onClick(() => onDismiss())
  }

  /* ── Picker button row ── */
  @Builder
  pickerButtonRow(onCancel: () => void, onConfirm: () => void) {
    Row({ space: 12 }) {
      Button('取消')
        .fontSize(15)
        .fontWeight(FontWeight.Medium)
        .fontColor(KikariaColors.SOFT_TEXT)
        .backgroundColor(KikariaColors.MIST)
        .borderRadius(12)
        .height(42)
        .layoutWeight(1)
        .onClick(() => onCancel())

      Button('确定')
        .fontSize(15)
        .fontWeight(FontWeight.Bold)
        .fontColor('#FFFFFF')
        .backgroundColor(KikariaColors.SKY)
        .borderRadius(12)
        .height(42)
        .layoutWeight(1)
        .onClick(() => onConfirm())
    }
    .width('100%')
  }

  build() {
    NavDestination() {
      Stack() {
        Scroll() {
        Column({ space: 18 }) {
          // Top bar
          Row() {
            Button() {
              Text('← 返回')
                .fontSize(17)
                .fontColor(KikariaColors.SOFT_TEXT)
            }
            .backgroundColor(Color.Transparent)
            .onClick(() => { navPathStack.pop() })

            Blank()

            Text('设置')
              .fontSize(17)
              .fontWeight(FontWeight.Medium)
              .fontColor(KikariaColors.DEEP_TEXT)
          }
          .width('100%')
          .padding({ left: 20, right: 20, top: 12 + ipadPortraitSettingsTopInset(), bottom: 12 })

          if (this.useTwoColumn) {
            Row({ space: settingsLandscapeColumnSpacing() }) {
              Column() {
                this.landscapeProfileSummary()
              }
              .width(settingsLandscapeLeftWidth())
              .alignItems(HorizontalAlign.Center)
              .justifyContent(FlexAlign.Center)

              Column({ space: 14 }) {
                this.settingsContent()
              }
              .width(settingsLandscapeRightWidth())
            }
            .width('100%')
            .constraintSize({ maxWidth: settingsLandscapeMaxWidth() })
          } else {
            Column({ space: 4 }) {
              this.sectionHeader('个人资料')
              SettingsSectionCard() { this.profileRow() }
            }.width('100%')

            this.settingsContent()
          }
        }
        .width('100%')
        .constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' })
        .padding({ left: this.contentPadH, right: this.contentPadH })
      }
      .width('100%')
      .height('100%')
      .backgroundColor(KikariaColors.PAGE_BG)
      .scrollBar(BarState.Off)

        // ── Countdown date picker overlay ──
        if (this.showCountdownPicker) {
          Stack() {
            this.overlayBackdrop(() => { this.showCountdownPicker = false })

            Column({ space: 16 }) {
              Text('设置截止日期')
                .fontSize(17)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.DEEP_TEXT)

              Row({ space: 8 }) {
                TextInput({ text: `${this.pickerEndYear}` })
                  .fontSize(18)
                  .fontWeight(FontWeight.Bold)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .textAlign(TextAlign.Center)
                  .backgroundColor(KikariaColors.MIST)
                  .borderRadius(10)
                  .width(70)
                  .height(44)
                  .type(InputType.Number)
                  .onChange((v: string) => { this.pickerEndYear = parseInt(v) || this.pickerEndYear })

                Text('/').fontSize(20).fontColor(KikariaColors.TERTIARY_TEXT)

                TextInput({ text: `${String(this.pickerEndMonth).padStart(2, '0')}` })
                  .fontSize(18)
                  .fontWeight(FontWeight.Bold)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .textAlign(TextAlign.Center)
                  .backgroundColor(KikariaColors.MIST)
                  .borderRadius(10)
                  .width(55)
                  .height(44)
                  .type(InputType.Number)
                  .onChange((v: string) => { this.pickerEndMonth = parseInt(v) || this.pickerEndMonth })

                Text('/').fontSize(20).fontColor(KikariaColors.TERTIARY_TEXT)

                TextInput({ text: `${String(this.pickerEndDay).padStart(2, '0')}` })
                  .fontSize(18)
                  .fontWeight(FontWeight.Bold)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .textAlign(TextAlign.Center)
                  .backgroundColor(KikariaColors.MIST)
                  .borderRadius(10)
                  .width(55)
                  .height(44)
                  .type(InputType.Number)
                  .onChange((v: string) => { this.pickerEndDay = parseInt(v) || this.pickerEndDay })
              }

              this.pickerButtonRow(
                () => { this.showCountdownPicker = false },
                () => {
                  const d = new Date(this.pickerEndYear, this.pickerEndMonth - 1, this.pickerEndDay, 23, 59, 59)
                  appState.updateCountdownRange(null, d.getTime())
                  this.showCountdownPicker = false
                  this.refreshState()
                }
              )
            }
            .width('80%')
            .padding(24)
            .borderRadius(20)
            .backgroundColor(KikariaColors.CARD_BG)
            .shadow({ radius: 24, color: KikariaColors.SHADOW_COLOR, offsetY: 8 })
          }
          .width('100%')
          .height('100%')
        }

        // ── Notification time picker overlay ──
        if (this.showNotificationPicker) {
          Stack() {
            this.overlayBackdrop(() => { this.showNotificationPicker = false })

            Column({ space: 16 }) {
              Text('设置提醒时间')
                .fontSize(17)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.DEEP_TEXT)

              Row({ space: 8 }) {
                TextInput({ text: `${String(this.pickerHour).padStart(2, '0')}` })
                  .fontSize(18)
                  .fontWeight(FontWeight.Bold)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .textAlign(TextAlign.Center)
                  .backgroundColor(KikariaColors.MIST)
                  .borderRadius(10)
                  .width(60)
                  .height(44)
                  .type(InputType.Number)
                  .onChange((v: string) => { this.pickerHour = parseInt(v) || this.pickerHour })

                Text(':').fontSize(20).fontColor(KikariaColors.TERTIARY_TEXT)

                TextInput({ text: `${String(this.pickerMinute).padStart(2, '0')}` })
                  .fontSize(18)
                  .fontWeight(FontWeight.Bold)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .textAlign(TextAlign.Center)
                  .backgroundColor(KikariaColors.MIST)
                  .borderRadius(10)
                  .width(60)
                  .height(44)
                  .type(InputType.Number)
                  .onChange((v: string) => { this.pickerMinute = parseInt(v) || this.pickerMinute })
              }

              this.pickerButtonRow(
                () => { this.showNotificationPicker = false },
                () => {
                  const d = new Date()
                  d.setHours(this.pickerHour, this.pickerMinute, 0, 0)
                  appState.updateNotificationTime(d.getTime())
                  this.showNotificationPicker = false
                  this.refreshState()
                }
              )
            }
            .width('80%')
            .padding(24)
            .borderRadius(20)
            .backgroundColor(KikariaColors.CARD_BG)
            .shadow({ radius: 24, color: KikariaColors.SHADOW_COLOR, offsetY: 8 })
          }
          .width('100%')
          .height('100%')
        }
      }
    }
    .hideTitleBar(true)
    .width('100%')
    .height('100%')
  }
}

@Entry
@Component
struct SettingsPage {
  build() {
    Navigation() {
      SettingsPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
