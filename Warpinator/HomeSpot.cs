namespace NermNermNerm.Warpinator;

public class HomeSpot : ModLet
{
    public const string HomeSpotObjectId = "NermNermNerm.Warpinator.HomeSpot";
    public const int HomeSpotPrice = 5000; // in gold

    private const string HomeLocationModDataKey = "NermNermNerm.Warpinator.HomeSpot";

    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        mod.Helper.Events.Content.AssetRequested += this.OnAssetRequested;

        // Seems really unlikely that performUseAction would get changed in a future SDV release.
        mod.Harmony.Patch(
            typeof(StardewValley.Object).GetMethod(nameof(StardewValley.Object.performUseAction)),
            new(typeof(HomeSpot), nameof(PatchPerformUseAction)));

        // But this one is private, so we'll be careful with it.
        var warpForRealMethod = typeof(StardewValley.Object).GetMethod("totemWarpForReal", BindingFlags.NonPublic | BindingFlags.Instance);
        if (warpForRealMethod is null)
        {
            this.LogWarning($"This version of StardewValley has changed the method 'StardewValley.Object.totemWarpForReal', so the custom warp-farm location feature of this mod will not work.");
        }
        else
        {
            mod.Harmony.Patch(warpForRealMethod, new HarmonyMethod(typeof(HomeSpot), nameof(PatchTotemWarpForReal)));
        }

