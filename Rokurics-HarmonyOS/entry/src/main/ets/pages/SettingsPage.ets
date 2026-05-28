/**
 * Settings page with user profile and AI provider configuration
 */
import { router } from '@kit.ArkUI'
import { UserProfile } from '../models/UserProfile'
import { SettingsStore } from '../services/SettingsStore'
import { OpenAICompatibleClient, AIConfiguration, ProviderHealthResult } from '../services/OpenAICompatibleClient'
import { colorAlpha,  RokuricsColors, FontWeight } from '../utils/RokuricsTheme'

function formatTime(d: Date): string {
  const h = String(d.getHours()).padStart(2, '0')
  const m = String(d.getMinutes()).padStart(2, '0')
  return `${h}:${m}`
}

function isEnglishLabel(text: string): boolean {
  return /^[A-Za-z]/.test(text)
}

function isTechValue(text: string): boolean {
  return /[.\\-_]/.test(text) || /^[a-z]/.test(text)
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
  @State showProfileEditor: boolean = false
  @State showAIEditor: boolean = false

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
        Column({ space: 28 }) {
          // ── Profile avatar area ──
          Column({ space: 10 }) {
            Stack() {
              Circle()
                .width(72).height(72)
                .fill(colorAlpha(RokuricsColors.aqua, '18'))
              Text(this.profile.initial)
                .fontSize(28)
                .fontWeight(FontWeight.SemiBold)
                .fontColor(RokuricsColors.aqua)
            }
            Text(this.profile.displayName)
              .fontSize(18)
              .fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
            Text(this.profile.displayHandle)
              .fontSize(12)
              .fontColor(RokuricsColors.tertiaryText)

            Button() {
              Text('编辑个人资料')
                .fontSize(13)
                .fontWeight(FontWeight.Medium)
                .fontColor(RokuricsColors.softText)
            }
            .padding({ left: 18, right: 18, top: 7, bottom: 7 })
            .borderRadius(16)
            .backgroundColor(Color.Transparent)
            .border({ width: 1, color: colorAlpha(RokuricsColors.softText, '30'), radius: 16 })
            .onClick(() => { this.showProfileEditor = !this.showProfileEditor })
          }
          .width('100%')

          // Expandable profile editor
          if (this.showProfileEditor) {
            Column({ space: 12 }) {
              Column({ space: 4 }) {
                Text('显示名称').fontSize(11).fontColor(RokuricsColors.tertiaryText)
                TextInput({ text: this.displayName, placeholder: '输入显示名称' })
                  .fontSize(15).fontColor(RokuricsColors.deepText)
                  .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '60'))
                  .borderRadius(8).padding({ left: 12, right: 12, top: 9, bottom: 9 })
                  .onChange((value: string) => { this.displayName = value })
              }
              Column({ space: 4 }) {
                Text('账号 (@handle)').fontSize(11).fontColor(RokuricsColors.tertiaryText)
                TextInput({ text: this.handle, placeholder: '输入账号' })
                  .fontSize(15).fontColor(RokuricsColors.deepText)
                  .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '60'))
                  .borderRadius(8).padding({ left: 12, right: 12, top: 9, bottom: 9 })
                  .onChange((value: string) => { this.handle = value })
              }
              Button() {
                Text(this.isSaving ? '保存中...' : '保存')
                  .fontSize(14).fontWeight(FontWeight.SemiBold).fontColor(Color.White)
              }
              .width('100%').height(42).borderRadius(10)
              .backgroundColor(RokuricsColors.aqua)
              .enabled(!this.isSaving)
              .onClick(() => this.saveProfile())
              if (this.savedMessage.length > 0) {
                Text(this.savedMessage).fontSize(12)
                  .fontColor(this.savedMessage.startsWith('已保存') ? RokuricsColors.mint : RokuricsColors.coral)
              }
            }
            .padding(16).borderRadius(16)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '50'))
            .width('100%')
          }

          // ── 转写 section ──
          this.SettingsSection('转写') {
            Column() {
              this.SettingsRow('Provider', 'Mac 安全转写')
              this.SettingsDivider()
              this.SettingsRow('模型', 'whisper.cpp')
              this.SettingsDivider()
              Row() {
                this.SettingsLinkRowContent('授权与测试')
              }
              .width('100%')
              .padding({ left: 16, right: 16, top: 13, bottom: 13 })
              .onClick(() => {
                this.getUIContext().getRouter().pushUrl({ url: 'pages/AuthTestPage' })
              })
            }
          }

          // ── AI section ──
          this.SettingsSection('AI') {
            Column() {
              Row() {
                this.SettingsRowContent('Provider', this.aiProviderKind === 'mock' ? 'Mock' : 'OpenAI')
              }
              .onClick(() => { this.showAIEditor = !this.showAIEditor })
              this.SettingsDivider()
              Row() {
                this.SettingsRowContent('模型', this.aiModelName)
              }
              .onClick(() => { this.showAIEditor = !this.showAIEditor })
              this.SettingsDivider()
              Row() {
                this.SettingsLinkRowContent('API 设置')
              }
              .onClick(() => { this.showAIEditor = !this.showAIEditor })
              this.SettingsDivider()
              Row() {
                this.SettingsLinkRowContent('测试')
              }
              .onClick(() => this.testConnection())
            }
          }

          // ── AI detail editor (expandable) ──
          if (this.showAIEditor) {
            Column({ space: 12 }) {
              // Provider kind toggle
              Row({ space: 0 }) {
                Button() {
                  Text('Mock').fontSize(12)
                    .fontColor(this.aiProviderKind === 'mock' ? Color.White : RokuricsColors.softText)
                }
                .padding({ left: 14, right: 14, top: 6, bottom: 6 })
                .borderRadius({ topLeft: 8, bottomLeft: 8 })
                .backgroundColor(this.aiProviderKind === 'mock' ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '40'))
                .onClick(() => { this.aiProviderKind = 'mock' })
                Button() {
                  Text('OpenAI').fontSize(12)
                    .fontColor(this.aiProviderKind === 'openaiCompatible' ? Color.White : RokuricsColors.softText)
                }
                .padding({ left: 14, right: 14, top: 6, bottom: 6 })
                .borderRadius({ topRight: 8, bottomRight: 8 })
                .backgroundColor(this.aiProviderKind === 'openaiCompatible' ? RokuricsColors.aqua : colorAlpha(RokuricsColors.glassSurface, '40'))
                .onClick(() => { this.aiProviderKind = 'openaiCompatible' })
              }
              if (this.aiProviderKind === 'openaiCompatible') {
                this.SettingsInput('API 地址', this.aiBaseURL, 'https://api.openai.com/v1',
                  (v: string) => { this.aiBaseURL = v })
                this.SettingsInput('模型名称', this.aiModelName, 'gpt-4o-mini',
                  (v: string) => { this.aiModelName = v })
                this.SettingsInput('API Key', this.aiAPIKey, 'sk-...',
                  (v: string) => { this.aiAPIKey = v }, true)
                Row({ space: 8 }) {
                  this.SettingsInput('温度', String(this.aiTemperature), '0.3',
                    (v: string) => { const n = parseFloat(v); if (!isNaN(n)) this.aiTemperature = n }, false, true)
                  this.SettingsInput('最大 Tokens', String(this.aiMaxTokens), '2000',
                    (v: string) => { const n = parseInt(v); if (!isNaN(n)) this.aiMaxTokens = n }, false, true)
                }
                .width('100%')
                if (this.aiProviderKind === 'openaiCompatible') {
                  Row({ space: 8 }) {
                    Button() {
                      Text(this.isTestingConnection ? '测试中...' : '测试连接')
                        .fontSize(12).fontColor(RokuricsColors.aqua)
                    }
                    .padding({ left: 12, right: 12, top: 6, bottom: 6 })
                    .border({ width: 1, color: colorAlpha(RokuricsColors.aqua, '40'), radius: 6 })
                    .backgroundColor(Color.Transparent)
                    .enabled(!this.isTestingConnection)
                    .onClick(() => this.testConnection())
                    if (this.testConnectionResult.length > 0) {
                      Text(this.testConnectionResult).fontSize(11)
                        .fontColor(this.testConnectionResult === '连接成功' ? RokuricsColors.mint : RokuricsColors.coral)
                    }
                  }
                }
              }
              Button() {
                Text('保存 AI 配置').fontSize(13).fontWeight(FontWeight.SemiBold).fontColor(Color.White)
              }
              .padding({ left: 16, right: 16, top: 8, bottom: 8 })
              .borderRadius(8).backgroundColor(RokuricsColors.aqua)
              .enabled(!this.isSavingAI)
              .onClick(() => this.saveAIConfig())
              if (this.aiSavedMessage.length > 0) {
                Text(this.aiSavedMessage).fontSize(11)
                  .fontColor(this.aiSavedMessage.startsWith('AI') ? RokuricsColors.mint : RokuricsColors.coral)
              }
            }
            .width('100%').padding(14).borderRadius(16)
            .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '50'))
          }

          // ── 关于 section ──
          this.SettingsSection('关于') {
            Column() {
              this.SettingsRow('存储', '本机')
              this.SettingsDivider()
              Row() {
                this.SettingsLinkRowContent('隐私政策')
              }
              .width('100%')
              .padding({ left: 16, right: 16, top: 13, bottom: 13 })
              .onClick(() => {
                this.getUIContext().getRouter().pushUrl({ url: 'pages/PrivacyPolicyPage' })
              })
              this.SettingsDivider()
              this.SettingsRow('版权', '1.0 (1)')
            }
          }
        }
        .width('100%')
        .padding({ left: 16, right: 16, bottom: 40 })
      }
      .width('100%').layoutWeight(1)
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

  // ── Settings grouped card builders ──

  @Builder
  SettingsSection(title: string, child: WrappedBuilder<[]>) {
    Column({ space: 8 }) {
      Text(title)
        .fontSize(13)
        .fontWeight(FontWeight.Medium)
        .fontColor(RokuricsColors.tertiaryText)
        .padding({ left: 6 })
        .width('100%')
      Column() {
        child.builder()
      }
      .width('100%')
      .borderRadius(18)
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
        radius: 18
      } as BorderOptions)
    }
    .width('100%')
  }

  @Builder
  SettingsRow(label: string, value: string) {
    Row() {
      this.SettingsRowContent(label, value)
    }
    .width('100%')
    .padding({ left: 16, right: 16, top: 13, bottom: 13 })
  }

  @Builder
  SettingsRowContent(label: string, value: string) {
    Text(label)
      .fontSize(15)
      .fontWeight(FontWeight.Medium)
      .fontColor(RokuricsColors.deepText)
      .fontFamily(isEnglishLabel(label) ? 'serif' : 'sans-serif')
    Blank()
    Text(value)
      .fontSize(14)
      .fontWeight(FontWeight.Regular)
      .fontColor(RokuricsColors.softText)
      .fontFamily(isTechValue(value) ? 'monospace' : (isEnglishLabel(value) ? 'serif' : 'sans-serif'))
  }

  @Builder
  SettingsLinkRow(label: string) {
    Row() {
      this.SettingsLinkRowContent(label)
    }
    .width('100%')
    .padding({ left: 16, right: 16, top: 13, bottom: 13 })
  }

  @Builder
  SettingsLinkRowContent(label: string) {
    Text(label)
      .fontSize(15)
      .fontWeight(FontWeight.Medium)
      .fontColor(RokuricsColors.deepText)
      .fontFamily(isEnglishLabel(label) ? 'serif' : 'sans-serif')
    Blank()
    Text('查看')
      .fontSize(14)
      .fontWeight(FontWeight.Regular)
      .fontColor(RokuricsColors.aqua)
    Text('›')
      .fontSize(16)
      .fontColor(RokuricsColors.tertiaryText)
      .margin({ left: 2 })
  }

  @Builder
  SettingsDivider() {
    Divider()
      .strokeWidth(0.5)
      .color(colorAlpha(RokuricsColors.softText, '10'))
      .margin({ left: 16, right: 16 })
  }

  @Builder
  SettingsInput(label: string, value: string, placeholder: string,
                onChange: (v: string) => void, isPassword?: boolean, isNumber?: boolean) {
    Column({ space: 3 }) {
      Text(label).fontSize(10).fontColor(RokuricsColors.tertiaryText)
      TextInput({ text: value, placeholder: placeholder })
        .fontSize(13).fontColor(RokuricsColors.deepText)
        .type(isPassword ? InputType.Password : isNumber ? InputType.Number : InputType.Normal)
        .backgroundColor(colorAlpha(RokuricsColors.glassSurface, '40'))
        .borderRadius(6).padding({ left: 10, right: 10, top: 7, bottom: 7 })
        .onChange(onChange)
    }
    .layoutWeight(1)
  }
}
