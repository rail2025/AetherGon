using System.Numerics;

namespace AetherGon.Core.Events;

public enum MoveDirection
{
    None,
    Left,
    Right
}

public record MovementCommand(MoveDirection Direction, float DeltaTime);

public record GameActionCommand(string ActionName);
