using System;
using AetherGon.Audio;
using AetherGon.Core;
using AetherGon.Foundation;
using AetherGon.Systems;
using AetherGon.UI;
using AetherGon.Windows;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using AetherGon.Windows;

namespace AetherGon;

public sealed class Plugin : IDalamudPlugin
{
    // Dalamud Services
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;

    private const string CommandName = "/agon";

    public readonly ServiceContainer Services;
    public readonly WindowSystem WindowSystem = new("AetherGon");

    public Configuration Configuration { get; init; }
    public AudioManager AudioManager { get; init; }

    private readonly MainWindow _mainWindow;
    private readonly ConfigWindow _configWindow;
    public readonly TitleWindow TitleWindow;

    public Plugin()
    {
        Services = new ServiceContainer();
        var eventBus = new EventBus();

        Services.Register(eventBus);
        Services.Register(WindowSystem);

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);
        Services.Register(Configuration);

        AudioManager = new AudioManager(Configuration);

        var textureManager = new TextureManager(TextureProvider, Log);
        Services.Register(textureManager);

        var inputService = new InputPollingService(eventBus, Framework, KeyState, WindowSystem);
        Services.Register(inputService);

        var gameEngine = new GameEngine(eventBus, Framework, Configuration);
        Services.Register(gameEngine);

        var renderService = new RenderService(eventBus, textureManager, Configuration);
        Services.Register(renderService);

        var audioReactor = new AudioReactor(eventBus, AudioManager);
        Services.Register(audioReactor);

        _mainWindow = new MainWindow(this);
        _configWindow = new ConfigWindow(this, AudioManager);
        TitleWindow = new TitleWindow(this);

        WindowSystem.AddWindow(_mainWindow);
        WindowSystem.AddWindow(_configWindow);
        WindowSystem.AddWindow(TitleWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the AetherGon game window."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleTitleUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleTitleUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;

        CommandManager.RemoveHandler(CommandName);

        WindowSystem.RemoveAllWindows();

        _mainWindow.Dispose();
        _configWindow.Dispose();
        TitleWindow.Dispose();
        AudioManager.Dispose();
        Services.Dispose();
    }

    private void OnCommand(string command, string args) => ToggleTitleUI();
    private void DrawUI() => WindowSystem.Draw();
    public void ToggleTitleUI() => TitleWindow.Toggle();
    public void ToggleMainUI() => _mainWindow.IsOpen = !_mainWindow.IsOpen;
    public void ToggleConfigUI() => _configWindow.Toggle();
}
