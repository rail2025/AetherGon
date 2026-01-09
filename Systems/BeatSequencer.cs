using AetherGon.Core.Events;
using AetherGon.Foundation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherGon.Systems;

public class BeatSequencer
{
    private class BeatData
    {
        [JsonPropertyName("Start Time")]
        public float StartTime { get; set; }

        [JsonPropertyName("Label")]
        public string Label { get; set; }
    }

    private readonly EventBus _eventBus;
    private List<BeatData> _beats = new();
    private int _nextBeatIndex = 0;

    public BeatSequencer(EventBus eventBus)
    {
        _eventBus = eventBus;
        LoadBeats();
    }

    private void LoadBeats()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            // Note: Resource name is typically "Namespace.Filename"
            // If the file is in a folder, it is "Namespace.Folder.Filename"
            string resourceName = "AetherGon.bgmaudacitylabelbeats.txt";

            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();

                _beats = JsonSerializer.Deserialize<List<BeatData>>(json)
                    ?.OrderBy(b => b.StartTime)
                    .ToList() ?? new();

                Plugin.Log.Info($"[BeatSequencer] SUCCESS: Loaded {_beats.Count} embedded beats.");
            }
            else
            {
                // Debug helper: List all resources if name is wrong
                var resources = string.Join(", ", assembly.GetManifestResourceNames());
                Plugin.Log.Error($"[BeatSequencer] Resource '{resourceName}' not found. Available: {resources}");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[BeatSequencer] CRITICAL ERROR loading embedded beats.");
        }
    }

    public void Reset()
    {
        _nextBeatIndex = 0;
    }

    public void Update(float timeAlive)
    {
        // Trigger all beats that happened since the last check
        while (_nextBeatIndex < _beats.Count && _beats[_nextBeatIndex].StartTime <= timeAlive)
        {
            _eventBus.Publish(new BeatPulseEvent());
            _nextBeatIndex++;
        }
    }
}
