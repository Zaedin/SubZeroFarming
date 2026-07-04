using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SubZeroFarming;

public class SubZeroFarmingModSystem : ModSystem
{
    private const string HarmonyId = "SubZeroFarming"; // Use a unique ID
    private Harmony? _harmony;

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Server;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);

        _harmony = new Harmony(HarmonyId);

        var original = AccessTools.Method(typeof(BlockEntityFastForwardGrowth), "Update");
        var transpiler = new HarmonyMethod(
            typeof(SubZeroFarmingModSystem).GetMethod(nameof(Transpiler),
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));

        _harmony.Patch(original, transpiler: transpiler);
        var info = Harmony.GetPatchInfo(
            AccessTools.Method(typeof(BlockEntityFastForwardGrowth), "Update"));

        if (info.Transpilers.Count > 0)
            api.Logger.Notification($"[{_harmony.Id}] Patch applied successfully. Crops should now grow below 0ºC.");
        else
            api.Logger.Error($"[{_harmony.Id}] Patch not applied. Please send your logs to the dev.");
    }

    public override void Dispose()
    {
        _harmony?.UnpatchAll(HarmonyId);
        base.Dispose();
    }

    [HarmonyPatch(typeof(BlockEntityFastForwardGrowth), "Update", typeof(float))]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var list = new List<CodeInstruction>(instructions);
        FileLog.Log("Patching BlockEntityFastForwardGrowth");

        for (var i = 0; i < list.Count - 1; i++)
        {
            var opcodeName = list[i].opcode.Name;
            if (opcodeName == null)
                continue;

            if (!opcodeName.StartsWith("ldloc"))
                continue;

            if (list[i + 1].opcode != OpCodes.Callvirt)
                continue;

            if (list[i + 1].operand is not MethodInfo mi)
                continue;

            if (mi.Name != "Invoke")
                continue;

            var p = mi.GetParameters();
            if (p.Length != 4 || p[3].ParameterType != typeof(bool))
                continue;

            // switch "growthPaused" with "false"
            list[i].opcode = OpCodes.Ldc_I4_0;
            list[i].operand = null;
            break;
        }

        return list;
    }
}