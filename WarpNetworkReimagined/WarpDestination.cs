using xTile.Dimensions;

namespace WarpNetworkReimagined;

public class WarpDestination
{
    public GameLocation? Target { get; }
    public StardewValley.Object? Totem { get; }
    public bool IsObeliskWarp => this.obeliskWarpCode is not null;

    public Point? TargetTile { get; }
    public bool IsReturn => this.TargetTile is not null; // Perhaps there should be a more formal way to specify this?

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
    public WarpDestination(GameLocation? target, StardewValley.Object? totem, string? obeliskWarpCode, Point? targetTile)
    {
        this.Target = target;
        this.Totem = totem;
        this.obeliskWarpCode = obeliskWarpCode;
        this.TargetTile = targetTile;
    }

    public string ButtonTitle
    {
        get
        {
            if (this.Target is null)
                return IF($"{this.Totem!.DisplayName} ({this.Totem.Stack})");
            if (this.TargetTile is not null)
                return LF($"Return to {LocationDisplayName(this.Target)}");
            if (this.IsObeliskWarp)
                return LF($"{LocationDisplayName(this.Target)} via obelisk");
            if (this.Totem is null)
                return LF($"{LocationDisplayName(this.Target)} via slow-warp");

            return LF($"{LocationDisplayName(this.Target)} via totem ({this.Totem.Stack})");
        }
    }

    private static readonly Regex obeliskDefaultActionPattern =
        new Regex(@"^\s*ObeliskWarp\s+(?<destinationLocation>[^\s]+) ", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static List<WarpDestination> GetDestinationsForMenu()
    {
        List<WarpDestination> destinations = new();

        List<string> obeliskLocationNames = new();
        var returnTarget = ModEntry.Instance.Return.ReturnDestination;
        if (ModEntry.Instance.Marionberry.HasReturn && returnTarget.Target is not null)
        {
            destinations.Add(returnTarget);
        }

        if (ModEntry.Instance.Marionberry.HasObeliskIntegration)
        {
            // This is pretty much a fallback, in case the user hasn't set a warp spot for the farm.
            var frontDoorSpot = Utility.getHomeOfFarmer(Game1.player).getFrontDoorSpot();
            obeliskLocationNames.Add(I("Farm"));
            destinations.Add(new WarpDestination(
                    Game1.getFarm(),
                    null,
                    IF($"ObeliskWarp Farm {frontDoorSpot.X} {frontDoorSpot.Y} false"),
                    null)
                );
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
                            destinations.Add(new WarpDestination(targetLocation, null, defaultAction, null));
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
                destinations.Add(new WarpDestination(location, null, null, null));
            }
        }

        foreach (var totem in ModEntry.Instance.TotemInventory.GetTotemInventory())
        {
            var inferredLocation = WarpDestination.FindLocationForTotem(totem);
            // Add the totem as a way to get to the destination unless there is already an obelisk method.
            if (!obeliskLocationNames.Any(o => o == inferredLocation?.Name))
            {
                destinations.Add(new WarpDestination(inferredLocation, totem, null, null));
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

            string lhsSortString = lhs.Target is Farm ? "" /* alphabetically first */ : LocationDisplayName(lhs.Target);
            string rhsSortString = rhs.Target is Farm ? "" /* alphabetically first */ : LocationDisplayName(rhs.Target);
            int locationComparisonResult = string.Compare(lhsSortString, rhsSortString, StringComparison.CurrentCulture);
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
        if (totem.modData.TryGetValue("WarpNetworkReimagined.Target", out string targetValue))
        {
            location = Game1.getLocationFromName(targetValue);
            if (location is null)
            {
                ModEntry.Instance.LogErrorOnce($"The mod that inserted {totem.QualifiedItemId} set WarpNetworkReimagined.Target to '{targetValue}', but that doesn't seem to be a location name.");
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
            Game1.timeOfDay < Utility.getStartTimeOfFestival())
        {
            Game1.addHUDMessage(new HUDMessage(L("You can't warp there now - today's festival is being set up.}")));
            return;
        }

        if (this.TargetTile is not null)
        {
            ModEntry.Instance.Return.DoWarp();
            return;
        }

        if (ModEntry.Instance.Marionberry.HasReturn)
        {
            ModEntry.Instance.Return.SetReturnPoint();
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
            int numMinutesToPass = 10 * (Game1.IsMultiplayer
                ? 0 :  ModEntry.Instance.Marionberry.HasFasterWarpPower ? ModEntry.Config.FastWarpTimeCost : ModEntry.Config.SlowWarpTimeCost);
            int? newTime = null;
            if (numMinutesToPass > 0)
            {
                newTime = Utility.ModifyTime(Game1.timeOfDay, numMinutesToPass);
                if (newTime > 2600)
                {
                    Game1.addHUDMessage(new HUDMessage(L("It's too late to use the marionberry slow-warp now")));
                    return;
                }
            }

            ModEntry.Instance.SlowWarp.WarpFarmer(this.Target, newTime);
        }
    }
}
