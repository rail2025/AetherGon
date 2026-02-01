using System.Numerics;

namespace AetherGon.Core.Entities;

public class Player
{
    public float Angle { get; set; }
    public float Radius { get; set; } = 55f;
    public float Speed { get; set; } = 4.5f;
}

public class Wall
{
    public float Angle { get; set; }
    public float Width { get; set; }
    public float Distance { get; set; }
    public int SideCount { get; set; } = 6;
}

public enum GameStatus
{
    Menu,
    Playing,
    GameOver,
    Paused
}
