using SQLite;

namespace LuckyDefense.Models;

public class GameSave
{
    [PrimaryKey, AutoIncrement]
    public int SaveId { get; set; }

    public DateTime CreatedAt { get; set; }

    public int RoundNumber { get; set; }
}
