using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace simplealchemy.src
{
    public class PoisonCoatableCB : CollectibleBehavior
    {
        public PoisonCoatableCB(CollectibleObject collObj) : base(collObj) { }

        // Right-click poison flask onto weapon in inventory → coat it.
        public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority, ref EnumHandling handling)
        {
            if (priority != EnumMergePriority.DirectMerge)
                return base.GetMergableQuantity(sinkStack, sourceStack, priority, ref handling);

            if (ExtractCoatingPotion(sourceStack) != null)
            {
                handling = EnumHandling.PreventDefault;
                return 1;
            }

            return base.GetMergableQuantity(sinkStack, sourceStack, priority, ref handling);
        }

        public override void TryMergeStacks(ItemStackMergeOperation op, ref EnumHandling handling)
        {
            if (op.CurrentPriority != EnumMergePriority.DirectMerge)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            var flask = op.SourceSlot.Itemstack?.Block as BlockLiquidContainerBase;
            ItemPotion potion = ExtractCoatingPotion(op.SourceSlot.Itemstack);
            if (potion == null)
            {
                handling = EnumHandling.PassThrough;
                return;
            }

            var tree = new TreeAttribute();
            tree.SetString("potionId", potion.potionId ?? "poison");
            tree.SetInt("tier", potion.tier);
            tree.SetInt("charges", 3 + potion.tier * 2);
            op.SinkSlot.Itemstack.Attributes["simplepoisoned"] = tree;
            op.SinkSlot.MarkDirty();

            flask.TryTakeLiquid(op.SourceSlot.Itemstack, 1f);
            op.SourceSlot.MarkDirty();

            op.MovedQuantity = 0;
            handling = EnumHandling.PreventDefault;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            var tree = inSlot.Itemstack?.Attributes.GetTreeAttribute("simplepoisoned");
            if (tree == null) return;

            int charges = tree.GetInt("charges");
            if (charges <= 0) return;

            string potionId = tree.GetString("potionId", "poison");
            string langKey = potionId switch
            {
                "walkslow"  => "simplealchemy:weapon_walkslow_charges",
                "weakmelee" => "simplealchemy:weapon_weakmelee_charges",
                _           => "simplealchemy:weapon_poisoned_charges"
            };
            dsc.AppendLine(Lang.Get(langKey, charges));
        }

        private static readonly System.Collections.Generic.HashSet<string> CoatableIds = new()
        {
            "poison", "walkslow", "weakmelee"
        };

        private static ItemPotion ExtractCoatingPotion(ItemStack sourceStack)
        {
            if (sourceStack == null) return null;
            var flask = sourceStack.Block as BlockLiquidContainerBase;
            if (flask == null) return null;
            var content = flask.GetContent(sourceStack);
            if (content?.Item is ItemPotion p && CoatableIds.Contains(p.potionId))
                return p;
            return null;
        }
    }
}
