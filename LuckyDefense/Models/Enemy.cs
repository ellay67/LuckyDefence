namespace LuckyDefense.Models;

public class Enemy
{
    public int Id { get; set; }
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public float Speed { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public int PathIndex { get; set; }
    public bool IsAlive => Health > 0;
    public bool IsMiniBoss { get; set; }
    public bool IsBoss { get; set; }
}
