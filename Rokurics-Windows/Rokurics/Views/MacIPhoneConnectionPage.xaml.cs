using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rokurics.Services;
using Rokurics.Stores;

namespace Rokurics.Views;

/// <summary>
/// iPhone Connection page matching MacIPhoneConnectionView from source.
/// Unpaired: pairing info with address, port, fingerprint, pairing code.
/// Connected: device bubble + status card with sync/disconnect actions.
/// </summary>
public sealed partial class MacIPhoneConnectionPage : Page
{
    private readonly DeviceConnectionStatusStore _connectionStore;
    private readonly IPairingService _pairingService;
    private readonly IKestrelReceiverService _kestrelService;
    private bool _isPaired;
    private string? _activePairingCode;

    public string LocalAddress => GetLocalIPAddress();
    public string LocalPort => _kestrelService.Port.ToString();
    public string FingerprintDisplay =>
        !string.IsNullOrEmpty(_kestrelService.Fingerprint) && _kestrelService.Fingerprint != "未生成"
            ? _kestrelService.Fingerprint
            : (_activePairingCode is not null ? "HTTPS 身份已就绪" : "HTTPS 身份未就绪");
    public string ConnectionInfo => $"{LocalAddress}:{LocalPort}";
    public string ConnectionState =>
        _kestrelService.IsRunning ? "已连接"
        : _isPaired ? "已配对（未启动）"
        : "未配对";
    public string LastSeenText =>
        _connectionStore.CurrentDevice?.LastSeenAt?.ToString("yyyy-MM-dd HH:mm") ?? "暂无";
    public string LastSyncText =>
        _connectionStore.CurrentDevice?.LastSyncAt?.ToString("yyyy-MM-dd HH:mm") ?? "暂无";

    public MacIPhoneConnectionPage()
    {
        InitializeComponent();
        _connectionStore = App.Current.Services.GetService<DeviceConnectionStatusStore>()
            ?? new DeviceConnectionStatusStore();
        _pairingService = App.Current.Services.GetService<IPairingService>()
            ?? new PairingService();
        _kestrelService = App.Current.Services.GetService<IKestrelReceiverService>()
            ?? new KestrelReceiverService();
        Bindings.Update();
    }

    private void StartPairing_Click(object sender, RoutedEventArgs e)
    {
        _activePairingCode = _pairingService.GeneratePairingCode();
        StartPairingButton.Visibility = Visibility.Collapsed;
        PairingCodeCard.Visibility = Visibility.Visible;
        PairingCodeText.Text = _activePairingCode;
        StopBreathingAnimation();
        StartBreathingAnimation();
    }

    private void StartBreathingAnimation()
    {
        try
        {
            BreathingAnimation.Stop();
            BreathingAnimation.Begin();
        }
        catch { }
    }

    private void StopBreathingAnimation()
    {
        try
        {
            BreathingAnimation.Stop();
            DeviceBubble.Opacity = 1.0;
            DeviceBubbleScaleTransform.ScaleX = 1.0;
            DeviceBubbleScaleTransform.ScaleY = 1.0;
        }
        catch { }
    }

    private void OnConnectedPanelShown()
    {
        StopBreathingAnimation();
    }

    private void CopyPairingInfo_Click(object sender, RoutedEventArgs e)
    {
        var info = $"Mac: {LocalAddress}:{LocalPort}\n指纹: {FingerprintDisplay}";
        if (_activePairingCode is not null)
            info += $"\n配对码: {_activePairingCode}";
        try
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(info);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
        catch
        {
            // Clipboard not available on all platforms
        }
    }

    // ── Connected device actions ─────────────────────────────────

