using BepInEx.Configuration;

namespace OmegasAbilityMerchant
{
    internal static class OmegasAbilityMerchantConfig
    {
        internal static ConfigEntry<int> InventoryWidth;

        internal static void LoadConfig(ConfigFile config)
        {
            InventoryWidth = config.Bind(
                section: ModInfo.Name,
                key: "Merchant Inventory Width",
                defaultValue: 8,
                description: "Target width (columns) for the ability merchant inventory grid.\n" +
                             "If the current width is smaller, it will expand to this value.\n" +
                             "能力商人のインベントリ幅（列数）。現在の幅が小さい場合、この値まで拡張されます。\n" +
                             "能力商人的物品栏宽度（列数）。若当前宽度更小，将扩展到该值。"
            );
        }
    }
}