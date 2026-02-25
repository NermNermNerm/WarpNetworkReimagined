using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace NermNermNerm.Warpinator;

public class WarpShop  : ModLet
{
    private static readonly Vector2 boothTile = new Vector2(84, 20);
    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        mod.Helper.Events.GameLoop.DayStarted += this.GameLoopOnDayStarted;

        mod.Helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
    }

    private int proximityCounter = 0;
    // private int facingCounter = 0;
    private int idleCounter = 0;

    // ISSUE:  Could be that a mod would want to change this - so it'd be better if it was loaded from content somewhere.
    private const int BoothLocationX = 84;
    private const int BoothLocationY = 20;

    private const int BoothWidthInTiles = 4;
    private const int BoothHeightInTiles = 5;

    private const string BoothSheetId = "NermNermNerm.Warpinator.TrollBooth";


    private enum BoothAnimationFrame
    {
        Empty = 0,
        Poof1,
        Poof2,
        Poof3,
        Poof4,
        Poof5,
        Present,
        Blink,
        EyesLeft,
        FaceRight,
    }

    private const int NumAnimationFrames = 1 + (int)BoothAnimationFrame.FaceRight;

    private record BoothAnimation(BoothAnimationFrame AnimationFrame, int NumTicks);

    private readonly List<BoothAnimation> boothAnimations = new();

    private StaticTile[]? tiles = null;

    private BoothAnimationFrame nowShowingFrame = BoothAnimationFrame.Empty;

    private bool IsNorvinPresent => this.nowShowingFrame >= BoothAnimationFrame.Present;

    private void SetBoothFrame(BoothAnimationFrame newFrame)
    {
        var mountain = Game1.getLocationFromName("Mountain");
        var buildingsLayer = mountain.Map.GetLayer(I("Buildings"));
        var frontLayer = mountain.Map.GetLayer(I("Front"));
        var sheet = mountain.Map.GetTileSheet(WarpShop.BoothSheetId);

        for (int dy = 0; dy < WarpShop.BoothHeightInTiles; ++dy) // The png is 80px tall
        {
            for (int dx = 0; dx < WarpShop.BoothWidthInTiles; ++dx) // and 64px wide.
            {
                int x = WarpShop.BoothLocationX + dx;
                int y = WarpShop.BoothLocationY + dy;
                int tileIndex = (int)newFrame * WarpShop.BoothWidthInTiles + dx
                        + dy * (WarpShop.NumAnimationFrames * WarpShop.BoothWidthInTiles); // dy*tiles-per-row

                (dy < 3 ? frontLayer : buildingsLayer).Tiles[x, y] =
                    new StaticTile(buildingsLayer, sheet, BlendMode.Alpha, tileIndex);
            }
        }

        this.nowShowingFrame = newFrame;
    }

    void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Game1.hasLoadedGame)
        {
            return;
        }

        var mountain = Game1.getLocationFromName("Mountain");

        // 1) Proximity check (once per second)
        if (++this.proximityCounter >= 60)
        {
            this.proximityCounter = 0;

            var nearestFarmer = Utility.isThereAFarmerWithinDistance(boothTile, 7, mountain);

            if (nearestFarmer is not null && !this.IsNorvinPresent && this.boothAnimations.Count == 0)
            {
                this.boothAnimations.AddRange([
                    new BoothAnimation(BoothAnimationFrame.Poof1, 5),
                    new BoothAnimation(BoothAnimationFrame.Poof2, 5),
                    new BoothAnimation(BoothAnimationFrame.Poof3, 5),
                    new BoothAnimation(BoothAnimationFrame.Poof4, 5),
                    new BoothAnimation(BoothAnimationFrame.Poof5, 5),
                    new BoothAnimation(BoothAnimationFrame.Present, 1),
                    ]);
                mountain.playSound("wand");
            }
            else if (nearestFarmer is null && this.IsNorvinPresent && this.boothAnimations.Count == 0)
            {
                this.boothAnimations.AddRange([
                    new BoothAnimation(BoothAnimationFrame.Poof5, 5),
                    new BoothAnimation(BoothAnimationFrame.Poof4, 5),
                    new BoothAnimation(BoothAnimationFrame.Poof3, 5),
                    new BoothAnimation(BoothAnimationFrame.Poof2, 5),
                    new BoothAnimation(BoothAnimationFrame.Poof1, 5),
                    new BoothAnimation(BoothAnimationFrame.Empty, 1),
                ]);
                mountain.playSound("wand");
            }
        }


        // 2) Facing the nearest player (every 10 ticks)
        // if (this.norvinPresent && ++this.facingCounter >= 10)
        //{
        //    this.facingCounter = 0;
            // FaceNearestPlayer();
        //}

        // 3) Idle animations (randomized)
        // if (this.norvinPresent && ++this.idleCounter >= Game1.random.Next(30, 120))
        // {
        //     this.idleCounter = 0;
        //     // PlayIdleAnimation();
        // }

        // Roll any animations
        if (this.boothAnimations.Any())
        {
            ++this.idleCounter;
            var currentAnimation = this.boothAnimations.First();
            if (this.nowShowingFrame != currentAnimation.AnimationFrame)
            {
                this.SetBoothFrame(currentAnimation.AnimationFrame);
            }
            else if (this.idleCounter >= currentAnimation.NumTicks)
            {
                this.boothAnimations.RemoveAt(0);
                var newFirst = this.boothAnimations.FirstOrDefault();
                if (newFirst is not null)
                {
                    this.SetBoothFrame(newFirst.AnimationFrame);
                    this.idleCounter = 0;
                }
            }
        }
    }

    private void GameLoopOnDayStarted(object? sender, DayStartedEventArgs e)
    {
        var mountain = Game1.getLocationFromName("Mountain");
        var buildingsLayer = mountain.Map.GetLayer(I("Buildings"));
        var frontLayer = mountain.Map.GetLayer(I("Front"));
        var backLayer = mountain.Map.GetLayer(I("Back"));

        if (this.tiles is null)
        {
            Texture2D texture = this.Mod.Helper.ModContent.Load<Texture2D>("assets/tollbooth.png");

            // Create a tilesheet
            TileSheet sheet = new TileSheet(
                id: WarpShop.BoothSheetId,
                map: mountain.Map,
                imageSource: this.Mod.Helper.ModContent.GetInternalAssetName("assets/tollbooth.png").BaseName,
                sheetSize: new xTile.Dimensions.Size(texture.Width / 16, texture.Height / 16),
                tileSize: new xTile.Dimensions.Size(16, 16)
            );

            mountain.Map.AddTileSheet(sheet);

            // VERY IMPORTANT: refresh the map’s internal tilesheet references
            mountain.Map.LoadTileSheets(Game1.mapDisplayDevice);

            int numTiles = (texture.Height / 16) * (texture.Width / 16);
            if (numTiles != WarpShop.BoothWidthInTiles * WarpShop.BoothHeightInTiles * WarpShop.NumAnimationFrames)
            {
                this.LogError($"Mismatch between number of tiles in the shop tilesheet and the number declared texture.Width/16={texture.Width/16} texture.height/16={texture.Height/16}");
            }
            this.tiles = new StaticTile[numTiles];
            for (int i = 0; i < texture.Height / 16 * texture.Width / 16; ++i)
            {
                this.tiles[i] = new StaticTile(buildingsLayer, sheet, BlendMode.Alpha, i++);
            }
        }

        for (int dy = 0; dy < 80 / 16; ++dy) // The png is 80px tall
        {
            for (int dx = 0; dx < 64 / 16; ++dx) // and 64px wide.
            {
                int x = WarpShop.BoothLocationX + dx;
                int y = WarpShop.BoothLocationY + dy;

                // Clear any existing stuff.
                Vector2 xy = new Vector2(x,y);
                // Remove terrain features (trees, bushes, grass, hoe dirt, etc.)
                mountain.terrainFeatures.Remove(xy);

                // Remove objects (seeds, debris, placed items)
                mountain.objects.Remove(xy);

                // Remove large terrain features (stumps, logs, boulders)
                for (int i = mountain.largeTerrainFeatures.Count - 1; i >= 0; i--)
                {
                    var ltf = mountain.largeTerrainFeatures[i];

                    if (ltf.getBoundingBox().Intersects(new Rectangle((x) * 64, (y) * 64, 64, 64)))
                        mountain.largeTerrainFeatures.RemoveAt(i);
                }

                // Prevent future tree growth.
                var backTile = backLayer.Tiles[x, y];
                if (backTile is null)
                {
                    var backTileSheet = mountain.Map.GetTileSheet("spring_outdoorsTileSheet") ?? mountain.Map.TileSheets.First();
                    backTile = new StaticTile( backLayer, backTileSheet, BlendMode.Alpha, 0 /* doesn't matter */);
                    backLayer.Tiles[x, y] = backTile;
                }
                backTile.Properties[I("Diggable")] = new xTile.ObjectModel.PropertyValue(I("F"));
            }
        }

        this.SetBoothFrame(BoothAnimationFrame.Empty);

        /* Something from warpnetwork:

                                 Point tilePos = warp.Position;
           if (id == "farm")
           {
               Utils.TryGetActualFarmPoint(ref tilePos, map.Data, Name);
           }
           var spot = new Location(tilePos.X, tilePos.Y).Above;

           ModEntry.monitor.Log($"Adding access point for destination '{id}' @ {spot.X}, {spot.Y}");

           Tile tile = Buildings.Tiles[spot];
           if (tile is null)
               ModEntry.monitor.Log($"No tile in building layer, could not add access point: '{id}' @ {spot.X}, {spot.Y}", LogLevel.Warn);
           else
               tile.Properties["Action"] = "warpnetwork " + id;
*/
    }
}
