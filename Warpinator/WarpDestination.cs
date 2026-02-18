using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using NermNermNerm.Warpinator;
using StardewValley;
using StardewValley.TokenizableStrings;
using xTile.Dimensions;
using static NermNermNerm.Stardew.LocalizeFromSource.SdvLocalize;

namespace Warpinator;

public class WarpDestination
{
    public GameLocation? Target { get; }
    public StardewValley.Object? Totem { get; }
    public bool IsObeliskWarp => this.obeliskWarpCode is not null;

    private readonly string? obeliskWarpCode;

    /// <summary>
    ///   A destination for warping.
    /// </summary>
    /// <param name="target">
    /// If known, the target of the warp.  It will only be null if <paramref name="totem"/> is not null
    /// and its destination can't be inferred from the name.</param>
    /// <param name="totem">
    /// If not null, the target is reached via a warp totem in the player's wallet.  Else travel will
    /// be via slow warp or obelisk.
    /// </param>
    /// <param name="obeliskWarpCode">
    /// If not null, <paramref name="totem"/> will be null and travel should be accomplished by running
    /// <code>who.currentLocation.performAction(text, who, new Location((int) tileLocation.X, (int) tileLocation.Y)</code>
    /// on the text.
    /// If null and <paramref name="totem"/> is null, then travel should be accomplished via slow-warp.
    /// </param>
    public WarpDestination(GameLocation? target, StardewValley.Object? totem, string? obeliskWarpCode)
    {
        this.Target = target;
        this.Totem = totem;
        this.obeliskWarpCode = obeliskWarpCode;
    }

    public string ButtonTitle =>
        (this.Target is null)
            ? IF($"{this.Totem!.DisplayName} ({this.Totem.Stack})")
            : (this.IsObeliskWarp
                ? LF($"{LocationDisplayName(this.Target)} via obelisk")
                : (this.Totem is null
                    ? LF($"{LocationDisplayName(this.Target)} via slow-warp")
                    : LF($"{LocationDisplayName(this.Target)} via totem ({this.Totem!.Stack})")));

