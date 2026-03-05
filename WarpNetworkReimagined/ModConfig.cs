namespace WarpNetworkReimagined;

public class ModConfig
{
    /// <summary>
    ///  The humber of 10-minute intervals that pass when the player clicks on the warp-home
    ///  ability without using a warp totem.
    /// </summary>
    public int SlowWarpTimeCost { get; set; } = 6;

    /// <summary>
    ///  The humber of 10-minute intervals that pass when the player clicks on the warp-home
    ///  ability without using a warp totem.
    /// </summary>
    public int FastWarpTimeCost { get; set; } = 3;

    /// <summary>
    ///   Makes it so that crafting recipes yield more than just one totem.
    /// </summary>
    public int TotemRecipeYield { get; set; } = 1;
}
