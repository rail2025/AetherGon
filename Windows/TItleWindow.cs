using System;
using System.Numerics;
using AetherGon.UI;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace AetherGon.Windows;

public class TitleWindow : Window, IDisposable
{
    private readonly Plugin _plugin;
    private readonly TextureManager _textureManager;

    public TitleWindow(Plugin plugin) : base("AetherGon Title###AetherGonTitleWindow")
    {
        _plugin = plugin;
        _textureManager = plugin.Services.Get<TextureManager>();

        // Remove standard window frame for a splash screen look
        this.Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar;

        // Set fixed initial size
        this.Size = new Vector2(500, 400);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void OnOpen()
    {
        // Start playing music when title opens
        _plugin.AudioManager.StartBgmPlaylist();
    }

    public void Dispose() { }

    public override void Draw()
    {
        // --- Background Image (Placeholder) ---
        // Uncomment when you have an icon/bg loaded in TextureManager
        /*
        var bgTexture = _textureManager.GetIcon("icon.png");
        if (bgTexture != null)
        {
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();
            ImGui.GetWindowDrawList().AddImage(bgTexture.Handle, windowPos, windowPos + windowSize);
        }
        */

        var windowWidth = ImGui.GetWindowWidth();
        var windowHeight = ImGui.GetWindowHeight();

        // --- Title Text ---
        // Center the title
        string titleText = "AETHERGON";
        ImGui.SetWindowFontScale(2.0f);
        var titleSize = ImGui.CalcTextSize(titleText);
        Vector2 titlePos = new Vector2((windowWidth - titleSize.X) * 0.5f, windowHeight * 0.2f);
        DrawTextWithOutline(titleText, ImGui.GetWindowPos() + titlePos, 0xFFFFFFFF, 0xFF000000);
        ImGui.SetWindowFontScale(1.0f);

        // --- Buttons ---
        float buttonWidth = 200f;
        float buttonHeight = 40f;
        float startY = windowHeight * 0.5f;
        Vector2 buttonSize = new Vector2(buttonWidth, buttonHeight);

        // Center Buttons
        ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) * 0.5f, startY));

        // Start Game
        if (DrawButtonWithOutline("StartGame", "ENTER THE HEXAGON", buttonSize))
        {
            this.IsOpen = false;
            _plugin.ToggleMainUI(); // Opens the Game Window
        }

        ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) * 0.5f, startY + buttonHeight + 10));

        // Settings
        if (DrawButtonWithOutline("Settings", "SETTINGS", buttonSize))
        {
            _plugin.ToggleConfigUI();
        }

        ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) * 0.5f, startY + (buttonHeight + 10) * 2));
        if (DrawButtonWithOutline("About", "ABOUT", buttonSize))
        {
            _plugin.ToggleAboutUI();
        }

        // --- Audio Controls (Bottom) ---
        float bottomY = windowHeight - 50f;
        ImGui.SetCursorPos(new Vector2(20, bottomY));

        bool bgmMuted = _plugin.Configuration.IsBgmMuted;
        if (DrawCheckboxWithOutline("MuteBGM", "Mute Music", ref bgmMuted))
        {
            _plugin.Configuration.IsBgmMuted = bgmMuted;
            _plugin.Configuration.Save();
            _plugin.AudioManager.UpdateBgmState();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(150); // Offset second box

        bool sfxMuted = _plugin.Configuration.IsSfxMuted;
        if (DrawCheckboxWithOutline("MuteSFX", "Mute SFX", ref sfxMuted))
        {
            _plugin.Configuration.IsSfxMuted = sfxMuted;
            _plugin.Configuration.Save();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(280);
        ImGui.SetNextItemWidth(80);
        var musicVolume = _plugin.Configuration.MusicVolume;
        if (ImGui.SliderFloat("##TitleVol", ref musicVolume, 0.0f, 1.0f, ""))
        {
            _plugin.Configuration.MusicVolume = musicVolume;
            _plugin.AudioManager.SetMusicVolume(musicVolume);
            _plugin.Configuration.Save();
        }
    }

    // --- Helpers ---

    private bool DrawButtonWithOutline(string id, string text, Vector2 size)
    {
        // Invisible button for interaction
        bool clicked = ImGui.Button($"##{id}", size);
        if (clicked) _plugin.AudioManager.PlaySfx("advance.wav"); // Used existing SFX

        // Manual Drawing
        var drawList = ImGui.GetWindowDrawList();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();

        // Button Background (Semi-transparent black)
        drawList.AddRectFilled(min, max, 0x88000000, 5f);

        // Hover Effect
        if (ImGui.IsItemHovered())
            drawList.AddRect(min, max, 0xFFFFFFFF, 5f);
        else
            drawList.AddRect(min, max, 0xFF888888, 5f);

        // Centered Text
        var textSize = ImGui.CalcTextSize(text);
        var textPos = min + (size - textSize) * 0.5f;
        DrawTextWithOutline(text, textPos, 0xFFFFFFFF, 0xFF000000);

        return clicked;
    }

    private bool DrawCheckboxWithOutline(string id, string text, ref bool isChecked)
    {
        bool clicked = ImGui.Checkbox($"##{id}", ref isChecked);
        if (clicked) _plugin.AudioManager.PlaySfx("advance.wav");

        ImGui.SameLine();

        // Draw Label with outline manually to match style
        var pos = ImGui.GetCursorScreenPos();
        // Adjust Y to center with checkbox
        pos.Y -= 3f;
        DrawTextWithOutline(text, pos, 0xFFFFFFFF, 0xFF000000);

        // Dummy to advance cursor past the manually drawn text
        ImGui.Dummy(ImGui.CalcTextSize(text));

        return clicked;
    }

    private void DrawTextWithOutline(string text, Vector2 pos, uint textColor, uint outlineColor)
    {
        var drawList = ImGui.GetWindowDrawList();
        float offset = 1f;

        drawList.AddText(pos + new Vector2(-offset, -offset), outlineColor, text);
        drawList.AddText(pos + new Vector2(offset, -offset), outlineColor, text);
        drawList.AddText(pos + new Vector2(-offset, offset), outlineColor, text);
        drawList.AddText(pos + new Vector2(offset, offset), outlineColor, text);
        drawList.AddText(pos, textColor, text);
    }
}
