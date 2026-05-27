/**
 * AI Chat page - mirrors IPhoneAIChatView.swift
 */
import { ChatMessage, ChatMessageRole, ChatConversation, ChatRequest, ChatResult } from '../models/ChatModels'
import { MockChatProvider, OpenAICompatibleChatProvider, createChatProvider } from '../providers/ProviderInterfaces'
import { ChatStore } from '../services/ChatStore'
import { SettingsStore } from '../services/SettingsStore'
import { AIConfiguration } from '../services/OpenAICompatibleClient'
import { RokuricsColors, FontWeight } from '../utils/RokuricsTheme'
import { SendIcon } from '../utils/CustomIcons'

@Entry
@Component
struct AIChatPage {
  private chatProvider: MockChatProvider | OpenAICompatibleChatProvider = new MockChatProvider()
  private chatStore: ChatStore = new ChatStore(getContext(this))
  private settingsStore: SettingsStore = new SettingsStore(getContext(this))
  @State messages: ChatMessage[] = []
  @State inputText: string = ''
  @State isGenerating: boolean = false
  @State errorText: string = ''
  @State activeConversation: ChatConversation | null = null
  @State recentConversations: ChatConversation[] = []
  @State showConversationList: boolean = false
  @State conversationTitle: string = '新对话'
  @State providerDisplayName: string = 'Mock'
  @State displayName: string = ''

  async aboutToAppear(): Promise<void> {
    await this.initProvider()
    await this.loadDisplayName()
    this.loadRecentConversations()
  }

  async initProvider(): Promise<void> {
    const kind = await this.settingsStore.getAIProviderKind()
    if (kind === 'openaiCompatible') {
      const config: AIConfiguration = {
        baseURL: await this.settingsStore.getAIBaseURL(),
        modelName: await this.settingsStore.getAIModelName(),
        apiKey: await this.settingsStore.getAIAPIKey(),
        temperature: await this.settingsStore.getAITemperature(),
        maxTokens: await this.settingsStore.getAIMaxTokens()
      }
      const provider = new OpenAICompatibleChatProvider(config)
      this.chatProvider = provider
      this.providerDisplayName = `OpenAI (${config.modelName})`
    }
  }

  async loadDisplayName(): Promise<void> {
    const settings = this.settingsStore
    const profile = await settings.getUserProfile()
    this.displayName = profile.displayName
  }

  async loadRecentConversations(): Promise<void> {
    this.recentConversations = await this.chatStore.loadAllConversations()
  }

  async startNewConversation(): Promise<void> {
    await this.saveCurrentConversation()
    const conv = new ChatConversation()
    conv.title = '新对话'
    this.activeConversation = conv
    this.messages = []
    this.conversationTitle = conv.title
    this.showConversationList = false
    this.errorText = ''
  }

  async selectConversation(id: string): Promise<void> {
    await this.saveCurrentConversation()
    const conv = await this.chatStore.loadConversation(id)
    if (conv) {
      this.activeConversation = conv
      this.messages = conv.messages
      this.conversationTitle = conv.title
      this.showConversationList = false
      this.errorText = ''
    }
  }

  async deleteConversation(id: string): Promise<void> {
    await this.chatStore.deleteConversation(id)
    if (this.activeConversation?.id === id) {
      this.activeConversation = null
      this.messages = []
      this.conversationTitle = '新对话'
    }
    await this.loadRecentConversations()
  }

  async saveCurrentConversation(): Promise<void> {
    if (this.messages.length === 0) return
    let conv: ChatConversation
    if (this.activeConversation) {
      conv = this.activeConversation
    } else {
      conv = new ChatConversation()
    }
    conv.messages = this.messages
    conv.updatedAt = new Date()
    const firstUserMsg = this.messages.find(m => m.role === ChatMessageRole.USER)
    if (firstUserMsg && conv.title === '新对话') {
      conv.title = firstUserMsg.content.substring(0, 30) + (firstUserMsg.content.length > 30 ? '...' : '')
    }
    await this.chatStore.saveConversation(conv)
    this.activeConversation = conv
    this.conversationTitle = conv.title
  }

