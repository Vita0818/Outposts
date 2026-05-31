/**
 * Home page - mirrors RokuricsHomeView.swift
 */
import { display } from '@kit.ArkUI'
import { ConfigurationConstant } from '@kit.AbilityKit'
import { getSharedRecordingManager, RecordingManager } from '../services/RecordingManager'
import { RecordingState } from '../models/RecordingModels'
import { formatClock } from '../utils/FormatHelpers'
import { colorAlpha, RokuricsColors, FontWeight, glassFillOpacity, glassStrokeHighOpacity,
  glassStrokeMidOpacity, glassAccentOpacity, applyColorMode } from '../utils/RokuricsTheme'
import { PersonIcon, BooksIcon, ChatIcon, ConnectionIcon } from '../utils/CustomIcons'
import { hapticLight } from '../utils/HapticFeedback'
import { DeviceConnectionCard } from '../components/DeviceConnectionCard'

@Entry
@Component
struct HomePage {
  private recordingManager: RecordingManager = getSharedRecordingManager(getContext(this))
  @StorageLink('colorMode') colorMode: ConfigurationConstant.ColorMode = ConfigurationConstant.ColorMode.COLOR_MODE_NOT_SET
  @State state: RecordingState = RecordingState.IDLE
  @State isActive: boolean = false
  @State breathePhase: number = 0
  @State orbScale: number = 0.84
  @State headerScale: number = 1
  @State isMacPaired: boolean = false
  @State connectionStatus: string = 'offline'

  aboutToAppear(): void {
    this.recordingManager.onStateChange((s: RecordingState) => {
      this.state = s
      this.isActive = this.isActiveState(s)
    })
    this.state = this.recordingManager.state
    this.isActive = this.isActiveState(this.state)
    this.recordingManager.reloadRecordings()
    this.calibrateScale()
    this.startBreathing()
  }

  private calibrateScale(): void {
    const w = display.getDefaultDisplaySync().width
    const h = display.getDefaultDisplaySync().height
    if (w < 360 || h < 760) {
      this.orbScale = 0.78
      this.headerScale = 0.92
    } else if (h < 820) {
      this.orbScale = 0.88
      this.headerScale = 0.96
    } else {
      this.orbScale = 1
      this.headerScale = 1
    }
  }

  private startBreathing(): void {
    const runBreatheIn = () => {
      animateTo({
        duration: 2400,
        curve: Curve.EaseInOut,
        onFinish: () => { runBreatheOut() }
      }, () => { this.breathePhase = 1.0 })
    }
    const runBreatheOut = () => {
      animateTo({
        duration: 2400,
        curve: Curve.EaseInOut,
        onFinish: () => { runBreatheIn() }
      }, () => { this.breathePhase = 0.0 })
    }
    runBreatheIn()
  }

  private isActiveState(s: RecordingState): boolean {
    return s === RecordingState.REQUESTING_PERMISSION ||
      s === RecordingState.CONFIGURING_SESSION ||
      s === RecordingState.RECORDING ||
      s === RecordingState.PAUSED ||
      s === RecordingState.STOPPING ||
      s === RecordingState.SAVING
  }

