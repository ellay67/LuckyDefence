namespace LuckyDefense.Models;

public class Projectile
{
    public float X { get; set; }
    public float Y { get; set; }
    public float TargetX { get; set; }
    public float TargetY { get; set; }
    public float Speed { get; set; } = 8f;
    public int Damage { get; set; }
    public Enemy? Target { get; set; }
    public bool IsActive { get; set; } = true;
    public int Rarity { get; set; } = 1; // 1=slime, 2=archer, 3=wizard, 4=dragon
}
