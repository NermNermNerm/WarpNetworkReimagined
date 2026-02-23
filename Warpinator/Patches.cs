namespace NermNermNerm.Warpinator;

internal static class Patches
{
    private static ModEntry mod = null!;

    internal static void Patch(ModEntry mod)
    {
        Patches.mod = mod;

        mod.Harmony.Patch(
            typeof(Wand).GetMethod(nameof(Wand.DoFunction)),
            new(typeof(Patches), nameof(WandDoFunctionPrefix)));
    }

    private static bool WandDoFunctionPrefix(GameLocation location, SObject __instance)
    {
        // if (__instance.isTemporarilyInvisible)
        // 	return true;


        if (Patches.mod.IsWandUseBlocked)
            return true;

        // Patches.mod.UseWandInNoTotemMode();
        Patches.mod.UseWandWithMenu();
        return false;
    }
}
