using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Reflection;

namespace AetherGon.UI;

public class TextureManager : IDisposable
{
    private readonly ITextureProvider _textureProvider;
    private readonly IPluginLog _log;

    private readonly List<IDalamudTextureWrap> _backgroundTextures = new();
    private readonly Dictionary<int, IDalamudTextureWrap> _bubbleTextures = new();
    private readonly Dictionary<string, IDalamudTextureWrap?> _iconTextures = new();

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
        if (_iconTextures.TryGetValue(name, out var tex)) return tex;
        _iconTextures[name] = null;
        Task.Run(async () =>
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(r => r.EndsWith(name, StringComparison.OrdinalIgnoreCase));

                if (resourceName != null)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var ms = new MemoryStream();
                        await stream.CopyToAsync(ms);
                        var texture = await _textureProvider.CreateFromImageAsync(ms.ToArray());
_iconTextures[name] = texture;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to load icon: {name}");
            }
        });

        return null;
    }
    public void Dispose()
    {
        foreach (var tex in _backgroundTextures) tex.Dispose();
        foreach (var tex in _bubbleTextures.Values) tex.Dispose();
        _backgroundTextures.Clear();
        _bubbleTextures.Clear();
        foreach (var tex in _iconTextures.Values) tex?.Dispose();
        _iconTextures.Clear();
    }
}
