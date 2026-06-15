# Flotis Windows (WinUI 3)

此目录为 `Flotis-Apple` 的 WinUI 3 迁移版本，默认功能包括：

- 浮动指令按钮面板
- 全局热键：
  - `Ctrl+Shift+0` 切换面板显示
  - `Ctrl+Shift+R` 开始/停止语音
  - `Ctrl+Shift+1..8` 插入对应命令
- 语音转写（Windows 本地识别）
- 外部转写配置页（与 macOS 端等价的 `OpenAI Compatible` 上传链路）
- 粘贴注入（写入剪贴板并发送 `Ctrl+V`）
- 剪贴板与麦克风权限状态轮询显示，并提供系统设置入口

## 启动

```bash
cd Flotis-Windows
dotnet restore
dotnet build
dotnet run --project Flotis.csproj
```

> 外部转写模式已接入与 macOS 端一致的音频采集与 `OpenAI Compatible` 上传流程（multipart 上传本地录音文件）。
> 如需进一步对齐，可补充窗口定位与窗口外观策略、上次语言/模式持久化、界面视觉细化。
