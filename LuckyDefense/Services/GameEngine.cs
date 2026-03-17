using LuckyDefense.Models;

namespace LuckyDefense.Services;

public class GameEngine
{
    // Grid: border cells = enemy path, interior cells = unit placement
    public const int GridColumns = 7;
    public const int GridRows = 5;

    // Game state
    public List<Unit> Units { get; } = new();
    public List<Enemy> Enemies { get; } = new();
    public List<Projectile> Projectiles { get; } = new();
    public List<PointF> PathPoints { get; private set; } = new();

    public int CurrentWave { get; set; } = 0;
    public int Health { get; set; } = 100;
    public int Money { get; set; } = 200;
    public int LuckLevel { get; set; } = 1;
    public bool IsWaveActive { get; set; }
    public float WaveCountdown { get; set; } = 3f;
    public bool WaitingToStart { get; set; } = true;
    public bool EnemyLeakedThisFrame { get; set; }
    public bool EnemyDiedThisFrame { get; set; }
    public bool UnitFiredThisFrame { get; set; }
    public int LastFiredRarity { get; set; }
    public bool BossWaveStarted { get; set; }   // true on the frame a boss wave begins
    public bool IsBossWave => CurrentWave > 0 && CurrentWave % 10 == 0;
    public bool IsMiniBossWave => CurrentWave > 0 && CurrentWave % 5 == 0 && !IsBossWave;
    public int EnemiesSpawned { get; set; }
    public int EnemiesThisWave { get; set; }
    public float SpawnTimer { get; set; }

    private int _nextEnemyId;
    private int _nextUnitId;
    private readonly Random _random = new();

    // Board pixel dimensions (set by drawable)
    public float BoardWidth { get; set; }
    public float BoardHeight { get; set; }
    public float CellWidth => BoardWidth / GridColumns;
    public float CellHeight => BoardHeight / GridRows;

    /// <summary>
    /// Build the path around the border of the grid (clockwise).
    /// Enemies spawn top-left, walk right along top, down right side,
    /// left along bottom. End = bottom-left corner.
    /// </summary>
    public void BuildPath()
    {
        PathPoints = new List<PointF>();

        // Top row: left to right
        for (int c = 0; c <= GridColumns - 1; c++)
            PathPoints.Add(new PointF(c + 0.5f, 0.5f));

        // Right column: top to bottom
        for (int r = 1; r <= GridRows - 1; r++)
            PathPoints.Add(new PointF(GridColumns - 0.5f, r + 0.5f));

        // Bottom row: right to left
        for (int c = GridColumns - 2; c >= 0; c--)
            PathPoints.Add(new PointF(c + 0.5f, GridRows - 0.5f));

        // Left column: bottom to top (but stop before top-left to avoid loop)
        // Actually end at bottom-left, so path is a U around 3 sides
        // Let's do full rectangle: left column going up to row 1
        for (int r = GridRows - 2; r >= 1; r--)
            PathPoints.Add(new PointF(0.5f, r + 0.5f));
    }

    public PointF GridToPixel(float gridX, float gridY)
    {
        return new PointF(gridX * CellWidth, gridY * CellHeight);
    }

    public PointF PathPointToPixel(PointF pathPt)
    {
        return new PointF(pathPt.X * CellWidth, pathPt.Y * CellHeight);
    }

    public bool IsBorderCell(int col, int row)
    {
        return row == 0 || row == GridRows - 1 || col == 0 || col == GridColumns - 1;
    }

    public void StartNextWave()
    {
        CurrentWave++;
        IsWaveActive = true;
        EnemiesSpawned = 0;
        SpawnTimer = 0;

        if (IsBossWave)
        {
            EnemiesThisWave = 6 + CurrentWave * 2;
            BossWaveStarted = true;
        }
        else if (IsMiniBossWave)
        {
            EnemiesThisWave = 5 + CurrentWave * 2;
            BossWaveStarted = true;
        }
        else
        {
            EnemiesThisWave = 5 + CurrentWave * 3;
            BossWaveStarted = false;
        }
    }

