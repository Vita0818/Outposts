/**
 * Auth & Test detail page — mirrors iOS transcription authorization / test sub-page
 */
import { ConfigurationConstant } from '@kit.AbilityKit'
import { colorAlpha, RokuricsColors, FontWeight, applyColorMode } from '../utils/RokuricsTheme'
import { BackIcon } from '../utils/CustomIcons'

@Entry
@Component
struct AuthTestPage {
  @StorageLink('colorMode') colorMode: ConfigurationConstant.ColorMode = ConfigurationConstant.ColorMode.COLOR_MODE_NOT_SET
  @State isTesting: boolean = false
  @State testResult: string = ''
  @State lastTestTime: string = ''

  build() {
    applyColorMode(this.colorMode)
    Column() {
      // Header
      Row() {
        Button() {
          BackIcon(18, RokuricsColors.deepText)
        }
        .width(44).height(44).borderRadius(22)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'))
        .shadow({ color: colorAlpha(RokuricsColors.shadowColor, '10'), radius: 12, offsetY: 6 })
        .border({
          width: 1,
          color: {
            colors: [
              [0xFFFFFF, 0.22],
              [RokuricsColors.glassStroke, 0.12],
              [RokuricsColors.aqua, 0.24]
            ],
            direction: GradientDirection.RightBottom
          },
          radius: 22
        } as BorderOptions)
        .onClick(() => this.getUIContext().getRouter().back())

        Text('授权与测试')
          .fontSize(24)
          .fontWeight(FontWeight.Bold)
          .fontColor(RokuricsColors.deepText)
          .margin({ left: 8 })
      }
      .width('100%')
      .padding({ left: 16, right: 16, top: 56, bottom: 20 })

      Scroll() {
        Column({ space: 20 }) {
          // Provider status card
          Column({ space: 12 }) {
            Text('转写 Provider')
              .fontSize(15).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
              .width('100%')

            Column({ space: 4 }) {
              this.StatusRow('Provider', 'Mac 安全转写')
              Divider().strokeWidth(0.5).color(colorAlpha(RokuricsColors.softText, '10'))
                .margin({ left: 16, right: 16 })
              this.StatusRow('模型', 'whisper.cpp')
              Divider().strokeWidth(0.5).color(colorAlpha(RokuricsColors.softText, '10'))
                .margin({ left: 16, right: 16 })
              this.StatusRow('授权状态', '已授权')
              Divider().strokeWidth(0.5).color(colorAlpha(RokuricsColors.softText, '10'))
                .margin({ left: 16, right: 16 })
              this.StatusRow('本地引擎', 'CoreML / GPU')
            }
            .width('100%')
            .borderRadius(16)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'))
            .border({
              width: 1,
              color: {
                colors: [
                  [0xFFFFFF, 0.10],
                  [RokuricsColors.glassStroke, 0.08],
                  [RokuricsColors.glassStrokeAccent, 0.06]
                ],
                direction: GradientDirection.RightBottom
              },
              radius: 16
            } as BorderOptions)
          }
          .width('100%')

          // Test connection card
          Column({ space: 12 }) {
            Text('连接测试')
              .fontSize(15).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
              .width('100%')

            Text('测试本地转写引擎是否正常工作，将发送一段短测试音频进行验证。')
              .fontSize(13)
              .fontColor(RokuricsColors.softText)

            Button() {
              Text(this.isTesting ? '测试中...' : '开始测试')
                .fontSize(15).fontWeight(FontWeight.SemiBold)
                .fontColor(Color.White)
                .width('100%').height(44).justifyContent(FlexAlign.Center)
            }
            .width('100%').height(44).borderRadius(22)
            .backgroundColor(this.isTesting ? RokuricsColors.tertiaryText : RokuricsColors.aqua)
            .enabled(!this.isTesting)
            .shadow({
              color: colorAlpha(RokuricsColors.aqua, '20'),
              radius: 12, offsetY: 5
            })
            .onClick(() => this.runTest())

            if (this.testResult.length > 0) {
              Row({ space: 6 }) {
                Circle()
                  .width(8).height(8)
                  .fill(this.testResult.startsWith('测试通过') ? RokuricsColors.mint : RokuricsColors.coral)
                Text(this.testResult)
                  .fontSize(13)
                  .fontColor(this.testResult.startsWith('测试通过') ? RokuricsColors.mint : RokuricsColors.coral)
              }
            }

            if (this.lastTestTime.length > 0) {
              Text(`上次测试: ${this.lastTestTime}`)
                .fontSize(11)
                .fontColor(RokuricsColors.tertiaryText)
            }
          }
          .width('100%')
          .padding(18).borderRadius(20)
          .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'))
          .border({
            width: 1,
            color: {
              colors: [
                [0xFFFFFF, 0.10],
                [RokuricsColors.glassStroke, 0.08],
                [RokuricsColors.glassStrokeAccent, 0.06]
              ],
              direction: GradientDirection.RightBottom
            },
            radius: 20
          } as BorderOptions)

          // Log card
          Column({ space: 8 }) {
            Text('测试日志')
              .fontSize(14).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
              .width('100%')
            Text('暂无测试记录')
              .fontSize(12)
              .fontColor(RokuricsColors.tertiaryText)
          }
          .width('100%')
          .padding(16).borderRadius(16)
          .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'))
        }
        .width('100%')
        .padding({ left: 16, right: 16, bottom: 40 })
      }
      .width('100%').layoutWeight(1).scrollBar(BarState.Off)
    }
    .width('100%').height('100%')
    .linearGradient({
      direction: GradientDirection.RightBottom,
      colors: [
        [RokuricsColors.pageGradientStart, 1.0],
        [RokuricsColors.pageGradientMid, 1.0],
        [RokuricsColors.pageGradientEnd, 1.0]
      ]
    })
  }

  @Builder
  StatusRow(label: string, value: string) {
    Row() {
      Text(label)
        .fontSize(14).fontWeight(FontWeight.Medium)
        .fontColor(RokuricsColors.deepText)
      Blank()
      Text(value)
        .fontSize(14).fontWeight(FontWeight.Regular)
        .fontColor(RokuricsColors.softText)
    }
    .width('100%')
    .padding({ left: 16, right: 16, top: 12, bottom: 12 })
  }

  private runTest(): void {
    this.isTesting = true
    this.testResult = ''
    setTimeout(() => {
      this.isTesting = false
      this.testResult = '测试通过 · 引擎就绪 (Mock)'
      const d = new Date()
      this.lastTestTime = `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:${String(d.getSeconds()).padStart(2, '0')}`
    }, 1200)
  }
}
