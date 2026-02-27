namespace NermNermNerm.Warpinator;

public class ModEntry
    : Mod, ISimpleLog
{
    public const string MarionBerryToolId = "NermNermNerm.Warpinator.MarionBerry";
    public const string MarionBerryToolQiid = ItemRegistry.type_tool + ModEntry.MarionBerryToolId;
    public const string TollReceiptObjectId = "NermNermNerm.Warpinator.TollReceipt";
    public const string TollReceiptObjectQiid = ItemRegistry.type_object + ModEntry.TollReceiptObjectId;
    public const string FasterWarpObjectId = "NermNermNerm.Warpinator.FasterWarp";
    public const string FasterWarpObjectQiid = ItemRegistry.type_object + ModEntry.FasterWarpObjectId;
    public const string OtherPlacesUpgradeObjectId = "NermNermNerm.Warpinator.OtherPlacesUpgrade";
    public const string OtherPlacesUpgradeObjectQiid = ItemRegistry.type_object + ModEntry.OtherPlacesUpgradeObjectId;
    public const string TotemWalletUpgradeObjectId = "NermNermNerm.Warpinator.TotemWalletUpgrade";
    public const string TotemWalletUpgradeObjectQiid = ItemRegistry.type_object + ModEntry.TotemWalletUpgradeObjectId;
    public const string ObeliskIntegrationObjectId = "NermNermNerm.Warpinator.ObeliskIntegration";
    public const string ObeliskIntegrationObjectQiid = ItemRegistry.type_object + ModEntry.ObeliskIntegrationObjectId;
    public const string ReturnUpgradeObjectId = "NermNermNerm.Warpinator.ReturnUpgrade";
    public const string ReturnUpgradeObjectQiid = ItemRegistry.type_object + ModEntry.ReturnUpgradeObjectId;

    public const string TwoTileSpritesPseudoPath = "Mods/NermNermNerm/Warpinator/Sprites";
    public const string OneTileSpritesPseudoPath = "Mods/NermNermNerm/Warpinator/1x1Sprites";
    public const string PortraitsPseudoPath = "Mods/NermNermNerm/Warpinator/Portraits";

    public readonly Powers Powers = new Powers();

    public static ModEntry Instance = null!;

    public static ModConfig Config = null!;
    public ModConfigMenu ConfigMenu = new ModConfigMenu();

    public Harmony Harmony = null!;

    public readonly TotemInventory TotemInventory = new();
    public readonly CraftedTotemMultiplier CraftedTotemMultiplier = new();
    public readonly HomeSpot HomeSpot = new();
    public readonly SlowWarp SlowWarp = new();
    public readonly WarpShop WarpShop = new();
    public readonly Marionberry Marionberry = new();

    public ModEntry() { }

    public override void Entry(IModHelper helper)
    {
        Instance = this;

        Initialize(this);
        this.Helper.Events.Content.LocaleChanged += (_, _) => this.Helper.GameContent.InvalidateCache("Data/Objects");

        this.Harmony = new Harmony(this.ModManifest.UniqueID);

        Config = this.Helper.ReadConfig<ModConfig>();
        this.ConfigMenu.Entry(this);
        this.Powers.Entry(this);

        Patches.Patch(this);

        this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;

        this.TotemInventory.Entry(this);
        this.CraftedTotemMultiplier.Entry(this);
        this.HomeSpot.Entry(this);
        this.SlowWarp.Entry(this);
        this.WarpShop.Entry(this);
        this.Marionberry.Entry(this);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(OneTileSpritesPseudoPath))
        {
            e.LoadFromModFile<Texture2D>("assets/1x1_Sprites.png", AssetLoadPriority.Exclusive);
        }
        // else if (e.NameWithoutLocale.IsEquivalentTo(ModEntry.PortraitsPseudoPath))
        // {
        //     e.LoadFromModFile<Texture2D>("assets/portraits.png", AssetLoadPriority.Exclusive);
        // }
        else if (e.NameWithoutLocale.IsEquivalentTo(TwoTileSpritesPseudoPath))
        {
            e.LoadFromModFile<Texture2D>("assets/Sprites.png", AssetLoadPriority.Exclusive);
        }

        // else if (e.NameWithoutLocale.StartsWith("Characters/Dialogue/"))
        // {
        //     e.Edit(editor =>
        //     {
        //         ConversationKeys.EditAssets(e.NameWithoutLocale, editor.AsDictionary<string, string>().Data);
        //     });
        // }
    }



    public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
    {
        if (isOnceOnly)
        {
            this.Monitor.LogOnce(message, level);
        }
        else
        {
            this.Monitor.Log(message, level);
        }
    }

    public bool IsWandUseBlocked => Game1.eventUp
                                 || Game1.isFestival()
                                 || Game1.fadeToBlack
                                 || Game1.player.swimming.Value
                                 || Game1.player.bathingClothes.Value
                                 || Game1.player.onBridge.Value;

    public IEnumerable<GameLocation> SlowWarpDestinations
    {
        get
        {
            yield return Game1.getFarm();

            if (this.Marionberry.HasOtherWarps)
            {
                // TODO: Find a way to let mods specify walkable spots or
                //   find all spots that are reachable by walking and have
                //   warp totem map items.
                yield return Game1.getLocationFromName(I("Beach"));
                yield return Game1.getLocationFromName(I("Mountain"));
            }
        }
    }

    public void UseWandWithMenu()
    {
        Game1.activeClickableMenu = new WarpMenu(this);
    }
}