    public void Update(float deltaTime)
    {
        if (BoardWidth <= 0 || BoardHeight <= 0) return;

        // Rebuild path each frame in case board size changed
        if (PathPoints.Count == 0)
            BuildPath();

        EnemyLeakedThisFrame = false;
        EnemyDiedThisFrame = false;
        UnitFiredThisFrame = false;
        BossWaveStarted = false;

        if (WaitingToStart)
        {
            WaveCountdown -= deltaTime;
            if (WaveCountdown <= 0)
            {
                WaitingToStart = false;
                StartNextWave();
            }
            return;
        }

        if (IsWaveActive)
            UpdateSpawning(deltaTime);

        UpdateEnemies(deltaTime);
        UpdateUnits(deltaTime);
        UpdateProjectiles(deltaTime);

        if (IsWaveActive && EnemiesSpawned >= EnemiesThisWave && Enemies.Count == 0)
        {
            IsWaveActive = false;
            WaveCountdown = 10f;
            GiveRoundIncome();
        }

        if (!IsWaveActive)
        {
            WaveCountdown -= deltaTime;
            if (WaveCountdown <= 0)
                StartNextWave();
        }
    }

    private void UpdateSpawning(float deltaTime)
    {
        if (EnemiesSpawned >= EnemiesThisWave) return;

        SpawnTimer -= deltaTime;
        if (SpawnTimer <= 0)
        {
            SpawnEnemy();
            SpawnTimer = 2.0f;
        }
    }

    private void SpawnEnemy()
    {
        if (PathPoints.Count == 0) return;

        var start = PathPointToPixel(PathPoints[0]);
        float baseHealth = 40 + CurrentWave * 20;
        float speed = 40f;
        bool isLastEnemy = EnemiesSpawned == EnemiesThisWave - 1;
        bool isMiniBoss = false;
        bool isBoss = false;

        // Last enemy of a boss/mini-boss wave is the boss
        if (isLastEnemy && IsBossWave)
        {
            isBoss = true;
            baseHealth *= 8;  // 8x health
            speed = 25f;      // slower
        }
        else if (isLastEnemy && IsMiniBossWave)
        {
            isMiniBoss = true;
            baseHealth *= 4;  // 4x health
            speed = 30f;      // a bit slower
        }

        var enemy = new Enemy
        {
            Id = _nextEnemyId++,
            Health = baseHealth,
            MaxHealth = baseHealth,
            Speed = speed,
            X = start.X,
            Y = start.Y,
            PathIndex = 0,
            IsMiniBoss = isMiniBoss,
            IsBoss = isBoss
        };

        Enemies.Add(enemy);
        EnemiesSpawned++;
    }

