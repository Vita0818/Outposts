using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Rokurics.Services;
using Rokurics.Stores;
using Rokurics.ViewModels;

namespace Rokurics;

public partial class App : Application
{
    private Window? _mainWindow;
    public static new App Current => (App)Application.Current;
    public IServiceProvider Services { get; }

    public App()
    {
        var services = new ServiceCollection();

        // Load settings to determine which providers to use
        var settings = AppSettings.Load();

        // Core persistence services
        services.AddSingleton<AudioFileStore>();
        services.AddSingleton<StudyLibraryStore>(sp =>
        {
            var audioStore = sp.GetRequiredService<AudioFileStore>();
            return new StudyLibraryStore(audioStore);
        });

        // State stores
        services.AddSingleton<DeviceConnectionStatusStore>();
        services.AddSingleton<StudyLibrarySyncStateStore>();

        // Recording manager
        services.AddSingleton<RecordingManager>(sp =>
        {
            var audioStore = sp.GetRequiredService<AudioFileStore>();
            var studyStore = sp.GetRequiredService<StudyLibraryStore>();
            return new RecordingManager(audioStore, studyStore);
        });

        // Provider interfaces — use real providers based on saved settings
        services.AddSingleton<ITranscriptionProvider, MockTranscriptionProvider>();
        services.AddSingleton<INoteGenerationProvider>(_ =>
            ProviderFactory.CreateNoteProvider(settings));
        services.AddSingleton<IChatProvider>(_ =>
            ProviderFactory.CreateChatProvider(settings));
        services.AddSingleton<IRecordingUploadClient, MockRecordingUploadClient>();

        // Infrastructure services (Kestrel, pairing, audio capture)
        services.AddSingleton<IKestrelReceiverService, KestrelReceiverService>();
        services.AddSingleton<IPairingService, PairingService>();
        services.AddSingleton<IWindowsAudioCapture, WindowsAudioCapture>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<StudyLibraryViewModel>(sp =>
        {
            var studyStore = sp.GetRequiredService<StudyLibraryStore>();
            return new StudyLibraryViewModel(studyStore);
        });

        Services = services.BuildServiceProvider();
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
}
