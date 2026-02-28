namespace NermNermNerm.Warpinator;

/// <summary>
/// Handles all the things the mod does around the "slow-warp" feature, where no play-time expires but
/// game time does.
/// </summary>
public class SlowWarp : ModLet
{
    public void WarpFarmer(GameLocation target, int? newTime)
    {
        if (target is Farm)
        {
            this.WarpToFarm(newTime);
        }
        else if (target.Name == "Beach")
        {
            this.DoWarpWithEffects(target, new Point(20,4), newTime);
        }
        else if (target.Name == "Mountain")
        {
            this.DoWarpWithEffects(target, new Point(31,20), newTime);
        }
        // TODO: Look for mod data that describes where the player should land.
        else if (DataLoader.Locations(Game1.content).TryGetValue(target.Name, out var data) &&
                 data.DefaultArrivalTile.HasValue)
        {
            this.DoWarpWithEffects(target, data.DefaultArrivalTile.Value, newTime);
        }
        else
        {
            ModEntry.Instance.LogWarning($"Failed to warp to '{target.Name}': could not find a default arrival tile.");
            Game1.chatBox.addErrorMessage(LF($"Could not find the warp totem at {target.Name} - unable to warp there."));
        }
    }

    private void WarpToFarm(int? newTime)
    {
        var warpTarget = this.Mod.HomeSpot.HomeLocation;
        if (warpTarget.location is null)
        {
            var frontDoorSpot = Utility.getHomeOfFarmer(Game1.player).getFrontDoorSpot();
            warpTarget = new(Game1.getFarm(), frontDoorSpot);
        }

        this.DoWarpWithEffects(warpTarget.location, warpTarget.tile, newTime);
    }

    public void DoWarpWithEffects(GameLocation whereTo, Point targetTile, int? newTime)
    {
        var who = Game1.player;
        var where = Game1.currentLocation!;
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
        var sourceTile = who.TilePoint;
        for (int index = sourceTile.X + 8; index >= sourceTile.X - 8; --index)
        {
            Game1.Multiplayer.broadcastSprites(where, new TemporaryAnimatedSprite(6, new Vector2(index, sourceTile.Y) * 64f, Color.White, 8, false, 50f, 0, -1, -1f, -1, 0)
            {
                layerDepth = 1f,
                delayBeforeAnimationStart = num * 25,
                motion = new Vector2(-0.25f, 0.0f)
            });
            ++num;
        }

        DelayedAction.fadeAfterDelay(new Game1.afterFadeFunction(() =>
        {
            var lr = new LocationRequest(whereTo.Name, whereTo is FarmHouse /*Note - Cabin is a subclass of FarmHouse */, whereTo);
            // Since we always warp to the front door of the farmhouse and cabins, face upwards so the farmer doesn't look wierd.
            // On the farm, since we're at a given x/y, might as well show our beautiful mug to the camera.
            Game1.warpFarmer( lr, targetTile.X, targetTile.Y, whereTo is FarmHouse ? 0 : 2);
            if (newTime.HasValue)
            {
                SafelySetTime(newTime.Value);
            }

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
