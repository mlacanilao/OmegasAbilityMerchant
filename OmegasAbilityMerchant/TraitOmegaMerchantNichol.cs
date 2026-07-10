using OmegasAbilityMerchant;

class TraitOmegaMerchantNichol : TraitMerchant
{
    public override ShopType ShopType
    {
        get
        {
            return ShopType.Specific;
        }
    }
    
    public override CurrencyType CurrencyType
    {
        get
        {
            return CurrencyType.Plat;
        }
    }
    
    public override string LangBarter
    {
        get
        {
            return "daBuyPlat";
        }
    }
    
    public override string IDTrainer
    {
        get
        {
            return base.GetParam(i: 1, def: null)
                .IsEmpty(defaultStr: TraitTrainer.ids[EClass.rnd(a: TraitTrainer.ids.Length)]);
        }
    }
    
    public override void OnBarter(bool reroll = false)
    {
        Card? merchantOwner = owner;
        if (merchantOwner is null)
        {
            AbilityMerchantStockFailureLogger.LogBarterUnavailable(owner: null, reason: "ownerMissing");
            return;
        }

        if (merchantOwner.things is null)
        {
            AbilityMerchantStockFailureLogger.LogBarterUnavailable(
                owner: merchantOwner,
                reason: "ownerThingsMissing");
            return;
        }

        var worldDate = EClass.world?.date;
        if (worldDate is null)
        {
            AbilityMerchantStockFailureLogger.LogBarterUnavailable(
                owner: merchantOwner,
                reason: "worldDateMissing");
            return;
        }

        bool shouldGenerateCustomStock = worldDate.IsExpired(merchantOwner.c_dateStockExpire)
                                         && (RestockDay >= 0 || merchantOwner.isRestocking == false);

        base.OnBarter(reroll: reroll);

        Thing? inventory = AbilityMerchantInventory.FindAndApplyConfiguredWidth(owner: merchantOwner);
        if (inventory is null)
        {
            return;
        }

        try
        {
            if (shouldGenerateCustomStock == false)
            {
                return;
            }

            AbilityMerchantStock.AddAbilitySkillbooks(inventory: inventory, owner: merchantOwner);
        }
        finally
        {
            AbilityMerchantInventory.FitHeight(inventory: inventory, owner: merchantOwner);
        }
    }
}
