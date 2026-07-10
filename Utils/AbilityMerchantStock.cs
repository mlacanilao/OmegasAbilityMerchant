using System;
using System.Collections.Generic;
using System.Linq;

namespace OmegasAbilityMerchant;

internal readonly struct AbilityMerchantStockResult
{
    internal AbilityMerchantStockResult(
        int candidateCount,
        int generatedCount,
        int failedItemCount,
        bool sourceUnavailable,
        bool capacityReached,
        string? firstFailureContext,
        string? firstFailureReason)
    {
        CandidateCount = candidateCount;
        GeneratedCount = generatedCount;
        FailedItemCount = failedItemCount;
        SourceUnavailable = sourceUnavailable;
        CapacityReached = capacityReached;
        FirstFailureContext = firstFailureContext;
        FirstFailureReason = firstFailureReason;
    }

    internal int CandidateCount { get; }

    internal int GeneratedCount { get; }

    internal int FailedItemCount { get; }

    internal bool SourceUnavailable { get; }

    internal bool CapacityReached { get; }

    internal string? FirstFailureContext { get; }

    internal string? FirstFailureReason { get; }

    internal bool HasSpecificFailure
    {
        get
        {
            return SourceUnavailable || CapacityReached || FailedItemCount > 0;
        }
    }
}

internal static class AbilityMerchantStock
{
    internal static AbilityMerchantStockResult AddAbilitySkillbooks(Thing inventory, Card? owner)
    {
        var items = inventory.things;
        if (items is null)
        {
            AbilityMerchantStockFailureLogger.LogInventoryMissing(owner: owner, reason: "stockContainerMissing");
            return new AbilityMerchantStockResult(
                candidateCount: 0,
                generatedCount: 0,
                failedItemCount: 0,
                sourceUnavailable: true,
                capacityReached: false,
                firstFailureContext: null,
                firstFailureReason: null);
        }

        List<SourceElement.Row>? abilityRows = GetAbilityRows(owner: owner);
        if (abilityRows is null)
        {
            return new AbilityMerchantStockResult(
                candidateCount: 0,
                generatedCount: 0,
                failedItemCount: 0,
                sourceUnavailable: true,
                capacityReached: false,
                firstFailureContext: null,
                firstFailureReason: null);
        }

        int generatedCount = 0;
        int failedItemCount = 0;
        bool capacityReached = false;
        string? firstFailureContext = null;
        string? firstFailureReason = null;

        foreach (SourceElement.Row abilityRow in abilityRows)
        {
            if (items.Count >= AbilityMerchantInventory.MaxPersistedStockCapacity)
            {
                capacityReached = true;
                AbilityMerchantStockFailureLogger.LogStockCapacityReached(
                    owner: owner,
                    itemCount: items.Count,
                    candidateCount: abilityRows.Count,
                    generatedCount: generatedCount);
                break;
            }

            Thing? generatedBook = null;
            try
            {
                generatedBook = ThingGen.CreateSkillbook(ele: abilityRow.id, num: 1);
                if (IsValidAbilitySkillbook(thing: generatedBook, elementId: abilityRow.id) == false)
                {
                    failedItemCount++;
                    RecordFirstFailure(
                        abilityRow: abilityRow,
                        reason: "generated item was not a matching ability skillbook",
                        firstFailureContext: ref firstFailureContext,
                        firstFailureReason: ref firstFailureReason);
                    DestroyUnaddedThing(
                        thing: generatedBook,
                        items: items,
                        owner: owner,
                        abilityRow: abilityRow);
                    generatedBook = null;
                    continue;
                }

                generatedBook.c_IDTState = 0;
                generatedBook.SetBlessedState(s: BlessedState.Normal);
                Thing addedThing = inventory.AddThing(t: generatedBook, tryStack: false);
                if (items.Contains(item: addedThing) == false)
                {
                    failedItemCount++;
                    RecordFirstFailure(
                        abilityRow: abilityRow,
                        reason: "inventory insertion could not be confirmed",
                        firstFailureContext: ref firstFailureContext,
                        firstFailureReason: ref firstFailureReason);
                    DestroyUnaddedThing(
                        thing: generatedBook,
                        items: items,
                        owner: owner,
                        abilityRow: abilityRow);
                    generatedBook = null;
                    continue;
                }

                generatedCount++;
                generatedBook = null;
            }
            catch (Exception exception)
            {
                failedItemCount++;
                RecordFirstFailure(
                    abilityRow: abilityRow,
                    reason: exception.GetType().FullName + ": " + exception.Message,
                    firstFailureContext: ref firstFailureContext,
                    firstFailureReason: ref firstFailureReason);
                DestroyUnaddedThing(
                    thing: generatedBook,
                    items: items,
                    owner: owner,
                    abilityRow: abilityRow);
            }
        }

        var result = new AbilityMerchantStockResult(
            candidateCount: abilityRows.Count,
            generatedCount: generatedCount,
            failedItemCount: failedItemCount,
            sourceUnavailable: false,
            capacityReached: capacityReached,
            firstFailureContext: firstFailureContext,
            firstFailureReason: firstFailureReason);
        AbilityMerchantStockFailureLogger.LogGenerationFailures(owner: owner, result: result);
        AbilityMerchantStockFailureLogger.LogIfGeneratedStockEmpty(owner: owner, result: result);
        return result;
    }