  build() {
    Column() {
      // Header
      Row() {
        // Glass circle back button
        Button() {
          Text('←')
            .fontSize(18)
            .fontWeight(FontWeight.SemiBold)
            .fontColor(RokuricsColors.deepText)
        }
        .width(44)
        .height(44)
        .borderRadius(22)
        .backgroundColor(RokuricsColors.glassSurface + '66')
        .shadow({
          color: RokuricsColors.shadowColor + '10',
          radius: 12,
          offsetY: 6
        })
        .border({
          width: 1,
          color: {
            colors: [
              [0xFFFFFF, 0.44],
              [0xEFFAF8, 0.14],
              [0x59C7C2, 0.12]
            ],
            direction: GradientDirection.RightBottom
          },
          radius: 22
        } as BorderOptions)
        .onClick(() => {
          this.saveCurrentConversation()
          this.getUIContext().getRouter().back()
        })

        Text(this.conversationTitle)
          .fontSize(20)
          .fontWeight(FontWeight.SemiBold)
          .fontColor(RokuricsColors.deepText)
          .maxLines(1)
          .textOverflow({ overflow: TextOverflow.Ellipsis })
          .layoutWeight(1)

        Button() {
          Text('+新')
            .fontSize(14)
            .fontWeight(FontWeight.Medium)
            .fontColor(RokuricsColors.aqua)
        }
        .backgroundColor(Color.Transparent)
        .margin({ right: 8 })
        .onClick(() => this.startNewConversation())

        Button() {
          Text('☰')
            .fontSize(18)
            .fontColor(RokuricsColors.aqua)
        }
        .width(44)
        .height(44)
        .backgroundColor(Color.Transparent)
        .onClick(() => {
          this.loadRecentConversations()
          this.showConversationList = !this.showConversationList
        })
      }
      .width('100%')
      .padding({ left: 16, right: 16, top: 56, bottom: 12 })

      // Conversation list overlay with glass card
      if (this.showConversationList) {
        Column() {
          Row() {
            Text('对话历史')
              .fontSize(16)
              .fontWeight(FontWeight.SemiBold)
              .fontColor(RokuricsColors.deepText)
            Blank()
            Button() {
              Text('+ 新建')
                .fontSize(14)
                .fontWeight(FontWeight.Medium)
                .fontColor(RokuricsColors.aqua)
            }
            .backgroundColor(Color.Transparent)
            .onClick(() => this.startNewConversation())
          }
          .width('100%')
          .padding(16)

          if (this.recentConversations.length === 0) {
            Text('暂无历史对话')
              .fontSize(13)
              .fontColor(RokuricsColors.tertiaryText)
              .margin({ top: 16, bottom: 24 })
          } else {
            List({ space: 6 }) {
              ForEach(this.recentConversations, (conv: ChatConversation) => {
                ListItem() {
                  Row() {
                    Column({ space: 2 }) {
                      Text(conv.title)
                        .fontSize(14)
                        .fontWeight(FontWeight.Medium)
                        .fontColor(conv.id === this.activeConversation?.id ?
                          RokuricsColors.aqua : RokuricsColors.deepText)
                        .maxLines(1)
                        .textOverflow({ overflow: TextOverflow.Ellipsis })
                      Text(`${conv.messages.length} 条消息`)
                        .fontSize(11)
                        .fontColor(RokuricsColors.tertiaryText)
                    }
                    .layoutWeight(1)
                    .alignItems(HorizontalAlign.Start)

                    Button() {
                      Text('🗑')
                        .fontSize(14)
                        .fontColor(RokuricsColors.tertiaryText)
                    }
                    .width(32)
                    .height(32)
                    .backgroundColor(Color.Transparent)
                    .onClick(() => this.deleteConversation(conv.id))
                  }
                  .width('100%')
                  .padding({ left: 14, right: 14, top: 10, bottom: 10 })
                  .borderRadius(12)
                  .backgroundColor(conv.id === this.activeConversation?.id ?
                    RokuricsColors.aqua + '14' : RokuricsColors.glassSurface + '50')
                  .onClick(() => this.selectConversation(conv.id))
                }
              })
            }
            .width('100%')
            .constraintSize({ maxHeight: 280 })
            .scrollBar(BarState.Off)
          }
        }
        .width('100%')
        .backgroundColor(RokuricsColors.glassSurface + 'F0')
        .borderRadius({ bottomLeft: 16, bottomRight: 16 })
        .shadow({
          color: RokuricsColors.shadowColor + '20',
          radius: 16,
          offsetY: 8
        })
        .zIndex(10)
      }

      // Messages area
      if (this.messages.length === 0) {
        // Greeting (mirrors Apple ChatGreeting)
        Column({ space: 16 }) {
          Text('Rokurics AI')
            .fontSize(28)
            .fontWeight(FontWeight.SemiBold)
            .fontColor(RokuricsColors.aqua)

          Text(`你好${this.displayName ? '，' + this.displayName : ''}`)
            .fontSize(24)
            .fontWeight(FontWeight.SemiBold)
            .fontColor(RokuricsColors.deepText)

          Text('基于你的学习资料进行智能问答')
            .fontSize(14)
            .fontColor(RokuricsColors.softText)

          // Quick prompt buttons
          Row({ space: 8 }) {
            this.QuickPrompt('总结要点')
            this.QuickPrompt('生成大纲')
            this.QuickPrompt('解答疑问')
          }
          .margin({ top: 12 })
        }
        .width('100%')
        .layoutWeight(1)
        .justifyContent(FlexAlign.Center)
      } else {
        List({ space: 12 }) {
          ForEach(this.messages, (msg: ChatMessage) => {
            ListItem() {
              this.ChatBubble(msg)
            }
          })
        }
        .width('100%')
        .layoutWeight(1)
        .padding({ left: 16, right: 16 })
        .scrollBar(BarState.Off)
      }

      // Error
      if (this.errorText.length > 0) {
        Text(this.errorText)
          .fontSize(12)
          .fontColor(RokuricsColors.coral)
          .padding({ left: 16, right: 16, top: 4, bottom: 4 })
      }

      // Input bar
      Row() {
        TextInput({ text: this.inputText, placeholder: '输入消息...' })
          .fontSize(15)
          .layoutWeight(1)
          .height(44)
          .borderRadius(22)
          .padding({ left: 16, right: 16 })
          .backgroundColor(RokuricsColors.glassSurface + '80')
          .onChange((value: string) => { this.inputText = value })

        Button() {
          SendIcon(16, '#FFFFFF')
        }
        .width(44)
        .height(44)
        .borderRadius(22)
        .backgroundColor(
          this.inputText.trim().length > 0 && !this.isGenerating ?
            RokuricsColors.aqua : RokuricsColors.tertiaryText
        )
        .shadow({
          color: (this.inputText.trim().length > 0 && !this.isGenerating ?
            RokuricsColors.aqua : RokuricsColors.tertiaryText) + '30',
          radius: 10,
          offsetY: 4
        })
        .enabled(this.inputText.trim().length > 0 && !this.isGenerating)
        .onClick(() => this.sendMessage())
      }
      .width('100%')
      .padding({ left: 16, right: 16, top: 8, bottom: 32 })
    }
    .width('100%')
    .height('100%')
    .backgroundColor(RokuricsColors.pageBackground)
  }

