namespace WarpNetworkReimagined;

public class CutScenes : ModLet
{
    private const string IntroCutScene = "WarpNetworkReimagined.NorvinIntro";

    public bool HasPlayerSeenIntroEvent => Game1.player.eventsSeen.Contains(CutScenes.IntroCutScene);

    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        mod.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
        mod.Helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Game1.currentLocation?.Name == "Mountain" && !Game1.eventUp && Game1.player.Tile.X >= 79 && Game1.player.Tile.X <= 80 && Game1.player.Tile.Y >= 17 && Game1.player.Tile.Y <= 18
            && !this.HasPlayerSeenIntroEvent)
        {
            string eventText = Game1.content.Load<Dictionary<string, string>>("Data\\Events\\Mountain")[CutScenes.IntroCutScene];
            Game1.currentLocation.startEvent(new Event(eventText, IF($@"Data\Events\Mountain\{CutScenes.IntroCutScene}"), CutScenes.IntroCutScene));
        }
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/Mountain"))
        {
            e.Edit(editor =>
            {
                var data = editor.AsDictionary<string, string>().Data;

                data[CutScenes.IntroCutScene] = SdvEvent(@$"distantBanjo
82 22
farmer 79 14 1

skippable

move farmer 0 4 2

{WarpShop.EventCommands.NorvinWarpIn}
move farmer 0 4 1
move farmer 2 0 1
{WarpShop.EventCommands.NorvinSay} ""Hey you!  You gotta pay the toll to cross the bridge!""
move farmer 0 4 1
move farmer 4 0 0
pause 2000
faceDirection farmer 1
viewport move 16 0 1000
pause 250
{WarpShop.EventCommands.NorvinFaceDirection} 1
pause 2000
viewport move -32 0 500
pause 1500
faceDirection farmer 0
pause 250
{WarpShop.EventCommands.NorvinFaceDirection} 2

pause 2500
{WarpShop.EventCommands.NorvinSay} ""Well...  You still gotta pay the toll.""
pause 3000

pause 1000
emote farmer 36
pause 500
move farmer -4 0 3

{WarpShop.EventCommands.NorvinSay} ""Okay be that way.  Can't cross the bridge until you pay up!""
move farmer 0 -4 0
{WarpShop.EventCommands.NorvinWarpOut}
pause 500
faceDirection farmer 1
pause 3
move farmer 0 -1 1
pause 1
move farmer 0 4 0
{WarpShop.EventCommands.NorvinWarpIn}
faceDirection farmer 2
emote farmer 16
pause 500
move farmer 0 1 1
move farmer 1 0 1

{WarpShop.EventCommands.NorvinSay} ""Gonna pay that toll?""
pause 2000
move farmer -1 0 1
{WarpShop.EventCommands.NorvinSay} ""..guess not..""
move farmer 0 -4 0
{WarpShop.EventCommands.NorvinWarpOut}
emote farmer 16
faceDirection farmer 1
pause 2000

move farmer 0 4 0
move farmer 1 0 1

{WarpShop.EventCommands.NorvinWarpIn}
{WarpShop.EventCommands.NorvinSay} ""look, you...""
pause 2000
emote farmer 8
pause 2000

{WarpShop.EventCommands.NorvinSay} ""How'd I get here? I live under the bridge. Crawl out when someone comes by.""
pause 5000
emote farmer 12
pause 600
{WarpShop.EventCommands.NorvinSay} ""What? Trolls live under bridges. That's what the books say, so it must be true.""
pause 5000
-- Player swishes weapon
emote farmer 12

{WarpShop.EventCommands.NorvinSay} ""Okay, okay! Fine. Don't tell the other trolls I told you, but...""
pause 5000
{WarpShop.EventCommands.NorvinSay} ""I actually live in a condo in South Zuzu.""
pause 5000
{WarpShop.EventCommands.NorvinSay} ""Sure, rent's cheap under the bridge, but you can't get cable.""
pause 5000
{WarpShop.EventCommands.NorvinSay} ""I just warp in when someone gets close.""
pause 6000
{WarpShop.EventCommands.NorvinSay} ""Anyway — about that toll.""
pause 5000

emote farmer 8
pause 1000

{WarpShop.EventCommands.NorvinSay} ""How do I do it?""
pause 5000
{WarpShop.EventCommands.NorvinSay} ""hmm...""
pause 5000

{WarpShop.EventCommands.NorvinSay} ""You’re wanting some of this warp action...""
pause 5000
{WarpShop.EventCommands.NorvinSay} ""I got a teleporter I could part with. Base model’s cheap, and upgradeable.""
pause 5000
{WarpShop.EventCommands.NorvinSay} ""I’ll make you a good deal on it.""
pause 5000

end fade
");
            });
        }

    }
}
