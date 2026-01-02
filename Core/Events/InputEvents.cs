using System.Numerics;

namespace AetherGon.Core.Events;

public enum MoveDirection
{
    None,
    Left,
    Right
}

// Published every frame the user holds a movement key
public record MovementCommand(MoveDirection Direction, float DeltaTime);

// Published when specific "trigger" keys are pressed (once per press)
public record GameActionCommand(string ActionName); // e.g., "Start", "Pause", "Restart"
