/**
 * OpenAI-compatible API client using @ohos.net.http
 */
import { http } from '@kit.NetworkKit'
import { ChatRequest, ChatMessage, ChatMessageRole, ChatResult } from '../models/ChatModels'

const TAG = 'RokuricsOpenAI'

interface OpenAIMessage {
  role: string
  content: string
}

interface ChatCompletionRequest {
  model: string
  messages: OpenAIMessage[]
  max_tokens?: number
  temperature?: number
  stream?: boolean
}

interface ChatCompletionMessage {
  role: string
  content: string
}

interface ChatCompletionChoice {
  index: number
  message: ChatCompletionMessage
  finish_reason: string | null
}

interface ChatCompletionUsage {
  prompt_tokens: number
  completion_tokens: number
  total_tokens: number
}

interface ChatCompletionResponse {
  id: string
  object: string
  created: number
  model: string
  choices: ChatCompletionChoice[]
  usage?: ChatCompletionUsage
}

export interface AIConfiguration {
  baseURL: string
  modelName: string
  apiKey: string
  temperature: number
  maxTokens: number
}

export interface ProviderHealthResult {
  reachable: boolean
  statusCode: number | null
  models: string[]
  latencyMs: number | null
  errorMessage: string | null
}

export class OpenAICompatibleClient {
  static async chatCompletion(
    config: AIConfiguration,
    request: ChatRequest,
    timeoutMs: number = 120000
  ): Promise<ChatResult> {
    const endpoint = `${config.baseURL.replace(/\/+$/, '')}/chat/completions`

    const messages: OpenAIMessage[] = [
      {
        role: 'system',
        content: '你是 Rokurics 的学习助手。请基于用户提供的学习资料上下文进行回答。如果上下文不足，请明确说明，不要编造内容。请使用中文回复。'
      }
    ]

    for (const msg of request.messages) {
      if (msg.role === ChatMessageRole.SYSTEM) continue
      messages.push({
        role: msg.role === ChatMessageRole.ASSISTANT ? 'assistant' : 'user',
        content: msg.content
      })
    }

    const body: ChatCompletionRequest = {
      model: config.modelName,
      messages: messages,
      max_tokens: request.maxTokens ?? config.maxTokens,
      temperature: request.temperature ?? config.temperature,
      stream: false
    }

    const bodyJson = JSON.stringify(body)

    const httpRequest = http.createHttp()
    try {
      const response = await httpRequest.request(endpoint, {
        method: http.RequestMethod.POST,
        header: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${config.apiKey}`
        },
        extraData: bodyJson,
        expectDataType: http.HttpDataType.STRING,
        connectTimeout: 30000,
        readTimeout: timeoutMs
      })

      if (response.responseCode !== 200) {
        let errMsg = `HTTP ${response.responseCode}`
        try {
          const errBody = JSON.parse(response.result as string) as Record<string, Object>
          const err = errBody['error'] as Record<string, Object> | undefined
          if (err && err['message']) {
            errMsg = err['message'] as string
          }
        } catch { /* ignore parse error */ }
        throw new Error(errMsg)
      }

      const data: ChatCompletionResponse = JSON.parse(response.result as string) as ChatCompletionResponse

      if (!data.choices || data.choices.length === 0) {
        throw new Error('AI 没有返回内容')
      }

      const choice = data.choices[0]
      const content = choice.message.content?.trim() ?? ''

      if (!content) {
        throw new Error('AI 返回了空内容')
      }

      const reply = new ChatMessage(ChatMessageRole.ASSISTANT, content)
      const result = new ChatResult(reply, 'openai-compatible', 'OpenAI Compatible')
      result.modelName = data.model ?? config.modelName
      result.finishReason = choice.finish_reason ?? 'stop'
      result.outputWasTruncated = choice.finish_reason === 'length'

      console.info(`[${TAG}] chat completion success, tokens: ${data.usage?.total_tokens ?? 'unknown'}`)
      return result
    } finally {
      httpRequest.destroy()
    }
  }

  static async testConnection(config: AIConfiguration): Promise<boolean> {
    const endpoint = `${config.baseURL.replace(/\/+$/, '')}/models`

    const httpRequest = http.createHttp()
    try {
      const response = await httpRequest.request(endpoint, {
        method: http.RequestMethod.GET,
        header: {
          'Authorization': `Bearer ${config.apiKey}`
        },
        expectDataType: http.HttpDataType.STRING,
        connectTimeout: 10000,
        readTimeout: 10000
      })
      return response.responseCode === 200
    } catch {
      return false
    } finally {
      httpRequest.destroy()
    }
  }

  static async validateConfiguration(config: AIConfiguration): Promise<ProviderHealthResult> {
    const endpoint = `${config.baseURL.replace(/\/+$/, '')}/models`
    const startedAt = Date.now()

    const httpRequest = http.createHttp()
    try {
      const response = await httpRequest.request(endpoint, {
        method: http.RequestMethod.GET,
        header: {
          'Authorization': `Bearer ${config.apiKey}`
        },
        expectDataType: http.HttpDataType.STRING,
        connectTimeout: 10000,
        readTimeout: 10000
      })

      const latencyMs = Date.now() - startedAt
      let modelIds: string[] = []

      if (response.responseCode === 200) {
        try {
          const data = JSON.parse(response.result as string) as Record<string, Object>
          const dataArray = data['data'] as Record<string, Object>[] | undefined
          if (dataArray) {
            modelIds = dataArray
              .map((m: Record<string, Object>) => (m['id'] as string) ?? '')
              .filter((id: string) => id.length > 0)
          }
        } catch { /* ignore parse errors */ }
      }

      return {
        reachable: response.responseCode === 200,
        statusCode: response.responseCode,
        models: modelIds,
        latencyMs: latencyMs,
        errorMessage: response.responseCode === 200 ? null : `HTTP ${response.responseCode}`
      }
    } catch (err) {
      return {
        reachable: false,
        statusCode: null,
        models: [],
        latencyMs: Date.now() - startedAt,
        errorMessage: `${err}`
      }
    } finally {
      httpRequest.destroy()
    }
  }

  static async chatCompletionSimple(
    config: AIConfiguration,
    systemPrompt: string,
    userMessage: string,
    timeoutMs: number = 180000
  ): Promise<string> {
    const endpoint = `${config.baseURL.replace(/\/+$/, '')}/chat/completions`

    const body: ChatCompletionRequest = {
      model: config.modelName,
      messages: [
        { role: 'system', content: systemPrompt },
        { role: 'user', content: userMessage }
      ],
      max_tokens: config.maxTokens,
      temperature: config.temperature,
      stream: false
    }

    const httpRequest = http.createHttp()
    try {
      const response = await httpRequest.request(endpoint, {
        method: http.RequestMethod.POST,
        header: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${config.apiKey}`
        },
        extraData: JSON.stringify(body),
        expectDataType: http.HttpDataType.STRING,
        connectTimeout: 30000,
        readTimeout: timeoutMs
      })

      if (response.responseCode !== 200) {
        let errMsg = `HTTP ${response.responseCode}`
        try {
          const errBody = JSON.parse(response.result as string) as Record<string, Object>
          const err = errBody['error'] as Record<string, Object> | undefined
          if (err && err['message']) errMsg = err['message'] as string
        } catch { /* ignore */ }
        throw new Error(errMsg)
      }

      const data: ChatCompletionResponse = JSON.parse(response.result as string) as ChatCompletionResponse
      if (!data.choices || data.choices.length === 0) {
        throw new Error('AI 没有返回内容')
      }

      const content = data.choices[0].message.content?.trim() ?? ''
      if (!content) throw new Error('AI 返回了空内容')

      console.info(`[${TAG}] completion success, model: ${data.model}`)
      return content
    } finally {
      httpRequest.destroy()
    }
  }
}
