using SQLite;

namespace LuckyDefense.Models;

public class PlayerUnit
{
    [PrimaryKey, AutoIncrement]
    public int PlayerUnitId { get; set; }

    public int PlayerId { get; set; }

    public int UnitTypeId { get; set; }

    public int Level { get; set; } = 1;

    public int PositionX { get; set; }

    public int PositionY { get; set; }
}
