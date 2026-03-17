using LuckyDefense.Services;

namespace LuckyDefense.Views;

public class GameBoardDrawable : IDrawable
{
    public GameEngine? Engine { get; set; }

    public int OpponentHealth { get; set; } = 100;
    public GameEngine? OpponentEngine { get; set; }
    public bool IsSoloMode { get; set; }


    // Terrain colors
    private static readonly Color GrassLight = Color.FromArgb("#2d5a27");
    private static readonly Color GrassDark = Color.FromArgb("#1e4620");
    private static readonly Color GrassPatch = Color.FromArgb("#3a6b32");
    private static readonly Color PathDirt = Color.FromArgb("#4a3728");
    private static readonly Color PathDirtLight = Color.FromArgb("#5c4a3a");
    private static readonly Color PathEdge = Color.FromArgb("#3a2a1c");
    private static readonly Color TreeTrunk = Color.FromArgb("#5c3d2e");
    private static readonly Color TreeLeaves = Color.FromArgb("#2e7d32");
    private static readonly Color TreeLeavesDark = Color.FromArgb("#1b5e20");
    private static readonly Color FlowerColor = Color.FromArgb("#fff176");
    private static readonly Color RockColor = Color.FromArgb("#616161");

    // Portal colors
    private static readonly Color PortalGreen = Color.FromArgb("#00e676");
    private static readonly Color PortalGreenGlow = Color.FromArgb("#69f0ae");
    private static readonly Color PortalRed = Color.FromArgb("#ff1744");
    private static readonly Color PortalRedGlow = Color.FromArgb("#ff5252");

    // Enemy (Goblin)
    private static readonly Color GoblinSkin = Color.FromArgb("#4caf50");
    private static readonly Color GoblinSkinDark = Color.FromArgb("#388e3c");
    private static readonly Color GoblinEyes = Color.FromArgb("#ffeb3b");
    private static readonly Color GoblinEar = Color.FromArgb("#66bb6a");

    // Unit colors by rarity
    private static readonly Color SlimeBody = Color.FromArgb("#78909c");
    private static readonly Color SlimeShine = Color.FromArgb("#b0bec5");
    private static readonly Color ArcherBody = Color.FromArgb("#1565c0");
    private static readonly Color ArcherHat = Color.FromArgb("#0d47a1");
    private static readonly Color WizardBody = Color.FromArgb("#7b1fa2");
    private static readonly Color WizardHat = Color.FromArgb("#4a148c");
    private static readonly Color WizardGlow = Color.FromArgb("#e040fb");
    private static readonly Color DragonBody = Color.FromArgb("#c62828");
    private static readonly Color DragonWing = Color.FromArgb("#e53935");

    // HUD
    private static readonly Color HealthBarBg = Color.FromArgb("#1a1a1a");
    private static readonly Color HealthBarFg = Color.FromArgb("#00e676");
    private static readonly Color HealthBarLow = Color.FromArgb("#ff5252");
    private static readonly Color ProjectileColor = Color.FromArgb("#ffea00");
    private static readonly Color DividerColor = Color.FromArgb("#e94560");
    private static readonly Color OpponentBg = Color.FromArgb("#0a0e18");

