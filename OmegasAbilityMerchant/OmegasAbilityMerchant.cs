using System.Runtime.CompilerServices;
using BepInEx;

namespace OmegasAbilityMerchant;

internal static class ModInfo
{
    internal const string Guid = "omegaplatinum.elin.omegasabilitymerchant";
    internal const string Name = "Omegas Ability Merchant";
    internal const string Version = "2.0.0";
}

[BepInPlugin(GUID: ModInfo.Guid, Name: ModInfo.Name, Version: ModInfo.Version)]
internal class OmegasAbilityMerchant : BaseUnityPlugin
{
    internal static OmegasAbilityMerchant? Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        OmegasAbilityMerchantConfig.LoadConfig(config: Config);
    }

    internal static void LogDebug(object message, [CallerMemberName] string caller = "")
    {
        Instance?.Logger.LogDebug(data: $"[{caller}] {message}");
    }

    internal static void LogInfo(object message)
    {
        Instance?.Logger.LogInfo(data: message);
    }

    internal static void LogError(object message)
    {
        Instance?.Logger.LogError(data: message);
    }
}
