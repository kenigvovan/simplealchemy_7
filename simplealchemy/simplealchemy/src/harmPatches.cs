using HarmonyLib;
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
