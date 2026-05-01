using Vintagestory.API.Common;

namespace simplealchemy.src.inventories
{
    public class ItemSlotCauldronFuel : ItemSlot
    {
        public ItemSlotCauldronFuel(InventoryBase inventory) : base(inventory)
        {
            MaxSlotStackSize = 64;
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            if (sourceSlot?.Itemstack?.Collectible == null) return false;
            var props = sourceSlot.Itemstack.Collectible.CombustibleProps;
            if (props == null) return false;
            return props.BurnDuration > 0 && props.BurnTemperature > 0;
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return CanHold(sourceSlot) && base.CanTakeFrom(sourceSlot, priority);
        }
    }

    public class ItemSlotCauldronIngredient : ItemSlot
    {
        public ItemSlotCauldronIngredient(InventoryBase inventory) : base(inventory)
        {
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            var stack = sourceSlot?.Itemstack;
            if (stack == null) return false;

            AssetLocation code = stack.Block?.Code ?? stack.Item?.Code;
            if (code == null) return false;

            var cfg = Config.Current;

            if (cfg.allowedIngredientsItems.Val.TryGetValue(code.Domain, out var paths)
                && paths.Contains(code.Path))
                return true;

            if (cfg.allowedIngredientsGroupsItems.Val.TryGetValue(code.Domain, out var prefixes))
            {
                foreach (var pref in prefixes)
                    if (code.Path.StartsWith(pref)) return true;
            }

            return false;
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return CanHold(sourceSlot) && base.CanTakeFrom(sourceSlot, priority);
        }
    }
}
