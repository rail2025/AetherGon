using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace AetherGon;

public enum Difficulty
{
    Easy,
    Hard,
    Insanity
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // --- New Architecture Settings ---
    public bool ShowGuideLines { get; set; } = true;
    public float HighScore { get; set; } = 0f;
    public Difficulty SelectedDifficulty { get; set; } = Difficulty.Hard;

    public bool IsSfxMuted { get; set; } = false;
    public bool IsBgmMuted { get; set; } = false;
    public float MusicVolume { get; set; } = 0.5f;
    public List<int> UnlockedBonusTracks { get; set; } = new();

    public bool IsGameWindowLocked { get; set; } = false;

    public bool OpenOnDeath { get; set; } = false;
    public bool OpenInQueue { get; set; } = false;
    public bool OpenInPartyFinder { get; set; } = false;
    public bool OpenDuringCrafting { get; set; } = false;

    [NonSerialized]
    private IDalamudPluginInterface? PluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.PluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.PluginInterface!.SavePluginConfig(this);
    }
}
