using SQLite;

namespace LuckyDefense.Models;

public class Player
{
    [PrimaryKey, AutoIncrement]
    public int PlayerId { get; set; }

    public int SaveId { get; set; }

    public int Money { get; set; }

    public int LuckLevel { get; set; }
}
