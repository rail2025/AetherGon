using System;
using System.Collections.Generic;
using AetherGon.Core.Entities;
using AetherGon.Core.Events;
using AetherGon.Foundation;
using AetherGon.Systems;
using Dalamud.Plugin.Services;

namespace AetherGon.Core;

public class GameEngine : IDisposable
{
    private readonly EventBus _eventBus;
    private readonly IFramework _framework;
    private readonly Configuration _config;

    private GameStatus _status = GameStatus.Menu;
    private Player _player = new() { Speed = 8.0f };
    private List<Wall> _walls = new();

    private float _worldRotation = 0f;
    private float _gameSpeed = 1.0f;
    private float _survivalTime = 0f;

    private float _stageTime = 0f;
    private int _stageCount = 1;

    private float _spawnTimer = 0f;
    private int _patternStep = 0;
    private int _currentPattern = 0;

    // NEW: Track mechanics to insert breathers
    private int _mechanicsCount = 0;

    private readonly Random _random = new();
    private MoveDirection _currentInput = MoveDirection.None;
    private float _startSpawnOffset = 0f;

    public GameEngine(EventBus eventBus, IFramework framework, Configuration config)
    {
        _eventBus = eventBus;
        _framework = framework;
        _config = config;

        _eventBus.Subscribe<MovementCommand>(OnMovement);
        _eventBus.Subscribe<GameActionCommand>(OnAction);
        _framework.Update += OnUpdate;

        _walls.Add(new Wall { Angle = 0, Width = MathF.PI / 3, Distance = 400f });
    }

    private void OnUpdate(IFramework framework)
    {
        var dt = (float)framework.UpdateDelta.TotalSeconds;

        _worldRotation += 0.5f * dt;

        if (_status == GameStatus.Playing)
        {
            _survivalTime += dt;
            _stageTime += dt;

            if (_stageTime > 60f)
            {
                _stageTime = 0f;
                _stageCount++;
                Plugin.Log.Info($"[GameEngine] Stage {_stageCount} Started!");
            }

            // CHANGE: Difficulty Math
            float baseVal = 0.5f;
            float rampVal = 0.7f;

            switch (_config.SelectedDifficulty)
            {
                case Difficulty.Easy:
                    baseVal = 0.4f;
                    rampVal = 0.4f;
                    break;
                case Difficulty.Hard:
                    baseVal = 0.5f;
                    rampVal = 0.7f;
                    break;
                case Difficulty.Insanity:
                    baseVal = 0.8f;
                    rampVal = 1.2f;
                    break;
            }

            float speedStart = baseVal + ((_stageCount - 1) * 0.05f);
            float ramp = (_stageTime / 60f) * rampVal;
            _gameSpeed = speedStart + ramp;

            if (_startSpawnOffset > 0f)
            {
                _startSpawnOffset -= (250f * _gameSpeed) * dt;
                if (_startSpawnOffset < 0f) _startSpawnOffset = 0f;
            }

            UpdatePlayerPhysics(dt);
            UpdateSpawner(dt);
            CheckCollisions();
        }
        else
        {
            _gameSpeed = 0.5f;
            UpdateSpawner(dt);
        }

        UpdateWallPhysics(dt);

        _eventBus.Publish(new WorldUpdatedEvent(_player, _walls, _worldRotation, _survivalTime, _status));
        _currentInput = MoveDirection.None;
    }

    private void UpdateSpawner(float dt)
    {
        _spawnTimer -= dt;
        if (_spawnTimer <= 0)
        {
            SpawnNextPattern();
        }
    }

