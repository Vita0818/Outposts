/**
 * Mac connection page - mirrors MacConnectionView.swift
 * Pairing form, connection test, paired device view
 */
import { ConfigurationConstant } from '@kit.AbilityKit'
import { colorAlpha, RokuricsColors, FontWeight, glassFillOpacity, glassStrokeHighOpacity,
  glassStrokeMidOpacity, glassAccentOpacity, applyColorMode } from '../utils/RokuricsTheme'
import { BackIcon, ConnectionIcon } from '../utils/CustomIcons'
import { DeviceConnectionCard } from '../components/DeviceConnectionCard'

function nowTimeLabel(): string {
  const d = new Date()
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

@Entry
@Component
struct MacConnectionPage {
  @State isPaired: boolean = false
  @State macHost: string = ''
  @State macPortText: string = '8787'
  @State macFingerprint: string = ''
  @State pairingCode: string = ''
  @State deviceModel: string = ''
  @State connectionStatus: string = 'offline'
  @State isTesting: boolean = false
  @State isPairing: boolean = false
  @StorageLink('colorMode') colorMode: ConfigurationConstant.ColorMode = ConfigurationConstant.ColorMode.COLOR_MODE_NOT_SET
  @State feedbackText: string = ''
  @State feedbackError: string = ''
  @State isFingerprintVisible: boolean = false

  build() {
    applyColorMode(this.colorMode)
    Column() {
      // Header
      Row() {
        Button() {
          BackIcon(18, RokuricsColors.deepText)
        }
        .width(44)
        .height(44)
        .borderRadius(22)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
        .shadow({
          color: colorAlpha(RokuricsColors.shadowColor, '10'),
          radius: 12,
          offsetY: 6
        })
        .border({
          width: 1,
          color: {
            colors: [
              [0xFFFFFF, glassStrokeHighOpacity],
              [RokuricsColors.glassStroke, glassStrokeMidOpacity],
              [RokuricsColors.aqua, 0.24]
            ],
            direction: GradientDirection.RightBottom
          },
          radius: 22
        } as BorderOptions)
        .onClick(() => this.getUIContext().getRouter().back())

        Text('Mac 连接')
          .fontSize(30)
          .fontWeight(FontWeight.Bold)
          .fontColor(RokuricsColors.deepText)
          .margin({ left: 8 })

        if (!this.isPaired) {
          Blank()
          Row({ space: 4 }) {
            Circle().width(7).height(7).fill(RokuricsColors.tertiaryText)
            Text('未配对')
              .fontSize(12)
              .fontWeight(FontWeight.Medium)
              .fontColor(RokuricsColors.tertiaryText)
          }
          .padding({ left: 8, right: 8, top: 4, bottom: 4 })
          .borderRadius(8)
          .backgroundColor(colorAlpha(RokuricsColors.tertiaryText, '14'))
        }
      }
      .width('100%')
      .padding({ left: 16, right: 16, top: 56, bottom: 24 })

      Scroll() {
        Column({ space: 22 }) {
          if (this.isPaired) {
            this.PairedContent()
          } else {
            this.UnpairedContent()
          }
        }
        .width('100%')
        .padding({ left: 16, right: 16, bottom: 40 })
      }
      .width('100%')
      .layoutWeight(1)
      .scrollBar(BarState.Off)
    }
    .width('100%')
    .height('100%')
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
  UnpairedContent() {
    Column({ space: 16 }) {
      // Settings form card
      Column({ space: 14 }) {
        this.InputField('Mac 地址', this.macHost, '输入 IP 地址或主机名',
          (v: string) => { this.macHost = v })
        this.InputField('端口', this.macPortText, '8787',
          (v: string) => { this.macPortText = v })
        this.InputField('指纹 (SHA-256)', this.macFingerprint,
          '64 位十六进制指纹', (v: string) => { this.macFingerprint = v },
          !this.isFingerprintVisible)
        Row() {
          Text(this.isFingerprintVisible ? '隐藏指纹' : '显示指纹')
            .fontSize(12)
            .fontColor(RokuricsColors.aqua)
        }
        .onClick(() => { this.isFingerprintVisible = !this.isFingerprintVisible })
        this.InputField('配对码 (6 位)', this.pairingCode,
          'Mac 上显示的 6 位数字', (v: string) => {
            if (v.length <= 6) this.pairingCode = v
          })
      }
      .padding(18)
      .borderRadius(28)
      .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
      .border({
        width: 1,
        color: {
          colors: [
            [0xFFFFFF, glassStrokeHighOpacity],
            [RokuricsColors.glassStroke, glassStrokeMidOpacity],
            [RokuricsColors.glassStrokeAccent, glassAccentOpacity]
          ],
          direction: GradientDirection.RightBottom
        },
        radius: 28
      } as BorderOptions)
      .shadow({
        color: colorAlpha(RokuricsColors.shadowColor, '10'),
        radius: 17,
        offsetY: 9
      })

      // Action buttons
      Column({ space: 10 }) {
        // Test connection
        Button() {
          Row({ space: 8 }) {
            Text(this.isTesting ? '测试中' : '测试连接')
              .fontSize(15)
              .fontWeight(FontWeight.SemiBold)
            if (this.isTesting) {
              Text('...').fontSize(15).fontColor(Color.White)
            }
          }
          .foregroundColor(Color.White)
          .width('100%')
          .height(48)
          .justifyContent(FlexAlign.Center)
        }
        .width('100%')
        .height(48)
        .borderRadius(24)
        .backgroundColor(this.canTest ? RokuricsColors.aqua : RokuricsColors.tertiaryText)
        .enabled(this.canTest && !this.isTesting)
        .shadow({
          color: colorAlpha(RokuricsColors.shadowColor, '14'),
          radius: 14,
          offsetY: 8
        })
        .onClick(() => this.testConnection())

        // Pair
        Button() {
          Row({ space: 8 }) {
            Text(this.isPairing ? '配对中' : '配对')
              .fontSize(15)
              .fontWeight(FontWeight.SemiBold)
            if (this.isPairing) {
              Text('...').fontSize(15).fontColor(Color.White)
            }
          }
          .foregroundColor(Color.White)
          .width('100%')
          .height(48)
          .justifyContent(FlexAlign.Center)
        }
        .width('100%')
        .height(48)
        .borderRadius(24)
        .backgroundColor(this.canPair ? RokuricsColors.aqua : RokuricsColors.tertiaryText)
        .enabled(this.canPair && !this.isPairing)
        .shadow({
          color: colorAlpha(RokuricsColors.shadowColor, '14'),
          radius: 14,
          offsetY: 8
        })
        .onClick(() => this.startPairing())
      }

      // Feedback
      if (this.feedbackText.length > 0) {
        Column({ space: 6 }) {
          Text(this.feedbackText)
            .fontSize(13)
            .fontWeight(FontWeight.SemiBold)
            .fontColor(this.feedbackError.length > 0 ? RokuricsColors.coral : RokuricsColors.mint)
          if (this.feedbackError.length > 0) {
            Text(this.feedbackError)
              .fontSize(12)
              .fontColor(RokuricsColors.coral)
          }
        }
        .width('100%')
        .padding(14)
        .borderRadius(14)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '80'))
      }
    }
  }

  @Builder
  PairedContent() {
    Column({ space: 20 }) {
      // Hero icon area with background orbs
      Stack() {
        // Background orbs
        Circle()
          .width(160).height(160)
          .fill(colorAlpha(RokuricsColors.aqua, '12'))
          .position({ x: '10%', y: '5%' })
        Circle()
          .width(120).height(120)
          .fill(colorAlpha(RokuricsColors.mint, '10'))
          .position({ x: '60%', y: '70%' })

        // Main hero circle with laptop icon
        Column({ space: 0 }) {
          Circle()
            .width(100).height(100)
            .linearGradient({
              direction: GradientDirection.RightBottom,
              colors: [
                [RokuricsColors.actionGradientStart, 1.0],
                [RokuricsColors.actionGradientEnd, 1.0]
              ]
            })
            .shadow({
              color: colorAlpha(RokuricsColors.aqua, '30'),
              radius: 24,
              offsetY: 10
            })
          ConnectionIcon(42, Color.White, true)
            .position({ x: '50%', y: '50%' })
            .translate({ x: '-50%', y: '-50%' })
        }
        .width(100).height(100)
      }
      .width('100%')
      .height(180)
      .justifyContent(FlexAlign.Center)

      // Device name and IP
      Column({ space: 4 }) {
        Row({ space: 8 }) {
          Text(this.deviceModel || 'MacBook Pro')
            .fontSize(24)
            .fontWeight(FontWeight.Bold)
            .fontFamily('serif')
            .fontColor(RokuricsColors.deepText)
          Text('演示')
            .fontSize(10)
            .fontWeight(FontWeight.Medium)
            .fontColor(RokuricsColors.coral)
            .padding({ left: 6, right: 6, top: 2, bottom: 2 })
            .borderRadius(4)
            .backgroundColor(colorAlpha(RokuricsColors.coral, '18'))
            .border({ width: 1, color: colorAlpha(RokuricsColors.coral, '40'), radius: 4 })
        }
        .justifyContent(FlexAlign.Center)
        Text(`10.195.248.123 · ${this.macPortText}`)
          .fontSize(14)
          .fontWeight(FontWeight.Medium)
          .fontColor(RokuricsColors.softText)
      }
      .width('100%')
      .alignItems(HorizontalAlign.Center)

      // Status info card
      Column({ space: 8 }) {
        this.StatusRow('状态', this.connectionStatus === 'connected' ? '已连接' : '离线')
        Divider().strokeWidth(0.5).color(colorAlpha(RokuricsColors.softText, '14'))
        this.StatusRow('最近连接', '0秒后')
        Divider().strokeWidth(0.5).color(colorAlpha(RokuricsColors.softText, '14'))
        this.StatusRow('最近同步', '暂无')
      }
      .width('100%')
      .padding(16)
      .borderRadius(20)
      .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
      .border({
        width: 1,
        color: {
          colors: [
            [0xFFFFFF, glassStrokeHighOpacity],
            [RokuricsColors.glassStroke, glassStrokeMidOpacity],
            [RokuricsColors.glassStrokeAccent, glassAccentOpacity]
          ],
          direction: GradientDirection.RightBottom
        },
        radius: 20
      } as BorderOptions)
      .shadow({
        color: colorAlpha(RokuricsColors.shadowColor, '10'),
        radius: 16,
        offsetY: 8
      })

      // Action buttons
      Column({ space: 10 }) {
        Button() {
          Row({ space: 8 }) {
            Text('↻').fontSize(16).fontColor(Color.White)
            Text('立即同步')
              .fontSize(15)
              .fontWeight(FontWeight.SemiBold)
              .fontColor(Color.White)
          }
          .width('100%')
          .height(48)
          .justifyContent(FlexAlign.Center)
        }
        .width('100%')
        .height(48)
        .borderRadius(24)
        .backgroundColor(RokuricsColors.aqua)
        .shadow({
          color: colorAlpha(RokuricsColors.aqua, '24'),
          radius: 14,
          offsetY: 6
        })
        .onClick(() => {
          this.feedbackText = '同步完成 [演示模式]'
          this.feedbackError = ''
        })

        Button() {
          Text('查看连接信息')
            .fontSize(15)
            .fontWeight(FontWeight.SemiBold)
            .fontColor(RokuricsColors.deepText)
            .width('100%')
            .height(48)
            .justifyContent(FlexAlign.Center)
        }
        .width('100%')
        .height(48)
        .borderRadius(24)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
        .border({
          width: 1,
          color: {
            colors: [
              [0xFFFFFF, glassStrokeHighOpacity],
              [RokuricsColors.glassStroke, glassStrokeMidOpacity]
            ],
            direction: GradientDirection.RightBottom
          },
          radius: 24
        } as BorderOptions)

        Button() {
          Text('断开连接')
            .fontSize(15)
            .fontWeight(FontWeight.SemiBold)
            .fontColor(RokuricsColors.coral)
            .width('100%')
            .height(48)
            .justifyContent(FlexAlign.Center)
        }
        .width('100%')
        .height(48)
        .borderRadius(24)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, glassFillOpacity))
        .border({
          width: 1,
          color: colorAlpha(RokuricsColors.coral, '40'),
          radius: 24
        })
        .onClick(() => {
          this.isPaired = false
          this.connectionStatus = 'offline'
          this.feedbackText = ''
          this.feedbackError = ''
        })
      }
      .width('100%')
    }
    .width('100%')
  }

  @Builder
  StatusRow(label: string, value: string) {
    Row() {
      Text(label)
        .fontSize(14)
        .fontWeight(FontWeight.Medium)
        .fontColor(RokuricsColors.softText)
      Blank()
      Text(value)
        .fontSize(14)
        .fontWeight(FontWeight.SemiBold)
        .fontColor(RokuricsColors.deepText)
    }
    .width('100%')
    .padding({ left: 4, right: 4, top: 4, bottom: 4 })
  }

  @Builder
  InputField(label: string, value: string, placeholder: string,
             onChange: (v: string) => void, isPassword?: boolean) {
    Column({ space: 4 }) {
      Text(label)
        .fontSize(12)
        .fontWeight(FontWeight.SemiBold)
        .fontColor(RokuricsColors.softText)
      TextInput({ text: value, placeholder: placeholder })
        .fontSize(15)
        .fontWeight(FontWeight.Medium)
        .fontColor(RokuricsColors.deepText)
        .type(isPassword ? InputType.Password : InputType.Normal)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '80'))
        .borderRadius(10)
        .padding({ left: 14, right: 14, top: 10, bottom: 10 })
        .onChange(onChange)
    }
    .width('100%')
  }

  private get canTest(): boolean {
    return this.macHost.trim().length > 0 && parseInt(this.macPortText) > 0
  }

  private get canPair(): boolean {
    return this.canTest &&
      this.macFingerprint.trim().length === 64 &&
      this.pairingCode.trim().length === 6
  }

  private testConnection(): void {
    this.isTesting = true
    this.feedbackText = '正在测试连接...'
    this.feedbackError = ''

    // Demo stub — real implementation requires HTTPS client
    setTimeout(() => {
      this.isTesting = false
      this.feedbackText = '连接测试完成 [演示模式]'
      this.feedbackError = ''
    }, 1500)
  }

  private startPairing(): void {
    this.isPairing = true
    this.feedbackText = '正在配对...'
    this.feedbackError = ''

    // Demo stub — real implementation requires crypto handshake
    setTimeout(() => {
      this.isPairing = false
      this.isPaired = true
      this.deviceModel = 'Mac'
      this.connectionStatus = 'connected'
      this.feedbackText = '配对成功 [演示模式]'
      this.feedbackError = ''
    }, 2000)
  }
}