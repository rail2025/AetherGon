using AetherGon.Core.Events;
using AetherGon.Foundation;
using AetherGon.Systems;
using AetherGon.UI;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys; // NEW
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace AetherGon.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin _plugin;
    private readonly TextureManager _textureManager;
    private readonly RenderService _renderService;
    private readonly EventBus _eventBus;

    public static readonly Vector2 BaseWindowSize = new(540, 720);
    public static Vector2 ScaledWindowSize => BaseWindowSize * ImGuiHelpers.GlobalScale;
    public const float HudAreaHeight = 110f;

    public MainWindow(Plugin plugin) : base("AetherGon")
    {
        _plugin = plugin;
        _textureManager = plugin.Services.Get<TextureManager>();
        _renderService = plugin.Services.Get<RenderService>();
        _eventBus = plugin.Services.Get<EventBus>();

        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    }

    public void Dispose() { }

    public override void Draw()
    {
        _renderService.Draw();

        // Debug Overlay
        ImGui.SetCursorPos(new Vector2(10, 30));

        // Read directly from Dalamud KeyState for debug
        bool spaceDown = Plugin.KeyState[VirtualKey.SPACE];
        bool leftDown = Plugin.KeyState[VirtualKey.A];
        bool rightDown = Plugin.KeyState[VirtualKey.D];

        ImGui.TextColored(new Vector4(1, 1, 0, 1), "GLOBAL INPUT DEBUG:");
        ImGui.Text($"Space: {spaceDown}");
        ImGui.Text($"A / Left: {leftDown}");
        ImGui.Text($"D / Right: {rightDown}");

        if (ImGui.Button("FORCE START CLICK"))
        {
            _eventBus.Publish(new GameActionCommand("Confirm"));
        }
    }
}
