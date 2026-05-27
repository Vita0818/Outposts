/**
 * Settings page with user profile and AI provider configuration
 */
import { router } from '@kit.ArkUI'
import { UserProfile } from '../models/UserProfile'
import { SettingsStore } from '../services/SettingsStore'
import { OpenAICompatibleClient, AIConfiguration, ProviderHealthResult } from '../services/OpenAICompatibleClient'
import { RokuricsColors, FontWeight } from '../utils/RokuricsTheme'

function formatTime(d: Date): string {
  const h = String(d.getHours()).padStart(2, '0')
  const m = String(d.getMinutes()).padStart(2, '0')
  return `${h}:${m}`
}

@Entry
@Component
struct SettingsPage {
  private settingsStore: SettingsStore = new SettingsStore(getContext(this))
  @State profile: UserProfile = new UserProfile()
  @State displayName: string = ''
  @State handle: string = ''
  @State isSaving: boolean = false
  @State savedMessage: string = ''
  @State aiProviderKind: string = 'mock'
  @State aiBaseURL: string = 'https://api.openai.com/v1'
  @State aiModelName: string = 'gpt-4o-mini'
  @State aiAPIKey: string = ''
  @State aiTemperature: number = 0.3
  @State aiMaxTokens: number = 2000
  @State isSavingAI: boolean = false
  @State aiSavedMessage: string = ''
  @State isTestingConnection: boolean = false
  @State testConnectionResult: string = ''
  @State lastConnectionTest: string = ''
  @State providerHealthSummary: string = ''
  @State providerModels: string[] = []
  @State providerLatency: string = ''
  @State uploadServerURL: string = ''
  @State uploadAPIKey: string = ''
  @State isSavingUpload: boolean = false
  @State uploadSavedMessage: string = ''

  aboutToAppear(): void {
    this.loadAllSettings()
  }

  async loadAllSettings(): Promise<void> {
    this.profile = await this.settingsStore.getUserProfile()
    this.displayName = this.profile.displayName
    this.handle = this.profile.handle
    this.aiProviderKind = await this.settingsStore.getAIProviderKind()
    this.aiBaseURL = await this.settingsStore.getAIBaseURL()
    this.aiModelName = await this.settingsStore.getAIModelName()
    this.aiAPIKey = await this.settingsStore.getAIAPIKey()
    this.aiTemperature = await this.settingsStore.getAITemperature()
    this.aiMaxTokens = await this.settingsStore.getAIMaxTokens()
    this.uploadServerURL = await this.settingsStore.getUploadServerURL()
    this.uploadAPIKey = await this.settingsStore.getUploadAPIKey()
  }

  async saveProfile(): Promise<void> {
    this.isSaving = true
    this.savedMessage = ''
    try {
      const updated: UserProfile = new UserProfile(this.displayName, this.handle, this.profile.avatar)
      await this.settingsStore.saveUserProfile(updated)
      this.profile = updated
      this.savedMessage = '已保存'
    } catch {
      this.savedMessage = '保存失败'
    } finally {
      this.isSaving = false
    }
  }

  async saveAIConfig(): Promise<void> {
    this.isSavingAI = true
    this.aiSavedMessage = ''
    try {
      await this.settingsStore.setAIProviderKind(this.aiProviderKind)
      await this.settingsStore.setAIBaseURL(this.aiBaseURL)
      await this.settingsStore.setAIModelName(this.aiModelName)
      await this.settingsStore.setAIAPIKey(this.aiAPIKey)
      await this.settingsStore.setAITemperature(this.aiTemperature)
      await this.settingsStore.setAIMaxTokens(this.aiMaxTokens)
      this.aiSavedMessage = 'AI 配置已保存'
    } catch {
      this.aiSavedMessage = '保存失败'
    } finally {
      this.isSavingAI = false
    }
  }