    private void SpawnNextPattern()
    {
        // Start New Pattern
        if (_patternStep <= 0)
        {
            _currentPattern = _random.Next(0, 6);
            _patternStep = _random.Next(6, 14);
            _mechanicsCount++; // Increment counter
        }

        float speedMult = _status == GameStatus.Playing ? 1.0f : 2.5f;
        float baseDelay = 0.8f / (_gameSpeed * 0.8f);

        float distance = 1800f - _startSpawnOffset;

        // Spawn Logic (Intra-Pattern Timing)
        switch (_currentPattern)
        {
            case 0: // Random Lanes
                SpawnWall(GetRandomLane(), distance);
                _spawnTimer = baseDelay * 0.25f;
                break;

            case 1: // Spiral
                int spiralLane = _patternStep % 6;
                SpawnWall(spiralLane, distance);
                _spawnTimer = baseDelay * 0.15f;
                break;

            case 2: // The "C" (Barrage)
                int gap = _random.Next(0, 6);
                for (int i = 0; i < 6; i++)
                {
                    if (i == gap) continue;
                    SpawnWall(i, distance);
                }
                _spawnTimer = baseDelay * 1.5f;
                _patternStep -= 4;
                break;

            case 3: // Alternating
                int offset = (_patternStep % 2);
                for (int i = 0; i < 6; i += 2)
                {
                    SpawnWall(i + offset, distance);
                }
                _spawnTimer = baseDelay * 0.4f;
                break;

            case 4: // Tunnel
                int safeLane = (_patternStep % 6);
                if (_random.NextDouble() > 0.5) safeLane = 5 - safeLane;

                for (int i = 0; i < 6; i++)
                {
                    if (i == safeLane) continue;
                    SpawnWall(i, distance);
                }
                _spawnTimer = baseDelay * 0.35f;
                break;

            case 5: // Ladder
                int start = (_patternStep * 2) % 6;
                SpawnWall(start, distance);
                SpawnWall((start + 1) % 6, distance);
                _spawnTimer = baseDelay * 0.25f;
                break;
        }

        _patternStep--;

        // Inter-Pattern Logic (The "Gap" Control)
        // If the pattern just finished...
        if (_patternStep <= 0)
        {
            if (_mechanicsCount % 3 == 0)
            {
                // BREATHER: Long pause every 3 mechanics
                _spawnTimer = baseDelay * 4.0f;
            }
            else
            {
                // NO GAP: Seamless transition to next mechanic
                _spawnTimer = baseDelay * 0.5f;
            }
        }
    }

    private int GetRandomLane() => _random.Next(0, 6);

    private void SpawnWall(int lane, float dist)
    {
        float angle = (lane % 6) * (MathF.PI / 3);
        _walls.Add(new Wall
        {
            Angle = angle,
            Width = MathF.PI / 3,
            Distance = dist
        });
    }

    private void UpdateWallPhysics(float dt)
    {
        for (int i = _walls.Count - 1; i >= 0; i--)
        {
            _walls[i].Distance -= 250f * _gameSpeed * dt;
            if (_walls[i].Distance + 30f < 45f) // Matches new Hexagon Radius
            {
                _walls.RemoveAt(i);
            }
        }
    }

    private void UpdatePlayerPhysics(float dt)
    {
        if (_currentInput == MoveDirection.Left) _player.Angle -= _player.Speed * dt;
        if (_currentInput == MoveDirection.Right) _player.Angle += _player.Speed * dt;

        if (_player.Angle < 0) _player.Angle += MathF.Tau;
        if (_player.Angle > MathF.Tau) _player.Angle -= MathF.Tau;
    }

    private bool CheckCollisions()
    {
        foreach (var wall in _walls)
        {
            if (wall.Distance > _player.Radius + 15) continue;
            if (wall.Distance < _player.Radius - 15) continue;

            float angleDiff = MathF.Abs(_player.Angle - wall.Angle);
            if (angleDiff > MathF.PI) angleDiff = MathF.Tau - angleDiff;

            if (angleDiff < (wall.Width * 0.85f) / 2)
            {
                HandleGameOver();
                return true;
            }
        }
        return false;
    }

    private void HandleGameOver()
    {
        Plugin.Log.Info($"[GameEngine] Game Over! Time: {_survivalTime:0.00}s");

        if (_survivalTime > _config.HighScore)
        {
            _config.HighScore = _survivalTime;
            _config.Save();
            Plugin.Log.Info($"[GameEngine] New High Score: {_config.HighScore}");
        }

        SetStatus(GameStatus.GameOver);
        _eventBus.Publish(new PlayerCrashedEvent());
    }

    private void OnMovement(MovementCommand cmd) => _currentInput = cmd.Direction;

    private void OnAction(GameActionCommand cmd)
    {
        if (cmd.ActionName == "Confirm" && _status != GameStatus.Playing)
        {
            StartGame();
        }
        else if (cmd.ActionName == "Pause" && _status == GameStatus.Playing)
        {
            SetStatus(GameStatus.Paused);
        }
    }

    private void StartGame()
    {
        _walls.Clear();
        _player.Angle = 0;
        _survivalTime = 0f;
        _stageTime = 0f;
        _stageCount = 1;
        _spawnTimer = 0f;
        _patternStep = 0;
        _mechanicsCount = 0; // Reset counter

        _startSpawnOffset = 1300f;

        SetStatus(GameStatus.Playing);
    }

    private void SetStatus(GameStatus newStatus)
    {
        _status = newStatus;
        _eventBus.Publish(new GameStateChangedEvent(_status));
    }

    public void Dispose()
    {
        _framework.Update -= OnUpdate;
        _eventBus.Unsubscribe<MovementCommand>(OnMovement);
        _eventBus.Unsubscribe<GameActionCommand>(OnAction);
    }
}
