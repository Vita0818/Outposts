/**
 * Mac connection page - mirrors MacConnectionView.swift
 * Pairing form, connection test, paired device view
 */
import { RokuricsColors, FontWeight, glassFillOpacity, glassStrokeHighOpacity,
  glassStrokeMidOpacity } from '../utils/RokuricsTheme'
import { BackIcon } from '../utils/CustomIcons'
import { DeviceConnectionCard } from '../components/DeviceConnectionCard'

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
  @State feedbackText: string = ''
  @State feedbackError: string = ''
  @State isFingerprintVisible: boolean = false

  build() {
    Column() {
      // Header
      Row() {
        Button() {
          BackIcon(18, RokuricsColors.deepText)
        }
        .width(44)
        .height(44)
        .borderRadius(22)
        .backgroundColor(RokuricsColors.glassSurface + glassFillOpacity)
        .shadow({
          color: RokuricsColors.shadowColor + '10',
          radius: 12,
          offsetY: 6
        })
        .border({
          width: 1,
          color: {
            colors: [
              [0xFFFFFF, glassStrokeHighOpacity],
              [0xEFFAF8, glassStrokeMidOpacity],
              [0x59C7C2, 0.10]
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
          .backgroundColor(RokuricsColors.tertiaryText + '14')
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
    .backgroundColor(RokuricsColors.pageBackground)
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
      .backgroundColor(RokuricsColors.glassSurface + glassFillOpacity)
      .border({
        width: 1,
        color: {
          colors: [
            [0xFFFFFF, glassStrokeHighOpacity],
            [0xEFFAF8, glassStrokeMidOpacity],
            [0x91E8D6, 0.10]
          ],
          direction: GradientDirection.RightBottom
        },
        radius: 28
      } as BorderOptions)
      .shadow({
        color: RokuricsColors.shadowColor + '10',
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
          color: RokuricsColors.shadowColor + '14',
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
          color: RokuricsColors.shadowColor + '14',
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
        .backgroundColor(RokuricsColors.glassSurface + '80')
      }
    }
  }

  @Builder
  PairedContent() {
    Column({ space: 24 }) {
      Blank()
        .height(28)

      // Connected device bubble
      Column({ space: 16 }) {
        Circle()
          .width(88)
          .height(88)
          .fill(RokuricsColors.aqua)
        Text('✓')
          .fontSize(36)
          .fontWeight(FontWeight.Bold)
          .fontColor(Color.White)
        Text(this.deviceModel || 'Mac')
          .fontSize(16)
          .fontWeight(FontWeight.SemiBold)
          .fontColor(RokuricsColors.deepText)
      }
      .width('100%')

      Blank()
        .height(18)

      // Connected device card
      DeviceConnectionCard({
        isPaired: this.isPaired,
        deviceModel: this.deviceModel,
        portText: this.macPortText,
        connectionStatus: this.connectionStatus
      })

      // Actions
      Column({ space: 12 }) {
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
        .backgroundColor(RokuricsColors.glassSurface + glassFillOpacity)
        .border({
          width: 1,
          color: RokuricsColors.coral + '40',
          radius: 24
        })
        .onClick(() => {
          this.isPaired = false
          this.connectionStatus = 'offline'
          this.feedbackText = ''
        })
      }
    }
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
        .backgroundColor(RokuricsColors.glassSurface + '80')
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

    // Mock test (real implementation would use HTTPS client)
    setTimeout(() => {
      this.isTesting = false
      this.feedbackText = '连接测试完成 (Mock)'
      this.feedbackError = ''
    }, 1500)
  }

  private startPairing(): void {
    this.isPairing = true
    this.feedbackText = '正在配对...'
    this.feedbackError = ''

    // Mock pairing (real implementation would use crypto handshake)
    setTimeout(() => {
      this.isPairing = false
      // Simulate successful pairing
      this.isPaired = true
      this.deviceModel = 'Mac'
      this.connectionStatus = 'connected'
      this.feedbackText = '配对成功'
      this.feedbackError = ''
    }, 2000)
  }
}