    // Decoration seed for consistent random placement
    private readonly Random _decoRng = new Random(42);
    private List<(float x, float y, int type)>? _decorations;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Engine == null) return;

        float totalW = dirtyRect.Width;
        float totalH = dirtyRect.Height;

        if (IsSoloMode)
        {
            // Solo: full screen for player board
            Engine.BoardWidth = totalW;
            Engine.BoardHeight = totalH;
            if (Engine.PathPoints.Count == 0)
                Engine.BuildPath();
            if (_decorations == null)
                GenerateDecorations();

            DrawPlayerBoard(canvas, totalW, totalH);
            DrawOverlay(canvas, dirtyRect, totalH);
            return;
        }

        // Multiplayer: split screen
        float playerH = totalH * 0.58f;
        float divH = 3f;
        float oppH = totalH - playerH - divH;

        Engine.BoardWidth = totalW;
        Engine.BoardHeight = playerH;
        if (Engine.PathPoints.Count == 0)
            Engine.BuildPath();
        if (_decorations == null)
            GenerateDecorations();

        canvas.SaveState();
        canvas.ClipRectangle(0, 0, totalW, playerH);
        DrawPlayerBoard(canvas, totalW, playerH);
        canvas.RestoreState();

        canvas.FillColor = DividerColor;
        canvas.FillRectangle(0, playerH, totalW, divH);

        canvas.FontColor = Color.FromArgb("#ffffff80");
        canvas.FontSize = 10;
        canvas.DrawString("YOUR FIELD", 8, playerH - 14, 80, 12,
            HorizontalAlignment.Left, VerticalAlignment.Center);
        canvas.DrawString("OPPONENT", 8, playerH + divH + 2, 80, 12,
            HorizontalAlignment.Left, VerticalAlignment.Center);

        canvas.SaveState();
        canvas.ClipRectangle(0, playerH + divH, totalW, oppH);
        canvas.Translate(0, playerH + divH);
        DrawOpponentBoard(canvas, totalW, oppH);
        canvas.RestoreState();

        DrawOverlay(canvas, dirtyRect, playerH);
    }

    private void GenerateDecorations()
    {
        _decorations = new List<(float, float, int)>();
        var rng = new Random(42);

        // Place random decorations in interior cells
        for (int c = 1; c < GameEngine.GridColumns - 1; c++)
        {
            for (int r = 1; r < GameEngine.GridRows - 1; r++)
            {
                // 2-4 grass patches per cell
                for (int i = 0; i < rng.Next(2, 5); i++)
                {
                    float x = c + (float)rng.NextDouble();
                    float y = r + (float)rng.NextDouble();
                    _decorations.Add((x, y, 0)); // 0 = grass tuft
                }
                // Chance of flower
                if (rng.NextDouble() < 0.3)
                {
                    float x = c + (float)rng.NextDouble();
                    float y = r + (float)rng.NextDouble();
                    _decorations.Add((x, y, 1)); // 1 = flower
                }
                // Chance of rock
                if (rng.NextDouble() < 0.15)
                {
                    float x = c + 0.2f + (float)rng.NextDouble() * 0.6f;
                    float y = r + 0.2f + (float)rng.NextDouble() * 0.6f;
                    _decorations.Add((x, y, 2)); // 2 = rock
                }
            }
        }

        // Path decorations (pebbles)
        for (int c = 0; c < GameEngine.GridColumns; c++)
        {
            for (int r = 0; r < GameEngine.GridRows; r++)
            {
                bool isBorder = (r == 0 || r == GameEngine.GridRows - 1 || c == 0 || c == GameEngine.GridColumns - 1);
                if (!isBorder) continue;
                for (int i = 0; i < rng.Next(1, 3); i++)
                {
                    float x = c + (float)rng.NextDouble();
                    float y = r + (float)rng.NextDouble();
                    _decorations.Add((x, y, 3)); // 3 = pebble
                }
            }
        }
    }

    private void DrawPlayerBoard(ICanvas canvas, float w, float h)
    {
        if (Engine == null) return;

        float cw = Engine.CellWidth;
        float ch = Engine.CellHeight;

        // 1. Fill entire board with grass base
        canvas.FillColor = GrassLight;
        canvas.FillRectangle(0, 0, w, h);

        // Subtle grass variation patches (no grid visible)
        var rng2 = new Random(123);
        for (int i = 0; i < 20; i++)
        {
            float px = (float)rng2.NextDouble() * w;
            float py = (float)rng2.NextDouble() * h;
            float ps = 20 + (float)rng2.NextDouble() * 40;
            canvas.FillColor = rng2.NextDouble() > 0.5 ? GrassDark : GrassPatch;
            canvas.FillEllipse(px - ps / 2, py - ps / 2, ps, ps * 0.7f);
        }

        // 2. Draw smooth continuous dirt path (one shape, no cell edges)
        // Top strip
        canvas.FillColor = PathDirt;
        canvas.FillRectangle(0, 0, w, ch);
        // Right strip
        canvas.FillRectangle(w - cw, 0, cw, h);
        // Bottom strip
        canvas.FillRectangle(0, h - ch, w, ch);
        // Left strip
        canvas.FillRectangle(0, 0, cw, h);

        // Smooth inner edge of path (soften transition from dirt to grass)
        canvas.FillColor = PathEdge;
        // Top inner edge
        canvas.FillRectangle(cw, ch - 2, w - 2 * cw, 3);
        // Bottom inner edge
        canvas.FillRectangle(cw, h - ch, w - 2 * cw, 3);
        // Left inner edge
        canvas.FillRectangle(cw - 2, ch, 3, h - 2 * ch);
        // Right inner edge
        canvas.FillRectangle(w - cw, ch, 3, h - 2 * ch);

        // Path texture (pebbles scattered across the dirt)
        if (_decorations != null)
        {
            foreach (var (dx, dy, dtype) in _decorations)
            {
                float px = dx * cw;
                float py = dy * ch;

                switch (dtype)
                {
                    case 0: // Grass tuft (interior only)
                        canvas.StrokeColor = GrassPatch;
                        canvas.StrokeSize = 1.5f;
                        canvas.DrawLine(px, py, px - 2, py - 5);
                        canvas.DrawLine(px, py, px + 2, py - 4);
                        canvas.DrawLine(px, py, px, py - 6);
                        break;
                    case 1: // Flower
                        canvas.FillColor = FlowerColor;
                        canvas.FillCircle(px, py, 2.5f);
                        canvas.FillColor = Colors.White;
                        canvas.FillCircle(px, py, 1.2f);
                        break;
                    case 2: // Rock
                        canvas.FillColor = RockColor;
                        canvas.FillEllipse(px - 4, py - 2, 8, 5);
                        canvas.FillColor = Color.FromArgb("#757575");
                        canvas.FillEllipse(px - 3, py - 2, 5, 3);
                        break;
                    case 3: // Pebble on path
                        canvas.FillColor = PathDirtLight;
                        canvas.FillCircle(px, py, 1.5f);
                        break;
                }
            }
        }

        // 3. Portals
        DrawPortal(canvas, 0.5f * cw, 0.5f * ch, cw * 0.4f, true);
        DrawPortal(canvas, 0.5f * cw, 1.5f * ch, cw * 0.35f, false);

        // 4. Draw units with shooting animation
        foreach (var unit in Engine.Units)
        {
            float cx = unit.GridX * cw + cw / 2;
            float cy = unit.GridY * ch + ch / 2;
            float size = MathF.Min(cw, ch) * 0.32f;

            // Shooting recoil: nudge unit away from target
            float offsetX = 0, offsetY = 0;
            bool isShooting = unit.ShootAnimTimer > 0;
            if (isShooting)
            {
                float recoil = unit.ShootAnimTimer * 15f; // pixels of recoil
                offsetX = -MathF.Cos(unit.ShootAngle) * recoil;
                offsetY = -MathF.Sin(unit.ShootAngle) * recoil;
            }

            // Draw muzzle flash when shooting
            if (isShooting && unit.ShootAnimTimer > 0.1f)
            {
                float flashX = cx + MathF.Cos(unit.ShootAngle) * size * 1.2f;
                float flashY = cy + MathF.Sin(unit.ShootAngle) * size * 1.2f;
                canvas.FillColor = Colors.White.WithAlpha(0.7f);
                canvas.FillCircle(flashX, flashY, size * 0.4f);
                canvas.FillColor = ProjectileColor.WithAlpha(0.5f);
                canvas.FillCircle(flashX, flashY, size * 0.6f);
            }

            canvas.SaveState();
            canvas.Translate(offsetX, offsetY);
            DrawUnitCharacter(canvas, unit.GridX, unit.GridY, unit.Rarity, unit.Level, cw, ch);
            canvas.RestoreState();
        }

        // 5. Draw enemies (goblins / bosses)
        foreach (var enemy in Engine.Enemies)
        {
            if (!enemy.IsAlive) continue;
            if (enemy.IsBoss)
                DrawBossGoblin(canvas, enemy.X, enemy.Y, cw * 0.42f, enemy.Health / enemy.MaxHealth);
            else if (enemy.IsMiniBoss)
                DrawBossGoblin(canvas, enemy.X, enemy.Y, cw * 0.32f, enemy.Health / enemy.MaxHealth);
            else
                DrawGoblin(canvas, enemy.X, enemy.Y, cw * 0.22f, enemy.Health / enemy.MaxHealth);
        }

        // 6. Draw projectiles (different per unit type)
        foreach (var proj in Engine.Projectiles)
        {
            if (!proj.IsActive) continue;

            float dx = proj.TargetX - proj.X;
            float dy = proj.TargetY - proj.Y;
            float angle = MathF.Atan2(dy, dx);

            switch (proj.Rarity)
            {
                case 1: // Slime: green blob
                    canvas.FillColor = Color.FromArgb("#66bb6a").WithAlpha(0.2f);
                    canvas.FillCircle(proj.X, proj.Y, 7);
                    canvas.FillColor = Color.FromArgb("#66bb6a");
                    canvas.FillCircle(proj.X, proj.Y, 4);
                    canvas.FillColor = Color.FromArgb("#a5d6a7");
                    canvas.FillCircle(proj.X - 1, proj.Y - 1, 1.5f);
                    break;

                case 2: // Archer: arrow
                    canvas.SaveState();
                    canvas.Translate(proj.X, proj.Y);
                    canvas.Rotate(angle * 180f / MathF.PI);
                    // Arrow shaft
                    canvas.StrokeColor = Color.FromArgb("#8d6e63");
                    canvas.StrokeSize = 2;
                    canvas.DrawLine(-8, 0, 5, 0);
                    // Arrow head
                    canvas.FillColor = Color.FromArgb("#bdbdbd");
                    var head = new PathF();
                    head.MoveTo(8, 0);
                    head.LineTo(4, -3);
                    head.LineTo(4, 3);
                    head.Close();
                    canvas.FillPath(head);
                    // Fletching
                    canvas.StrokeColor = Color.FromArgb("#e53935");
                    canvas.StrokeSize = 1;
                    canvas.DrawLine(-7, 0, -9, -3);
                    canvas.DrawLine(-7, 0, -9, 3);
                    canvas.RestoreState();
                    break;

                case 3: // Wizard: magic orb with sparkles
                    canvas.FillColor = Color.FromArgb("#e040fb").WithAlpha(0.15f);
                    canvas.FillCircle(proj.X, proj.Y, 10);
                    canvas.FillColor = Color.FromArgb("#e040fb").WithAlpha(0.3f);
                    canvas.FillCircle(proj.X, proj.Y, 7);
                    canvas.FillColor = Color.FromArgb("#e040fb");
                    canvas.FillCircle(proj.X, proj.Y, 4);
                    canvas.FillColor = Colors.White.WithAlpha(0.8f);
                    canvas.FillCircle(proj.X + 1, proj.Y - 1, 1.5f);
                    // Sparkle particles
                    canvas.FillColor = Color.FromArgb("#ce93d8");
                    float sparkleOffset = (proj.X + proj.Y) % 6;
                    canvas.FillCircle(proj.X + MathF.Sin(sparkleOffset) * 6, proj.Y + MathF.Cos(sparkleOffset) * 6, 1.5f);
                    canvas.FillCircle(proj.X - MathF.Cos(sparkleOffset) * 5, proj.Y - MathF.Sin(sparkleOffset) * 5, 1f);
                    break;

                case 4: // Dragon: fireball with flame trail
                    // Flame trail
                    canvas.FillColor = Color.FromArgb("#ff6f00").WithAlpha(0.1f);
                    canvas.FillCircle(proj.X, proj.Y, 14);
                    canvas.FillColor = Color.FromArgb("#ff6f00").WithAlpha(0.2f);
                    canvas.FillCircle(proj.X, proj.Y, 10);
                    // Trailing flames
                    float trailX = proj.X - MathF.Cos(angle) * 6;
                    float trailY = proj.Y - MathF.Sin(angle) * 6;
                    canvas.FillColor = Color.FromArgb("#ff8f00").WithAlpha(0.4f);
                    canvas.FillCircle(trailX, trailY, 5);
                    float trailX2 = proj.X - MathF.Cos(angle) * 11;
                    float trailY2 = proj.Y - MathF.Sin(angle) * 11;
                    canvas.FillColor = Color.FromArgb("#ffab00").WithAlpha(0.2f);
                    canvas.FillCircle(trailX2, trailY2, 3.5f);
                    // Core fireball
                    canvas.FillColor = Color.FromArgb("#ff3d00");
                    canvas.FillCircle(proj.X, proj.Y, 5);
                    canvas.FillColor = Color.FromArgb("#ffab00");
                    canvas.FillCircle(proj.X, proj.Y, 3);
                    canvas.FillColor = Colors.White.WithAlpha(0.7f);
                    canvas.FillCircle(proj.X, proj.Y, 1.5f);
                    break;

                default:
                    canvas.FillColor = ProjectileColor;
                    canvas.FillCircle(proj.X, proj.Y, 3);
                    break;
            }
        }
    }

    private void DrawPortal(ICanvas canvas, float cx, float cy, float radius, bool isStart)
    {
        Color main = isStart ? PortalGreen : PortalRed;
        Color glow = isStart ? PortalGreenGlow : PortalRedGlow;

        // Outer glow
        canvas.FillColor = main.WithAlpha(0.15f);
        canvas.FillCircle(cx, cy, radius * 2f);
        canvas.FillColor = main.WithAlpha(0.25f);
        canvas.FillCircle(cx, cy, radius * 1.5f);

        // Portal ring
        canvas.StrokeColor = main;
        canvas.StrokeSize = 3;
        canvas.DrawCircle(cx, cy, radius);
        canvas.StrokeColor = glow;
        canvas.StrokeSize = 1.5f;
        canvas.DrawCircle(cx, cy, radius * 0.7f);

        // Inner swirl effect
        canvas.FillColor = main.WithAlpha(0.4f);
        canvas.FillCircle(cx, cy, radius * 0.5f);
        canvas.FillColor = glow.WithAlpha(0.3f);
        canvas.FillCircle(cx + radius * 0.15f, cy - radius * 0.1f, radius * 0.25f);

        // Label
        canvas.FontColor = glow;
        canvas.FontSize = 8;
        string label = isStart ? "SPAWN" : "EXIT";
        canvas.DrawString(label, cx - radius, cy + radius + 2, radius * 2, 10,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawGoblin(ICanvas canvas, float cx, float cy, float size, float healthPct)
    {
        // Body
        canvas.FillColor = GoblinSkin;
        canvas.FillEllipse(cx - size, cy - size * 0.8f, size * 2, size * 1.8f);

        // Ears (pointy triangles)
        canvas.FillColor = GoblinEar;
        // Left ear
        var leftEar = new PathF();
        leftEar.MoveTo(cx - size * 0.7f, cy - size * 0.3f);
        leftEar.LineTo(cx - size * 1.3f, cy - size * 1.2f);
        leftEar.LineTo(cx - size * 0.3f, cy - size * 0.6f);
        leftEar.Close();
        canvas.FillPath(leftEar);

        // Right ear
        var rightEar = new PathF();
        rightEar.MoveTo(cx + size * 0.7f, cy - size * 0.3f);
        rightEar.LineTo(cx + size * 1.3f, cy - size * 1.2f);
        rightEar.LineTo(cx + size * 0.3f, cy - size * 0.6f);
        rightEar.Close();
        canvas.FillPath(rightEar);

        // Eyes (yellow with black pupils)
        canvas.FillColor = GoblinEyes;
        canvas.FillCircle(cx - size * 0.3f, cy - size * 0.2f, size * 0.2f);
        canvas.FillCircle(cx + size * 0.3f, cy - size * 0.2f, size * 0.2f);
        canvas.FillColor = Colors.Black;
        canvas.FillCircle(cx - size * 0.25f, cy - size * 0.15f, size * 0.1f);
        canvas.FillCircle(cx + size * 0.25f, cy - size * 0.15f, size * 0.1f);

        // Mouth (angry line)
        canvas.StrokeColor = GoblinSkinDark;
        canvas.StrokeSize = 1.5f;
        canvas.DrawLine(cx - size * 0.25f, cy + size * 0.25f, cx + size * 0.25f, cy + size * 0.2f);

        // Health bar
        float barW = size * 2.5f;
        float barH = 3;
        float barX = cx - barW / 2;
        float barY = cy - size * 1.3f;

        canvas.FillColor = HealthBarBg;
        canvas.FillRoundedRectangle(barX, barY, barW, barH, 1.5f);
        canvas.FillColor = healthPct > 0.3f ? HealthBarFg : HealthBarLow;
        canvas.FillRoundedRectangle(barX, barY, barW * healthPct, barH, 1.5f);
    }

    private void DrawBossGoblin(ICanvas canvas, float cx, float cy, float size, float healthPct)
    {
        // Red aura glow
        canvas.FillColor = Color.FromArgb("#ff1744").WithAlpha(0.12f);
        canvas.FillCircle(cx, cy, size * 2.2f);
        canvas.FillColor = Color.FromArgb("#ff1744").WithAlpha(0.08f);
        canvas.FillCircle(cx, cy, size * 2.8f);

        // Body (darker, meaner green)
        canvas.FillColor = Color.FromArgb("#2e7d32");
        canvas.FillEllipse(cx - size, cy - size * 0.8f, size * 2, size * 1.8f);

        // Armor/shoulders
        canvas.FillColor = Color.FromArgb("#424242");
        canvas.FillEllipse(cx - size * 1.1f, cy - size * 0.4f, size * 0.5f, size * 0.6f);
        canvas.FillEllipse(cx + size * 0.6f, cy - size * 0.4f, size * 0.5f, size * 0.6f);

        // Ears (bigger, pointier)
        canvas.FillColor = Color.FromArgb("#388e3c");
        var leftEar = new PathF();
        leftEar.MoveTo(cx - size * 0.7f, cy - size * 0.3f);
        leftEar.LineTo(cx - size * 1.5f, cy - size * 1.5f);
        leftEar.LineTo(cx - size * 0.3f, cy - size * 0.6f);
        leftEar.Close();
        canvas.FillPath(leftEar);

        var rightEar = new PathF();
        rightEar.MoveTo(cx + size * 0.7f, cy - size * 0.3f);
        rightEar.LineTo(cx + size * 1.5f, cy - size * 1.5f);
        rightEar.LineTo(cx + size * 0.3f, cy - size * 0.6f);
        rightEar.Close();
        canvas.FillPath(rightEar);

        // Horns
        canvas.FillColor = Color.FromArgb("#5d4037");
        var lhorn = new PathF();
        lhorn.MoveTo(cx - size * 0.3f, cy - size * 0.7f);
        lhorn.LineTo(cx - size * 0.5f, cy - size * 1.4f);
        lhorn.LineTo(cx - size * 0.1f, cy - size * 0.7f);
        lhorn.Close();
        canvas.FillPath(lhorn);

        var rhorn = new PathF();
        rhorn.MoveTo(cx + size * 0.3f, cy - size * 0.7f);
        rhorn.LineTo(cx + size * 0.5f, cy - size * 1.4f);
        rhorn.LineTo(cx + size * 0.1f, cy - size * 0.7f);
        rhorn.Close();
        canvas.FillPath(rhorn);

        // Eyes (red, angry)
        canvas.FillColor = Color.FromArgb("#ff1744");
        canvas.FillCircle(cx - size * 0.3f, cy - size * 0.2f, size * 0.22f);
        canvas.FillCircle(cx + size * 0.3f, cy - size * 0.2f, size * 0.22f);
        canvas.FillColor = Colors.Black;
        canvas.FillCircle(cx - size * 0.25f, cy - size * 0.15f, size * 0.1f);
        canvas.FillCircle(cx + size * 0.25f, cy - size * 0.15f, size * 0.1f);

        // Angry mouth with teeth
        canvas.StrokeColor = Color.FromArgb("#1b5e20");
        canvas.StrokeSize = 2;
        canvas.DrawLine(cx - size * 0.35f, cy + size * 0.2f, cx + size * 0.35f, cy + size * 0.15f);
        // Teeth
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(cx - size * 0.15f, cy + size * 0.12f, size * 0.1f, size * 0.12f);
        canvas.FillRectangle(cx + size * 0.05f, cy + size * 0.1f, size * 0.1f, size * 0.12f);

        // Health bar (bigger)
        float barW = size * 3f;
        float barH = 5;
        float barX = cx - barW / 2;
        float barY = cy - size * 1.8f;

        canvas.FillColor = HealthBarBg;
        canvas.FillRoundedRectangle(barX, barY, barW, barH, 2);
        canvas.FillColor = healthPct > 0.3f ? HealthBarFg : HealthBarLow;
        canvas.FillRoundedRectangle(barX, barY, barW * healthPct, barH, 2);

        // Boss label
        canvas.StrokeColor = Color.FromArgb("#ff1744");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(barX, barY, barW, barH, 2);
    }

    private void DrawUnitCharacter(ICanvas canvas, int gx, int gy, int rarity, int level, float cw, float ch)
    {
        float cx = gx * cw + cw / 2;
        float cy = gy * ch + ch / 2;
        float size = MathF.Min(cw, ch) * 0.32f;

        switch (rarity)
        {
            case 1: DrawSlime(canvas, cx, cy, size, level); break;
            case 2: DrawArcher(canvas, cx, cy, size, level); break;
            case 3: DrawWizard(canvas, cx, cy, size, level); break;
            case 4: DrawDragon(canvas, cx, cy, size, level); break;
            default: DrawSlime(canvas, cx, cy, size, level); break;
        }
    }

    private void DrawSlime(ICanvas canvas, float cx, float cy, float size, int level)
    {
        // Glow
        canvas.FillColor = SlimeBody.WithAlpha(0.15f);
        canvas.FillCircle(cx, cy + size * 0.1f, size * 1.4f);

        // Body (blob shape)
        canvas.FillColor = SlimeBody;
        canvas.FillEllipse(cx - size, cy - size * 0.5f, size * 2, size * 1.5f);

        // Shine
        canvas.FillColor = SlimeShine.WithAlpha(0.4f);
        canvas.FillEllipse(cx - size * 0.5f, cy - size * 0.4f, size * 0.6f, size * 0.4f);

        // Eyes
        canvas.FillColor = Colors.White;
        canvas.FillCircle(cx - size * 0.3f, cy - size * 0.1f, size * 0.18f);
        canvas.FillCircle(cx + size * 0.2f, cy - size * 0.1f, size * 0.18f);
        canvas.FillColor = Colors.Black;
        canvas.FillCircle(cx - size * 0.25f, cy - size * 0.05f, size * 0.08f);
        canvas.FillCircle(cx + size * 0.25f, cy - size * 0.05f, size * 0.08f);

        // Level badge
        DrawLevelBadge(canvas, cx, cy + size * 0.6f, size * 0.35f, level, SlimeBody);
    }

    private void DrawArcher(ICanvas canvas, float cx, float cy, float size, int level)
    {
        // Glow
        canvas.FillColor = ArcherBody.WithAlpha(0.15f);
        canvas.FillCircle(cx, cy, size * 1.4f);

        // Body
        canvas.FillColor = ArcherBody;
        canvas.FillEllipse(cx - size * 0.6f, cy - size * 0.3f, size * 1.2f, size * 1.3f);

        // Hood/hat
        canvas.FillColor = ArcherHat;
        var hood = new PathF();
        hood.MoveTo(cx - size * 0.5f, cy - size * 0.2f);
        hood.LineTo(cx, cy - size * 1.1f);
        hood.LineTo(cx + size * 0.5f, cy - size * 0.2f);
        hood.Close();
        canvas.FillPath(hood);

        // Bow (arc on the right side)
        canvas.StrokeColor = Color.FromArgb("#8d6e63");
        canvas.StrokeSize = 2;
        canvas.DrawArc(cx + size * 0.3f, cy - size * 0.6f, size * 0.8f, size * 1.2f, 60, 240, false, false);
        // Bowstring
        canvas.StrokeColor = Color.FromArgb("#bdbdbd");
        canvas.StrokeSize = 1;
        canvas.DrawLine(cx + size * 0.55f, cy - size * 0.45f, cx + size * 0.55f, cy + size * 0.45f);

        // Eyes
        canvas.FillColor = Colors.White;
        canvas.FillCircle(cx - size * 0.15f, cy - size * 0.05f, size * 0.12f);
        canvas.FillCircle(cx + size * 0.15f, cy - size * 0.05f, size * 0.12f);
        canvas.FillColor = Colors.Black;
        canvas.FillCircle(cx - size * 0.12f, cy - size * 0.02f, size * 0.06f);
        canvas.FillCircle(cx + size * 0.18f, cy - size * 0.02f, size * 0.06f);

        DrawLevelBadge(canvas, cx, cy + size * 0.7f, size * 0.35f, level, ArcherBody);
    }

    private void DrawWizard(ICanvas canvas, float cx, float cy, float size, int level)
    {
        // Magical glow
        canvas.FillColor = WizardGlow.WithAlpha(0.1f);
        canvas.FillCircle(cx, cy, size * 1.6f);
        canvas.FillColor = WizardGlow.WithAlpha(0.15f);
        canvas.FillCircle(cx, cy, size * 1.2f);

        // Body/robe
        canvas.FillColor = WizardBody;
        var robe = new PathF();
        robe.MoveTo(cx - size * 0.7f, cy + size * 0.8f);
        robe.LineTo(cx - size * 0.5f, cy - size * 0.2f);
        robe.LineTo(cx + size * 0.5f, cy - size * 0.2f);
        robe.LineTo(cx + size * 0.7f, cy + size * 0.8f);
        robe.Close();
        canvas.FillPath(robe);

        // Wizard hat
        canvas.FillColor = WizardHat;
        var hat = new PathF();
        hat.MoveTo(cx - size * 0.6f, cy - size * 0.2f);
        hat.LineTo(cx, cy - size * 1.4f);
        hat.LineTo(cx + size * 0.6f, cy - size * 0.2f);
        hat.Close();
        canvas.FillPath(hat);

        // Hat brim
        canvas.FillColor = WizardBody;
        canvas.FillEllipse(cx - size * 0.7f, cy - size * 0.35f, size * 1.4f, size * 0.3f);

        // Star on hat
        canvas.FillColor = WizardGlow;
        canvas.FillCircle(cx, cy - size * 0.7f, size * 0.12f);

        // Eyes (glowing)
        canvas.FillColor = WizardGlow;
        canvas.FillCircle(cx - size * 0.2f, cy, size * 0.1f);
        canvas.FillCircle(cx + size * 0.2f, cy, size * 0.1f);

        // Staff (left side)
        canvas.StrokeColor = Color.FromArgb("#8d6e63");
        canvas.StrokeSize = 2;
        canvas.DrawLine(cx - size * 0.8f, cy + size * 0.8f, cx - size * 0.5f, cy - size * 0.6f);
        // Crystal on staff
        canvas.FillColor = WizardGlow;
        canvas.FillCircle(cx - size * 0.5f, cy - size * 0.7f, size * 0.12f);

        DrawLevelBadge(canvas, cx, cy + size * 0.9f, size * 0.35f, level, WizardBody);
    }

    private void DrawDragon(ICanvas canvas, float cx, float cy, float size, int level)
    {
        // Fire glow
        canvas.FillColor = DragonBody.WithAlpha(0.12f);
        canvas.FillCircle(cx, cy, size * 1.8f);
        canvas.FillColor = Color.FromArgb("#ff6f00").WithAlpha(0.1f);
        canvas.FillCircle(cx, cy, size * 1.4f);

        // Wings
        canvas.FillColor = DragonWing;
        // Left wing
        var lwing = new PathF();
        lwing.MoveTo(cx - size * 0.3f, cy - size * 0.2f);
        lwing.LineTo(cx - size * 1.3f, cy - size * 0.8f);
        lwing.LineTo(cx - size * 1.0f, cy + size * 0.2f);
        lwing.LineTo(cx - size * 0.3f, cy + size * 0.1f);
        lwing.Close();
        canvas.FillPath(lwing);
        // Right wing
        var rwing = new PathF();
        rwing.MoveTo(cx + size * 0.3f, cy - size * 0.2f);
        rwing.LineTo(cx + size * 1.3f, cy - size * 0.8f);
        rwing.LineTo(cx + size * 1.0f, cy + size * 0.2f);
        rwing.LineTo(cx + size * 0.3f, cy + size * 0.1f);
        rwing.Close();
        canvas.FillPath(rwing);

        // Body
        canvas.FillColor = DragonBody;
        canvas.FillEllipse(cx - size * 0.6f, cy - size * 0.5f, size * 1.2f, size * 1.2f);

        // Belly
        canvas.FillColor = Color.FromArgb("#ef5350");
        canvas.FillEllipse(cx - size * 0.3f, cy - size * 0.1f, size * 0.6f, size * 0.6f);

        // Head horns
        canvas.FillColor = Color.FromArgb("#ff8f00");
        canvas.FillEllipse(cx - size * 0.35f, cy - size * 0.8f, size * 0.15f, size * 0.35f);
        canvas.FillEllipse(cx + size * 0.2f, cy - size * 0.8f, size * 0.15f, size * 0.35f);

        // Eyes (fierce)
        canvas.FillColor = Color.FromArgb("#ff6f00");
        canvas.FillCircle(cx - size * 0.2f, cy - size * 0.2f, size * 0.12f);
        canvas.FillCircle(cx + size * 0.2f, cy - size * 0.2f, size * 0.12f);
        canvas.FillColor = Colors.Black;
        canvas.FillCircle(cx - size * 0.17f, cy - size * 0.18f, size * 0.06f);
        canvas.FillCircle(cx + size * 0.17f, cy - size * 0.18f, size * 0.06f);

        // Nostrils (fire hint)
        canvas.FillColor = Color.FromArgb("#ff6f00");
        canvas.FillCircle(cx - size * 0.08f, cy + size * 0.05f, size * 0.04f);
        canvas.FillCircle(cx + size * 0.08f, cy + size * 0.05f, size * 0.04f);

        DrawLevelBadge(canvas, cx, cy + size * 0.8f, size * 0.35f, level, DragonBody);
    }

    private void DrawLevelBadge(ICanvas canvas, float cx, float cy, float radius, int level, Color bgColor)
    {
        canvas.FillColor = Color.FromArgb("#000000aa");
        canvas.FillCircle(cx, cy, radius);
        canvas.StrokeColor = Colors.White.WithAlpha(0.5f);
        canvas.StrokeSize = 1;
        canvas.DrawCircle(cx, cy, radius);
        canvas.FontColor = Colors.White;
        canvas.FontSize = radius * 1.2f;
        canvas.DrawString(level.ToString(), cx - radius, cy - radius,
            radius * 2, radius * 2,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawOpponentBoard(ICanvas canvas, float w, float h)
    {
        if (OpponentEngine == null) return;

        // Set opponent engine board dimensions for this frame
        OpponentEngine.BoardWidth = w;
        OpponentEngine.BoardHeight = h;

        float cw = w / GameEngine.GridColumns;
        float ch = h / GameEngine.GridRows;

        // Full terrain
        canvas.FillColor = GrassLight;
        canvas.FillRectangle(0, 0, w, h);

        var rng2 = new Random(999);
        for (int i = 0; i < 15; i++)
        {
            float px = (float)rng2.NextDouble() * w;
            float py = (float)rng2.NextDouble() * h;
            float ps = 15 + (float)rng2.NextDouble() * 30;
            canvas.FillColor = rng2.NextDouble() > 0.5 ? GrassDark : GrassPatch;
            canvas.FillEllipse(px - ps / 2, py - ps / 2, ps, ps * 0.7f);
        }

        // Dirt path
        canvas.FillColor = PathDirt;
        canvas.FillRectangle(0, 0, w, ch);
        canvas.FillRectangle(w - cw, 0, cw, h);
        canvas.FillRectangle(0, h - ch, w, ch);
        canvas.FillRectangle(0, 0, cw, h);

        canvas.FillColor = PathEdge;
        canvas.FillRectangle(cw, ch - 1, w - 2 * cw, 2);
        canvas.FillRectangle(cw, h - ch, w - 2 * cw, 2);
        canvas.FillRectangle(cw - 1, ch, 2, h - 2 * ch);
        canvas.FillRectangle(w - cw, ch, 2, h - 2 * ch);

        // Portals
        DrawPortal(canvas, 0.5f * cw, 0.5f * ch, cw * 0.3f, true);
        DrawPortal(canvas, 0.5f * cw, 1.5f * ch, cw * 0.25f, false);

        // Draw opponent units from opponent engine — full character models
        foreach (var unit in OpponentEngine.Units)
        {
            float cx = unit.GridX * cw + cw / 2;
            float cy = unit.GridY * ch + ch / 2;
            float size = MathF.Min(cw, ch) * 0.28f;

            switch (unit.Rarity)
            {
                case 1: DrawSlime(canvas, cx, cy, size, unit.Level); break;
                case 2: DrawArcher(canvas, cx, cy, size, unit.Level); break;
                case 3: DrawWizard(canvas, cx, cy, size, unit.Level); break;
                case 4: DrawDragon(canvas, cx, cy, size, unit.Level); break;
                default: DrawSlime(canvas, cx, cy, size, unit.Level); break;
            }
        }

        // Draw opponent enemies from opponent engine — smooth 30fps local simulation
        foreach (var enemy in OpponentEngine.Enemies)
        {
            if (!enemy.IsAlive) continue;
            // Scale enemy position from opponent engine coords to this board's coords
            float ex = OpponentEngine.BoardWidth > 0 ? enemy.X / OpponentEngine.BoardWidth * w : enemy.X;
            float ey = OpponentEngine.BoardHeight > 0 ? enemy.Y / OpponentEngine.BoardHeight * h : enemy.Y;
            float size = cw * (enemy.IsBoss ? 0.35f : enemy.IsMiniBoss ? 0.26f : 0.16f);

            if (enemy.IsBoss || enemy.IsMiniBoss)
                DrawBossGoblin(canvas, ex, ey, size, enemy.Health / enemy.MaxHealth);
            else
                DrawGoblin(canvas, ex, ey, size, enemy.Health / enemy.MaxHealth);
        }

        // Draw opponent projectiles
        foreach (var proj in OpponentEngine.Projectiles)
        {
            if (!proj.IsActive) continue;
            float px = OpponentEngine.BoardWidth > 0 ? proj.X / OpponentEngine.BoardWidth * w : proj.X;
            float py = OpponentEngine.BoardHeight > 0 ? proj.Y / OpponentEngine.BoardHeight * h : proj.Y;
            canvas.FillColor = ProjectileColor.WithAlpha(0.3f);
            canvas.FillCircle(px, py, 4);
            canvas.FillColor = ProjectileColor;
            canvas.FillCircle(px, py, 2);
        }

        // Opponent HP badge
        float badgeW = 65;
        float badgeH = 22;
        float bx = w - badgeW - 6;
        canvas.FillColor = Color.FromArgb("#1a0a10");
        canvas.FillRoundedRectangle(bx, 5, badgeW, badgeH, 6);
        canvas.StrokeColor = Color.FromArgb("#ff475740");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(bx, 5, badgeW, badgeH, 6);
        canvas.FontColor = Color.FromArgb("#ff6b81");
        canvas.FontSize = 12;
        canvas.DrawString($"HP {OpponentHealth}", bx, 5, badgeW, badgeH,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    private void DrawOverlay(ICanvas canvas, RectF rect, float playerH)
    {
        if (Engine == null) return;

        string? text = null;
        if (Engine.WaitingToStart)
            text = $"Starting in {(int)Engine.WaveCountdown + 1}...";
        else if (!Engine.IsWaveActive && Engine.WaveCountdown > 0)
            text = $"Next wave in {(int)Engine.WaveCountdown + 1}s";

        if (text == null) return;

        float boxW = 220;
        float boxH = 44;
        float boxX = (rect.Width - boxW) / 2;
        float boxY = playerH / 2 - boxH / 2;

        canvas.FillColor = Color.FromArgb("#DD1a2235");
        canvas.FillRoundedRectangle(boxX, boxY, boxW, boxH, 12);
        canvas.StrokeColor = Color.FromArgb("#3a5a3e");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(boxX, boxY, boxW, boxH, 12);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 18;
        canvas.DrawString(text, boxX, boxY, boxW, boxH,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
