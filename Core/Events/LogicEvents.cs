using System.Collections.Generic;
using AetherGon.Core.Entities;

namespace AetherGon.Core.Events;

public record GameStateChangedEvent(GameStatus NewState);
public record PlayerCrashedEvent;
// Added TimeAlive parameter
public record WorldUpdatedEvent(Player Player, List<Wall> Walls, float WorldRotation, float TimeAlive, GameStatus Status);
public record BeatPulseEvent;
