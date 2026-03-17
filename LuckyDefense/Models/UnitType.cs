using SQLite;

namespace LuckyDefense.Models;

public class UnitType
{
    [PrimaryKey]
    public int UnitTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int BasePower { get; set; }

    // 1=Common, 2=Rare, 3=Epic, 4=Legendary
    public int Rarity { get; set; }

    public float AttackRange { get; set; }

    public float AttackSpeed { get; set; }
}
