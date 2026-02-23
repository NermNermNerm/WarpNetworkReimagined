namespace NermNermNerm.Warpinator;

/// <summary>
/// Handles all the things the mod does around the "slow-warp" feature, where no play-time expires but
/// game time does.
/// </summary>
public class SlowWarp : ModLet
{
    // TODO: Add stuff to do with


    public void WarpFarmer(GameLocation target, int newTime)
    {
        if (DataLoader.Locations(Game1.content).TryGetValue(target.Name, out var data) &&
            data.DefaultArrivalTile.HasValue)
        {
            Point tile = data.DefaultArrivalTile.Value;

            this.DoWarpEffects(() =>
            {
                Game1.warpFarmer(target.Name, tile.X, tile.Y, false);
                if (newTime >= 0)
                {
                    SafelySetTime(newTime);
                }
            }, Game1.player, Game1.player.currentLocation);
        }
        else
        {
            ModEntry.Instance.LogWarning($"Failed to warp to '{target.Name}': could not find a default arrival tile.");
            Game1.chatBox.addErrorMessage(LF($"Could not find the warp totem at {target.Name} - unable to warp there."));
        }
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
