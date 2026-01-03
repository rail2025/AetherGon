using AetherGon.Core.Entities;
using AetherGon.Core.Events;
using AetherGon.Foundation;
using AetherGon.UI;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Lumina.Data.Parsing.Uld.UldRoot;

namespace AetherGon.Systems;

public class RenderService : IDisposable
{
    private readonly EventBus _eventBus;
    private readonly TextureManager _textureManager;
    private readonly Configuration _config;

    // Game State Cache
    private Player _lastPlayerState = new();
    private List<Wall> _lastWalls = new();
    private float _worldRotation = 0f;
    private float _timeAlive = 0f;
    private bool _hasData = false;

    // Visual Constants
    private const float HEXAGON_RADIUS = 45f;
    private const float WALL_DEPTH = 30f;

    // Visual State
    private readonly Random _rng = new();
    private int _themeIndex = 0;
    private float _themeTimer = 0f;
    private float _strobeTimer = 0f;
    private bool _isStrobing = false;

    private GameStatus _currentStatus = GameStatus.Menu;

    // Color Palettes (BG, Main, Player)
    private readonly List<Palette> _palettes = new()
    {
        new(new(0.1f, 0.0f, 0.0f, 1f), new(1.0f, 0.2f, 0.2f, 1f), new(1f, 1f, 0f, 1f)), // Red/Red/Yellow
        new(new(0.0f, 0.1f, 0.1f, 1f), new(0.2f, 0.8f, 1.0f, 1f), new(1f, 0f, 1f, 1f)), // Cyan/Cyan/Magenta
        new(new(0.1f, 0.0f, 0.1f, 1f), new(0.8f, 0.2f, 1.0f, 1f), new(0f, 1f, 0f, 1f)), // Purple/Purple/Green
        new(new(0.1f, 0.1f, 0.0f, 1f), new(1.0f, 0.8f, 0.2f, 1f), new(0f, 1f, 1f, 1f)), // Gold/Gold/Cyan
        new(new(0.0f, 0.0f, 0.0f, 1f), new(1.0f, 1.0f, 1.0f, 1f), new(1f, 0.2f, 0.2f, 1f)) // Black/White/Red
    };

    private record Palette(Vector4 Bg, Vector4 Fg, Vector4 Player);

    public RenderService(EventBus eventBus, TextureManager textureManager, Configuration config)
    {
        _eventBus = eventBus;
        _textureManager = textureManager;
        _config = config;
        _eventBus.Subscribe<WorldUpdatedEvent>(OnWorldUpdate);
    }

    private void OnWorldUpdate(WorldUpdatedEvent evt)
    {
        _lastPlayerState = evt.Player;
        _lastWalls = new List<Wall>(evt.Walls);
        _worldRotation = evt.WorldRotation;
        _timeAlive = evt.TimeAlive;
        _currentStatus = evt.Status;
        _hasData = true;
    }

    public void Draw(Action onReturnToTitle = null)
    {
        if (!_hasData) return;

        var drawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        var center = windowPos + windowSize * 0.5f;
        var scale = ImGuiHelpers.GlobalScale;
        var dt = ImGui.GetIO().DeltaTime;

        // --- UPDATE VISUALS ---
        UpdateVisualEffects(dt);

        // Get current colors based on visual state
        var colors = CalculateCurrentColors();

        // Background (Fill Window)
        drawList.AddRectFilled(windowPos, windowPos + windowSize, colors.Bg);

        string controlsText = "Controls: A/D or \u2190 / \u2192 , Press SPACE or Enter to start"; // Left/Right Arrows
        var textSize = ImGui.CalcTextSize(controlsText);
        ImGui.SetCursorPos(new Vector2((windowSize.X - textSize.X) * 0.5f, windowSize.Y - textSize.Y - (20f * scale)));
        
        ImGui.TextColored(new Vector4(1, 1, 1, 0.5f), controlsText);

        if (_currentStatus == GameStatus.Menu || _currentStatus == GameStatus.GameOver)
        {
            DrawDifficultySelect(drawList, windowSize, scale, onReturnToTitle);
        }

        // UI: Timer & High Score (Top Right)
        string timeStr = $"{_timeAlive:0.00}";
        float currentBest = _config.HighScores.TryGetValue(_config.SelectedDifficulty, out var s) ? s : 0f;

        // Match the high score label to the button text
        string diffLabel = _config.SelectedDifficulty switch
        {
            Difficulty.Easy => "EASY",
            Difficulty.Hard => "WARRIOR OF LIGHT",
            Difficulty.Insanity => "AETHERGON",
            _ => "UNKNOWN"
        };
        string bestStr = $"BEST ({diffLabel}): {currentBest:0.00}";

        // Calculate Text Sizes
        var timeSize = ImGui.CalcTextSize(timeStr);
        var bestSize = ImGui.CalcTextSize(bestStr);
        float padding = 20f * scale;

        // Draw Timer (Large)
        ImGui.SetWindowFontScale(1.5f); // Make timer bigger
        ImGui.SetCursorPos(new Vector2(windowSize.X - timeSize.X * 1.5f - padding, padding));
        ImGui.TextColored(colors.Player, timeStr);
        ImGui.SetWindowFontScale(1.0f); // Reset scale

        // Draw Best (Small, under timer)
        ImGui.SetCursorPos(new Vector2(windowSize.X - bestSize.X - padding, padding + (30f * scale)));
        ImGui.TextColored(new Vector4(1, 1, 1, 0.7f), bestStr);

        // Guides
        DrawGuidelines(drawList, center, scale, colors.Fg);

        // Center Hexagon
        float visualRotation = _worldRotation + (MathF.PI / 6f);
        DrawPoly(drawList, center, HEXAGON_RADIUS * scale, 6, colors.Bg, visualRotation); // Center matches BG (void)
        DrawPolyStroke(drawList, center, HEXAGON_RADIUS * scale, 6, colors.Fg, visualRotation, 3f);

        // Walls
        foreach (var wall in _lastWalls)
        {
            DrawWall(drawList, center, wall, scale, colors.Fg);
        }

        // Player
        DrawPlayer(drawList, center, scale, colors.Player);

        DrawGuidelines(drawList, center, scale, colors.Fg);

        
    }

