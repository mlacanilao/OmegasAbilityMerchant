using BepInEx.Configuration;

namespace OmegasAbilityMerchant;

internal static class OmegasAbilityMerchantConfig
{
    internal const int DefaultInventoryWidth = 8;
    internal const int MinInventoryWidth = 8;
    internal const int MaxInventoryWidth = 64;

    internal static ConfigEntry<int>? InventoryWidth { get; private set; }

    internal static void LoadConfig(ConfigFile config)
    {
        InventoryWidth = config.Bind(
            section: ModInfo.Name,
            key: "Merchant Inventory Width",
            defaultValue: DefaultInventoryWidth,
            description: "Target width (columns) for the ability merchant inventory grid. The default is 8.\n" +
                         "Values below 8 behave as 8, and values above 64 behave as 64. Inventory height adjusts to fit all merchant stock.\n" +
                         "能力商人のインベントリ幅（列数）の目標値です。既定値は8です。\n" +
                         "8未満の値は8として、64を超える値は64として扱われます。商人の全在庫に合わせて高さが調整されます。\n" +
                         "能力商人物品栏的目标宽度（列数）。默认值为8。\n" +
                         "低于8的值按8处理，高于64的值按64处理。物品栏高度会根据商人的全部库存进行调整。"
        );
    }

    internal static int GetInventoryWidth()
    {
        int configuredWidth = InventoryWidth?.Value ?? DefaultInventoryWidth;

        if (configuredWidth < MinInventoryWidth)
        {
            return MinInventoryWidth;
        }

        if (configuredWidth > MaxInventoryWidth)
        {
            return MaxInventoryWidth;
        }

        return configuredWidth;
    }
}
