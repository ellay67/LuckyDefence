namespace LuckyDefense.Models;

public class Unit
{
    public int Id { get; set; }
    public int UnitTypeId { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; } = 1;
    public int BasePower { get; set; }
    public float AttackRange { get; set; }
    public float AttackSpeed { get; set; }
    public int Rarity { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }

    // Combat state
    public float AttackCooldown { get; set; }
    public Enemy? Target { get; set; }

    // Animation state
    public float ShootAnimTimer { get; set; }  // > 0 means currently in shoot animation
    public float ShootAngle { get; set; }       // angle toward target when shooting

    public int Power => BasePower * Level;
}
