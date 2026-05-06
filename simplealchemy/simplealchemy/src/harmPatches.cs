using effectshud.src;
using effectshud.src.DefaultEffects;
using Effect = effectshud.src.Effect;
using HarmonyLib;
using simplealchemy.src.gui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace simplealchemy.src
{
    [HarmonyPatch]
    public class harmPatches
    {
        /// <summary>
        /// Prefix on PlayerInventoryManager.DropMouseSlotItems: while the ImGui cauldron grid is active,
        /// suppress the engine's "drop the mouse-held stack into the world" behaviour, so clicks inside
        /// the dialog don't eject the item onto the floor.
        /// </summary>
        public static bool Prefix_DropMouseSlotItems()
        {
            return !ImGuiInventoryGrid.SuppressMouseDrop;
        }

        // Called on server when a melee weapon connects with a target.
        // Applies poison DoT to players (via effectshud) or flat damage to mobs (fallback until effectshud supports all entities).
        public static void Postfix_OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
        {
            if (world.Side != EnumAppSide.Server) return;
            if (attackedEntity == null || itemslot?.Itemstack == null) return;

            var tree = itemslot.Itemstack.Attributes.GetTreeAttribute("simplepoisoned");
            if (tree == null) return;

            int charges = tree.GetInt("charges");
            if (charges <= 0)
            {
                itemslot.Itemstack.Attributes.RemoveAttribute("simplepoisoned");
                itemslot.MarkDirty();
                return;
            }

            int tier = tree.GetInt("tier", 1);
            string potionId = tree.GetString("potionId", "poison");

            float chance = Math.Min(Config.Current.weaponCoatingChancePerTier.Val * tier, 1f);
            if (world.Rand.NextDouble() > chance)
            {
                tree.SetInt("charges", charges - 1);
                itemslot.MarkDirty();
                return;
            }

            Effect eff;
            switch (potionId)
            {
                case "walkslow":
                    var walkSlow = new WalkSlowEffect();
                    walkSlow.Tier = tier;
                    walkSlow.SetExpiryInRealSeconds(20 + tier * 10);
                    eff = walkSlow;
                    break;
                case "weakmelee":
                    var weakMelee = new WeakMeleeEffect();
                    weakMelee.Tier = tier;
                    weakMelee.SetExpiryInRealSeconds(20 + tier * 10);
                    eff = weakMelee;
                    break;
                default:
                    var poison = new PoisonEffect();
                    poison.Tier = tier;
                    poison.SetExpiryInRealSeconds(15 + tier * 10);
                    eff = poison;
                    break;
            }

            bool applied = effectshud.src.effectshud.ApplyEffectOnEntity(attackedEntity, eff);
            if (!applied && potionId == "poison")
            {
                // Fallback for mobs without EBEffectsAffected — flat bonus damage.
                // TODO: remove once effectshud supports all entity types.
                attackedEntity.ReceiveDamage(new Vintagestory.API.Common.DamageSource
                {
                    Source = Vintagestory.API.Common.EnumDamageSource.Internal,
                    Type = Vintagestory.API.Common.EnumDamageType.Poison
                }, tier * 2f);
            }

            tree.SetInt("charges", charges - 1);
            itemslot.MarkDirty();
        }

        public static bool Postfix_GetHeldItemInfo(Vintagestory.API.Common.CollectibleObject __instance, ItemSlot inSlot,
       StringBuilder dsc,
       IWorldAccessor world,
       bool withDebugInfo)
        {
            ItemStack itemstack = inSlot.Itemstack;
            EntityPlayer entity = (world.Side == EnumAppSide.Client) ? (world as IClientWorldAccessor).Player.Entity : null;
            string smithName = itemstack.Attributes.GetString("smithname");
            float spoilState = __instance.AppendPerishableInfoText(inSlot, dsc, world);
            FoodNutritionProperties nutriProps = __instance.GetNutritionProperties(world, itemstack, entity);
            if (nutriProps != null)
            {
                float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, itemstack, entity);
                float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, itemstack, entity);
                if (Math.Abs(nutriProps.Health * healthLossMul) > 0.001f)
                {
                    dsc.AppendLine(Lang.Get("When eaten: {0} sat, {1} hp", new object[]
                    {
                        Math.Round((double)(nutriProps.Satiety * satLossMul)),
                        nutriProps.Health * healthLossMul
                    }));
                }
                else
                {
                    dsc.AppendLine(Lang.Get("When eaten: {0} sat", new object[]
                    {
                        Math.Round((double)(nutriProps.Satiety * satLossMul))
                    }));
                }
                dsc.AppendLine(Lang.Get("Food Category: {0}", new object[]
                {
                    Lang.Get("foodcategory-" + nutriProps.FoodCategory.ToString().ToLowerInvariant(), Array.Empty<object>())
                }));
            }
            return true;
        }
    }
 }
