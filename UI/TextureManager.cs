using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AetherGon.UI;

public class TextureManager : IDisposable
{
    private readonly ITextureProvider _textureProvider;
    private readonly IPluginLog _log;

    private readonly List<IDalamudTextureWrap> _backgroundTextures = new();
    private readonly Dictionary<int, IDalamudTextureWrap> _bubbleTextures = new();

    public TextureManager(ITextureProvider textureProvider, IPluginLog log)
    {
        _textureProvider = textureProvider;
        _log = log;
        LoadTextures();
    }

    private void LoadTextures()
    {
        // Placeholder for loading logic
        // Use _log.Info("Loading textures..."); instead of Plugin.Log.Info
    }

    public IDalamudTextureWrap? GetBackground(int index)
    {
        if (index < 0 || index >= _backgroundTextures.Count) return null;
        return _backgroundTextures[index];
    }

    public int GetBackgroundCount() => _backgroundTextures.Count;

    public IDalamudTextureWrap? GetBubbleTexture(int type)
    {
        return _bubbleTextures.TryGetValue(type, out var tex) ? tex : null;
    }
    
    public IDalamudTextureWrap? GetIcon(string name)
    {
        // Currently returns null or you can implement loading logic here later
        // Example: return _textureProvider.GetIcon(name);
        return null;
    }
    public void Dispose()
    {
        foreach (var tex in _backgroundTextures) tex.Dispose();
        foreach (var tex in _bubbleTextures.Values) tex.Dispose();
        _backgroundTextures.Clear();
        _bubbleTextures.Clear();
    }
}