    private static List<SourceElement.Row>? GetAbilityRows(Card? owner)
    {
        var elementMap = EClass.sources?.elements?.map;
        if (elementMap is null)
        {
            AbilityMerchantStockFailureLogger.LogSourceUnavailable(owner: owner, reason: "elementMapMissing");
            return null;
        }

        try
        {
            return elementMap.Values
                .Where(
                    predicate: row =>
                        row is not null
                        && row.id > 0
                        && row.group == "ABILITY"
                        && row.category == "ability"
                        && row.categorySub == "ability"
                        && row.aliasRef != "mold")
                .OrderBy(keySelector: row => row._index)
                .ThenBy(keySelector: row => row.id)
                .ToList();
        }
        catch (Exception exception)
        {
            AbilityMerchantStockFailureLogger.LogSourceUnavailable(
                owner: owner,
                reason: exception.GetType().FullName + ": " + exception.Message);
            return null;
        }
    }

    private static bool IsValidAbilitySkillbook(Thing? thing, int elementId)
    {
        if (thing is null || thing.id != "book_skill" || thing.refVal != elementId)
        {
            return false;
        }

        return thing.trait is TraitBookSkill;
    }

    private static void RecordFirstFailure(
        SourceElement.Row abilityRow,
        string reason,
        ref string? firstFailureContext,
        ref string? firstFailureReason)
    {
        if (firstFailureContext is not null)
        {
            return;
        }

        firstFailureContext = "id=" + abilityRow.id.ToString() + ", alias=" + abilityRow.alias;
        firstFailureReason = reason;
    }

    private static void DestroyUnaddedThing(
        Thing? thing,
        ThingContainer items,
        Card? owner,
        SourceElement.Row abilityRow)
    {
        if (thing is null || thing.isDestroyed || items.Contains(item: thing))
        {
            return;
        }

        try
        {
            thing.Destroy();
        }
        catch (Exception exception)
        {
            AbilityMerchantStockFailureLogger.LogCleanupFailure(
                owner: owner,
                elementId: abilityRow.id,
                exception: exception);
        }
    }
}

internal static class AbilityMerchantStockFailureLogger
{
    private static readonly HashSet<string> LoggedErrorKeys = new HashSet<string>();

    internal static void LogBarterUnavailable(Card? owner, string reason)
    {
        LogOnce(
            key: "barterUnavailable|" + GetOwnerId(owner: owner) + "|" + reason,
            message: "Ability merchant barter could not start. owner=" + FormatOwner(owner: owner) +
                     ", reason=" + reason + ".");
    }

    internal static void LogInventoryMissing(Card? owner, string reason)
    {
        LogOnce(
            key: "inventoryMissing|" + GetOwnerId(owner: owner) + "|" + reason,
            message: "Ability merchant inventory is unavailable. owner=" + FormatOwner(owner: owner) +
                     ", reason=" + reason + ".");
    }

    internal static void LogInventorySizingException(Card? owner, string phase, Exception exception)
    {
        LogOnce(
            key: "inventorySizing|" + GetOwnerId(owner: owner) + "|" + phase + "|" +
                 exception.GetType().FullName,
            message: "Ability merchant inventory sizing failed. owner=" + FormatOwner(owner: owner) +
                     ", phase=" + phase + ", exceptionType=" + exception.GetType().FullName +
                     ", message=" + exception.Message + ".");
    }