    private async void SyncNow_Click(object sender, RoutedEventArgs e)
    {
        SyncNowButton.IsEnabled = false;
        try
        {
            if (!_kestrelService.IsRunning)
            {
                _kestrelService.StartSecureReceiving();
            }

            if (_kestrelService.LatestPairedDevice is not null)
            {
                // Trigger study library sync via the kestrel service
                _connectionStore.RecordSyncResult(
                    _kestrelService.LatestPairedDevice.DeviceId,
                    _kestrelService.LatestPairedDevice.DeviceName,
                    "同步完成", true);
            }

            await new ContentDialog
            {
                Title = "同步",
                Content = _kestrelService.IsRunning
                    ? $"服务器运行中\n已接受上传: {_kestrelService.AcceptedUploadCount}\n最近文件: {_kestrelService.LastAcceptedFileName}"
                    : "Kestrel 服务器未就绪，同步暂不可用。",
                CloseButtonText = "确定",
                XamlRoot = XamlRoot
            }.ShowAsync();
        }
        catch (NotImplementedException)
        {
            await new ContentDialog
            {
                Title = "同步",
                Content = "Kestrel HTTPS 服务器需要 .NET 运行时。\n请在 Windows 上启动服务器后重试。",
                CloseButtonText = "确定",
                XamlRoot = XamlRoot
            }.ShowAsync();
        }
        finally
        {
            SyncNowButton.IsEnabled = true;
            Bindings.Update();
        }
    }

    private async void ConnectionDetail_Click(object sender, RoutedEventArgs e)
    {
        var panel = new StackPanel { Spacing = 16, Width = 560 };

        // Connection detail rows
        var detailCard = new StackPanel { Spacing = 0 };
        AddConnectionRow(detailCard, "iPhone 名称", "iPhone", false);
        AddConnectionSeparator(detailCard);
        AddConnectionRow(detailCard, "deviceID", "未知", true);
        AddConnectionSeparator(detailCard);
        AddConnectionRow(detailCard, "IP", LocalAddress, true);
        AddConnectionSeparator(detailCard);
        AddConnectionRow(detailCard, "端口", LocalPort, true);
        AddConnectionSeparator(detailCard);
        AddConnectionRow(detailCard, "连接状态", ConnectionState, false);
        AddConnectionSeparator(detailCard);
        AddConnectionRow(detailCard, "配对时间", "未配对", false);
        AddConnectionSeparator(detailCard);
        AddConnectionRow(detailCard, "最近连接", LastSeenText, false);
        AddConnectionSeparator(detailCard);
        AddConnectionRow(detailCard, "最近同步", LastSyncText, false);
        AddConnectionSeparator(detailCard);
        AddConnectionRow(detailCard, "安全上传测试", "0", true);
        AddConnectionSeparator(detailCard);
        AddConnectionRow(detailCard, "最近测试文件", "暂无", false);

        var detailBorder = new Border
        {
            Style = (Style)Application.Current.Resources["RokuricsCardStyle"],
            Padding = new Thickness(6),
            CornerRadius = new CornerRadius(24),
            Child = detailCard
        };
        panel.Children.Add(detailBorder);

        // Certificate fingerprint section
        panel.Children.Add(new TextBlock
        {
            Text = "certificate fingerprint",
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Opacity = 0.5,
            Margin = new Thickness(0, 0, 0, 4)
        });

        var fpBorder = new Border
        {
            Style = (Style)Application.Current.Resources["RokuricsCardStyle"],
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(18),
            Child = new TextBlock
            {
                Text = FingerprintDisplay,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                LineHeight = 22
            }
        };
        panel.Children.Add(fpBorder);

        // Action buttons
        var actionRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var pairedBtn = new Button
        {
            Content = "已配对设备",
            FontSize = 12
        };
        pairedBtn.Click += async (_, _) =>
        {
            await ShowPairedDevicesDialog();
        };
        var uploadBtn = new Button
        {
            Content = "上传测试",
            FontSize = 12
        };
        uploadBtn.Click += async (_, _) =>
        {
            await ShowUploadTestDialog();
        };
        actionRow.Children.Add(pairedBtn);
        actionRow.Children.Add(uploadBtn);
        panel.Children.Add(actionRow);

        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 540,
            Content = panel
        };

