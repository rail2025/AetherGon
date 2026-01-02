using System;
using AetherGon.Audio;
using AetherGon.Core.Events;
using AetherGon.Foundation;

namespace AetherGon.Systems;

public class AudioReactor : IDisposable
{
    private readonly EventBus _eventBus;
    private readonly AudioManager _audioManager;

    public AudioReactor(EventBus eventBus, AudioManager audioManager)
    {
        _eventBus = eventBus;
        _audioManager = audioManager;

        // Subscribe to Game Events
        _eventBus.Subscribe<PlayerCrashedEvent>(OnCrash);
        _eventBus.Subscribe<GameStateChangedEvent>(OnStateChange);
        _eventBus.Subscribe<GameActionCommand>(OnAction);
    }

    private void OnCrash(PlayerCrashedEvent evt)
    {
        _audioManager.PlaySfx("Sfx.bomb.mp3"); // Ensure this file exists or use "bomb.mp3"
    }

    private void OnAction(GameActionCommand cmd)
    {
        if (cmd.ActionName == "Confirm")
            _audioManager.PlaySfx("Sfx.shot.wav"); // Use as a "Select" sound
    }

    private void OnStateChange(GameStateChangedEvent evt)
    {
        if (evt.NewState == Core.Entities.GameStatus.Playing)
        {
            _audioManager.StartBgmPlaylist();
        }
        else if (evt.NewState == Core.Entities.GameStatus.GameOver || evt.NewState == Core.Entities.GameStatus.Paused)
        {
            _audioManager.StopBgm();
        }
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<PlayerCrashedEvent>(OnCrash);
        _eventBus.Unsubscribe<GameStateChangedEvent>(OnStateChange);
        _eventBus.Unsubscribe<GameActionCommand>(OnAction);
    }
}