  async saveUploadConfig(): Promise<void> {
    this.isSavingUpload = true
    this.uploadSavedMessage = ''
    try {
      await this.settingsStore.setUploadServerURL(this.uploadServerURL)
      await this.settingsStore.setUploadAPIKey(this.uploadAPIKey)
      this.uploadSavedMessage = '上传配置已保存'
    } catch {
      this.uploadSavedMessage = '保存失败'
    } finally {
      this.isSavingUpload = false
    }
  }

  async testConnection(): Promise<void> {
    this.isTestingConnection = true
    this.testConnectionResult = ''
    this.lastConnectionTest = ''
    this.providerModels = []
    this.providerLatency = ''
    try {
      const config: AIConfiguration = {
        baseURL: this.aiBaseURL,
        modelName: this.aiModelName,
        apiKey: this.aiAPIKey,
        temperature: this.aiTemperature,
        maxTokens: this.aiMaxTokens
      }
      const health: ProviderHealthResult = await OpenAICompatibleClient.validateConfiguration(config)
      this.testConnectionResult = health.reachable ? '连接成功' : '连接失败'
      if (health.reachable) {
        this.lastConnectionTest = formatTime(new Date())
        this.providerHealthSummary = `已连接 · ${this.aiModelName}`
        this.providerModels = health.models.slice(0, 10)
        this.providerLatency = health.latencyMs !== null ? `${health.latencyMs}ms` : ''
      } else {
        this.providerHealthSummary = health.errorMessage ?? '连接失败'
      }
    } catch {
      this.testConnectionResult = '连接失败'
      this.providerHealthSummary = '连接失败'
    } finally {
      this.isTestingConnection = false
    }
  }

