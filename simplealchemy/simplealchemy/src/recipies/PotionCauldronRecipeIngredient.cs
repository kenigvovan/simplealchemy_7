using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace simplealchemy.src.recipies
{
    [ProtoContract]
    public class PotionCauldronRecipeIngredient : CraftingRecipeIngredient
    {
        // Token: 0x06000082 RID: 130 RVA: 0x00008AE0 File Offset: 0x00006CE0
        public PotionCauldronRecipeIngredient Clone()
        {
            PotionCauldronRecipeIngredient potionCauldronRecipeIngredient = new PotionCauldronRecipeIngredient();
            potionCauldronRecipeIngredient.Code = base.Code.Clone();
            potionCauldronRecipeIngredient.Type = this.Type;
            potionCauldronRecipeIngredient.Name = base.Name;
            potionCauldronRecipeIngredient.Quantity = this.Quantity;
            potionCauldronRecipeIngredient.ConsumeQuantity = this.ConsumeQuantity;
            potionCauldronRecipeIngredient.ConsumeLitres = this.ConsumeLitres;
            potionCauldronRecipeIngredient.IsWildCard = this.IsWildCard;
            potionCauldronRecipeIngredient.IsTool = this.IsTool;
            potionCauldronRecipeIngredient.Litres = this.Litres;
            potionCauldronRecipeIngredient.AllowedVariants = ((this.AllowedVariants == null) ? null : ((string[])this.AllowedVariants.Clone()));
            CraftingRecipeIngredient craftingRecipeIngredient = potionCauldronRecipeIngredient;
            ItemStack resolvedItemstack = this.ResolvedItemstack;
            craftingRecipeIngredient.ResolvedItemstack = ((resolvedItemstack != null) ? resolvedItemstack.Clone() : null);
            CraftingRecipeIngredient craftingRecipeIngredient2 = potionCauldronRecipeIngredient;
            JsonItemStack returnedStack = this.ReturnedStack;
            craftingRecipeIngredient2.ReturnedStack = ((returnedStack != null) ? returnedStack.Clone() : null);
            PotionCauldronRecipeIngredient potionCauldronRecipeIngredient2 = potionCauldronRecipeIngredient;
            bool flag = this.Attributes != null;
            if (flag)
            {
                potionCauldronRecipeIngredient2.Attributes = this.Attributes.Clone();
            }
            return potionCauldronRecipeIngredient2;
        }

        // Token: 0x04000048 RID: 72
        public int? ConsumeQuantity;
        // Token: 0x04000049 RID: 73
        public float Litres = -1f;
        // Token: 0x0400004A RID: 74
        public float? ConsumeLitres;
    }
}
