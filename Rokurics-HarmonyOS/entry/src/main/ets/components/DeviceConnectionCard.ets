/**
 * Device connection card - mirrors DeviceConnectionCard.swift
 * Reusable card showing Mac connection status, port, and status pills
 */
import { RokuricsColors, FontWeight, glassFillOpacity, glassStrokeHighOpacity,
  glassStrokeMidOpacity } from '../utils/RokuricsTheme'
import { ConnectionIcon } from '../utils/CustomIcons'

@Component
export struct DeviceConnectionCard {
  @Prop isPaired: boolean = false
  @Prop deviceModel: string = ''
  @Prop portText: string = '8787'
  @Prop connectionStatus: string = 'offline'
  onCardTap?: () => void

  build() {
    Column() {
      Row() {
        // Left: status icon
        Stack() {
          Circle()
            .width(58)
            .height(58)
            .fill(RokuricsColors.mistGreen)
        }
        .width(58)
        .height(58)

        ConnectionIcon(24, this.isPaired ? RokuricsColors.softTeal : RokuricsColors.tertiaryText, this.isPaired)

        // Center: title + subtitle
        Column({ space: 6 }) {
          Row({ space: 6 }) {
            Text(this.isPaired ? '已连接' : 'Mac 未连接')
              .fontSize(18)
              .fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
            if (this.isPaired && this.deviceModel.length > 0) {
              Text(this.deviceModel)
                .fontSize(12)
                .fontColor(RokuricsColors.tertiaryText)
            }
          }

          Text(this.isPaired ? '本地传输已就绪' : '本地传输待配置')
            .fontSize(14)
            .fontWeight(FontWeight.Medium)
            .fontColor(RokuricsColors.softText)
        }
        .alignItems(HorizontalAlign.Start)
        .margin({ left: 15 })

        Blank()

        // Right: port number
        Column({ space: 6 }) {
          Text(this.portText)
            .fontSize(24)
            .fontWeight(FontWeight.Bold)
            .fontColor(RokuricsColors.softTeal)
          Text('端口')
            .fontSize(11)
            .fontWeight(FontWeight.SemiBold)
            .fontColor(RokuricsColors.tertiaryText)
        }
        .alignItems(HorizontalAlign.End)
      }
      .width('100%')

      // Bottom status pills
      if (this.isPaired) {
        Row() {
          Row({ space: 4 }) {
            Circle()
              .width(7)
              .height(7)
              .fill(this.connectionStatus === 'connected' ? RokuricsColors.mint : RokuricsColors.tertiaryText)
            Text(this.connectionStatus === 'connected' ? '已连接' : '离线')
              .fontSize(11)
              .fontWeight(FontWeight.Medium)
              .fontColor(this.connectionStatus === 'connected' ? RokuricsColors.mint : RokuricsColors.tertiaryText)
          }
          .padding({ left: 8, right: 8, top: 3, bottom: 3 })
          .borderRadius(8)
          .backgroundColor(
            this.connectionStatus === 'connected' ? RokuricsColors.mint + '18' : RokuricsColors.tertiaryText + '14'
          )

          Blank()

          Row({ space: 4 }) {
            Circle().width(7).height(7).fill(RokuricsColors.softTeal)
            Text('局域网 HTTP')
              .fontSize(11)
              .fontWeight(FontWeight.Medium)
              .fontColor(RokuricsColors.softTeal)
          }
          .padding({ left: 8, right: 8, top: 3, bottom: 3 })
          .borderRadius(8)
          .backgroundColor(RokuricsColors.softTeal + '14')
        }
        .width('100%')
        .margin({ top: 12 })
      }
    }
    .width('100%')
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
    .onClick(() => {
      if (this.onCardTap) {
        this.onCardTap()
      }
    })
  }
}

/**
 * Metrics tile - mirrors RokuricsMetricTile in RokuricsCardStyle.swift
 */
@Component
export struct MetricTile {
  @Prop title: string = ''
  @Prop value: string = ''
  @Prop tint: string = RokuricsColors.aqua

  build() {
    Column({ space: 6 }) {
      Text(this.title)
        .fontSize(12)
        .fontWeight(FontWeight.SemiBold)
        .fontColor(RokuricsColors.softText)
        .maxLines(1)
      Row({ space: 4 }) {
        Text(this.value)
          .fontSize(26)
          .fontWeight(FontWeight.Bold)
          .fontColor(this.tint)
          .maxLines(1)
      }
      .alignItems(VerticalAlign.Center)
    }
    .width('100%')
    .padding(14)
    .borderRadius(20)
    .backgroundColor(RokuricsColors.glassSurface + '59')
    .border({
      width: 1,
      color: {
        colors: [
          [0xFFFFFF, 0.30],
          [0xEFFAF8, 0.10]
        ],
        direction: GradientDirection.RightBottom
      },
      radius: 20
    } as BorderOptions)
    .shadow({
      color: RokuricsColors.shadowColor + '05',
      radius: 8,
      offsetY: 4
    })
  }
}

/**
 * Transfer queue card - mirrors TransferQueueCard.swift
 */
@Component
export struct TransferQueueCard {
  @Prop pendingCount: number = 0
  @Prop completedCount: number = 0
  @Prop lastSyncText: string = '暂无'

  build() {
    Column({ space: 16 }) {
      Row({ space: 10 }) {
        Text('⇄')
          .fontSize(15)
          .fontWeight(FontWeight.SemiBold)
          .fontColor(RokuricsColors.aqua)
        Text('本地传输队列')
          .fontSize(18)
          .fontWeight(FontWeight.SemiBold)
          .fontColor(RokuricsColors.deepText)
      }
      .width('100%')

      Row({ space: 10 }) {
        MetricTile({ title: '待传输', value: `${this.pendingCount}`, tint: RokuricsColors.aqua })
        MetricTile({ title: '已完成', value: `${this.completedCount}`, tint: RokuricsColors.mint })
      }
      .width('100%')

      Row({ space: 10 }) {
        Text('🕐').fontSize(14)
        Text('最近同步').fontSize(14).fontWeight(FontWeight.Medium).fontColor(RokuricsColors.softText)
        Blank()
        Text(this.lastSyncText).fontSize(14).fontWeight(FontWeight.SemiBold).fontColor(RokuricsColors.deepText)
      }
      .width('100%')
      .padding({ left: 14, right: 14, top: 10, bottom: 10 })
      .borderRadius(20)
      .backgroundColor(RokuricsColors.glassSurface + '56')
    }
    .width('100%')
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
  }
}
