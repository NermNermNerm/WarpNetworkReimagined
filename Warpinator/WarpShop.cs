using StardewValley.GameData.Shops;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace NermNermNerm.Warpinator;

public class WarpShop  : ModLet
{
    private const string NorvinsShopId = "Warpinator-Norvin";
    private const string OpenShopTileAction = "OpenNorvinsShop";

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

    private BoothAnimationFrame currentAnimationFrame = BoothAnimationFrame.Empty;

    private bool IsNorvinPresent => this.currentAnimationFrame >= BoothAnimationFrame.Present;

    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        mod.Helper.Events.GameLoop.DayStarted += this.GameLoopOnDayStarted;
        mod.Helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        mod.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
        mod.Helper.Events.GameLoop.DayEnding += OnDayEnding;

        GameLocation.RegisterTileAction(WarpShop.OpenShopTileAction, this.OpenNorvinShop);
    }

    private bool HasPaidToll
    {
        get => Game1.player.modData.ContainsKey("Warpinator.HasPaidToll");
        set
        {
            if (value)
            {
                Game1.player.modData["Warpinator.HasPaidToll"] = I("T");
            }
            else
            {
                Game1.player.modData.Remove("Warpinator.HasPaidToll");
            }
        }
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        this.HasPaidToll = false;
        // Shouldn't be possible to get more than one of these, so we can take the easy way out.
        Game1.player.Items.ReduceId(ModEntry.TollReceiptObjectId, 1);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
        {
            e.Edit(editor =>
            {
                this.EditShopAssets(editor.AsDictionary<string, ShopData>().Data);
            });
        }
    }

    private void EditShopAssets(IDictionary<string, ShopData> data)
    {
        data[I(WarpShop.NorvinsShopId)] = new ShopData()
        {
        };
    }

    private bool OpenNorvinShop(GameLocation _, string[] _1, Farmer farmer, Point _2)
    {
        var stock = new Dictionary<ISalable, ItemStockInformation>();

        void addStock(string itemQiid, int price, int quantity = 1)
        {
            stock[ItemRegistry.Create(itemQiid)] = new ItemStockInformation(price, quantity);
        }

        if (!this.HasPaidToll)
        {
            addStock(ModEntry.TollReceiptObjectId, 100);
        }

        if (Game1.player.Items.Any(i => i?.QualifiedItemId == ModEntry.MarionBerryToolQiid))
        {
            addStock(HomeSpot.HomeSpotObjectId, 50);

            if (!this.Mod.Marionberry.HasOtherWarps)
            {
                addStock(ModEntry.OtherPlacesUpgradeObjectQiid, 2000);
            }

            if (!this.Mod.Marionberry.HasFasterWarpPower)
            {
                addStock(ModEntry.FasterWarpObjectQiid, 25000);
            }

            if (!this.Mod.Marionberry.HasTotemWallet)
            {
                addStock(ModEntry.TotemWalletUpgradeObjectQiid, 20000);
            }

            if (!this.Mod.Marionberry.HasObeliskIntegration)
            {
                addStock(ModEntry.ObeliskIntegrationObjectQiid, 900000);
            }

            if (!this.Mod.Marionberry.HasReturn)
            {
                addStock(ModEntry.ReturnUpgradeObjectQiid, 1000000);
            }
        }
        else
        {
            stock[ItemRegistry.Create(ModEntry.MarionBerryToolQiid)] = new ItemStockInformation(1000, 1);
        }

        // Game1.playSound("clank");

        var menu = new ShopMenu(I(WarpShop.NorvinsShopId), stock)
        {
            portraitTexture = this.Mod.Helper.ModContent.Load<Texture2D>(SdvLocalize.I("assets/norvin-portrait.png")),
            potraitPersonDialogue = this.HasPaidToll ? SdvLocalize.L("Want some upgrades?") : SdvLocalize.L("Gonna pay that toll??"),
            onPurchase = this.OnPurchase
        };

        Game1.activeClickableMenu = menu;
        return true; // handled
    }

    private bool OnPurchase(ISalable salable, Farmer who, int countTaken, ItemStockInformation stock)
    {
        if (salable.QualifiedItemId == ModEntry.TollReceiptObjectQiid)
        {
            this.HasPaidToll = true;
        }

        return false;
    }

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

                (dy < 3 ? frontLayer : buildingsLayer).Tiles[x, y] = this.tiles![tileIndex];
            }
        }

        this.currentAnimationFrame = newFrame;
    }

    void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Game1.hasLoadedGame || !Context.IsWorldReady || Game1.eventUp || Game1.paused)
        {
            return;
        }

        var mountain = Game1.getLocationFromName("Mountain");

        // 1) Proximity check (once per second)
        if (++this.proximityCounter >= 60  && this.boothAnimations.Count == 0)
        {
            this.proximityCounter = 0;

            if (this.IsNorvinPresent)
            {
                // Norvin leaves when the players are > 20 tiles away
                var nearestFarmer = Utility.isThereAFarmerWithinDistance(new Vector2(WarpShop.BoothLocationX + (WarpShop.BoothWidthInTiles>>1), WarpShop.BoothLocationY + (WarpShop.BoothHeightInTiles>>1)), 30, mountain);
                if (nearestFarmer is null)
                {
                    this.boothAnimations.AddRange([
                        new BoothAnimation(BoothAnimationFrame.Poof5, 5),
                        new BoothAnimation(BoothAnimationFrame.Poof4, 5),
                        new BoothAnimation(BoothAnimationFrame.Poof3, 5),
                        new BoothAnimation(BoothAnimationFrame.Poof2, 5),
                        new BoothAnimation(BoothAnimationFrame.Poof1, 5),
                        new BoothAnimation(BoothAnimationFrame.Empty, 1),
                    ]);
                    mountain.playSound("thudStep");
                }
            }
            else
            {
                // Norvin appears when the players are < 12 tiles away
                var nearestFarmer = Utility.isThereAFarmerWithinDistance(new Vector2(WarpShop.BoothLocationX + (WarpShop.BoothWidthInTiles>>1), WarpShop.BoothLocationY + (WarpShop.BoothHeightInTiles>>1)), 12, mountain);

                if (nearestFarmer is not null)
                {
                    this.boothAnimations.AddRange([
                        new BoothAnimation(BoothAnimationFrame.Poof1, 5),
                        new BoothAnimation(BoothAnimationFrame.Poof2, 5),
                        new BoothAnimation(BoothAnimationFrame.Poof3, 5),
                        new BoothAnimation(BoothAnimationFrame.Poof4, 5),
                        new BoothAnimation(BoothAnimationFrame.Poof5, 5),
                        new BoothAnimation(BoothAnimationFrame.Present, 1),
                    ]);
                    mountain.playSound("explosion");
                }
            }
        }

        if (Game1.random.Next(60 * 5) == 0 && this.boothAnimations.Count == 0 && this.IsNorvinPresent)
        {
            // Blink about every 5 seconds
            this.boothAnimations.AddRange([
                new BoothAnimation(BoothAnimationFrame.Blink, 20),
                new BoothAnimation(this.currentAnimationFrame, 20),
                ]);
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
            if (this.currentAnimationFrame != currentAnimation.AnimationFrame)
            {
                this.SetBoothFrame(currentAnimation.AnimationFrame);
                this.idleCounter = 0;
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
        // In this event, we draw the booth and do other prep-work for it.
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
                this.tiles[i] = new StaticTile(buildingsLayer, sheet, BlendMode.Alpha, i);
            }

            for (int index = 0; index < WarpShop.NumAnimationFrames; ++index)
            {
                for (int x = 1; x <= 2; ++x)
                {
                    int y = 4;
                    var tile = this.tiles[
                        x + index * WarpShop.BoothWidthInTiles +
                        y * WarpShop.BoothWidthInTiles * WarpShop.NumAnimationFrames];
                    tile.Properties[I("Action")] = WarpShop.OpenShopTileAction;
                }
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
    }
}
