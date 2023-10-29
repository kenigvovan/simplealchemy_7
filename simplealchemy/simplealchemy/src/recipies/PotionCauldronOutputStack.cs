using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace simplealchemy.src.recipies
{
    public class PotionCauldronOutputStack : JsonItemStack
    {
        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        public override void FromBytes(BinaryReader reader, IClassRegistryAPI instancer)
        {
            base.FromBytes(reader, instancer);
            this.Litres = reader.ReadSingle();
        }

        // Token: 0x06000002 RID: 2 RVA: 0x00002068 File Offset: 0x00000268
        public override void ToBytes(BinaryWriter writer)
        {
            base.ToBytes(writer);
            writer.Write(this.Litres);
        }

        // Token: 0x06000003 RID: 3 RVA: 0x00002080 File Offset: 0x00000280
        public PotionCauldronOutputStack Clone()
        {
            PotionCauldronOutputStack potionCauldronOutputStack = new PotionCauldronOutputStack();
            potionCauldronOutputStack.Code = this.Code.Clone();
            ItemStack resolvedItemstack = this.ResolvedItemstack;
            potionCauldronOutputStack.ResolvedItemstack = ((resolvedItemstack != null) ? resolvedItemstack.Clone() : null);
            potionCauldronOutputStack.StackSize = this.StackSize;
            potionCauldronOutputStack.Type = this.Type;
            potionCauldronOutputStack.Litres = this.Litres;
            PotionCauldronOutputStack potionCauldronOutputStack2 = potionCauldronOutputStack;
            bool flag = this.Attributes != null;
            if (flag)
            {
                potionCauldronOutputStack2.Attributes = this.Attributes.Clone();
            }
            return potionCauldronOutputStack2;
        }

        // Token: 0x04000001 RID: 1
        public float Litres;
    }

}