  build() {
    Column() {
      Row() {
        Button() {
          Text('←').fontSize(20).fontColor(RokuricsColors.deepText)
        }
        .width(44)
        .height(44)
        .backgroundColor(Color.Transparent)
        .onClick(() => this.getUIContext().getRouter().back())

        Text('设置')
          .fontSize(24)
          .fontWeight(FontWeight.Bold)
          .fontColor(RokuricsColors.deepText)
      }
      .width('100%')
      .padding({ left: 16, right: 16, top: 56, bottom: 24 })

      Scroll() {
        Column({ space: 24 }) {
          // Avatar
          Column({ space: 12 }) {
            Circle()
              .width(80)
              .height(80)
              .fill(RokuricsColors.aqua + '20')
            Text(this.profile.initial)
              .fontSize(32)
              .fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.aqua)
            Text(this.profile.displayName)
              .fontSize(20)
              .fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
            Text(this.profile.displayHandle)
              .fontSize(14)
              .fontColor(RokuricsColors.softText)
          }
          .width('100%')

          // Profile form
          Column({ space: 16 }) {
            Column({ space: 6 }) {
              Text('显示名称').fontSize(12).fontColor(RokuricsColors.softText)
              TextInput({ text: this.displayName, placeholder: '输入显示名称' })
                .fontSize(16).fontColor(RokuricsColors.deepText)
                .backgroundColor(RokuricsColors.glassSurface + '80')
                .borderRadius(10).padding({ left: 14, right: 14, top: 10, bottom: 10 })
                .onChange((value: string) => { this.displayName = value })
            }
            .width('100%')

            Column({ space: 6 }) {
              Text('账号 (@handle)').fontSize(12).fontColor(RokuricsColors.softText)
              TextInput({ text: this.handle, placeholder: '输入账号' })
                .fontSize(16).fontColor(RokuricsColors.deepText)
                .backgroundColor(RokuricsColors.glassSurface + '80')
                .borderRadius(10).padding({ left: 14, right: 14, top: 10, bottom: 10 })
                .onChange((value: string) => { this.handle = value })
            }
            .width('100%')

            Button() {
              Text('保存').fontSize(16).fontWeight(FontWeight.SemiBold).fontColor(Color.White)
            }
            .width('100%').height(48).borderRadius(12)
            .backgroundColor(RokuricsColors.aqua)
            .enabled(!this.isSaving)
            .onClick(() => this.saveProfile())

            if (this.savedMessage.length > 0) {
              Text(this.savedMessage).fontSize(13)
                .fontColor(this.savedMessage.startsWith('已保存') ? RokuricsColors.mint : RokuricsColors.coral)
            }
          }
          .padding(20).borderRadius(20)
          .backgroundColor(RokuricsColors.glassSurface + '66')

          // AI Provider Configuration
          Column({ space: 16 }) {
            Text('AI 提供商配置')
              .fontSize(16).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)

            // Provider kind toggle
            Row({ space: 12 }) {
              Text('提供商').fontSize(14).fontColor(RokuricsColors.softText).width(80)
              Row({ space: 0 }) {
                Button() {
                  Text('Mock')
                    .fontSize(13)
                    .fontColor(this.aiProviderKind === 'mock' ? Color.White : RokuricsColors.softText)
                }
                .padding({ left: 16, right: 16, top: 8, bottom: 8 })
                .borderRadius({ topLeft: 8, bottomLeft: 8 })
                .backgroundColor(this.aiProviderKind === 'mock' ? RokuricsColors.aqua : RokuricsColors.glassSurface + '50')
                .onClick(() => { this.aiProviderKind = 'mock' })

                Button() {
                  Text('OpenAI')
                    .fontSize(13)
                    .fontColor(this.aiProviderKind === 'openaiCompatible' ? Color.White : RokuricsColors.softText)
                }
                .padding({ left: 16, right: 16, top: 8, bottom: 8 })
                .borderRadius({ topRight: 8, bottomRight: 8 })
                .backgroundColor(this.aiProviderKind === 'openaiCompatible' ? RokuricsColors.aqua : RokuricsColors.glassSurface + '50')
                .onClick(() => { this.aiProviderKind = 'openaiCompatible' })
              }
            }

            if (this.aiProviderKind === 'openaiCompatible') {
              Column({ space: 12 }) {
                Column({ space: 4 }) {
                  Text('API 地址').fontSize(12).fontColor(RokuricsColors.softText)
                  TextInput({ text: this.aiBaseURL, placeholder: 'https://api.openai.com/v1' })
                    .fontSize(14).fontColor(RokuricsColors.deepText)
                    .backgroundColor(RokuricsColors.glassSurface + '40')
                    .borderRadius(8).padding({ left: 12, right: 12, top: 8, bottom: 8 })
                    .onChange((value: string) => { this.aiBaseURL = value })
                }

                Column({ space: 4 }) {
                  Text('模型名称').fontSize(12).fontColor(RokuricsColors.softText)
                  TextInput({ text: this.aiModelName, placeholder: 'gpt-4o-mini' })
                    .fontSize(14).fontColor(RokuricsColors.deepText)
                    .backgroundColor(RokuricsColors.glassSurface + '40')
                    .borderRadius(8).padding({ left: 12, right: 12, top: 8, bottom: 8 })
                    .onChange((value: string) => { this.aiModelName = value })
                }

                Column({ space: 4 }) {
                  Text('API Key').fontSize(12).fontColor(RokuricsColors.softText)
                  TextInput({ text: this.aiAPIKey, placeholder: 'sk-...' })
                    .fontSize(14).fontColor(RokuricsColors.deepText)
                    .type(InputType.Password)
                    .backgroundColor(RokuricsColors.glassSurface + '40')
                    .borderRadius(8).padding({ left: 12, right: 12, top: 8, bottom: 8 })
                    .onChange((value: string) => { this.aiAPIKey = value })
                }

                Row({ space: 12 }) {
                  Column({ space: 4 }) {
                    Text('温度').fontSize(12).fontColor(RokuricsColors.softText)
                    TextInput({ text: String(this.aiTemperature), placeholder: '0.3' })
                      .fontSize(14).fontColor(RokuricsColors.deepText)
                      .type(InputType.Number)
                      .backgroundColor(RokuricsColors.glassSurface + '40')
                      .borderRadius(8).padding({ left: 12, right: 12, top: 8, bottom: 8 })
                      .onChange((value: string) => {
                        const n = parseFloat(value)
                        if (!isNaN(n)) this.aiTemperature = n
                      })
                  }
                  .layoutWeight(1)

                  Column({ space: 4 }) {
                    Text('最大 Tokens').fontSize(12).fontColor(RokuricsColors.softText)
                    TextInput({ text: String(this.aiMaxTokens), placeholder: '2000' })
                      .fontSize(14).fontColor(RokuricsColors.deepText)
                      .type(InputType.Number)
                      .backgroundColor(RokuricsColors.glassSurface + '40')
                      .borderRadius(8).padding({ left: 12, right: 12, top: 8, bottom: 8 })
                      .onChange((value: string) => {
                        const n = parseInt(value)
                        if (!isNaN(n)) this.aiMaxTokens = n
                      })
                  }
                  .layoutWeight(1)
                }
                .width('100%')

                if (this.testConnectionResult.length > 0) {
                  Text(this.testConnectionResult)
                    .fontSize(13)
                    .fontColor(this.testConnectionResult === '连接成功' ? RokuricsColors.mint : RokuricsColors.coral)
                }
              }
            }

            // Provider health summary
            Column({ space: 6 }) {
              Text('提供商状态')
                .fontSize(14).fontWeight(FontWeight.SemiBold)
                .fontColor(RokuricsColors.deepText)
                .width('100%')

              Row() {
                Circle()
                  .width(8).height(8)
                  .fill(this.providerHealthSummary.startsWith('已连接') ? RokuricsColors.mint : RokuricsColors.tertiaryText)
                Text(this.aiProviderKind === 'openaiCompatible' ? 'OpenAI 兼容模式' : 'Mock 模式')
                  .fontSize(13).fontColor(RokuricsColors.softText)
                  .margin({ left: 6 })
              }

              if (this.aiProviderKind === 'openaiCompatible') {
                Row() {
                  Text('模型:')
                    .fontSize(11).fontColor(RokuricsColors.tertiaryText)
                  Text(this.aiModelName)
                    .fontSize(11).fontColor(RokuricsColors.deepText)
                    .margin({ left: 4 })
                }
              }

              if (this.providerHealthSummary.length > 0) {
                Text(this.providerHealthSummary)
                  .fontSize(12)
                  .fontColor(this.providerHealthSummary.startsWith('已连接') ?
                    RokuricsColors.mint : RokuricsColors.coral)
              }

              if (this.providerLatency.length > 0) {
                Text(`延迟: ${this.providerLatency}`)
                  .fontSize(11)
                  .fontColor(RokuricsColors.tertiaryText)
              }

              if (this.lastConnectionTest.length > 0) {
                Text(`上次测试: ${this.lastConnectionTest}`)
                  .fontSize(11)
                  .fontColor(RokuricsColors.tertiaryText)
              }

              if (this.providerModels.length > 0) {
                Text(`可用模型 (${this.providerModels.length}):`)
                  .fontSize(11).fontColor(RokuricsColors.tertiaryText)
                  .margin({ top: 6 })
                Text(this.providerModels.join(', '))
                  .fontSize(10).fontColor(RokuricsColors.softText)
                  .maxLines(3).textOverflow({ overflow: TextOverflow.Ellipsis })
              }
            }
            .width('100%')
            .padding(12).borderRadius(12)
            .backgroundColor(RokuricsColors.glassSurface + '30')
            .margin({ top: 4 })

            Row({ space: 12 }) {
              if (this.aiProviderKind === 'openaiCompatible') {
                Button() {
                  Text(this.isTestingConnection ? '测试中...' : '测试连接')
                    .fontSize(13).fontColor(RokuricsColors.aqua)
                }
                .padding({ left: 16, right: 16, top: 8, bottom: 8 })
                .border({ width: 1, color: RokuricsColors.aqua + '40', radius: 8 })
                .backgroundColor(Color.Transparent)
                .enabled(!this.isTestingConnection)
                .onClick(() => this.testConnection())
              }

              Blank()

              Button() {
                Text('保存 AI 配置')
                  .fontSize(14).fontWeight(FontWeight.SemiBold).fontColor(Color.White)
              }
              .padding({ left: 20, right: 20, top: 10, bottom: 10 })
              .borderRadius(10)
              .backgroundColor(RokuricsColors.aqua)
              .enabled(!this.isSavingAI)
              .onClick(() => this.saveAIConfig())
            }
            .width('100%')

            if (this.aiSavedMessage.length > 0) {
              Text(this.aiSavedMessage)
                .fontSize(13)
                .fontColor(this.aiSavedMessage.startsWith('AI') ? RokuricsColors.mint : RokuricsColors.coral)
            }
          }
          .padding(20).borderRadius(20)
          .backgroundColor(RokuricsColors.glassSurface + '66')

          // Upload server configuration
          Column({ space: 12 }) {
            Text('上传服务器配置')
              .fontSize(16).fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
            Column({ space: 4 }) {
              Text('服务器地址').fontSize(12).fontColor(RokuricsColors.softText)
              TextInput({ text: this.uploadServerURL, placeholder: 'http://your-server.com' })
                .fontSize(14).fontColor(RokuricsColors.deepText)
                .backgroundColor(RokuricsColors.glassSurface + '40')
                .borderRadius(8).padding({ left: 12, right: 12, top: 8, bottom: 8 })
                .onChange((v: string) => { this.uploadServerURL = v })
            }
            Column({ space: 4 }) {
              Text('API Key（可选）').fontSize(12).fontColor(RokuricsColors.softText)
              TextInput({ text: this.uploadAPIKey, placeholder: 'Bearer token' })
                .fontSize(14).fontColor(RokuricsColors.deepText)
                .type(InputType.Password)
                .backgroundColor(RokuricsColors.glassSurface + '40')
                .borderRadius(8).padding({ left: 12, right: 12, top: 8, bottom: 8 })
                .onChange((v: string) => { this.uploadAPIKey = v })
            }
            Button() {
              Text('保存上传配置')
                .fontSize(14).fontWeight(FontWeight.SemiBold).fontColor(Color.White)
            }
            .width('100%').height(44).borderRadius(10)
            .backgroundColor(RokuricsColors.aqua)
            .enabled(!this.isSavingUpload)
            .onClick(() => this.saveUploadConfig())
            if (this.uploadSavedMessage.length > 0) {
              Text(this.uploadSavedMessage).fontSize(13)
                .fontColor(this.uploadSavedMessage.startsWith('上传') ? RokuricsColors.mint : RokuricsColors.coral)
            }
          }
          .padding(20).borderRadius(20)
          .backgroundColor(RokuricsColors.glassSurface + '66')

          // About
          Column({ space: 8 }) {
            Text('关于 Rokurics').fontSize(15).fontWeight(FontWeight.SemiBold).fontColor(RokuricsColors.deepText)
            Text('版本 1.0.0').fontSize(13).fontColor(RokuricsColors.tertiaryText)
            Text('学习录音与 AI 笔记\nHarmonyOS 平台').fontSize(13).fontColor(RokuricsColors.softText).textAlign(TextAlign.Center)
          }
          .width('100%').padding(20).borderRadius(20)
          .backgroundColor(RokuricsColors.glassSurface + '40')
          .margin({ top: 12 })
        }
        .width('100%')
        .padding({ left: 16, right: 16, bottom: 40 })
      }
      .width('100%').layoutWeight(1)
    }
    .width('100%').height('100%')
    .backgroundColor(RokuricsColors.pageBackground)
  }
}