    private static readonly Regex obeliskDefaultActionPattern =
        new Regex(@"^\s*ObeliskWarp\s+(?<destinationLocation>[^\s]+) ", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static List<WarpDestination> GetDestinationsForMenu()
    {
        List<WarpDestination> destinations = new();

        List<string> obeliskLocationNames = new();
        if (ModEntry.Instance.IsObeliskUseEnabled)
        {
            // This is pretty much a fallback, in case the user hasn't set a warp spot for the farm.
            var frontDoorSpot = Utility.getHomeOfFarmer(Game1.player).getFrontDoorSpot();
            obeliskLocationNames.Add(I("Farm"));
            destinations.Add(new WarpDestination(
                    Game1.getFarm(),
                    null,
                    IF($"ObeliskWarp Farm {frontDoorSpot.X} {frontDoorSpot.Y} false")
                ));
            foreach (var building in Game1.getFarm().buildings)
            {
                var data = building.GetData();
                if (data is null) continue;

                string? defaultAction = data.DefaultAction;
                if (defaultAction is not null)
                {
                    defaultAction = TokenParser.ParseText(defaultAction); // I can't imagine how this would do anything for obelisks, but the base game does it for all buildings.
                    Match m = WarpDestination.obeliskDefaultActionPattern.Match(defaultAction);
                    if (m.Success)
                    {
                        string locationName = m.Groups[I("destinationLocation")].Value!;

                        GameLocation? targetLocation = Game1.getLocationFromName(locationName);
                        if (targetLocation is null)
                        {
                            ModEntry.Instance.LogWarning($"Found a building on the farm that looks like an obelisk, '{data.Name}', but it's targetting an unknown location, '{locationName}'.");
                        }
                        else if (!obeliskLocationNames.Contains(locationName))
                        {
                            obeliskLocationNames.Add(locationName);
                            destinations.Add(new WarpDestination(targetLocation, null, defaultAction));
                        }
                    }
                    // else it's not an obelisk
                }
                // else it's not an obelisk
            }
        }

        foreach (var location in ModEntry.Instance.SlowWarpDestinations)
        {
            if (!obeliskLocationNames.Contains(location.Name))
            {
                destinations.Add(new WarpDestination(location, null, null));
            }
        }

        foreach (var totem in ModEntry.Instance.TotemInventory.GetTotemInventory())
        {
            var inferredLocation = WarpDestination.FindLocationForTotem(totem);
            // Add the totem as a way to get to the destination unless there is already an obelisk method.
            if (!obeliskLocationNames.Any(o => o == inferredLocation?.Name))
            {
                destinations.Add(new WarpDestination(inferredLocation, totem, null));
            }
        }


        int destinationComparer(WarpDestination lhs, WarpDestination rhs)
        {
            if (lhs.Target is null || rhs.Target is null)
            {
                // Unknown totems go first.
                if (rhs.Target is not null)
                {
                    return -1;
                }
                if (lhs.Target is not null)
                {
                    return 1;
                }

                // totem must be non-null because target is null.
                return string.Compare(lhs.Totem!.DisplayName, rhs.Totem!.DisplayName, StringComparison.CurrentCulture);
            }

            int locationComparisonResult = string.Compare(LocationDisplayName(lhs.Target), LocationDisplayName(rhs.Target), StringComparison.CurrentCulture);
            if (locationComparisonResult != 0)
            {
                return locationComparisonResult;
            }

            // The only way we get here is for cases where we have both a totem and a slow-warp way to reach a
            // destination, we'll put the slow-warp case first.  (That is, exactlyl one of lhs and rhs must have
            // a non-null totem value).
            return lhs.Totem is null ? -1 : 1;
        }
        destinations.Sort(destinationComparer);

        return destinations;
    }

    /// <summary>
    ///   The location where you warp to on Ginger Island has a display name of "IslandSouth" rather than a
    /// proper translated name.
    /// </summary>
    private static string LocationDisplayName(GameLocation location)
        => location.Name == "IslandSouth" ? L("Ginger Island") : location.DisplayName;

    private static readonly Regex totemNameRegex = new Regex("^Warp Totem: (?<target>.*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);


    private static GameLocation? FindLocationForTotem(StardewValley.Object totem)
    {
        // AFAIK, the base game offers you no reliable way to know where a totem is going to go,
        //  as they're hard-coded.  There's an unreliable way, as the non-translated "Name" property
        //  usually is 'Warp Totem: <location>' where <location> may be the name of the location.
        switch (totem.QualifiedItemId)
        {
            case "(O)688": return Game1.getFarm();
            case "(O)689": return Game1.getLocationFromName("mountain");
            case "(O)690": return Game1.getLocationFromName("beach");
            case "(O)261": return Game1.getLocationFromName("desert");
            case "(O)886": return Game1.getLocationFromName("islandsouth");
        }

        // See if it's a custom totem where the author tagged it with a destination.
        GameLocation? location = null;
        if (totem.modData.TryGetValue("NermNermNerm.Warpinator.Target", out string targetValue))
        {
            location = Game1.getLocationFromName(targetValue);
            if (location is null)
            {
                ModEntry.Instance.LogErrorOnce($"The mod that inserted {totem.QualifiedItemId} set NermNermNerm.Warpinator.Target to '{targetValue}', but that doesn't seem to be a location name.");
            }
        }

        // Failing that, try and see if the name pattern works.
        Match m = WarpDestination.totemNameRegex.Match(totem.Name);
        if (m.Success)
        {
            string locationName = m.Groups[I("target")].Value;
            location = Game1.getLocationFromName(locationName) ?? Game1.getLocationFromName(locationName, isStructure: true);
        }

        if (location is null)
        {
            ModEntry.Instance.LogWarningOnce($"Can't figure out the target of a warp totem called: {totem.Name}  -- qiid: {totem.QualifiedItemId}");
        }

        return location;
    }


    public void DoWarp()
    {
        if (Utility.isFestivalDay() && Game1.whereIsTodaysFest == this.Target?.Name &&
            Utility.getStartTimeOfFestival() < Game1.timeOfDay)
        {
            Game1.addHUDMessage(new HUDMessage(L("You can't warp there now - today's festival is being set up.}")));
        }

        if (this.Totem is not null)
        {
            ModEntry.Instance.TotemInventory.ReduceCount(this.Totem);
            this.Totem.performUseAction(Game1.currentLocation);
        }
        else if (this.obeliskWarpCode is not null)
        {
            var location = new Location((int)Game1.player.Tile.X, (int)Game1.player.Tile.Y);
            Game1.player.currentLocation.performAction(this.obeliskWarpCode, Game1.player, location);
            // ^ That returns a bool - maybe we could log it?  not sure that'd be really helpful diagnostic.
        }
        else if (this.Target is not null) { // This condition should be guaranteed true, but this lets the static analysis know.
            int numMinutesToPass = Game1.IsMultiplayer ? 0 :  ModEntry.Config.WarpHomeTimeCost * 10;
            int newTime = -1;
            if (numMinutesToPass > 0)
            {
                newTime = Utility.ModifyTime(Game1.timeOfDay, numMinutesToPass);
                if (newTime > 2600)
                {
                    Game1.addHUDMessage(new HUDMessage(L("It's too late to use the marionberry slow-warp now")));
                    return;
                }
            }

            this.UseWandInNoTotemMode(this.Target, newTime);
        }
    }


    private void UseWandInNoTotemMode(GameLocation target, int newTime)
    {
        Point tile = new();
        if (DataLoader.Locations(Game1.content).TryGetValue(target.Name, out var data) &&
            data.DefaultArrivalTile.HasValue)
        {
            tile = data.DefaultArrivalTile.Value;
        }
        else
        {
            ModEntry.Instance.LogWarning($"Failed to warp to '{target.Name}': could not find a default arrival tile.");
            Game1.chatBox.addErrorMessage(LF($"Could not find the warp totem at {target.Name} - unable to warp there."));
            return;
        }

        this.DoWarpEffects(() =>
        {
            Game1.warpFarmer(target.Name, tile.X, tile.Y, false);
            if (newTime >= 0)
            {
                SafelySetTime(newTime);
            }
        }, Game1.player, Game1.player.currentLocation);
    }

    private void DoWarpEffects(Action action, Farmer who, GameLocation where)
    {
        for (int index = 0; index < 12; ++index)
            Game1.Multiplayer.broadcastSprites(where, new TemporaryAnimatedSprite(
                354,
                Game1.random.Next(25, 75), 6, 1,
                new Vector2(
                    Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192),
                    Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)),
                false,
                Game1.random.NextDouble() < 0.5)
            );
        Game1.playSound("wand");
        Game1.displayFarmer = false;
        who.temporarilyInvincible = true;
        who.temporaryInvincibilityTimer = -2000;
        who.freezePause = 1000;
        Game1.flashAlpha = 1f;
        int num = 0;
        var tile = who.TilePoint;
        for (int index = tile.X + 8; index >= tile.X - 8; --index)
        {
            Game1.Multiplayer.broadcastSprites(where, new TemporaryAnimatedSprite(6, new Vector2(index, tile.Y) * 64f, Color.White, 8, false, 50f, 0, -1, -1f, -1, 0)
            {
                layerDepth = 1f,
                delayBeforeAnimationStart = num * 25,
                motion = new Vector2(-0.25f, 0.0f)
            });
            ++num;
        }

        DelayedAction.fadeAfterDelay(new Game1.afterFadeFunction(() =>
        {
            action();
            Game1.changeMusicTrack(I("none"));
            Game1.fadeToBlackAlpha = 0.99f;
            Game1.screenGlow = false;
            Game1.player.temporarilyInvincible = false;
            Game1.player.temporaryInvincibilityTimer = 0;
            Game1.displayFarmer = true;
        }), 1000);
    }


    // LIFTED FROM: https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI.Mods.ConsoleCommands/Framework/Commands/World/SetTimeCommand.cs#L58
    /// <summary>Safely transition to the given time, allowing NPCs to update their schedule.</summary>
    /// <param name="time">The time of day.</param>
    public static void SafelySetTime(int time)
    {
        // transition to new time
        int intervals = Utility.CalculateMinutesBetweenTimes(Game1.timeOfDay, time) / 10;
        if (intervals > 0)
        {
            for (int i = 0; i < intervals; i++)
                Game1.performTenMinuteClockUpdate();
        }
        else if (intervals < 0)
        {
            for (int i = 0; i > intervals; i--)
            {
                Game1.timeOfDay = Utility.ModifyTime(Game1.timeOfDay, -20); // offset 20 mins so game updates to next interval
                Game1.performTenMinuteClockUpdate();
            }
        }

        // reset ambient light
        // White is the default non-raining color. If it's raining or dark out, UpdateGameClock
        // below will update it automatically.
        Game1.outdoorLight = Color.White;
        Game1.ambientLight = Color.White;

        // run clock update (to correct lighting, etc)
        Game1.gameTimeInterval = 0;
        Game1.UpdateGameClock(Game1.currentGameTime);
    }
}