    private void DrawDifficultySelect(ImDrawListPtr drawList, Vector2 windowSize, float scale, Action onReturnToTitle)
    {
        Vector2 startPos = new Vector2(20 * scale, windowSize.Y - (100 * scale));

        if (onReturnToTitle != null)
        {
            ImGui.SetCursorPos(new Vector2(startPos.X, startPos.Y - (35 * scale)));
            if (ImGui.Button("Return to Title"))
            {
                onReturnToTitle.Invoke();
            }
        }

        DrawOutlinedText(drawList, ImGui.GetWindowPos() + startPos, "DIFFICULTY:", scale);
        ImGui.SetCursorPos(new Vector2(startPos.X, startPos.Y + (25 * scale)));

        void DrawDiffButton(string label, Difficulty diff)
        {
            bool isSelected = _config.SelectedDifficulty == diff;
            var color = isSelected ? new Vector4(0.2f, 0.8f, 0.2f, 1f) : new Vector4(0.5f, 0.5f, 0.5f, 0.5f);

            ImGui.PushStyleColor(ImGuiCol.Button, color);
            if (ImGui.Button(label, new Vector2(120 * scale, 22 * scale)))
            {
                _config.SelectedDifficulty = diff;
                _config.Save();
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();
        }

        DrawDiffButton("EASY", Difficulty.Easy);
        DrawDiffButton("WARRIOR OF LIGHT", Difficulty.Hard);
        DrawDiffButton("AETHERGON", Difficulty.Insanity);
    }

    private void DrawOutlinedText(ImDrawListPtr drawList, Vector2 pos, string text, float scale)
    {
        uint black = 0xFF000000;
        uint white = 0xFFFFFFFF;
        float offset = 1f * scale; // 1px offset scaled

        // Draw 4 offsets in Black
        drawList.AddText(pos + new Vector2(-offset, -offset), black, text); // NW
        drawList.AddText(pos + new Vector2(offset, -offset), black, text); // NE
        drawList.AddText(pos + new Vector2(-offset, offset), black, text); // SW
        drawList.AddText(pos + new Vector2(offset, offset), black, text); // SE

        // Draw Center in White
        drawList.AddText(pos, white, text);
    }
    private void UpdateVisualEffects(float dt)
    {
        _themeTimer += dt;
        _strobeTimer += dt;

        // Swap Theme every 5 seconds
        if (_themeTimer > 5.0f)
        {
            _themeTimer = 0f;
            _themeIndex = (_themeIndex + 1) % _palettes.Count;

            // Chance to trigger hyper strobe mode
            if (_rng.NextDouble() > 0.7) _isStrobing = true;
        }

        // Stop strobing after 1.5 seconds
        if (_isStrobing && _themeTimer > 1.5f)
        {
            _isStrobing = false;
        }
    }

    private (uint Bg, uint Fg, uint Player) CalculateCurrentColors()
    {
        var p = _palettes[_themeIndex];
        Vector4 finalBg = p.Bg;
        Vector4 finalFg = p.Fg;

        // Pulse Effect (BPM Sim) - Oscillate brightness slightly
        float pulse = (MathF.Sin(_timeAlive * 8f) + 1f) * 0.5f; // 0 to 1
        // Modulate BG intensity
        finalBg += new Vector4(0.05f, 0.05f, 0.05f, 0) * pulse;

        // Strobe Effect (Invert colors rapidly)
        if (_isStrobing)
        {
            // Rapid flash (every 0.1s)
            if ((_strobeTimer % 0.25f) > 0.175f)
            {
                // Invert Logic
                var temp = finalBg;
                finalBg = finalFg;
                finalFg = temp;
            }
        }

        return (
            ImGui.ColorConvertFloat4ToU32(finalBg),
            ImGui.ColorConvertFloat4ToU32(finalFg),
            ImGui.ColorConvertFloat4ToU32(p.Player)
        );
    }

    private void DrawWall(ImDrawListPtr drawList, Vector2 center, Wall wall, float scale, uint color)
    {
        float rawInner = wall.Distance;
        float rawOuter = wall.Distance + WALL_DEPTH;

        if (rawOuter <= HEXAGON_RADIUS) return;
        if (rawInner < HEXAGON_RADIUS) rawInner = HEXAGON_RADIUS;

        float effectiveAngle = wall.Angle + _worldRotation;
        float halfWidth = wall.Width / 2f;
        float drawWidth = halfWidth * 0.95f;

        float startAngle = effectiveAngle - drawWidth;
        float endAngle = effectiveAngle + drawWidth;

        float innerRadius = rawInner * scale;
        float outerRadius = rawOuter * scale;

        if (innerRadius > 2000) return;

        var p1 = PolarToCartesian(center, innerRadius, startAngle);
        var p2 = PolarToCartesian(center, innerRadius, endAngle);
        var p3 = PolarToCartesian(center, outerRadius, endAngle);
        var p4 = PolarToCartesian(center, outerRadius, startAngle);

        drawList.AddQuadFilled(p1, p2, p3, p4, color);
    }

    private void DrawGuidelines(ImDrawListPtr drawList, Vector2 center, float scale, uint color)
    {
        // Fade out guides (use alpha from color but reduced)
        uint guideColor = (color & 0x00FFFFFF) | 0x44000000;

        for (int i = 0; i < 6; i++)
        {
            float angle = _worldRotation + (i * MathF.PI / 3f) + (MathF.PI / 6f);
            var start = PolarToCartesian(center, HEXAGON_RADIUS * scale, angle);
            var end = PolarToCartesian(center, 900f * scale, angle);
            drawList.AddLine(start, end, guideColor, 1.0f);
        }
    }

    private void DrawPlayer(ImDrawListPtr drawList, Vector2 center, float scale, uint color)
    {
        float orbitRadius = (HEXAGON_RADIUS + 15f) * scale;
        float angle = _lastPlayerState.Angle + _worldRotation;

        var tip = PolarToCartesian(center, orbitRadius, angle);
        var leftBase = PolarToCartesian(center, orbitRadius - (8f * scale), angle - 0.2f);
        var rightBase = PolarToCartesian(center, orbitRadius - (8f * scale), angle + 0.2f);

        drawList.AddTriangleFilled(tip, leftBase, rightBase, color);
    }

    private void DrawPoly(ImDrawListPtr drawList, Vector2 center, float radius, int sides, uint color, float rotation)
    {
        drawList.PathClear();
        for (int i = 0; i < sides; i++)
        {
            float angle = rotation + (i * MathF.Tau / sides);
            drawList.PathLineTo(PolarToCartesian(center, radius, angle));
        }
        drawList.PathFillConvex(color);
    }

    private void DrawPolyStroke(ImDrawListPtr drawList, Vector2 center, float radius, int sides, uint color, float rotation, float thickness)
    {
        drawList.PathClear();
        for (int i = 0; i <= sides; i++)
        {
            float angle = rotation + (i * MathF.Tau / sides);
            drawList.PathLineTo(PolarToCartesian(center, radius, angle));
        }
        drawList.PathStroke(color, ImDrawFlags.None, thickness);
    }

    private Vector2 PolarToCartesian(Vector2 center, float radius, float angle)
    {
        return new Vector2(
            center.X + radius * MathF.Cos(angle),
            center.Y + radius * MathF.Sin(angle)
        );
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<WorldUpdatedEvent>(OnWorldUpdate);
    }
}
