namespace NermNermNerm.Warpinator;

public class TotemInventory : ModLet
{
    private const string TotemInventoryKey = "Warpinator.TotemInventory";
    private const char InventoryItemSplitCharacter = '|';
    private const char CountIdSeparator = ' ';

    public override void Entry(ModEntry mod)
    {
        base.Entry(mod);

        mod.Helper.Events.Player.InventoryChanged += this.PlayerOnInventoryChanged;
    }

    public void CheckPlayerInventoryForTotems()
    {
        this.CheckForTotems(Game1.player.Items);
    }

    private void CheckForTotems(IEnumerable<Item> items)
    {
        List<StardewValley.Object>? updatedTotemCounts = null;

        foreach (var newTotem in items.OfType<StardewValley.Object>().Where(this.IsWarpTotem).ToArray())
        {
            updatedTotemCounts ??= this.GetTotemInventory();

            var existingTotem = updatedTotemCounts.FirstOrDefault(e => e.QualifiedItemId == newTotem.QualifiedItemId);
            if (existingTotem is null)
            {
                var clone = (StardewValley.Object)newTotem.getOne();
                clone.Stack = newTotem.Stack;
                updatedTotemCounts.Add(clone);
            }
            else
            {
                existingTotem.Stack += newTotem.Stack;
            }

            string hudMessageTitle = newTotem.Stack == 1
                ? LF($"Added a {newTotem.DisplayName} to your Marionberry wallet.")
                : LF($"Added {newTotem.Stack} charges of {newTotem.DisplayName} to your Marionberry wallet.");
            Game1.hudMessages.Add(new HUDMessage(hudMessageTitle, timeLeft: 5250) { messageSubject = newTotem });

            Game1.player.removeItemFromInventory(newTotem);
        }

        if (updatedTotemCounts is not null)
        {
            this.SaveTotemInventory(updatedTotemCounts);
        }
    }

    private void PlayerOnInventoryChanged(object? sender, InventoryChangedEventArgs eventArgs)
    {
        if (!this.Mod.Marionberry.HasTotemWallet || !Game1.player.Items.Any(i => i?.QualifiedItemId == ModEntry.MarionBerryToolQiid))
        {
            // Player hasn't got a marionberry or the totem upgrade, so we don't do anything.
            return;
        }

        this.CheckForTotems(eventArgs.Added);
    }

    public List<StardewValley.Object> GetTotemInventory()
    {
        List<StardewValley.Object> newInventory = new();

        bool anyWarnings = false;
        Game1.CustomData.TryGetValue(TotemInventory.TotemInventoryKey, out string? inventoryAsString);
        foreach (string entryPair in (inventoryAsString ?? "").Split(TotemInventory.InventoryItemSplitCharacter,  StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = entryPair.Split(CountIdSeparator);

            if (parts.Length != 2 || !int.TryParse(parts[0], out int quantity))
            {
                this.LogError($"The warp totem inventory custom data contained an improperly formatted line, '{entryPair}'.  Skipping it.");
                anyWarnings = true;
            }
            else
            {

                var item = ItemRegistry.Create(parts[1], quantity);
                if (item is StardewValley.Object obj)
                {
                    // Consider: Checking IsWarpTotem
                    newInventory.Add(obj);
                }
                else
                {
                    this.LogError($"The warp totem inventory custom data contained an entry for a kind of warp totem that doesn't exist, '{parts[1]}'.  Skipping it.");
                    anyWarnings = true;
                }
            }
        }

        if (anyWarnings)
        {
            this.SaveTotemInventory(newInventory);
        }

        return newInventory;
    }

    private void SaveTotemInventory(IEnumerable<StardewValley.Object> totems)
    {
        Game1.CustomData[TotemInventory.TotemInventoryKey] = string.Join(
            TotemInventory.InventoryItemSplitCharacter,
            totems.Select(o => IF($"{o.Stack}{TotemInventory.CountIdSeparator}{o.QualifiedItemId}"))
        );
    }

    /// <summary>
    ///   This is called to report that the player used one of the totems in the wallet.
    /// </summary>
    /// <param name="totem">The totem that was used.</param>
    public void ReduceCount(StardewValley.Object totem)
    {
        var allTotems = this.GetTotemInventory();

        var item = allTotems.First(t => t.QualifiedItemId == totem.QualifiedItemId);
        if (item.Stack == 0)
        {
            throw new InvalidOperationException(I("Call to ReduceCount on a totem where the reported quantity was zero."));
        }

        --item.Stack;

        this.SaveTotemInventory(allTotems);
    }

    private bool IsWarpTotem(StardewValley.Object item)
    {
        return item.GetContextTags().Contains(I("totem_item"))
            && item.Name.StartsWith(I("Warp Totem: "));
    }
}