    internal static void LogInventoryCapacityExceeded(Card? owner, int itemCount, int width, int height)
    {
        LogOnce(
            key: "inventoryCapacityExceeded|" + GetOwnerId(owner: owner) + "|" + itemCount.ToString(),
            message: "Ability merchant inventory exceeds Elin's persisted container capacity. owner=" +
                     FormatOwner(owner: owner) + ", itemCount=" + itemCount.ToString() +
                     ", width=" + width.ToString() + ", height=" + height.ToString() + ".");
    }

    internal static void LogStockCapacityReached(
        Card? owner,
        int itemCount,
        int candidateCount,
        int generatedCount)
    {
        LogOnce(
            key: "stockCapacityReached|" + GetOwnerId(owner: owner) + "|" + itemCount.ToString(),
            message: "Ability merchant stopped adding skillbooks at Elin's persisted container capacity. owner=" +
                     FormatOwner(owner: owner) + ", itemCount=" + itemCount.ToString() +
                     ", candidateCount=" + candidateCount.ToString() +
                     ", generatedCount=" + generatedCount.ToString() + ".");
    }

    internal static void LogSourceUnavailable(Card? owner, string reason)
    {
        LogOnce(
            key: "sourceUnavailable|" + GetOwnerId(owner: owner) + "|" + reason,
            message: "Ability merchant source rows are unavailable; custom stock was not generated. owner=" +
                     FormatOwner(owner: owner) + ", reason=" + reason + ".");
    }

    internal static void LogGenerationFailures(Card? owner, AbilityMerchantStockResult result)
    {
        if (result.FailedItemCount == 0)
        {
            return;
        }

        LogOnce(
            key: "generationFailures|" + GetOwnerId(owner: owner) + "|" +
                 result.FailedItemCount.ToString() + "|" + result.FirstFailureContext + "|" +
                 result.FirstFailureReason,
            message: "Ability merchant skipped one or more invalid skillbooks. owner=" +
                     FormatOwner(owner: owner) + ", candidateCount=" + result.CandidateCount.ToString() +
                     ", generatedCount=" + result.GeneratedCount.ToString() +
                     ", failedItemCount=" + result.FailedItemCount.ToString() +
                     ", firstFailure=" + (result.FirstFailureContext ?? "<unknown>") +
                     ", reason=" + (result.FirstFailureReason ?? "<unknown>") + ".");
    }

    internal static void LogIfGeneratedStockEmpty(Card? owner, AbilityMerchantStockResult result)
    {
        if (result.GeneratedCount != 0 || result.HasSpecificFailure)
        {
            return;
        }

        LogOnce(
            key: "generatedStockEmpty|" + GetOwnerId(owner: owner) + "|" + result.CandidateCount.ToString(),
            message: "Ability merchant generated no custom skillbook stock. owner=" +
                     FormatOwner(owner: owner) + ", candidateCount=" + result.CandidateCount.ToString() + ".");
    }

    internal static void LogCleanupFailure(Card? owner, int elementId, Exception exception)
    {
        LogOnce(
            key: "cleanupFailure|" + GetOwnerId(owner: owner) + "|" + elementId.ToString() + "|" +
                 exception.GetType().FullName,
            message: "Ability merchant could not destroy a failed generated item. owner=" +
                     FormatOwner(owner: owner) + ", elementId=" + elementId.ToString() +
                     ", exceptionType=" + exception.GetType().FullName +
                     ", message=" + exception.Message + ".");
    }

    private static void LogOnce(string key, string message)
    {
        if (LoggedErrorKeys.Add(item: key) == false)
        {
            return;
        }

        OmegasAbilityMerchant.LogError(message: message);
    }

    private static string FormatOwner(Card? owner)
    {
        if (owner is null)
        {
            return "<null>";
        }

        return owner.id + "#" + owner.uid.ToString();
    }

    private static string GetOwnerId(Card? owner)
    {
        if (owner is null)
        {
            return "<null>";
        }

        if (string.IsNullOrEmpty(value: owner.id))
        {
            return "<empty>";
        }

        return owner.id;
    }
}
