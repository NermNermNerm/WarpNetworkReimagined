using System.Diagnostics;
using StardewModdingAPI.Utilities;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData.Shops;
using StardewValley.Mods;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace WarpNetworkReimagined;

public class WarpShop  : ModLet
{
    private const string NorvinsShopId = "WarpNetworkReimagined-Norvin";
    private const string OpenShopTileAction = "OpenNorvinsShop";

    public const string IntroEventId = "WarpNetworkReimagined.NorvinIntro";

    private int proximityCounter = 0;
    // private int facingCounter = 0;
    private int idleCounter = 0;

    // ISSUE:  Could be that a mod would want to change this - so it'd be better if it was loaded from content somewhere.
    private const int BoothLocationX = 84;
    private const int BoothLocationY = 20;

    private const int BoothWidthInTiles = 4;
    private const int BoothHeightInTiles = 5;

    private const string BoothSheetId = "WarpNetworkReimagined.TrollBooth";

    private const int MessageDurationInMs = 4500;
    private const int FadeIntervalInMs = 500;


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

    public class EventCommands
    {
        public const string NorvinWarpIn = "WarpNet2_NorvinWarpIn";
        public const string NorvinWarpOut = "WarpNet2_NorvinWarpOut";
        public const string NorvinSay = "WarpNet2_NorvinSay";
        public const string NorvinFaceDirection = "WarpNet2_NorvinFaceDirection";
    }

    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        mod.Helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        mod.Helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        mod.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        mod.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
        mod.Helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        mod.Helper.Events.Display.RenderedStep += this.DisplayOnRenderedStep;
        GameLocation.RegisterTileAction(WarpShop.OpenShopTileAction, this.OpenNorvinShop);