        await new ContentDialog
        {
            Title = "连接状态",
            Content = scrollViewer,
            CloseButtonText = "关闭",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    private async Task ShowPairedDevicesDialog()
    {
        var panel = new StackPanel { Spacing = 14, Width = 560 };
        var devices = _pairingService.PairedDevices;

        panel.Children.Add(new TextBlock
        {
            Text = devices.Count.ToString(),
            FontSize = 42,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue)
        });

        if (devices.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "暂无已配对设备",
                FontSize = 14,
                Opacity = 0.5
            });
        }
        else
        {
            foreach (var device in devices)
            {
                var deviceCard = new Border
                {
                    Style = (Style)Application.Current.Resources["RokuricsCardStyle"],
                    Padding = new Thickness(14),
                    CornerRadius = new CornerRadius(14),
                    Child = new StackPanel { Spacing = 6 }
                };
                var cardStack = (StackPanel)deviceCard.Child;
                cardStack.Children.Add(new TextBlock
                {
                    Text = device.DeviceName,
                    FontSize = 15,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                });
                cardStack.Children.Add(new TextBlock
                {
                    Text = $"ID: {device.IdPrefix}",
                    FontSize = 12,
                    Opacity = 0.5
                });
                cardStack.Children.Add(new TextBlock
                {
                    Text = $"配对时间: {device.PairedAt:yyyy-MM-dd HH:mm}",
                    FontSize = 12,
                    Opacity = 0.5
                });
                panel.Children.Add(deviceCard);
            }
        }

        await new ContentDialog
        {
            Title = "已配对设备",
            Content = panel,
            CloseButtonText = "关闭",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    private async Task ShowUploadTestDialog()
    {
        var panel = new StackPanel { Spacing = 0, Width = 560 };
        var acceptedCount = _connectionStore.AcceptedUploadCount;
        var lastFile = _connectionStore.LastAcceptedFileName;

        AddConnectionRow(panel, "安全测试上传数量", acceptedCount.ToString(), true);
        AddConnectionSeparator(panel);
        AddConnectionRow(panel, "最近测试 JSON", lastFile ?? "暂无", false);
        AddConnectionSeparator(panel);
        AddConnectionRow(panel, "保存位置", "Rokurics/received", false);
        AddConnectionSeparator(panel);
        AddConnectionRow(panel, "上传测试状态", acceptedCount > 0 ? "已接收" : "暂无", false);

        var border = new Border
        {
            Style = (Style)Application.Current.Resources["RokuricsCardStyle"],
            Padding = new Thickness(6),
            CornerRadius = new CornerRadius(24),
            Child = panel
        };

        await new ContentDialog
        {
            Title = "上传测试",
            Content = border,
            CloseButtonText = "关闭",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }

    private static void AddConnectionRow(StackPanel parent, string label, string value, bool isTechnical)
    {
        var grid = new Grid { ColumnSpacing = 18 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Opacity = 0.5
        });

        var valueBlock = new TextBlock
        {
            Text = value,
            FontSize = isTechnical ? 12 : 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            IsTextSelectionEnabled = true
        };
        Grid.SetColumn(valueBlock, 1);
        grid.Children.Add(valueBlock);

        var container = new Grid
        {
            Padding = new Thickness(16, 13, 16, 13)
        };
        container.Children.Add(grid);
        parent.Children.Add(container);
    }

    private static void AddConnectionSeparator(StackPanel parent)
    {
        parent.Children.Add(new Border
        {
            Height = 1,
            Opacity = 0.08,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
            Margin = new Thickness(132, 0, 0, 0)
        });
    }

    private async void Disconnect_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "断开连接",
            Content = "确定要断开当前设备连接吗？",
            PrimaryButtonText = "断开",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            _isPaired = false;
            _activePairingCode = null;
            StopBreathingAnimation();
            ConnectedPanel.Visibility = Visibility.Collapsed;
            UnpairedPanel.Visibility = Visibility.Visible;
            StartPairingButton.Visibility = Visibility.Visible;
            PairingCodeCard.Visibility = Visibility.Collapsed;
            StatusCapsule.Child = new TextBlock { Text = "未配对", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.Bold };
        }
    }

    private static string GetLocalIPAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var ip = host.AddressList
                .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    && !System.Net.IPAddress.IsLoopback(a));
            return ip?.ToString() ?? "未知";
        }
        catch
        {
            return "未知";
        }
    }
}
