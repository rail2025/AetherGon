using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace AetherGon.Windows;

public class WarningWindow : Window, IDisposable
{
    private readonly Plugin _plugin;

    public WarningWindow(Plugin plugin) : base("Photosensitivity Warning###AetherGonWarning")
    {
        _plugin = plugin;

        this.Size = new Vector2(400, 320);
        this.Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar;

        this.PositionCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.PushFont(Dalamud.Interface.UiBuilder.DefaultFont);
        ImGui.TextColored(new Vector4(1.0f, 0.2f, 0.2f, 1.0f), "WARNING: PHOTOSENSITIVITY / EPILEPSY SEIZURES");
        ImGui.PopFont();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped("This game contains rapidly flashing lights, high-contrast color cycling, and strobing effects.");
        ImGui.Spacing();
        ImGui.TextWrapped("A very small percentage of individuals may experience epileptic seizures when exposed to such light patterns.");
        ImGui.Spacing();
        ImGui.TextWrapped("If you have an epileptic condition, consult your physician before playing.");
        ImGui.Spacing();
        ImGui.TextWrapped("DISCONTINUE USE IMMEDIATELY if you experience dizziness, altered vision, eye or muscle twitches, loss of awareness, or disorientation.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var availableWidth = ImGui.GetContentRegionAvail().X;
        if (ImGui.Button("I UNDERSTAND & WISH TO PROCEED", new Vector2(availableWidth, 40)))
        {
            _plugin.Configuration.HasSeenFlashWarning = true;
            _plugin.Configuration.Save();

            this.IsOpen = false;
            _plugin.TitleWindow.IsOpen = true;
        }
    }
}