  @Builder
  ChatBubble(msg: ChatMessage) {
    Row() {
      if (msg.role === ChatMessageRole.ASSISTANT) {
        Column() {
          Text(msg.content)
            .fontSize(15)
            .fontColor(RokuricsColors.deepText)
            .padding(14)
            .borderRadius({ topLeft: 4, topRight: 16, bottomLeft: 16, bottomRight: 16 })
            .backgroundColor(RokuricsColors.glassSurface + 'B8')
            .border({
              width: 1,
              color: {
                colors: [
                  [0xFFFFFF, 0.30],
                  [0xEFFAF8, 0.10]
                ],
                direction: GradientDirection.RightBottom
              },
              radius: { topLeft: 4, topRight: 16, bottomLeft: 16, bottomRight: 16 }
            } as BorderOptions)
        }
        .alignItems(HorizontalAlign.Start)
        .constraintSize({ maxWidth: '80%' })
        Blank()
      } else {
        Blank()
        Column() {
          Text(msg.content)
            .fontSize(15)
            .fontColor(Color.White)
            .padding(14)
            .borderRadius({ topLeft: 16, topRight: 4, bottomLeft: 16, bottomRight: 16 })
            .backgroundColor(RokuricsColors.aqua)
            .shadow({
              color: RokuricsColors.aqua + '20',
              radius: 8,
              offsetY: 4
            })
        }
        .alignItems(HorizontalAlign.End)
        .constraintSize({ maxWidth: '80%' })
      }
    }
    .width('100%')
  }

  @Builder
  QuickPrompt(text: string) {
    Text(text)
      .fontSize(13)
      .fontWeight(FontWeight.Medium)
      .fontColor(RokuricsColors.aqua)
      .padding({ left: 14, right: 14, top: 8, bottom: 8 })
      .borderRadius(16)
      .backgroundColor(RokuricsColors.aqua + '10')
      .border({ width: 1, color: RokuricsColors.aqua + '40', radius: 16 })
      .onClick(() => {
        this.inputText = text
        this.sendMessage()
      })
  }

  async sendMessage(): Promise<void> {
    const text: string = this.inputText.trim()
    if (text.length === 0 || this.isGenerating) return

    this.inputText = ''
    this.isGenerating = true
    this.errorText = ''

    const userMsg: ChatMessage = new ChatMessage(ChatMessageRole.USER, text)
    const newMessages: ChatMessage[] = []
    for (const m of this.messages) { newMessages.push(m) }
    newMessages.push(userMsg)
    this.messages = newMessages

    try {
      const request: ChatRequest = new ChatRequest()
      request.messages = this.messages
      request.context = null
      request.maxTokens = 2000
      request.temperature = 0.3

      const result: ChatResult = await this.chatProvider.send(request)

      const updated: ChatMessage[] = []
      for (const m of this.messages) { updated.push(m) }
      updated.push(result.message)
      this.messages = updated

      await this.saveCurrentConversation()
    } catch {
      this.errorText = '发送失败'
    } finally {
      this.isGenerating = false
    }
  }
}