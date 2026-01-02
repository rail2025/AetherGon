using System.Numerics;

namespace AetherGon.Core.Entities;

public class Player
{
    public float Angle { get; set; } // In Radians (0 to 2PI)
    public float Radius { get; set; } = 55f; // Fixed distance from center
    public float Speed { get; set; } = 4.5f; // Radians per second
}

public class Wall
{
    public float Angle { get; set; } // Center angle of the wall
    public float Width { get; set; } // Arc length/span in Radians
    public float Distance { get; set; } // Current distance from center
    public int SideCount { get; set; } = 6; // Which shape (Hexagon, etc.)
}

public enum GameStatus
{
    Menu,
    Playing,
    GameOver,
    Paused
}