    private void UpdateEnemies(float deltaTime)
    {
        for (int i = Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = Enemies[i];
            if (!enemy.IsAlive)
            {
                Money += 10;
                EnemyDiedThisFrame = true;
                Enemies.RemoveAt(i);
                continue;
            }

            // Move along path waypoints
            if (enemy.PathIndex + 1 < PathPoints.Count)
            {
                var target = PathPointToPixel(PathPoints[enemy.PathIndex + 1]);
                float dx = target.X - enemy.X;
                float dy = target.Y - enemy.Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist < 3f)
                {
                    enemy.PathIndex++;
                }
                else
                {
                    float speed = enemy.Speed * deltaTime;
                    enemy.X += dx / dist * speed;
                    enemy.Y += dy / dist * speed;
                }
            }
            else
            {
                // Reached end of path — damage player
                int damage = enemy.IsBoss ? 30 : enemy.IsMiniBoss ? 20 : 10;
                Health -= damage;
                EnemyLeakedThisFrame = true;
                Enemies.RemoveAt(i);
            }
        }
    }

    private void UpdateUnits(float deltaTime)
    {
        foreach (var unit in Units)
        {
            // Tick down shoot animation
            if (unit.ShootAnimTimer > 0)
                unit.ShootAnimTimer -= deltaTime;

            unit.AttackCooldown -= deltaTime;
            if (unit.AttackCooldown > 0) continue;

            float ux = (unit.GridX + 0.5f) * CellWidth;
            float uy = (unit.GridY + 0.5f) * CellHeight;
            Enemy? nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var enemy in Enemies)
            {
                if (!enemy.IsAlive) continue;
                float dx = enemy.X - ux;
                float dy = enemy.Y - uy;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                float range = unit.AttackRange * CellWidth;

                if (dist <= range && dist < nearestDist)
                {
                    nearest = enemy;
                    nearestDist = dist;
                }
            }

            if (nearest != null)
            {
                Projectiles.Add(new Projectile
                {
                    X = ux,
                    Y = uy,
                    TargetX = nearest.X,
                    TargetY = nearest.Y,
                    Target = nearest,
                    Damage = unit.Power,
                    Speed = 300f,
                    Rarity = unit.Rarity
                });

                unit.AttackCooldown = 1f / unit.AttackSpeed;
                unit.Target = nearest;
                UnitFiredThisFrame = true;
                LastFiredRarity = unit.Rarity;

                // Trigger shoot animation
                unit.ShootAnimTimer = 0.2f;
                float dx2 = nearest.X - ux;
                float dy2 = nearest.Y - uy;
                unit.ShootAngle = MathF.Atan2(dy2, dx2);
            }
        }
    }

    private void UpdateProjectiles(float deltaTime)
    {
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            var proj = Projectiles[i];
            if (!proj.IsActive)
            {
                Projectiles.RemoveAt(i);
                continue;
            }

            if (proj.Target != null && proj.Target.IsAlive)
            {
                proj.TargetX = proj.Target.X;
                proj.TargetY = proj.Target.Y;
            }

            float dx = proj.TargetX - proj.X;
            float dy = proj.TargetY - proj.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist < 5f)
            {
                if (proj.Target != null && proj.Target.IsAlive)
                    proj.Target.Health -= proj.Damage;
                proj.IsActive = false;
                Projectiles.RemoveAt(i);
            }
            else
            {
                float speed = proj.Speed * deltaTime;
                proj.X += dx / dist * speed;
                proj.Y += dy / dist * speed;
            }
        }
    }

    private void GiveRoundIncome()
    {
        Money += 80 + CurrentWave * 10;
    }

    public bool IsCellOccupied(int col, int row)
    {
        return Units.Any(u => u.GridX == col && u.GridY == row);
    }

    public (int col, int row)? GetRandomEmptyCell()
    {
        var emptyCells = new List<(int col, int row)>();
        for (int c = 0; c < GridColumns; c++)
        {
            for (int r = 0; r < GridRows; r++)
            {
                // Only interior cells (not border) are valid for units
                if (!IsBorderCell(c, r) && !IsCellOccupied(c, r))
                    emptyCells.Add((c, r));
            }
        }

        if (emptyCells.Count == 0) return null;
        return emptyCells[_random.Next(emptyCells.Count)];
    }

    public Unit? PlaceUnit(int unitTypeId, string name, int basePower, float attackRange, float attackSpeed, int rarity, int col, int row)
    {
        if (IsBorderCell(col, row) || IsCellOccupied(col, row))
            return null;

        var unit = new Unit
        {
            Id = _nextUnitId++,
            UnitTypeId = unitTypeId,
            Name = name,
            BasePower = basePower,
            AttackRange = attackRange,
            AttackSpeed = attackSpeed,
            Rarity = rarity,
            GridX = col,
            GridY = row
        };

        Units.Add(unit);
        return unit;
    }

    /// <summary>
    /// Check if any 3 units share the same UnitTypeId and Level.
    /// If so, merge them into 1 unit with Level+1, keeping the first unit's position.
    /// Returns true if a merge happened.
    /// </summary>
    public bool TryAutoMerge()
    {
        // Group units by (UnitTypeId, Level)
        var groups = Units.GroupBy(u => (u.UnitTypeId, u.Level));

        foreach (var group in groups)
        {
            var matches = group.ToList();
            if (matches.Count >= 3)
            {
                // Keep the first unit, remove the other two
                var keeper = matches[0];
                var toRemove1 = matches[1];
                var toRemove2 = matches[2];

                // Remove projectiles targeting removed units' targets
                Units.Remove(toRemove1);
                Units.Remove(toRemove2);

                // Level up the keeper
                keeper.Level++;
                keeper.AttackCooldown = 0;

                return true;
            }
        }

        return false;
    }
}
