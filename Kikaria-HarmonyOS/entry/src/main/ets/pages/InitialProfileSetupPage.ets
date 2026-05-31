/**
 * InitialProfileSetupPage.ets - First-launch profile setup wizard.
 * Appears before OnboardingPage to capture display name and handle.
 *
 * Translated from InitialProfileSetupView in source/Kikaria/ContentView.swift (line 6098)
 */
import { appState } from '../data/AppState'
import { KikariaColors } from '../components/KikariaTheme'
import { ProfileAvatar } from '../components/KikariaComponents'
import {
  isTabletWidth, isPadPortrait, pageHorizontalPadding,
  formMaxWidth, setupCardMaxWidth, avatarSize
} from '../components/KikariaAdaptiveLayout'
import { navPathStack } from '../data/NavigationService'

@Builder
export function InitialProfileSetupPageBuilder(name: string, param: Object) {
  InitialProfileSetupPageContent()
}

@Component
export struct InitialProfileSetupPageContent {
  @State displayName: string = ''
  @State userHandle: string = ''
  @State toastMessage: string = ''
  @State toastVisible: boolean = false
  @State contentPadH: number = 24

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    // Pre-fill with existing profile values if they're not defaults
    const p = appState.userProfile
    this.displayName = p.displayName
    this.userHandle = p.userHandle
  }

  get trimmedDisplayName(): string {
    return this.displayName.trim()
  }

  get canSave(): boolean {
    return this.trimmedDisplayName.length > 0
  }

  get isExpanded(): boolean {
    return isTabletWidth()
  }

  get setupMaxW(): number {
    return setupCardMaxWidth()
  }

  get avatarSz(): number {
    return avatarSize()
  }

  get cardRadius(): number {
    return this.isExpanded ? 38 : 34
  }

  get spacing(): number {
    return this.isExpanded ? 28 : 22
  }

  get innerSpacing(): number {
    return this.isExpanded ? 16 : 14
  }

  get titleSize(): number {
    return this.isExpanded ? 34 : 30
  }

  get subtitleSize(): number {
    return this.isExpanded ? 18 : 16
  }

  get buttonSize(): number {
    return this.isExpanded ? 18 : 17
  }

  showToast(msg: string): void {
    this.toastMessage = msg
    this.toastVisible = true
    setTimeout(() => { this.toastVisible = false }, 2000)
  }

  saveProfile(): void {
    if (!this.canSave) {
      return
    }
    const trimmedHandle = this.userHandle.trim().replace(/^@/, '')
    const finalHandle = trimmedHandle.length > 0 ? trimmedHandle : this.generatedHandle()
    appState.userProfile = {
      displayName: this.trimmedDisplayName,
      userHandle: finalHandle,
      avatarIconName: 'person'
    }
    appState.completeProfileSetup()
    appState.saveAppState()
    // Navigate to onboarding next
    navPathStack.replacePathByName('OnboardingPage', undefined)
  }

  generatedHandle(): string {
    const normalized = this.trimmedDisplayName
      .toLowerCase()
      .replace(/[^a-z0-9_一-鿿]/g, '_')
      .replace(/_+/g, '_')
      .replace(/^_|_$/g, '')
    return normalized.length > 0 ? normalized : 'kikaria_user'
  }

  build() {
    NavDestination() {
      Stack() {
        Scroll() {
          Column({ space: this.spacing }) {
            Column({ space: this.isExpanded ? 12 : 10 }) {
              Text('欢迎使用 Kikaria')
                .fontSize(this.titleSize)
                .fontWeight(FontWeight.Bold)
                .fontColor(KikariaColors.DEEP_TEXT)

              Text('先设置你的个人资料')
                .fontSize(this.subtitleSize)
                .fontWeight(FontWeight.Medium)
                .fontColor(KikariaColors.SOFT_TEXT)
            }
            .width('100%')
            .alignItems(HorizontalAlign.Center)

            // Avatar
            ProfileAvatar({
              displayName: this.trimmedDisplayName.length > 0 ? this.trimmedDisplayName : 'K',
              avatarSize: this.avatarSz,
              avatarFontSize: Math.round(this.avatarSz * 0.38),
              avatarColor: KikariaColors.SKY,
              textColor: '#FFFFFF'
            })

            // Form fields
            Column({ space: this.innerSpacing }) {
              Column({ space: 6 }) {
                Text('昵称')
                  .fontSize(13)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SOFT_TEXT)
                  .width('100%')
                  .padding({ left: 4 })

                TextInput({ text: this.displayName, placeholder: '输入你的昵称' })
                  .fontSize(17)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
                  .borderRadius(14)
                  .padding(16)
                  .width('100%')
                  .onChange((value: string) => {
                    this.displayName = value
                  })
              }
              .width('100%')

              Column({ space: 6 }) {
                Text('用户名')
                  .fontSize(13)
                  .fontWeight(FontWeight.Medium)
                  .fontColor(KikariaColors.SOFT_TEXT)
                  .width('100%')
                  .padding({ left: 4 })

                TextInput({ text: this.userHandle, placeholder: 'kikaria_user' })
                  .fontSize(17)
                  .fontColor(KikariaColors.DEEP_TEXT)
                  .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
                  .borderRadius(14)
                  .padding(16)
                  .width('100%')
                  .onChange((value: string) => {
                    this.userHandle = value.replace(/^@/, '')
                  })
              }
              .width('100%')
            }
            .width('100%')

            // Start button
            Button() {
              Text('开始使用')
                .fontSize(this.buttonSize)
                .fontWeight(FontWeight.Bold)
                .fontColor('#FFFFFF')
            }
            .width('100%')
            .height(this.isExpanded ? 56 : 52)
            .borderRadius(999)
            .linearGradient({
              angle: 135,
              colors: [[KikariaColors.SKY, 0], [KikariaColors.CYAN, 1]]
            })
            .shadow({
              radius: this.canSave ? 16 : 4,
              color: `${KikariaColors.SKY}${this.canSave ? '38' : '0A'}`,
              offsetY: 8
            })
            .enabled(this.canSave)
            .opacity(this.canSave ? 1.0 : 0.48)
            .margin({ top: 4 })
            .onClick(() => {
              this.saveProfile()
            })
          }
          .padding(this.isExpanded ? 32 : 24)
          .width(`${this.setupMaxW}vp`)
          .borderRadius(this.cardRadius)
          .backgroundColor(KikariaColors.CARD_BG_TRANSLUCENT)
          .backdropBlur(24)
          .shadow({
            radius: this.isExpanded ? 28 : 24,
            color: KikariaColors.SHADOW_COLOR,
            offsetY: this.isExpanded ? 16 : 14
          })
          .padding({ left: this.contentPadH, right: this.contentPadH })
          .constraintSize({ maxWidth: formMaxWidth() })
          .width('100%')
          .height('100%')
          .justifyContent(FlexAlign.Center)
        }
        .width('100%')
        .height('100%')
        .backgroundColor(KikariaColors.PAGE_BG)
        .scrollBar(BarState.Off)
        .align(Alignment.Center)

        // Toast
        if (this.toastVisible) {
          Row() {
            Text(this.toastMessage)
              .fontSize(15)
              .fontColor('#FFFFFF')
              .padding({ left: 20, right: 20, top: 12, bottom: 12 })
          }
          .borderRadius(20)
          .backgroundColor(`${KikariaColors.DEEP_TEXT}E6`)
          .position({ top: 80, left: '10%' })
          .width('80%')
          .justifyContent(FlexAlign.Center)
        }
      }
      .width('100%')
      .height('100%')
    }
    .hideTitleBar(true)
    .width('100%')
    .height('100%')
  }
}

@Entry
@Component
struct InitialProfileSetupPage {
  build() {
    Navigation() {
      InitialProfileSetupPageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
