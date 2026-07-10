using System;
using System.Collections.Generic;

namespace OmegasAbilityMerchant;

internal static class AbilityMerchantInventory
{
    private const int MaxPersistedContainerHeight = 99;
    private static readonly HashSet<int> ResizeRetryInventoryIds = new HashSet<int>();

    internal const int MaxPersistedStockCapacity =
        OmegasAbilityMerchantConfig.MaxInventoryWidth * MaxPersistedContainerHeight;

    internal static Thing? FindAndApplyConfiguredWidth(Card? owner)
    {
        var ownerThings = owner?.things;
        if (ownerThings is null)
        {
            AbilityMerchantStockFailureLogger.LogInventoryMissing(owner: owner, reason: "ownerThingsMissing");
            return null;
        }

        Thing? inventory = ownerThings.Find(id: "chest_merchant");
        var items = inventory?.things;
        if (inventory is null || items is null)
        {
            AbilityMerchantStockFailureLogger.LogInventoryMissing(owner: owner, reason: "postBaseChestMissing");
            return null;
        }

        int width = OmegasAbilityMerchantConfig.GetInventoryWidth();
        int height = Math.Min(
            val1: MaxPersistedContainerHeight,
            val2: Math.Max(val1: 1, val2: items.height));
        TryApplySize(
            inventory: inventory,
            items: items,
            width: width,
            height: height,
            owner: owner,
            phase: "applyConfiguredWidth");
        return inventory;
    }

    internal static void FitHeight(Thing? inventory, Card? owner)
    {
        var items = inventory?.things;
        if (inventory is null || items is null)
        {
            AbilityMerchantStockFailureLogger.LogInventoryMissing(owner: owner, reason: "fitContainerMissing");
            return;
        }

        try
        {
            int width = Math.Min(
                val1: OmegasAbilityMerchantConfig.MaxInventoryWidth,
                val2: Math.Max(
                    val1: OmegasAbilityMerchantConfig.MinInventoryWidth,
                    val2: items.width));
            long neededHeightLong = GetNeededHeight(itemCount: items.Count, width: width);
            if (neededHeightLong > MaxPersistedContainerHeight
                && width < OmegasAbilityMerchantConfig.MaxInventoryWidth)
            {
                width = GetOverflowSafeWidth(
                    itemCount: items.Count,
                    maxWidth: OmegasAbilityMerchantConfig.MaxInventoryWidth);
                neededHeightLong = GetNeededHeight(itemCount: items.Count, width: width);
            }

            int neededHeight = (int)Math.Min(
                val1: MaxPersistedContainerHeight,
                val2: Math.Max(val1: 1L, val2: neededHeightLong));
            if (neededHeightLong > MaxPersistedContainerHeight)
            {
                AbilityMerchantStockFailureLogger.LogInventoryCapacityExceeded(
                    owner: owner,
                    itemCount: items.Count,
                    width: width,
                    height: neededHeight);
            }

            TryApplySize(
                inventory: inventory,
                items: items,
                width: width,
                height: neededHeight,
                owner: owner,
                phase: "fitHeight");
        }
        catch (Exception exception)
        {
            AbilityMerchantStockFailureLogger.LogInventorySizingException(
                owner: owner,
                phase: "fitHeight",
                exception: exception);
        }
    }

    private static long GetNeededHeight(int itemCount, int width)
    {
        return ((long)itemCount + width - 1L) / width;
    }

    private static int GetOverflowSafeWidth(int itemCount, int maxWidth)
    {
        long widthLong = ((long)itemCount + MaxPersistedContainerHeight - 1L)
                         / MaxPersistedContainerHeight;
        long boundedWidth = Math.Min(
            val1: maxWidth,
            val2: Math.Max(val1: 1L, val2: widthLong));
        return (int)boundedWidth;
    }

    private static bool TryApplySize(
        Thing inventory,
        ThingContainer items,
        int width,
        int height,
        Card? owner,
        string phase)
    {
        bool retryRefresh = ResizeRetryInventoryIds.Contains(item: inventory.uid);
        if (items.width == width && items.height == height && retryRefresh == false)
        {
            return true;
        }

        try
        {
            if (items.width == width && items.height == height)
            {
                items.RefreshGrid();
            }
            else
            {
                items.ChangeSize(w: width, h: height);
            }

            ResizeRetryInventoryIds.Remove(item: inventory.uid);
            return true;
        }
        catch (Exception exception)
        {
            ResizeRetryInventoryIds.Add(item: inventory.uid);
            AbilityMerchantStockFailureLogger.LogInventorySizingException(
                owner: owner,
                phase: phase,
                exception: exception);
            return false;
        }
    }
}
