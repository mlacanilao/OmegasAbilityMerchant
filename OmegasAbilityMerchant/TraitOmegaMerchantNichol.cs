using System;
using System.Linq;
using OmegasAbilityMerchant;
using UnityEngine;

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
            return base.GetParam(i: 1, def: null).IsEmpty(defaultStr: TraitTrainer.ids[EClass.rnd(a: TraitTrainer.ids.Length)]);
        }
    }
    
    void _OnBarter()
    {
        int inventoryWidth = OmegasAbilityMerchantConfig.InventoryWidth?.Value ?? 16;
        
        var inventory = this.owner?.things?.Find(id: "chest_merchant");
        if (inventory is null)
        {
            inventory = ThingGen.Create(id: "chest_merchant");
            this.owner?.AddThing(t: inventory);
        }
        var items = inventory?.things;
        
        if (items?.width != inventoryWidth)
        {
            items?.ChangeSize(w: inventoryWidth, h: items.height);
        }

        var abilities = EClass.sources.elements.rows
            .Where(
                predicate: r =>
                    r.group == "ABILITY"
                    && r.category == "ability"
                    && r.categorySub == "ability"
                    && r.aliasRef != "mold"
            )
            .ToList();
        
        foreach (var ability in abilities)
        {
            Thing thing = ThingGen.CreateSkillbook(ele: ability.id, num: 1);
            thing.c_IDTState = 0;
            thing.SetBlessedState(s: BlessedState.Normal);
            inventory?.AddThing(t: thing);
        }

        if (items != null)
        {
            int w = Math.Max(val1: 1, val2: items.width);
            int neededH = Math.Max(val1: 1, val2: (items.Count + w - 1) / w);
            
            if (items.height != neededH)
            {
                items.ChangeSize(w: w, h: neededH);
            }
        }
    }
}