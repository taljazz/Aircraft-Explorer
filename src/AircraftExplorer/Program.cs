using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AircraftExplorer;
using AircraftExplorer.Aircraft;
using AircraftExplorer.Audio;
using AircraftExplorer.Config;
using AircraftExplorer.Education;
using AircraftExplorer.Input;
using AircraftExplorer.Modes;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile(Path.Combine("Data", "Config", "appsettings.json"), optional: true, reloadOnChange: false)
    .Build();

var appSettings = new AppSettings();
configuration.Bind(appSettings);

var services = new ServiceCollection();

// Configuration
services.AddSingleton(appSettings);

// Audio
services.AddSingleton<ISpeechService, TolkSpeechService>();
services.AddSingleton<ISpatialAudioService, SpatialAudioService>();
services.AddSingleton<SpeechQueue>(sp =>
    new SpeechQueue(sp.GetRequiredService<ISpeechService>(), appSettings.Speech.DebounceMilliseconds));

// Input — register KeyboardInputProvider both as concrete and as IInputProvider
services.AddSingleton<KeyboardInputProvider>();
services.AddSingleton<IInputProvider>(sp => sp.GetRequiredService<KeyboardInputProvider>());
services.AddSingleton<IInputProvider, FlightHardwareInputProvider>();
services.AddSingleton<InputManager>();

// Data
services.AddSingleton<AircraftRegistry>(sp =>
{
    var dataPath = Path.Combine(AppContext.BaseDirectory, appSettings.AircraftDataPath);
    return new AircraftRegistry(dataPath);
});

// Education
services.AddSingleton<IEducationProvider>(sp =>
{
    var dataPath = Path.Combine(AppContext.BaseDirectory, appSettings.EducationDataPath);
    return new EducationContentLoader(dataPath);
});

// Mode system
services.AddSingleton<AppModeManager>();
services.AddSingleton<ModeContext>(sp => new ModeContext
{
    Speech = sp.GetRequiredService<ISpeechService>(),
    SpatialAudio = sp.GetRequiredService<ISpatialAudioService>(),
    AircraftRegistry = sp.GetRequiredService<AircraftRegistry>(),
    EducationProvider = sp.GetRequiredService<IEducationProvider>(),
    Settings = sp.GetRequiredService<AppSettings>(),
    InputManager = sp.GetRequiredService<InputManager>()
});

// App host
services.AddSingleton<AppHost>();

var provider = services.BuildServiceProvider();

var host = provider.GetRequiredService<AppHost>();
host.Run();

// Cleanup disposable services
if (provider is IDisposable disposable)
    disposable.Dispose();
