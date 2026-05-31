/**
 * Privacy policy page — minimal stub mirroring iOS sub-page pattern
 */
import { ConfigurationConstant } from '@kit.AbilityKit'
import { colorAlpha, RokuricsColors, FontWeight, applyColorMode } from '../utils/RokuricsTheme'
import { BackIcon } from '../utils/CustomIcons'

@Entry
@Component
struct PrivacyPolicyPage {
  @StorageLink('colorMode') colorMode: ConfigurationConstant.ColorMode = ConfigurationConstant.ColorMode.COLOR_MODE_NOT_SET

  build() {
    applyColorMode(this.colorMode)
    Column() {
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

        Text('隐私政策')
          .fontSize(24)
          .fontWeight(FontWeight.Bold)
          .fontColor(RokuricsColors.deepText)
          .margin({ left: 8 })
      }
      .width('100%')
      .padding({ left: 16, right: 16, top: 56, bottom: 20 })

      Scroll() {
        Column({ space: 16 }) {
          Column({ space: 12 }) {
            Text('Rokurics 隐私政策')
              .fontSize(18).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
            Text('最后更新: 2026-05')
              .fontSize(12).fontColor(RokuricsColors.tertiaryText)
          }
          .width('100%')

          Column({ space: 14 }) {
            Text('数据收集')
              .fontSize(15).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
            Text('Rokurics 将所有录音数据、转写文本和 AI 笔记存储在您的本地设备上。默认情况下，不会向任何远程服务器上传数据。')
              .fontSize(13).fontColor(RokuricsColors.softText).lineHeight(20)

            Text('第三方服务')
              .fontSize(15).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
            Text('如您选择配置 OpenAI 兼容 API 进行 AI 处理，您的对话内容将发送至您指定的 API 端点。Rokurics 不托管或中转这些请求。')
              .fontSize(13).fontColor(RokuricsColors.softText).lineHeight(20)

            Text('录音权限')
              .fontSize(15).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
            Text('Rokurics 仅在您主动发起录音时使用麦克风权限。录音文件以原始格式保存在应用沙盒内，其他应用无法访问。')
              .fontSize(13).fontColor(RokuricsColors.softText).lineHeight(20)

            Text('数据删除')
              .fontSize(15).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
            Text('您可以在应用内随时删除任何录音、转写或笔记。删除操作不可撤销。')
              .fontSize(13).fontColor(RokuricsColors.softText).lineHeight(20)
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
}
