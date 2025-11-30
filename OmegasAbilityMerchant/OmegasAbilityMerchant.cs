using BepInEx;

namespace OmegasAbilityMerchant
{
    internal static class ModInfo
    {
        internal const string Name = "Omegas Ability Merchant";
        internal const string Guid = "omegaplatinum.elin.abilitymerchant";
        internal const string Version = "1.0.0";
    }

    [BepInPlugin(GUID: ModInfo.Guid, Name: ModInfo.Name, Version: ModInfo.Version)]
    internal class OmegasAbilityMerchant : BaseUnityPlugin
    {
        internal static OmegasAbilityMerchant Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            OmegasAbilityMerchantConfig.LoadConfig(config: Config);
        }
        
        internal static void Log(object payload)
        {
            Instance?.Logger.LogInfo(data: payload);
        }
    }
}