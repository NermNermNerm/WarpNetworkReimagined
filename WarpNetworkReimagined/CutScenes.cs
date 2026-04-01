using StardewValley.GameData.Characters;

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

    private void OnUpdateTicked(object? sender, EventArgs e)
    {
        if (Game1.currentLocation?.Name == "Mountain" && !Game1.eventUp && Game1.player.Tile.X >= 77 && Game1.player.Tile.X <= 89 && Game1.player.Tile.Y >= 17 && Game1.player.Tile.Y <= 18
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
move farmer 2 0 1
move farmer 0 4 1
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

speak Norvin ""How'd I get here? I live under the bridge. Crawl out when someone comes by.""
pause 500
emote farmer 12
pause 600
speak Norvin ""What? Trolls live under bridges. That's what the books say, so it must be true.""
pause 500
-- Player swishes weapon
emote farmer 12

speak Norvin ""Okay, okay! Fine. Don't tell the other trolls I told you, but...""
speak Norvin ""I actually live in a condo in South Zuzu.""
speak Norvin ""Sure, rent's cheap under the bridge, but you can't get cable.""
speak Norvin ""I just warp in when someone gets close.""
speak Norvin ""Anyway — about that toll.""
pause 500

emote farmer 8
pause 1000

speak Norvin ""How do I do it?""
speak Norvin ""hmm...""
-- lightbulb emote of some kind?
speak Norvin ""You’re wanting some of this warp action...""
speak Norvin ""I got a teleporter I could part with. Base model’s cheap, and upgradeable.""
speak Norvin ""I’ll make you a good deal on it...""
pause 2000

end fade
");
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Characters"))
        {
            e.Edit(editor =>
            {
                var data = editor.AsDictionary<string, CharacterData>().Data;
                data[I("Norvin")] = new CharacterData() { DisplayName = L("Norvin") };
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo($"Portraits/Norvin"))
        {
            e.LoadFromModFile<Texture2D>("assets/norvin-portrait.png", AssetLoadPriority.Medium);
        }
    }
}
