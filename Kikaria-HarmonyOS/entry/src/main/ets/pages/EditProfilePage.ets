/**
 * EditProfilePage.ets - Edit user profile (display name, handle).
 *
 * Translated from EditProfileView in source/Kikaria/ContentView.swift (line 6340)
 */
import { appState } from '../data/AppState'
import { KikariaColors } from '../components/KikariaTheme'
import { isPadLandscape, pageHorizontalPadding } from '../components/KikariaAdaptiveLayout'
import { navPathStack } from '../data/NavigationService'

@Builder
export function EditProfilePageBuilder(name: string, param: Object) {
  EditProfilePageContent()
}

@Component
export struct EditProfilePageContent {
  @State displayName: string = ''
  @State userHandle: string = ''
  @State contentPadH: number = 24

  aboutToAppear(): void {
    this.contentPadH = pageHorizontalPadding()
    this.displayName = appState.userProfile.displayName
    this.userHandle = appState.userProfile.userHandle
  }

  build() {
    NavDestination() {
      Column({ space: 18 }) {
        // Top bar
        Row() {
          Button() {
            Text('取消')
              .fontSize(17)
              .fontColor(KikariaColors.SOFT_TEXT)
          }
          .backgroundColor(Color.Transparent)
          .onClick(() => { navPathStack.pop() })

          Blank()

          Text('编辑资料')
            .fontSize(17)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.DEEP_TEXT)

          Blank()

          Button() {
            Text('保存')
              .fontSize(17)
              .fontWeight(FontWeight.Bold)
              .fontColor(KikariaColors.SKY)
          }
          .backgroundColor(Color.Transparent)
          .onClick(() => {
            const trimmedName = this.displayName.trim()
            const trimmedHandle = this.userHandle.trim()
            if (trimmedName.length === 0) {
              return
            }
            appState.userProfile = {
              displayName: trimmedName,
              userHandle: trimmedHandle.length > 0 ? trimmedHandle.replace(/^@/, '') : 'user',
              avatarIconName: 'person'
            }
            appState.saveAppState()
            navPathStack.pop()
          })
        }
        .width('100%')
        .padding({ left: 20, right: 20, top: 12, bottom: 12 })

        // Avatar placeholder
        Column({ space: 12 }) {
          Row() {
            Text(this.displayName.trim().charAt(0).toUpperCase())
              .fontSize(40)
              .fontWeight(FontWeight.Bold)
              .fontColor('#FFFFFF')
          }
          .width(80)
          .height(80)
          .borderRadius(40)
          .backgroundColor(KikariaColors.SKY)
          .justifyContent(FlexAlign.Center)

          Text('轻触更换头像（暂不支持）')
            .fontSize(12)
            .fontColor(KikariaColors.SOFT_TEXT)
        }
        .width('100%')
        .padding({ top: 12, bottom: 8 })

        // Name field
        Column({ space: 8 }) {
          Text('显示名称')
            .fontSize(13)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.SOFT_TEXT)
            .width('100%')

          TextInput({ text: this.displayName, placeholder: '输入名称' })
            .fontSize(17)
            .fontColor(KikariaColors.DEEP_TEXT)
            .backgroundColor(KikariaColors.CARD_BG)
            .borderRadius(12)
            .padding({ left: 16, right: 16, top: 12, bottom: 12 })
            .width('100%')
            .onChange((value: string) => {
              this.displayName = value
            })
        }
        .width('100%')

        // Handle field
        Column({ space: 8 }) {
          Text('用户标识')
            .fontSize(13)
            .fontWeight(FontWeight.Medium)
            .fontColor(KikariaColors.SOFT_TEXT)
            .width('100%')

          TextInput({ text: this.userHandle, placeholder: 'user_handle' })
            .fontSize(17)
            .fontColor(KikariaColors.DEEP_TEXT)
            .backgroundColor(KikariaColors.CARD_BG)
            .borderRadius(12)
            .padding({ left: 16, right: 16, top: 12, bottom: 12 })
            .width('100%')
            .onChange((value: string) => {
              this.userHandle = value.replace(/^@/, '')
            })
        }
        .width('100%')
      }
      .width('100%')
      .constraintSize({ maxWidth: isPadLandscape() ? 760 : '100%' })
      .padding({ left: this.contentPadH, right: this.contentPadH })
      .backgroundColor(KikariaColors.PAGE_BG)
    }
    .hideTitleBar(true)
    .width('100%')
    .height('100%')
  }
}

@Entry
@Component
struct EditProfilePage {
  build() {
    Navigation() {
      EditProfilePageContent()
    }
    .width('100%')
    .height('100%')
    .backgroundColor(KikariaColors.PAGE_BG)
    .hideTitleBar(true)
    .hideBackButton(true)
  }
}