        var obWarpForRealMethod = typeof(StardewValley.Buildings.Building).GetMethod("obeliskWarpForReal", BindingFlags.NonPublic | BindingFlags.Static);
        if (obWarpForRealMethod is null)
        {
            this.LogWarning($"This version of StardewValley has changed the method 'StardewValley.Buildings.Building.obeliskWarpForReal', so the custom warp-farm location feature of this mod will not work.");
        }
        else
        {
            mod.Harmony.Patch(obWarpForRealMethod, new HarmonyMethod(typeof(HomeSpot), nameof(HomeSpot.PatchObeliskWarpForReal)));
        }
    }

    private static readonly Regex HomeSpotValueRegex = new(@"^(?<x>\d+),(?<y>\d+),(?<loc>.+)$", RegexOptions.Compiled);

    /// <summary>
    ///   Gets the location that the player has set to return to on the farm.  If the player has
    /// never set the location, it returns <code>(null,(0,0))</code>.
    /// </summary>
    public (GameLocation? location, Point tile) HomeLocation
    {
        // Save/Load from player's modData
        get
        {
            Game1.player.modData.TryGetValue(HomeSpot.HomeLocationModDataKey, out string valueAsString);
            if (valueAsString is null)
            {
                return default;
            }

            var match = HomeSpot.HomeSpotValueRegex.Match(valueAsString);
            if (!match.Success)
            {
                this.LogError($"player moddata[{HomeSpot.HomeLocationModDataKey}] is invalid: {valueAsString}");
                return default;
            }

            int x = int.Parse(match.Groups[I("x")].Value);
            int y = int.Parse(match.Groups[I("y")].Value);
            string locAsString = match.Groups[I("loc")].Value;
            var loc = Game1.getLocationFromName(locAsString);
            if (loc is null)
            {
                // getLocation doesn't seem to be able to find cabins...  Try to find it ourselves...
                loc = Game1.getFarm().buildings
                    .Select(b => b.GetIndoors())
                    .Where(b => b is not null)
                    .FirstOrDefault(b => b.Name == locAsString);
            }

            if (loc is null)
            {
                this.LogError($"player moddata[{HomeSpot.HomeLocationModDataKey}] has invalid location: {valueAsString}");
                return default;
            }

            return (loc, new Point(x, y));
        }

        set
        {
            if (value.location is null)
            {
                Game1.player.modData.Remove(HomeLocationModDataKey);
            }
            else
            {
                Game1.player.modData[HomeSpot.HomeLocationModDataKey] = IF($"{value.tile.X},{value.tile.Y},{value.location.Name}");
            }
        }
    }

    /// <summary>
    ///   Hijacks StardewValley.Object.performUseAction when the user is holding a warp location marker to
    ///   set the location of where the user warps to when warping to the farm.
    /// </summary>
    private static bool PatchPerformUseAction(StardewValley.Object __instance , ref bool __result)
    {
        if (__instance.ItemId != HomeSpot.HomeSpotObjectId)
        {
            return true;
        }

        if (Game1.currentLocation is Farm || Game1.currentLocation is FarmHouse || Game1.currentLocation is Cabin)
        {
            ModEntry.Instance.HomeSpot.HomeLocation = (Game1.currentLocation, Game1.player.TilePoint);
            Game1.playSound("jingle1");
            string message = Game1.currentLocation is Farm
                ? L("Your Marionberry will take you here when you warp to the farm.") // 'here' because it'll actually go to the specific x,y coordinate.
                : Game1.currentLocation is Cabin
                    ? L("Your Marionberry will take you to this cabin when you warp to the farm.")
                    : L("Your Marionberry will take you to the main farmhouse when you warp to the farm.");
            Game1.addHUDMessage(new HUDMessage(message, timeLeft: 5250) { messageSubject = __instance });
            Game1.player.removeItemFromInventory(__instance);
        }
        else
        {
            Game1.playSound("clank");
            Game1.addHUDMessage(new HUDMessage(L("You need to be on the farm or in a farmhouse or cabin to use this item."), timeLeft: 5250) { messageSubject = __instance });
        }

        // This means that the click was "handled".  If you don't set it and the player happens to be hovering
        // over something clickable (like furniture), it can result in picking up the furniture.
        __result = true;

        // Note that this cancels the base-game interaction and any other mod's patches.  However, it seems like
        // that should be a safe move, since other patches would have the same "return true if this object doesn't
        // apply to my mod" semantics that we have up at the top of the method.
        return false;
    }


    /// <summary>
    ///   Hijacks StardewValley.Object.totemWarpForReal (private) to make it so that warps to the farm get sent
    ///   to a place of the user's choosing rather than the totem.
    /// </summary>
    private static bool PatchTotemWarpForReal(StardewValley.Object __instance)
    {
        // Only do warp-totem farm.
        if (__instance.QualifiedItemId != "(O)688")
        {
            return true;
        }

        var warpTarget = ModEntry.Instance.HomeSpot.HomeLocation;
        if (warpTarget.location is null)
        {
            return true; // User hasn't overridden it, so let the base game put us where it will.
        }

        var lr = new LocationRequest(warpTarget.location.Name, warpTarget.location is not Farm, warpTarget.location);
        // Since we always warp to the front door of the farmhouse and cabins, face upwards so the farmer doesn't look wierd.
        // On the farm, since we're at a given x/y, might as well show our beautiful mug to the camera.
        Game1.warpFarmer( lr, warpTarget.tile.X, warpTarget.tile.Y, warpTarget.location is Farm ? 2 : 0);

        Game1.fadeToBlackAlpha = 0.99f;
        Game1.screenGlow = false;
        Game1.player.temporarilyInvincible = false;
        Game1.player.temporaryInvincibilityTimer = 0;
        Game1.displayFarmer = true;
        return false; // Don't run main function; we took care of it.
    }

    private static bool PatchObeliskWarpForReal(string destination)
    {
        // NOTE!  The method we're patching takes an argument, 'Farmer who'...  But if you look at the
        //  implementation of that method, it ignores it -- the money-shot call, Game1.warpFarmer always
        //  warps Game1.player.
        if (destination != I("Farm") || Game1.player.ActiveItem.QualifiedItemId != ModEntry.MarionBerryToolQiid)
        {
            return true; // Not using the Marionberry or not going to farm.
        }

        var warpTarget = ModEntry.Instance.HomeSpot.HomeLocation;
        if (warpTarget.location is null)
        {
            return true; // User hasn't overridden it, so let the base game put us where it will.
        }

        var lr = new LocationRequest(warpTarget.location.Name, warpTarget.location is not Farm, warpTarget.location);
        // Since we always warp to the front door of the farmhouse and cabins, face upwards so the farmer doesn't look wierd.
        // On the farm, since we're at a given x/y, might as well show our beautiful mug to the camera.

        Game1.warpFarmer( lr, warpTarget.tile.X, warpTarget.tile.Y, warpTarget.location is Farm ? 2 : 0);

        // Note this code is identical to PatchTotemWarpForReal, but what we're really striving for is
        // to be identical to Building.obeliskWarpForReal
        Game1.fadeToBlackAlpha = 0.99f;
        Game1.screenGlow = false;
        Game1.player.temporarilyInvincible = false;
        Game1.player.temporaryInvincibilityTimer = 0;
        Game1.displayFarmer = true;
        return false; // Don't run main function; we took care of it.
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(editor =>
            {
                this.EditAssets(editor.AsDictionary<string, ObjectData>().Data);
            });
        }
    }

    private void EditAssets(IDictionary<string, ObjectData> objects)
    {
        objects[HomeSpot.HomeSpotObjectId] = new ObjectData()
        {
            Name = HomeSpot.HomeSpotObjectId,
            DisplayName = L("Home Totem"),
            Description = L("Use this to set where on your farm you'd like your Marionberry to warp you to when you warp to the farm."),
            Type = I("Crafting"), // dunno what would work here.  "Crafting" is what rain totem uses.
            Category = 0, // again, same as rain totem.
            Price = HomeSpot.HomeSpotPrice, // todo: normalize this and make sure it shows up as this price in the shop.
            Texture = ModEntry.OneTileSpritesPseudoPath,
            SpriteIndex = 6, // the green chip
            ContextTags = ["not_giftable", "not_placeable", "prevent_loss_on_death"],
        };
    }
}
