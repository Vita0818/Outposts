/**
 * Preference-based settings store for user profile and app config
 * Uses @ohos.data.preferences
 */
import { preferences } from '@kit.ArkData'
import { UserProfile } from '../models/UserProfile'

export class SettingsStore {
  private store: preferences.Preferences | null = null
  private context: Context

  constructor(context: Context) {
    this.context = context
  }

  async init(): Promise<void> {
    this.store = await preferences.getPreferences(this.context, 'RokuricsSettings')
  }

  async getUserProfile(): Promise<UserProfile> {
    if (!this.store) await this.init()
    const displayName = await this.store!.get('profile.displayName', UserProfile.DEFAULT_DISPLAY_NAME) as string
    const handle = await this.store!.get('profile.handle', UserProfile.DEFAULT_HANDLE) as string
    const avatar = await this.store!.get('profile.avatar', UserProfile.DEFAULT_AVATAR) as string
    return new UserProfile(displayName, handle, avatar)
  }

  async saveUserProfile(profile: UserProfile): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put('profile.displayName', profile.displayName)
    await this.store!.put('profile.handle', profile.handle)
    await this.store!.put('profile.avatar', profile.avatar)
    await this.store!.flush()
  }

  async getStringArray(key: string, defaultVal: string[]): Promise<string[]> {
    if (!this.store) await this.init()
    const raw = await this.store!.get(key, '') as string
    return raw ? JSON.parse(raw) : defaultVal
  }

  async putStringArray(key: string, val: string[]): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put(key, JSON.stringify(val))
    await this.store!.flush()
  }

  async getString(key: string, defaultVal: string = ''): Promise<string> {
    if (!this.store) await this.init()
    return await this.store!.get(key, defaultVal) as string
  }

  async putString(key: string, val: string): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put(key, val)
    await this.store!.flush()
  }

  async getBoolean(key: string, defaultVal: boolean = false): Promise<boolean> {
    if (!this.store) await this.init()
    return await this.store!.get(key, defaultVal) as boolean
  }

  async putBoolean(key: string, val: boolean): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put(key, val)
    await this.store!.flush()
  }

  async getNumber(key: string, defaultVal: number = 0): Promise<number> {
    if (!this.store) await this.init()
    return await this.store!.get(key, defaultVal) as number
  }

  async putNumber(key: string, val: number): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put(key, val)
    await this.store!.flush()
  }

  // AI provider configuration
  async getAIProviderKind(): Promise<string> {
    if (!this.store) await this.init()
    return await this.store!.get('ai.providerKind', 'mock') as string
  }

  async setAIProviderKind(kind: string): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put('ai.providerKind', kind)
    await this.store!.flush()
  }

  async getAIBaseURL(): Promise<string> {
    if (!this.store) await this.init()
    return await this.store!.get('ai.baseURL', 'https://api.openai.com/v1') as string
  }

  async setAIBaseURL(url: string): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put('ai.baseURL', url)
    await this.store!.flush()
  }

  async getAIModelName(): Promise<string> {
    if (!this.store) await this.init()
    return await this.store!.get('ai.modelName', 'gpt-4o-mini') as string
  }

  async setAIModelName(name: string): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put('ai.modelName', name)
    await this.store!.flush()
  }

  async getAIAPIKey(): Promise<string> {
    if (!this.store) await this.init()
    return await this.store!.get('ai.apiKey', '') as string
  }

  async setAIAPIKey(key: string): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put('ai.apiKey', key)
    await this.store!.flush()
  }

  async getAITemperature(): Promise<number> {
    if (!this.store) await this.init()
    return await this.store!.get('ai.temperature', 0.3) as number
  }

  async setAITemperature(temp: number): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put('ai.temperature', temp)
    await this.store!.flush()
  }

  async getAIMaxTokens(): Promise<number> {
    if (!this.store) await this.init()
    return await this.store!.get('ai.maxTokens', 2000) as number
  }

  async setAIMaxTokens(tokens: number): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put('ai.maxTokens', tokens)
    await this.store!.flush()
  }

  // Upload server configuration
  async getUploadServerURL(): Promise<string> {
    if (!this.store) await this.init()
    return await this.store!.get('upload.serverURL', '') as string
  }

  async setUploadServerURL(url: string): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put('upload.serverURL', url)
    await this.store!.flush()
  }

  async getUploadAPIKey(): Promise<string> {
    if (!this.store) await this.init()
    return await this.store!.get('upload.apiKey', '') as string
  }

  async setUploadAPIKey(key: string): Promise<void> {
    if (!this.store) await this.init()
    await this.store!.put('upload.apiKey', key)
    await this.store!.flush()
  }
}