  build() {
    applyColorMode(this.colorMode)
    Stack() {
      // Ambient background bubbles
      this.AmbientBackground()

      Column() {
        // Header
        Row() {
          Text('Rokurics')
            .fontSize(39 * this.headerScale)
            .fontWeight(FontWeight.Medium)
            .fontColor(RokuricsColors.deepText)
            .fontFamily('serif')

          Blank()

          // Profile avatar button (glass circle)
          Button() {
            PersonIcon(20 * this.headerScale, RokuricsColors.aqua)
          }
          .width(46 * this.headerScale)
          .height(46 * this.headerScale)
          .borderRadius(23 * this.headerScale)
          .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '5C'))
          .shadow({
            color: colorAlpha(RokuricsColors.shadowColor, '14'),
            radius: 12,
            offsetY: 6
          })
          .border({
            width: 1,
            color: {
              colors: [
                [0xFFFFFF, 0.28],
                [RokuricsColors.glassStroke, 0.12],
                [RokuricsColors.aqua, 0.24]
              ],
              direction: GradientDirection.RightBottom
            },
            radius: 23 * this.headerScale
          } as BorderOptions)
          .onClick(() => {
            this.getUIContext().getRouter().pushUrl({ url: 'pages/SettingsPage' })
          })
        }
        .width('100%')
        .padding({
          left: 24 * this.headerScale,
          right: 24 * this.headerScale,
          top: 18 * this.headerScale + 42
        })

        Blank()

        // Recording Orb
        Button() {
          Stack() {
            // Orbiting ambient satellites (animated)
            Stack() {
              // Satellite 1: mint → paleAqua, opacity 0.42
              Circle()
                .width(88 * this.orbScale)
                .height(88 * this.orbScale)
                .linearGradient({
                  direction: GradientDirection.RightBottom,
                  colors: [
                    [RokuricsColors.mint, 0.42],
                    [RokuricsColors.paleAqua, 0.42]
                  ]
                })
                .stroke('#FFFFFF')
                .strokeWidth(1)
                .shadow({
                  color: colorAlpha(RokuricsColors.shadowColor, '08'),
                  radius: 14,
                  offsetY: 8
                })
                .position({
                  x: (94 - this.breathePhase * 6) * this.orbScale + '%',
                  y: (28 + this.breathePhase * 4) * this.orbScale + '%'
                })

              // Satellite 2: skyCyan → mistGreen, opacity 0.32
              Circle()
                .width(76 * this.orbScale)
                .height(76 * this.orbScale)
                .linearGradient({
                  direction: GradientDirection.RightBottom,
                  colors: [
                    [RokuricsColors.skyCyan, 0.32],
                    [RokuricsColors.mistGreen, 0.32]
                  ]
                })
                .stroke('#FFFFFF')
                .strokeWidth(1)
                .shadow({
                  color: colorAlpha(RokuricsColors.shadowColor, '08'),
                  radius: 14,
                  offsetY: 8
                })
                .position({
                  x: (60 + this.breathePhase * 5) * this.orbScale + '%',
                  y: (32 - this.breathePhase * 3) * this.orbScale + '%'
                })

              // Satellite 3: aqua → paleAqua, opacity 0.30
              Circle()
                .width(74 * this.orbScale)
                .height(74 * this.orbScale)
                .linearGradient({
                  direction: GradientDirection.RightBottom,
                  colors: [
                    [RokuricsColors.aqua, 0.30],
                    [RokuricsColors.paleAqua, 0.30]
                  ]
                })
                .stroke('#FFFFFF')
                .strokeWidth(1)
                .shadow({
                  color: colorAlpha(RokuricsColors.shadowColor, '08'),
                  radius: 14,
                  offsetY: 8
                })
                .position({
                  x: (50 - this.breathePhase * 4) * this.orbScale + '%',
                  y: (68 + this.breathePhase * 3) * this.orbScale + '%'
                })

              // Satellite 4: mistGreen → mint, opacity 0.34
              Circle()
                .width(68 * this.orbScale)
                .height(68 * this.orbScale)
                .linearGradient({
                  direction: GradientDirection.RightBottom,
                  colors: [
                    [RokuricsColors.mistGreen, 0.34],
                    [RokuricsColors.mint, 0.34]
                  ]
                })
                .stroke('#FFFFFF')
                .strokeWidth(1)
                .shadow({
                  color: colorAlpha(RokuricsColors.shadowColor, '08'),
                  radius: 14,
                  offsetY: 8
                })
                .position({
                  x: (28 + this.breathePhase * 5) * this.orbScale + '%',
                  y: (62 - this.breathePhase * 3) * this.orbScale + '%'
                })
            }
            .width(272 * this.orbScale)
            .height(286 * this.orbScale)

            // Sound ripple (animated breathing)
            Circle()
              .width(238 * this.orbScale)
              .height(238 * this.orbScale)
              .stroke(colorAlpha(RokuricsColors.aqua, '08'))
              .strokeWidth(1.4)
              .fill(Color.Transparent)
              .opacity(0.07 + this.breathePhase * 0.05)

            Circle()
              .width(202 * this.orbScale)
              .height(202 * this.orbScale)
              .stroke(colorAlpha(RokuricsColors.aqua, '10'))
              .strokeWidth(1.4)
              .fill(Color.Transparent)
              .opacity(0.10 + this.breathePhase * 0.05)

            // Recording red ripple
            if (this.state === RecordingState.RECORDING) {
              Circle()
                .width(222 * this.orbScale)
                .height(222 * this.orbScale)
                .stroke(colorAlpha(RokuricsColors.coral, '20'))
                .strokeWidth(1.4)
                .fill(Color.Transparent)
                .opacity(0.16 + this.breathePhase * 0.08)
            } else if (this.state === RecordingState.PAUSED) {
              Circle()
                .width(222 * this.orbScale)
                .height(222 * this.orbScale)
                .stroke(colorAlpha(RokuricsColors.softTeal, '15'))
                .strokeWidth(1.4)
                .fill(Color.Transparent)
                .opacity(0.11 + this.breathePhase * 0.06)
            }

            // Main orb with gradient fill
            Circle()
              .width(190 * this.orbScale)
              .height(190 * this.orbScale)
              .linearGradient({
                direction: GradientDirection.RightBottom,
                colors: [
                  [RokuricsColors.actionGradientStart, 1.0],
                  [RokuricsColors.actionGradientEnd, 1.0]
                ]
              })
              .shadow({
                color: colorAlpha(RokuricsColors.shadowColor, '24'),
                radius: 30 * this.orbScale,
                offsetY: 18 * this.orbScale
              })

            // Center content
            if (this.isActive) {
              Text(this.getCenterText())
                .fontSize(34 * this.orbScale)
                .fontWeight(FontWeight.Bold)
                .fontColor('#FFFFFF')
                .fontFamily('serif')
                .opacity(this.isPausedBlinking() ? (0.35 + this.breathePhase * 0.65) : 0.97)
            } else {
              Stack() {
                Rect({ width: 56 * this.orbScale, height: 8 * this.orbScale })
                  .radius(4)
                  .fill(Color.White)
                Rect({ width: 8 * this.orbScale, height: 56 * this.orbScale })
                  .radius(4)
                  .fill(Color.White)
              }
              .opacity(0.97)
            }
          }
          .width(272 * this.orbScale)
          .height(286 * this.orbScale)
          .scale({
            x: (this.isActive ? 1.018 : 0.992) + this.breathePhase * 0.008,
            y: (this.isActive ? 1.018 : 0.992) + this.breathePhase * 0.008
          })
        }
        .width(272 * this.orbScale)
        .height(286 * this.orbScale)
        .backgroundColor(Color.Transparent)
        .onClick(() => {
          hapticLight()
          this.getUIContext().getRouter().pushUrl({ url: 'pages/RecordingSessionPage' })
        })

        Blank()

        // Navigation Card with glass styling
        Row() {
          // Study Library
          Button() {
            Column({ space: 8 }) {
              BooksIcon(27, RokuricsColors.aqua)
              Text('学习库')
                .fontSize(13)
                .fontWeight(FontWeight.SemiBold)
                .fontColor(RokuricsColors.deepText)
            }
            .width('100%')
            .height(104)
            .justifyContent(FlexAlign.Center)
          }
          .layoutWeight(1)
          .height(104)
          .backgroundColor(Color.Transparent)
          .onClick(() => {
            this.getUIContext().getRouter().pushUrl({ url: 'pages/RecordingLibraryPage' })
          })

          // Divider
          Rect()
            .width(1)
            .height(54)
            .fill(colorAlpha(RokuricsColors.softText, '14'))

          // AI Chat
          Button() {
            Column({ space: 8 }) {
              ChatIcon(27, RokuricsColors.mint)
              Text('AI 对话')
                .fontSize(13)
                .fontWeight(FontWeight.SemiBold)
                .fontColor(RokuricsColors.deepText)
            }
            .width('100%')
            .height(104)
            .justifyContent(FlexAlign.Center)
          }
          .layoutWeight(1)
          .height(104)
          .backgroundColor(Color.Transparent)
          .onClick(() => {
            this.getUIContext().getRouter().pushUrl({ url: 'pages/AIChatPage' })
          })

          // Divider
          Rect()
            .width(1)
            .height(54)
            .fill(colorAlpha(RokuricsColors.softText, '14'))

          // Mac Connection
          Button() {
            Column({ space: 8 }) {
              ConnectionIcon(27, RokuricsColors.softTeal, this.isMacPaired)
              Text('Mac 连接')
                .fontSize(13)
                .fontWeight(FontWeight.SemiBold)
                .fontColor(RokuricsColors.deepText)
            }
            .width('100%')
            .height(104)
            .justifyContent(FlexAlign.Center)
          }
          .layoutWeight(1)
          .height(104)
          .backgroundColor(Color.Transparent)
          .onClick(() => {
            this.getUIContext().getRouter().pushUrl({ url: 'pages/MacConnectionPage' })
          })
        }
        .width('88%')
        .height(104)
        .borderRadius(30)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '66'))
        .border({
          width: 1,
          color: {
            colors: [
              [0xFFFFFF, 0.24],
              [RokuricsColors.glassStroke, 0.14],
              [RokuricsColors.glassStrokeAccent, 0.24]
            ],
            direction: GradientDirection.RightBottom
          },
          radius: 30
        } as BorderOptions)
        .shadow({
          color: colorAlpha(RokuricsColors.shadowColor, '12'),
          radius: 20,
          offsetY: 11
        })
        .margin({ bottom: 16 * this.headerScale })

        // Device connection preview card (compact)
        if (this.isMacPaired) {
          Column() {
            Row({ space: 8 }) {
              Circle()
                .width(8)
                .height(8)
                .fill(RokuricsColors.mint)
              Text('Mac 已连接 · 8787')
                .fontSize(12)
                .fontWeight(FontWeight.Medium)
                .fontColor(RokuricsColors.softText)
            }
            .padding({ left: 14, right: 14, top: 8, bottom: 8 })
            .borderRadius(14)
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
              radius: 14
            } as BorderOptions)
          }
          .margin({ bottom: 34 * this.headerScale })
        } else {
          Blank().height(18 * this.headerScale)
        }
      }
      .width('100%')
      .height('100%')
      .backgroundColor(Color.Transparent)
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
  AmbientBackground() {
    Stack() {
      // Bubble 1: paleAqua → mint, opacity 0.30
      Circle()
        .width(150)
        .height(150)
        .linearGradient({
          direction: GradientDirection.RightBottom,
          colors: [
            [RokuricsColors.paleAqua, 0.30],
            [RokuricsColors.mint, 0.30]
          ]
        })
        .stroke('#FFFFFF')
        .strokeWidth(1)
        .opacity(0.30)
        .position({ x: -30, y: '-10%' })

      // Bubble 2: skyCyan → mistGreen, opacity 0.22
      Circle()
        .width(190)
        .height(190)
        .linearGradient({
          direction: GradientDirection.RightBottom,
          colors: [
            [RokuricsColors.skyCyan, 0.22],
            [RokuricsColors.mistGreen, 0.22]
          ]
        })
        .stroke('#FFFFFF')
        .strokeWidth(1)
        .opacity(0.22)
        .position({ x: '85%', y: '15%' })

      // Bubble 3: mint → aqua, opacity 0.18
      Circle()
        .width(170)
        .height(170)
        .linearGradient({
          direction: GradientDirection.RightBottom,
          colors: [
            [RokuricsColors.mint, 0.18],
            [RokuricsColors.aqua, 0.18]
          ]
        })
        .stroke('#FFFFFF')
        .strokeWidth(1)
        .opacity(0.18)
        .position({ x: '75%', y: '70%' })
    }
    .width('100%')
    .height('100%')
  }

  private getCenterText(): string {
    switch (this.state) {
      case RecordingState.RECORDING:
      case RecordingState.PAUSED:
        return formatClock(this.recordingManager.elapsedSeconds)
      default:
        return '...'
    }
  }

  private isPausedBlinking(): boolean {
    return this.state === RecordingState.PAUSED
  }
}