using AetherGon.Core.Events;
using AetherGon.Foundation;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using System;
using System.Linq;

namespace AetherGon.Systems;

public class InputPollingService : IDisposable
{
    private readonly EventBus _eventBus;
    private readonly IFramework _framework;
    private readonly IKeyState _keyState;
    private readonly WindowSystem _windowSystem;
    private long _frameCount = 0;
    public InputPollingService(EventBus eventBus, IFramework framework, IKeyState keyState, WindowSystem windowSystem)
    {
        _eventBus = eventBus;
        _framework = framework;
        _keyState = keyState;
        _windowSystem = windowSystem;

        _framework.Update += OnFrameworkUpdate;
        Plugin.Log.Info("[InputSystem] Switched to IKeyState (Global Input).");
    }
    private bool _wasWindowOpen = false;
    private void OnFrameworkUpdate(IFramework framework)
    {
        // Get Window State
        var window = _windowSystem.Windows.FirstOrDefault(x => x.WindowName == "AetherGon");
        bool isWindowOpen = window != null && window.IsOpen;

        // Detect Transition: Open -> Closed
        if (_wasWindowOpen && !isWindowOpen)
        {
            _eventBus.Publish(new GameActionCommand("Pause"));
        }

        // Update State Tracker
        _wasWindowOpen = isWindowOpen;

        // Abort inputs if closed
        if (!isWindowOpen) return;

        _frameCount++;

        // CONFIRM (Start/Restart)
        bool mouseClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.GetIO().WantCaptureMouse;
        if (IsJustPressed(VirtualKey.SPACE) || IsJustPressed(VirtualKey.RETURN) || ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            _eventBus.Publish(new GameActionCommand("Confirm"));
        }

        // PAUSE
        if (IsJustPressed(VirtualKey.ESCAPE))
        {
            _eventBus.Publish(new GameActionCommand("Pause"));
        }

        // MOVEMENT (Continuous)
        if (_keyState[VirtualKey.A] || _keyState[VirtualKey.LEFT])
        {
            _eventBus.Publish(new MovementCommand(MoveDirection.Left, ImGui.GetIO().DeltaTime));
        }

        if (_keyState[VirtualKey.D] || _keyState[VirtualKey.RIGHT])
        {
            _eventBus.Publish(new MovementCommand(MoveDirection.Right, ImGui.GetIO().DeltaTime));
        }
    }

    private bool IsJustPressed(VirtualKey key)
    {
        return _keyState[key];
    }

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
    }
}
