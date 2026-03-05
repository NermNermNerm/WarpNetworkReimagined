namespace WarpNetworkReimagined;

using StardewValley.Extensions;

public class Marionberry : ModLet
{
    private class ModDataKeys
    {
        public const string HasFasterWarp = "WarpNetworkReimagined.HasFasterWarp";
        public const string HasOtherWarps = "WarpNetworkReimagined.HasOtherWarps";
        public const string HasTotemWallet = "WarpNetworkReimagined.HasTotemWallet";
        public const string HasObeliskIntegration = "WarpNetworkReimagined.HasObeliskIntegration";
        public const string HasReturn = "WarpNetworkReimagined.HasReturn";
    }

    public bool HasFasterWarpPower => Game1.player.modData.ContainsKey(ModDataKeys.HasFasterWarp);
    public bool HasOtherWarps => Game1.player.modData.ContainsKey(ModDataKeys.HasOtherWarps);
    public bool HasTotemWallet => Game1.player.modData.ContainsKey(ModDataKeys.HasTotemWallet);
    public bool HasObeliskIntegration => Game1.player.modData.ContainsKey(ModDataKeys.HasObeliskIntegration);
    public bool HasReturn => Game1.player.modData.ContainsKey(ModDataKeys.HasReturn);

    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        mod.Harmony.Patch(
            typeof(StardewValley.Object).GetMethod(nameof(StardewValley.Object.actionWhenPurchased)),
            new(typeof(Marionberry), nameof(Marionberry.PrefixActionWhenPurchased)));

        this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
    }

    private static bool PrefixActionWhenPurchased(StardewValley.Object __instance, ref bool __result)
    {
        void gotPowerItem(string modDataKey, string hudMessage)
        {
            Game1.player.modData[modDataKey] = I("T");
            Game1.drawObjectDialogue(hudMessage); // Closes the existing menu
            Game1.player.holdUpItemThenMessage(__instance, false);
            Game1.Multiplayer.broadcastSprites(
                Game1.currentLocation,
                new TemporaryAnimatedSprite(10, Game1.player.Position - new Vector2(0,128-12), Color.LightBlue,
                    layerDepth: (Game1.player.GetBoundingBox().Bottom + 3000) / 10000f)
            );
            Game1.playSound("discoverMineral");
        }

        switch (__instance.ItemId)
        {
            case ModEntry.FasterWarpObjectId:
                gotPowerItem(ModDataKeys.HasFasterWarp, L("Your Marionberry's warping speed has been upgraded."));
                __result = true;
                return false; // skip vanilla behavior
            case ModEntry.OtherPlacesUpgradeObjectId:
                gotPowerItem(ModDataKeys.HasOtherWarps, L("Your Marionberry can now slow-warp you to other places in the valley."));
                __result = true;
                return false; // skip vanilla behavior
            case ModEntry.ObeliskIntegrationObjectId:
                gotPowerItem(ModDataKeys.HasObeliskIntegration, L("Your Marionberry can now instantly warp you to the farm and take advantage of obelisks."));
                __result = true;
                return false; // skip vanilla behavior
            case ModEntry.TotemWalletUpgradeObjectId:
                gotPowerItem(ModDataKeys.HasTotemWallet, L("Your Marionberry now has a wallet for keeping your warp totems."));
                ModEntry.Instance.TotemInventory.CheckPlayerInventoryForTotems();
                __result = true;
                return false; // skip vanilla behavior
            case ModEntry.ReturnUpgradeObjectId:
                gotPowerItem(ModDataKeys.HasReturn, L("Your Marionberry can now return you to the place you warped from."));
                __result = true;
                return false; // skip vanilla behavior
        }

        return true; // let vanilla handle everything else
    }


    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
        {
            e.Edit(editor =>
            {
                EditToolAssets(editor.AsDictionary<string, ToolData>().Data);
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(editor =>
            {
                EditObjectAssets(editor.AsDictionary<string, ObjectData>().Data);
            });
        }
    }

    private static void EditToolAssets(IDictionary<string, ToolData> data)
    {
        data[ModEntry.MarionBerryToolId] = new ToolData
        {
            ClassName = I("Wand"),
            Name = I("Warp Home Gadget"),
            AttachmentSlots = 0,
            SalePrice = 0,
            DisplayName = L("teleporter"),
            Description = L("Norvin the bridge troll's old Marionberry(tm) teleporter."),
            Texture = ModEntry.OneTileSpritesPseudoPath,
            SpriteIndex = 7,
            MenuSpriteIndex = -1,
            UpgradeLevel = 0,
            ConventionalUpgradeFrom = null,
            UpgradeFrom = null,
            CanBeLostOnDeath = false,
            SetProperties = null,
        };
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
