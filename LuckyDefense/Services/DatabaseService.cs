using SQLite;
using LuckyDefense.Models;

namespace LuckyDefense.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _db = null!;

    private async Task Init()
    {
        if (_db != null)
            return;

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "luckydefense.db");
        _db = new SQLiteAsyncConnection(dbPath);

        await _db.CreateTableAsync<UnitType>();
        await _db.CreateTableAsync<GameSave>();
        await _db.CreateTableAsync<Player>();
        await _db.CreateTableAsync<PlayerUnit>();

        await SeedUnitTypes();
    }

    private async Task SeedUnitTypes()
    {
        int count = await _db.Table<UnitType>().CountAsync();
        if (count > 0)
            return;

        var unitTypes = new List<UnitType>
        {
            // Common units (Rarity 1)
            new() { UnitTypeId = 1, Name = "Archer",   BasePower = 10, Rarity = 1, AttackRange = 3f, AttackSpeed = 1.0f },
            new() { UnitTypeId = 2, Name = "Guard",    BasePower = 12, Rarity = 1, AttackRange = 2f, AttackSpeed = 0.8f },
            new() { UnitTypeId = 3, Name = "Scout",    BasePower = 8,  Rarity = 1, AttackRange = 4f, AttackSpeed = 1.2f },

            // Rare units (Rarity 2)
            new() { UnitTypeId = 4, Name = "Knight",   BasePower = 25, Rarity = 2, AttackRange = 2f, AttackSpeed = 0.7f },
            new() { UnitTypeId = 5, Name = "Mage",     BasePower = 30, Rarity = 2, AttackRange = 4f, AttackSpeed = 1.5f },

            // Epic units (Rarity 3)
            new() { UnitTypeId = 6, Name = "Paladin",  BasePower = 50, Rarity = 3, AttackRange = 3f, AttackSpeed = 0.6f },
            new() { UnitTypeId = 7, Name = "Wizard",   BasePower = 55, Rarity = 3, AttackRange = 5f, AttackSpeed = 1.8f },

            // Legendary units (Rarity 4)
            new() { UnitTypeId = 8, Name = "Dragon",   BasePower = 100, Rarity = 4, AttackRange = 5f, AttackSpeed = 2.0f },
        };

        await _db.InsertAllAsync(unitTypes);
    }

    // UnitType queries
    public async Task<List<UnitType>> GetAllUnitTypes()
    {
        await Init();
        return await _db.Table<UnitType>().ToListAsync();
    }

    public async Task<List<UnitType>> GetUnitTypesByRarity(int rarity)
    {
        await Init();
        return await _db.Table<UnitType>().Where(u => u.Rarity == rarity).ToListAsync();
    }

    public async Task<UnitType?> GetUnitType(int id)
    {
        await Init();
        return await _db.Table<UnitType>().Where(u => u.UnitTypeId == id).FirstOrDefaultAsync();
    }

    // GameSave queries
    public async Task<int> SaveGame(GameSave save)
    {
        await Init();
        if (save.SaveId == 0)
        {
            await _db.InsertAsync(save);
            return save.SaveId;
        }
        await _db.UpdateAsync(save);
        return save.SaveId;
    }

    public async Task<GameSave?> GetLatestSave()
    {
        await Init();
        return await _db.Table<GameSave>().OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync();
    }

    // Player queries
    public async Task<int> SavePlayer(Player player)
    {
        await Init();
        if (player.PlayerId == 0)
        {
            await _db.InsertAsync(player);
            return player.PlayerId;
        }
        await _db.UpdateAsync(player);
        return player.PlayerId;
    }

    public async Task<Player?> GetPlayerBySave(int saveId)
    {
        await Init();
        return await _db.Table<Player>().Where(p => p.SaveId == saveId).FirstOrDefaultAsync();
    }

    // PlayerUnit queries
    public async Task SavePlayerUnit(PlayerUnit unit)
    {
        await Init();
        if (unit.PlayerUnitId == 0)
            await _db.InsertAsync(unit);
        else
            await _db.UpdateAsync(unit);
    }

    public async Task<List<PlayerUnit>> GetPlayerUnits(int playerId)
    {
        await Init();
        return await _db.Table<PlayerUnit>().Where(u => u.PlayerId == playerId).ToListAsync();
    }

    public async Task DeletePlayerUnit(int playerUnitId)
    {
        await Init();
        await _db.DeleteAsync<PlayerUnit>(playerUnitId);
    }
}
