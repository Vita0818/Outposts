using Microsoft.UI.Xaml;
using Rokurics.Models;
using Rokurics.Services;
using Rokurics.Stores;
using Rokurics.ViewModels;
using System.Runtime.InteropServices;

namespace Rokurics;

public partial class App : Application
{
    private readonly ServiceRegistry _services = new();
    private Window? _mainWindow;

    public static new App Current => (App)Application.Current;
    public IServiceProvider Services => _services;

    public App()
    {
        TryEnablePerMonitorDpiAwareness();

        var settings = AppSettings.Load();

        _services.AddSingleton<AudioFileStore>(sp => new AudioFileStore());
        _services.AddSingleton<StudyLibraryStore>(sp => new StudyLibraryStore(sp.GetRequiredService<AudioFileStore>()));
        _services.AddSingleton<DeviceConnectionStatusStore>(sp => new DeviceConnectionStatusStore());
        _services.AddSingleton<StudyLibrarySyncStateStore>(sp => new StudyLibrarySyncStateStore());
        _services.AddSingleton<RecordingManager>(sp =>
        {
            var audioStore = sp.GetRequiredService<AudioFileStore>();
            var studyStore = sp.GetRequiredService<StudyLibraryStore>();
            return new RecordingManager(audioStore, studyStore);
        });
        _services.AddSingleton<ITranscriptionProvider>(sp => new MockTranscriptionProvider());
        _services.AddSingleton<INoteGenerationProvider>(sp => ProviderFactory.CreateNoteProvider(settings));
        _services.AddSingleton<IChatProvider>(sp => ProviderFactory.CreateChatProvider(settings));
        _services.AddSingleton<IRecordingUploadClient>(sp => new MockRecordingUploadClient());
        _services.AddSingleton<IKestrelReceiverService>(sp => new KestrelReceiverService());
        _services.AddSingleton<IPairingService>(sp => new PairingService());
        _services.AddSingleton<IWindowsAudioCapture>(sp => new WindowsAudioCapture());
        _services.AddSingleton<MainViewModel>(sp => new MainViewModel());
        _services.AddTransient<ChatViewModel>(sp => new ChatViewModel());
        _services.AddTransient<SettingsViewModel>(sp => new SettingsViewModel());
        _services.AddSingleton<StudyLibraryViewModel>(sp =>
            new StudyLibraryViewModel(sp.GetRequiredService<StudyLibraryStore>()));

        InitializeComponent();
    }

    private static void TryEnablePerMonitorDpiAwareness()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10))
            return;

        const long DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
        SetThreadDpiAwarenessContext((IntPtr)DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.SysInt)]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
}

internal sealed class ServiceRegistry : IServiceProvider
{
    private readonly Dictionary<Type, Func<ServiceRegistry, object>> _singletons = new();
    private readonly Dictionary<Type, Func<ServiceRegistry, object>> _transients = new();
    private readonly Dictionary<Type, object> _singletonValues = new();

    public void AddSingleton<TService>(Func<ServiceRegistry, TService> factory)
        where TService : class
        => _singletons[typeof(TService)] = sp => factory(sp);

    public void AddTransient<TService>(Func<ServiceRegistry, TService> factory)
        where TService : class
        => _transients[typeof(TService)] = sp => factory(sp);

    public void AddSingleton<TService>() where TService : class, new()
        => AddSingleton(sp => new TService());

    public void AddTransient<TService>() where TService : class, new()
        => AddTransient(sp => new TService());

    public object? GetService(Type serviceType)
    {
        if (_singletonValues.TryGetValue(serviceType, out var value))
            return value;

        if (_singletons.TryGetValue(serviceType, out var singletonFactory))
        {
            var instance = singletonFactory(this);
            _singletonValues[serviceType] = instance;
            return instance;
        }

        if (_transients.TryGetValue(serviceType, out var transientFactory))
        {
            return transientFactory(this);
        }

        return null;
    }
}

public static class ServiceProviderExtensions
{
    public static TService? GetService<TService>(this IServiceProvider provider)
        where TService : class
    {
        var service = provider.GetService(typeof(TService));
        return service as TService;
    }

    public static TService GetRequiredService<TService>(this IServiceProvider provider)
        where TService : class
    {
        return provider.GetService<TService>()
            ?? throw new InvalidOperationException(
                $"Service '{typeof(TService).FullName}' has not been registered.");
    }
}
