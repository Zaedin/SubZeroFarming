using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SubZeroFarming;

public class SubZeroFarmingModSystem : ModSystem
{
    private const string HarmonyId = "SubZeroFarming"; // Use a unique ID

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Server;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.Start(api);
        
        var config = api.LoadModConfig<FarmingOverrides>("subzerofarming.json");

        if (config == null)
        {
            config = new FarmingOverrides();
            api.StoreModConfig(config, "subzerofarming.json");
        }
        
        api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, () =>
        {
            var farming = api.ModLoader.GetModSystem<ModSystemFarming>();

            if (farming?.Config == null)
            {
                api.Logger.Error("ModSystemFarming config not found");
                return;
            }

            farming.Config.DelayGrowthBelowTemperature = config.DelayGrowthBelowTemperature;
            farming.Config.LossPerDegree = config.LossPerDegree;
            farming.Config.DelayGrowthBelowSunLight = config.DelayGrowthBelowSunLight;
            farming.Config.LossPerLevel = config.LossPerLevel;
        });
    }
}