        Event.RegisterCommand(EventCommands.NorvinWarpIn, this.EventNorvinWarpIn);
        Event.RegisterCommand(EventCommands.NorvinWarpOut, this.EventNorvinWarpOut);
        Event.RegisterCommand(EventCommands.NorvinSay, this.EventNorvinSay);
        Event.RegisterCommand(EventCommands.NorvinFaceDirection, this.EventNorvinFaceDirection);
    }

    private class FloatingText
    {
        public string Text = "";
        public Vector2 WorldPosition;
        public float Alpha = 0f;
        public int Timer = 0;
    }

    private readonly PerScreen<FloatingText?> psNorvinText = new ();
    private FloatingText? norvinText
    {
        get => this.psNorvinText.Value;
        set => this.psNorvinText.Value = value;
    }

    private void DisplayOnRenderedStep(object? sender, RenderedStepEventArgs e)
    {
        if (e.Step == RenderSteps.World_AlwaysFront && this.norvinText is not null)
        {
            if (this.norvinText != null)
            {
                Vector2 screenPos = Game1.GlobalToLocal(this.norvinText.WorldPosition);
                SpriteText.drawStringWithScrollCenteredAt(
                    e.SpriteBatch,
                    this.norvinText.Text,
                    (int)screenPos.X,
                    (int)screenPos.Y,
                    alpha: this.norvinText.Alpha,
                    scrollType: 1,
                    layerDepth: 1f // above everything but UI
                );
            }
        }
    }

    private bool HasPaidToll
    {
        get => Game1.player.modData.ContainsKey("WarpNetworkReimagined.HasPaidToll");
        set
        {
            if (value)
            {
                Game1.player.modData["WarpNetworkReimagined.HasPaidToll"] = I("T");
            }
            else
            {
                Game1.player.modData.Remove("WarpNetworkReimagined.HasPaidToll");
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

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.PrepareTileSheet();
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

        if (Game1.player.Items.Any(i => i?.QualifiedItemId == Marionberry.MarionberryQiid))
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
            stock[ItemRegistry.Create(Marionberry.MarionberryQiid)] = new ItemStockInformation(1000, 1);
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

        void gotPowerItem(string modDataKey, string hudMessage)
        {
            Game1.player.modData[modDataKey] = I("T");

            // This is a pretty spicy bit of code.  Setting heldItem to null is crucial because
            // otherwise, when drawObjectDialog runs, needs to put up its own "menu", which means
            // it wants to close the one that's up right now, and that means it'll try and complete
            // the sale of the "heldItem".  Setting it to null prevents it from doing that
            // completion here (if we keep on doing this drawObjectDialogue animation) and also
            // if we drop it and find some other way to animate the sale.
            (Game1.activeClickableMenu as ShopMenu)!.heldItem = null;
            Game1.drawObjectDialogue(hudMessage); // Closes the existing menu

            var item = ItemRegistry.Create(salable.QualifiedItemId);
            Game1.player.holdUpItemThenMessage(item, false);
            Game1.Multiplayer.broadcastSprites(
                Game1.currentLocation,
                new TemporaryAnimatedSprite(10, Game1.player.Position - new Vector2(0,128-12), Color.LightBlue,
                    layerDepth: (Game1.player.GetBoundingBox().Bottom + 3000) / 10000f)
            );
            Game1.playSound("discoverMineral");
        }

        switch (salable.QualifiedItemId)
        {
            case ModEntry.FasterWarpObjectQiid:
                gotPowerItem(Marionberry.ModDataKeys.HasFasterWarp, L("Your Marionberry's warping speed has been upgraded."));
                return true; // If we hadn't already closed the menu, this would cause the menu to close.
            case ModEntry.OtherPlacesUpgradeObjectQiid:
                gotPowerItem(Marionberry.ModDataKeys.HasOtherWarps, L("Your Marionberry can now slow-warp you to other places in the valley."));
                return true; // ditto
            case ModEntry.ObeliskIntegrationObjectQiid:
                gotPowerItem(Marionberry.ModDataKeys.HasObeliskIntegration, L("Your Marionberry can now instantly warp you to the farm and take advantage of obelisks."));
                return true; // ditto
            case ModEntry.TotemWalletUpgradeObjectQiid:
                gotPowerItem(Marionberry.ModDataKeys.HasTotemWallet, L("Your Marionberry now has a wallet for keeping your warp totems."));
                ModEntry.Instance.TotemInventory.CheckPlayerInventoryForTotems();
                return true; // ditto
            case ModEntry.ReturnUpgradeObjectQiid:
                gotPowerItem(Marionberry.ModDataKeys.HasReturn, L("Your Marionberry can now return you to the place you warped from."));
                return true; // ditto
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
        // TODO: Used to have `Game1.eventUp` in here, but perhaps what we want is to say 'Game1.eventUp && event is not our event'.
        if (!Game1.hasLoadedGame || !Context.IsWorldReady || Game1.paused || Game1.activeClickableMenu is not null || Game1.currentLocation?.Name != "Mountain")
        {
            this.norvinText = null;
            return;
        }

        var mountain = Game1.currentLocation;

        // 1) Proximity check (once per second)
        if (!Game1.eventUp && ++this.proximityCounter >= 60  && this.boothAnimations.Count == 0)
        {
            this.proximityCounter = 0;

            if (this.IsNorvinPresent)
            {
                // Norvin leaves when the players are far away
                var nearestFarmer = Utility.isThereAFarmerWithinDistance(new Vector2(WarpShop.BoothLocationX + (WarpShop.BoothWidthInTiles>>1), WarpShop.BoothLocationY + (WarpShop.BoothHeightInTiles>>1)), 20, mountain);
                if (nearestFarmer is null)
                {
                    this.DoWarpOutAnimation();
                }
            }
            else if (this.Mod.CutScenes.HasPlayerSeenIntroEvent)
            {
                // Norvin appears when the players are < 12 tiles away
                var nearestFarmer = Utility.isThereAFarmerWithinDistance(new Vector2(WarpShop.BoothLocationX + (WarpShop.BoothWidthInTiles>>1), WarpShop.BoothLocationY + (WarpShop.BoothHeightInTiles>>1)), 12, mountain);

                if (nearestFarmer is not null)
                {
                    this.DoWarpInAnimation();
                }
            }
        }

        if ((this.currentAnimationFrame == BoothAnimationFrame.Present || this.currentAnimationFrame == BoothAnimationFrame.EyesLeft)
            && Game1.random.Next(60 * 5) == 0 && this.boothAnimations.Count == 0)
        {
            // Blink about every 5 seconds
            this.boothAnimations.AddRange([
                new BoothAnimation(BoothAnimationFrame.Blink, 20),
                new BoothAnimation(this.currentAnimationFrame, 20),
                ]);
        }

        // Update yelling
        if (this.norvinText != null)
        {
            this.norvinText.Timer -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;

            if (this.norvinText.Timer < WarpShop.FadeIntervalInMs)
                this.norvinText.Alpha = this.norvinText.Timer / (float)WarpShop.FadeIntervalInMs;
            else if (this.norvinText.Alpha < 1f)
            {
                this.norvinText.Alpha = Math.Min(1f, (WarpShop.MessageDurationInMs-this.norvinText.Timer) / (float)WarpShop.FadeIntervalInMs);
            }

            if (this.norvinText.Timer <= 0)
                this.norvinText = null;
        }

        // Maybe yell at the player
        if (!Game1.eventUp && this.IsNorvinPresent && this.norvinText is null && this.boothAnimations.Count == 0 && !this.HasPaidToll && Game1.random.Next(60 * 15) == 0)
        {
            var nearestFarmer = Utility.isThereAFarmerWithinDistance(new Vector2(WarpShop.BoothLocationX + (WarpShop.BoothWidthInTiles>>1), WarpShop.BoothLocationY + (WarpShop.BoothHeightInTiles>>1)), 25, mountain);
            int durationInTicks = WarpShop.MessageDurationInMs * 60 / 1000;
            if (nearestFarmer == Game1.player && nearestFarmer.Tile.X < WarpShop.BoothLocationX)
            {
                this.boothAnimations.AddRange([
                    new BoothAnimation(BoothAnimationFrame.EyesLeft, durationInTicks),
                    new BoothAnimation(this.currentAnimationFrame, 10),
                ]);
            }
            else if (nearestFarmer == Game1.player && nearestFarmer.Tile.X > WarpShop.BoothLocationX+6)
            {
                this.boothAnimations.AddRange([
                    new BoothAnimation(BoothAnimationFrame.FaceRight, durationInTicks),
                    new BoothAnimation(this.currentAnimationFrame, 10),
                ]);
            }

            string text = Game1.random.ChooseFrom([
                L("HEY YOU! PAY THE TOLL!"),
                L("You gotta pay the toll!"),
                L("Wanna cross the bridge?  Pay the toll!"),
                L("Toll first! Walking later!"),
                L("Hold it! Toll booth, buddy!")
            ]);
            this.norvinText = new FloatingText
            {
                Text = text,
                WorldPosition = new Vector2(WarpShop.BoothLocationX*64+128, WarpShop.BoothLocationY*64+64),
                Alpha = 0f,
                Timer = WarpShop.MessageDurationInMs
            };
        }

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

    private void DoWarpInAnimation()
    {
        this.boothAnimations.AddRange([
            new BoothAnimation(BoothAnimationFrame.Poof1, 5),
            new BoothAnimation(BoothAnimationFrame.Poof2, 5),
            new BoothAnimation(BoothAnimationFrame.Poof3, 5),
            new BoothAnimation(BoothAnimationFrame.Poof4, 5),
            new BoothAnimation(BoothAnimationFrame.Poof5, 5),
            new BoothAnimation(BoothAnimationFrame.Present, 1),
        ]);
        Game1.playSound("clubSmash");
        // Game1.currentLocation.localSound(); would be a better choice, as we could focus it on Norvin's shop...  but... too lazy.
    }

    private void DoWarpOutAnimation()
    {
        this.boothAnimations.AddRange([
            new BoothAnimation(BoothAnimationFrame.Poof5, 5),
            new BoothAnimation(BoothAnimationFrame.Poof4, 5),
            new BoothAnimation(BoothAnimationFrame.Poof3, 5),
            new BoothAnimation(BoothAnimationFrame.Poof2, 5),
            new BoothAnimation(BoothAnimationFrame.Poof1, 5),
            new BoothAnimation(BoothAnimationFrame.Empty, 1),
        ]);
        Game1.playSound("thudStep");
        // Game1.currentLocation.localSound(); would be a better choice, as we could focus it on Norvin's shop...  but... too lazy.
    }

    private void PrepareTileSheet()
    {
        var mountain = Game1.getLocationFromName("Mountain");
        var buildingsLayer = mountain.Map.GetLayer(I("Buildings"));
        var backLayer = mountain.Map.GetLayer(I("Back"));

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

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // In this event, we draw the booth and do other prep-work for it.
        var mountain = Game1.getLocationFromName("Mountain");
        var backLayer = mountain.Map.GetLayer(I("Back"));

        for (int dy = 0; dy < 80 / 16; ++dy) // The png is 80px tall
        {
            for (int dx = 0; dx < 64 / 16; ++dx) // and 64px wide.
            {
                int x = WarpShop.BoothLocationX + dx;
                int y = WarpShop.BoothLocationY + dy;

                // Clear any existing stuff.
                Vector2 xy = new Vector2(x, y);
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
                    var backTileSheet = mountain.Map.GetTileSheet("spring_outdoorsTileSheet") ??
                                        mountain.Map.TileSheets.First();
                    backTile = new StaticTile(backLayer, backTileSheet, BlendMode.Alpha, 0 /* doesn't matter */);
                    backLayer.Tiles[x, y] = backTile;
                }

                backTile.Properties[I("Diggable")] = new xTile.ObjectModel.PropertyValue(I("F"));
            }
        }

        this.SetBoothFrame(BoothAnimationFrame.Empty);
    }


    private void EventNorvinWarpIn(Event @event, string[] args, EventContext context)
    {
        if (this.boothAnimations.Count > 0)
        {
            this.boothAnimations.Clear();
            this.LogWarning($"Error in event -- WarpIn called when other animations are playing");
        }
        this.DoWarpInAnimation();
        @event.CurrentCommand++;
    }

    private void EventNorvinWarpOut(Event @event, string[] args, EventContext context)
    {
        this.boothAnimations.Clear(); // We don't complain about existing ones, as they might be blinks
        this.DoWarpOutAnimation();
        @event.CurrentCommand++;
    }

    private void EventNorvinSay(Event @event, string[] args, EventContext context)
    {
        if (args.Length != 2)
        {
            this.LogError($"Incorrect arguments given to 'nSay', {args.Length}.");
            @event.CurrentCommand++;
            return;
        }

        string text = args[1];
        this.norvinText = new FloatingText
        {
            Text = text,
            WorldPosition = new Vector2(WarpShop.BoothLocationX*64+128, WarpShop.BoothLocationY*64+64),
            Alpha = 0f,
            Timer = WarpShop.MessageDurationInMs
        };

        @event.CurrentCommand++;
    }

    private void EventNorvinFaceDirection(Event @event, string[] args, EventContext context)
    {
        if (args.Length != 2)
        {
            this.LogError($"Incorrect arguments given to 'nFaceDirection', {args.Length}.");
            @event.CurrentCommand++;
            return;
        }

        int direction;
        if (!int.TryParse(args[1], out direction) || direction < 1 || direction > 3)
        {
            // Norvin doesn't have a way to face up, which is direction==0.
            this.LogError($"Invalid direction given to nFaceDirection, '{args[1]}'.  Should be between 1 and 3");
        }

        this.boothAnimations.Clear();
        this.SetBoothFrame(direction switch
        {
            0 => BoothAnimationFrame.Present, // Up -- actually we don't support this.
            1 => BoothAnimationFrame.FaceRight,
            2 => BoothAnimationFrame.Present,
            _ => BoothAnimationFrame.EyesLeft,
        });

        @event.CurrentCommand++;
    }
}
