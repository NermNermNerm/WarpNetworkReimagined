namespace WarpNetworkReimagined;

using StardewValley.Extensions;

public class Marionberry : ModLet
{
    public class ModDataKeys
    {
        public const string HasFasterWarp = "WarpNetworkReimagined.HasFasterWarp";
        public const string HasOtherWarps = "WarpNetworkReimagined.HasOtherWarps";
        public const string HasTotemWallet = "WarpNetworkReimagined.HasTotemWallet";
        public const string HasObeliskIntegration = "WarpNetworkReimagined.HasObeliskIntegration";
        public const string HasReturn = "WarpNetworkReimagined.HasReturn";
    }

    public const string MarionberryId = "WarpNetworkReimagined.MarionBerry";
    public const string MarionberryQiid = ItemRegistry.type_object + Marionberry.MarionberryId;

    public bool HasFasterWarpPower => Game1.player.modData.ContainsKey(ModDataKeys.HasFasterWarp);
    public bool HasOtherWarps => Game1.player.modData.ContainsKey(ModDataKeys.HasOtherWarps);
    public bool HasTotemWallet => Game1.player.modData.ContainsKey(ModDataKeys.HasTotemWallet);
    public bool HasObeliskIntegration => Game1.player.modData.ContainsKey(ModDataKeys.HasObeliskIntegration);
    public bool HasReturn => Game1.player.modData.ContainsKey(ModDataKeys.HasReturn);

    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;

        var method = typeof(StardewValley.Object).GetMethod(nameof(StardewValley.Object.performUseAction));
        this.Mod.Harmony.Patch(method,
            new HarmonyMethod(typeof(Marionberry), nameof(Marionberry.PerformUseActionPrefix)));
    }

    private static bool PerformUseActionPrefix(GameLocation location, StardewValley.Object __instance, ref bool __result)
    {
        if (__instance.QualifiedItemId != Marionberry.MarionberryQiid)
        {
            return true;
        }

        // Pigeoned this up from what's in the decompiled code to make it so we don't let the marionberry get used in
        //  the same conditions other objects can't get used.  Didn't try to make the code prettier, so we can be sure
        //  we're doing the same shenanigans.
        if (!Game1.player.canMove || __instance.isTemporarilyInvisible)
            return true;
        bool flag = !Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack && !Game1.player.swimming.Value && !Game1.player.bathingClothes.Value && !Game1.player.onBridge.Value;
        if (!flag)
            return true;
        // end copypasta

        Game1.activeClickableMenu = new WarpMenu(ModEntry.Instance);
        return false;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(editor =>
            {
                EditObjectAssets(editor.AsDictionary<string, ObjectData>().Data);
            });
        }
    }

    private static void EditObjectAssets(IDictionary<string, ObjectData> data)
    {
        // TODO: Move to WarpShop
        data[ModEntry.TollReceiptObjectId] = new ObjectData()
        {
            Name = I("Toll Receipt"),
            DisplayName = L("Toll Receipt"),
            Description = L("Proof that you paid your toll.  Good for one day only."),
            Texture = ModEntry.OneTileSpritesPseudoPath,
            SpriteIndex = 1,
        };
        //

        data[Marionberry.MarionberryId] = new ObjectData()
        {
            Name = I("Marionberry"),
            DisplayName = L("Marionberry Teleporter"),
            Description = L("Norvin the bridge troll's old Marionberry(tm) teleporter."),
            Texture = ModEntry.OneTileSpritesPseudoPath,
            SpriteIndex = 7,
            CanBeGivenAsGift = false,
            CanBeTrashed = false,
        };

        data[ModEntry.FasterWarpObjectId] = new ObjectData()
        {
            Name = I("Faster Warp Upgrade"),
            DisplayName = L("Faster Warp Upgrade"),
            Description = L("Reduces the time your Marionberry's 'slow-warp' takes."),
            Texture = ModEntry.OneTileSpritesPseudoPath,
            SpriteIndex = 2,
        };
        data[ModEntry.OtherPlacesUpgradeObjectId] = new ObjectData()
        {
            Name = I("Beach and Mountain Upgrade"),
            DisplayName = L("Valley Slow-Warp Upgrade"),
            Description = L("Allows slow-warp to locations around the valley that have a warp shrine."),
            Texture = ModEntry.OneTileSpritesPseudoPath,
            SpriteIndex = 3,
        };
        data[ModEntry.TotemWalletUpgradeObjectId] = new ObjectData()
        {
            Name = I("Totem Wallet Upgrade"),
            DisplayName = L("Totem Wallet Upgrade"),
            Description = L("Enables your Marionberry's totem wallet."),
            Texture = ModEntry.OneTileSpritesPseudoPath,
            SpriteIndex = 4,
        };
        data[ModEntry.ObeliskIntegrationObjectId] = new ObjectData()
        {
            Name = I("Obelisk Integration Upgrade"),
            DisplayName = L("Obelisk Integration Upgrade"),
            Description = L("Enables your Marionberry to instantaneously warp you to your farm and any location to which you've built an obelisk for."),
            Texture = ModEntry.OneTileSpritesPseudoPath,
            SpriteIndex = 5,
        };
        data[ModEntry.ReturnUpgradeObjectId] = new ObjectData()
        {
            Name = I("Return To Last Warp Upgrade"),
            DisplayName = L("Return To Last Warp Upgrade"),
            Description = L("Enables your Marionberry to warp you back to the last location you warped from."),
            Texture = ModEntry.OneTileSpritesPseudoPath,
            SpriteIndex = 6,
        };
    }
}
