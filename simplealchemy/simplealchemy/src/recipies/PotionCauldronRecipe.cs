using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace simplealchemy.src.recipies
{
    [ProtoContract]
    public class PotionCauldronRecipe : IByteSerializable, IRecipeBase<PotionCauldronRecipe>
    {
        // Token: 0x17000018 RID: 24
        // (get) Token: 0x06000071 RID: 113 RVA: 0x00007EA5 File Offset: 0x000060A5
        // (set) Token: 0x06000072 RID: 114 RVA: 0x00007EAD File Offset: 0x000060AD
        [ProtoMember(1)]
        public AssetLocation Name { get; set; }

        // Token: 0x17000019 RID: 25
        // (get) Token: 0x06000073 RID: 115 RVA: 0x00007EB6 File Offset: 0x000060B6
        // (set) Token: 0x06000074 RID: 116 RVA: 0x00007EBE File Offset: 0x000060BE
        [ProtoMember(2)]
        public bool Enabled { get; set; } = true;

        // Token: 0x1700001A RID: 26
        // (get) Token: 0x06000075 RID: 117 RVA: 0x00007EC8 File Offset: 0x000060C8
       
        IRecipeIngredient[] IRecipeBase<PotionCauldronRecipe>.Ingredients
        {
            get
            {
                return this.Ingredients;
            }
        }

        // Token: 0x1700001B RID: 27
        // (get) Token: 0x06000076 RID: 118 RVA: 0x00007EDD File Offset: 0x000060DD
        
        IRecipeOutput IRecipeBase<PotionCauldronRecipe>.Output
        {
            get
            {
                return this.Output;
            }
        }

        // Token: 0x06000077 RID: 119 RVA: 0x00007EE8 File Offset: 0x000060E8
        public bool Matches(ItemSlot[] inputSlots, out int outputStackSize)
        {
            outputStackSize = 0;
            List<KeyValuePair<ItemSlot, PotionCauldronRecipeIngredient>> list = this.pairInput(inputSlots);
            bool flag = list == null;
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                outputStackSize = this.getOutputSize(list);
                result = (outputStackSize >= 0);
            }
            return result;
        }

        // Token: 0x06000078 RID: 120 RVA: 0x00007F24 File Offset: 0x00006124
        private List<KeyValuePair<ItemSlot, PotionCauldronRecipeIngredient>> pairInput(ItemSlot[] inputStacks)
        {
            List<PotionCauldronRecipeIngredient> list = new List<PotionCauldronRecipeIngredient>(this.Ingredients);
            Queue<ItemSlot> queue = new Queue<ItemSlot>();
            foreach (ItemSlot itemSlot in inputStacks)
            {
                bool flag = !itemSlot.Empty;
                if (flag)
                {
                    queue.Enqueue(itemSlot);
                }
            }
            bool flag2 = queue.Count != this.Ingredients.Length;
            List<KeyValuePair<ItemSlot, PotionCauldronRecipeIngredient>> result;
            if (flag2)
            {
                result = null;
            }
            else
            {
                List<KeyValuePair<ItemSlot, PotionCauldronRecipeIngredient>> list2 = new List<KeyValuePair<ItemSlot, PotionCauldronRecipeIngredient>>();
                while (queue.Count > 0)
                {
                    ItemSlot itemSlot2 = queue.Dequeue();
                    bool flag3 = false;
                    for (int j = 0; j < list.Count; j++)
                    {
                        PotionCauldronRecipeIngredient potionCauldronRecipeIngredient = list[j];
                        if(potionCauldronRecipeIngredient.Code.Path.Equals("spiritportion-*"))
                        {
                            var c = 3;
                        }
                        bool flag4 = potionCauldronRecipeIngredient.SatisfiesAsIngredient(itemSlot2.Itemstack, true);
                        if (flag4)
                        {
                            list2.Add(new KeyValuePair<ItemSlot, PotionCauldronRecipeIngredient>(itemSlot2, potionCauldronRecipeIngredient));
                            flag3 = true;
                            list.RemoveAt(j);
                            break;
                        }
                    }
                    bool flag5 = !flag3;
                    if (flag5)
                    {
                        return null;
                    }
                }
                bool flag6 = list.Count > 0;
                if (flag6)
                {
                    result = null;
                }
                else
                {
                    result = list2;
                }
            }
            return result;
        }

        // Token: 0x06000079 RID: 121 RVA: 0x00008048 File Offset: 0x00006248
        private int getOutputSize(List<KeyValuePair<ItemSlot, PotionCauldronRecipeIngredient>> matched)
        {
            int num = -1;
            foreach (KeyValuePair<ItemSlot, PotionCauldronRecipeIngredient> keyValuePair in matched)
            {
                ItemSlot key = keyValuePair.Key;
                PotionCauldronRecipeIngredient value = keyValuePair.Value;
                bool flag = value.ConsumeQuantity == null;
                if (flag)
                {
                    num = key.StackSize / value.Quantity;
                }
            }
            bool flag2 = num == -1;
            int result;
            if (flag2)
            {
                result = -1;
            }
            else
            {
                foreach (KeyValuePair<ItemSlot, PotionCauldronRecipeIngredient> keyValuePair2 in matched)
                {
                    ItemSlot key2 = keyValuePair2.Key;
                    PotionCauldronRecipeIngredient value2 = keyValuePair2.Value;
                    bool flag3 = value2.ConsumeQuantity == null;
                    if (flag3)
                    {
                        bool flag4 = key2.StackSize % value2.Quantity != 0;
                        if (flag4)
                        {
                            return -1;
                        }
                        bool flag5 = num != key2.StackSize / value2.Quantity;
                        if (flag5)
                        {
                            return -1;
                        }
                    }
                    else
                    {
                        bool flag6 = key2.StackSize < value2.Quantity * num;
                        if (flag6)
                        {
                            return -1;
                        }
                    }
                }
                result = this.Output.StackSize * num;
            }
            return result;
        }

        // Token: 0x0600007A RID: 122 RVA: 0x000081BC File Offset: 0x000063BC
        public bool TryCraftNow(ICoreAPI api, int nowPreparationTicks, ItemSlot[] inputslots)
        {
            bool flag = this.PreparationTicks > 0 && nowPreparationTicks < this.PreparationTicks;
            bool result;
            if (flag)
            {
                result = false;
            }
            else
            {
                List<KeyValuePair<ItemSlot, PotionCauldronRecipeIngredient>> list = this.pairInput(inputslots);
                ItemStack itemStack = this.Output.ResolvedItemstack.Clone();
                itemStack.StackSize = this.getOutputSize(list);
                bool flag2 = itemStack.StackSize < 0;
                if (flag2)
                {
                    result = false;
                }
                else
                {
                    TransitionableProperties[] transitionableProperties = itemStack.Collectible.GetTransitionableProperties(api.World, itemStack, null);
                    TransitionableProperties transitionableProperties2 = (transitionableProperties != null && transitionableProperties.Length != 0) ? transitionableProperties[0] : null;
                    bool flag3 = transitionableProperties2 != null;
                    if (flag3)
                    {
                        CollectibleObject.CarryOverFreshness(api, inputslots, new ItemStack[]
                        {
                            itemStack
                        }, transitionableProperties2);
                    }
                    ItemStack itemStack2 = null;
                    foreach (KeyValuePair<ItemSlot, PotionCauldronRecipeIngredient> keyValuePair in list)
                    {
                        bool flag4 = keyValuePair.Value.ConsumeQuantity != null;
                        if (flag4)
                        {
                            itemStack2 = keyValuePair.Key.Itemstack;
                            itemStack2.StackSize -= keyValuePair.Value.ConsumeQuantity.Value * (itemStack.StackSize / this.Output.StackSize);
                            bool flag5 = itemStack2.StackSize <= 0;
                            if (flag5)
                            {
                                itemStack2 = null;
                            }
                            break;
                        }
                    }
                    bool flag6 = this.shouldBeInLiquidSlot(itemStack);
                    if (flag6)
                    {
                        inputslots[0].Itemstack = itemStack;
                    }
                    else
                    {
                        inputslots[0].Itemstack = itemStack2;
                    }
                    inputslots[0].MarkDirty();
                    inputslots[1].MarkDirty();
                    result = true;
                }
            }
            return result;
        }

        // Token: 0x0600007B RID: 123 RVA: 0x00008368 File Offset: 0x00006568
        public bool shouldBeInLiquidSlot(ItemStack stack)
        {
            bool result;
            if (stack == null)
            {
                result = false;
            }
            else
            {
                JsonObject itemAttributes = stack.ItemAttributes;
                bool? flag = (itemAttributes != null) ? new bool?(itemAttributes["waterTightContainerProps"].Exists) : null;
                bool flag2 = true;
                result = (flag.GetValueOrDefault() == flag2 & flag != null);
            }
            return result;
        }

        // Token: 0x0600007C RID: 124 RVA: 0x000083C0 File Offset: 0x000065C0
        public void ToBytes(BinaryWriter writer)
        {
            writer.Write(this.Code);
            writer.Write(this.Ingredients.Length);
            for (int i = 0; i < this.Ingredients.Length; i++)
            {
                this.Ingredients[i].ToBytes(writer);
            }
            writer.Write(MinCauldronTier);
            writer.Write(MinTemperature);
            writer.Write(MaxTemperature);

            this.Output.ToBytes(writer);
            
            writer.Write(this.PreparationTicks);
        }

        // Token: 0x0600007D RID: 125 RVA: 0x0000842C File Offset: 0x0000662C
        public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            this.Code = reader.ReadString();
            this.Ingredients = new PotionCauldronRecipeIngredient[reader.ReadInt32()];
            for (int i = 0; i < this.Ingredients.Length; i++)
            {
                this.Ingredients[i] = new PotionCauldronRecipeIngredient();
                this.Ingredients[i].FromBytes(reader, resolver);
                this.Ingredients[i].Resolve(resolver, "Barrel Recipe (FromBytes)");
            }
            this.MinCauldronTier = reader.ReadInt32();
            this.MinTemperature = reader.ReadInt32();
            this.MaxTemperature = reader.ReadInt32();

            this.Output = new PotionCauldronOutputStack();
            this.Output.FromBytes(reader, resolver.ClassRegistry);
            this.Output.Resolve(resolver, "Barrel Recipe (FromBytes)", true);
            this.PreparationTicks = reader.ReadInt32();
        }

        // Token: 0x0600007E RID: 126 RVA: 0x000084E0 File Offset: 0x000066E0
        public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
        {
            Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();
            bool flag = this.Ingredients == null || this.Ingredients.Length == 0;
            Dictionary<string, string[]> result;
            if (flag)
            {
                result = dictionary;
            }
            else
            {
                foreach (PotionCauldronRecipeIngredient potionCauldronRecipeIngredient in this.Ingredients)
                {
                    bool flag2 = !potionCauldronRecipeIngredient.Code.Path.Contains("*");
                    if (!flag2)
                    {
                        int num = potionCauldronRecipeIngredient.Code.Path.IndexOf("*");
                        int num2 = potionCauldronRecipeIngredient.Code.Path.Length - num - 1;
                        List<string> list = new List<string>();
                        bool flag3 = potionCauldronRecipeIngredient.Type == 0;
                        if (flag3)
                        {
                            for (int j = 0; j < world.Blocks.Count; j++)
                            {
                                bool flag4 = world.Blocks[j].Code == null || world.Blocks[j].IsMissing;
                                if (!flag4)
                                {
                                    bool flag5 = WildcardUtil.Match(potionCauldronRecipeIngredient.Code, world.Blocks[j].Code);
                                    if (flag5)
                                    {
                                        string text = world.Blocks[j].Code.Path.Substring(num);
                                        string text2 = text.Substring(0, text.Length - num2);
                                        bool flag6 = potionCauldronRecipeIngredient.AllowedVariants != null && !ArrayExtensions.Contains<string>(potionCauldronRecipeIngredient.AllowedVariants, text2);
                                        if (!flag6)
                                        {
                                            list.Add(text2);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int k = 0; k < world.Items.Count; k++)
                            {
                                bool flag7 = world.Items[k].Code == null || world.Items[k].IsMissing;
                                if (!flag7)
                                {
                                    bool flag8 = WildcardUtil.Match(potionCauldronRecipeIngredient.Code, world.Items[k].Code);
                                    if (flag8)
                                    {
                                        string text3 = world.Items[k].Code.Path.Substring(num);
                                        string text4 = text3.Substring(0, text3.Length - num2);
                                        bool flag9 = potionCauldronRecipeIngredient.AllowedVariants != null && !ArrayExtensions.Contains<string>(potionCauldronRecipeIngredient.AllowedVariants, text4);
                                        if (!flag9)
                                        {
                                            list.Add(text4);
                                        }
                                    }
                                }
                            }
                        }
                        dictionary[potionCauldronRecipeIngredient.Name] = list.ToArray();
                    }
                }
                result = dictionary;
            }
            return result;
        }

        // Token: 0x0600007F RID: 127 RVA: 0x000087A4 File Offset: 0x000069A4
        public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            bool flag = true;
            for (int i = 0; i < this.Ingredients.Length; i++)
            {
                PotionCauldronRecipeIngredient potionCauldronRecipeIngredient = this.Ingredients[i];
                bool flag2 = potionCauldronRecipeIngredient.Resolve(world, sourceForErrorLogging);
                flag = (flag && flag2);
                bool flag3 = flag2;
                if (flag3)
                {
                    WaterTightContainableProps containableProps = BlockLiquidContainerBase.GetContainableProps(potionCauldronRecipeIngredient.ResolvedItemstack);
                    bool flag4 = containableProps != null;
                    if (flag4)
                    {
                        bool flag5 = potionCauldronRecipeIngredient.Litres < 0f;
                        if (flag5)
                        {
                            bool flag6 = potionCauldronRecipeIngredient.Quantity > 0;
                            if (flag6)
                            {
                                world.Logger.Warning("Barrel recipe {0}, ingredient {1} does not define a litres attribute but a quantity, will assume quantity=litres for backwards compatibility.", new object[]
                                {
                                    sourceForErrorLogging,
                                    potionCauldronRecipeIngredient.Code
                                });
                                potionCauldronRecipeIngredient.Litres = (float)potionCauldronRecipeIngredient.Quantity;
                                PotionCauldronRecipeIngredient potionCauldronRecipeIngredient2 = potionCauldronRecipeIngredient;
                                int? consumeQuantity = potionCauldronRecipeIngredient.ConsumeQuantity;
                                potionCauldronRecipeIngredient2.ConsumeLitres = ((consumeQuantity != null) ? new float?((float)consumeQuantity.GetValueOrDefault()) : null);
                            }
                            else
                            {
                                potionCauldronRecipeIngredient.Litres = 1f;
                            }
                        }
                        potionCauldronRecipeIngredient.Quantity = (int)(containableProps.ItemsPerLitre * potionCauldronRecipeIngredient.Litres);
                        bool flag7 = potionCauldronRecipeIngredient.ConsumeLitres != null;
                        if (flag7)
                        {
                            potionCauldronRecipeIngredient.ConsumeQuantity = new int?((int)(containableProps.ItemsPerLitre * potionCauldronRecipeIngredient.ConsumeLitres).Value);
                        }
                    }
                }
            }
            flag &= this.Output.Resolve(world, sourceForErrorLogging, true);
            bool flag8 = flag;
            if (flag8)
            {
                WaterTightContainableProps containableProps2 = BlockLiquidContainerBase.GetContainableProps(this.Output.ResolvedItemstack);
                bool flag9 = containableProps2 != null;
                if (flag9)
                {
                    bool flag10 = this.Output.Litres < 0f;
                    if (flag10)
                    {
                        bool flag11 = this.Output.Quantity > 0;
                        if (flag11)
                        {
                            world.Logger.Warning("Barrel recipe {0}, output {1} does not define a litres attribute but a stacksize, will assume stacksize=litres for backwards compatibility.", new object[]
                            {
                                sourceForErrorLogging,
                                this.Output.Code
                            });
                            this.Output.Litres = (float)this.Output.Quantity;
                        }
                        else
                        {
                            this.Output.Litres = 1f;
                        }
                    }
                    this.Output.Quantity = (int)(containableProps2.ItemsPerLitre * this.Output.Litres);
                }
            }
            return flag;
        }

        // Token: 0x06000080 RID: 128 RVA: 0x00008A08 File Offset: 0x00006C08
        public PotionCauldronRecipe Clone()
        {
            PotionCauldronRecipeIngredient[] array = new PotionCauldronRecipeIngredient[this.Ingredients.Length];
            for (int i = 0; i < this.Ingredients.Length; i++)
            {
                array[i] = this.Ingredients[i].Clone();
            }
            return new PotionCauldronRecipe
            {
                MinTemperature = this.MinTemperature,
                MaxTemperature = this.MaxTemperature,
                MinCauldronTier = this.MinCauldronTier,
                PreparationTicks = this.PreparationTicks,
                Output = this.Output.Clone(),
                Code = this.Code,
                Enabled = this.Enabled,
                Name = this.Name,
                RecipeId = this.RecipeId,
                Ingredients = array
            };
        }

        [ProtoMember(3)]
        public int RecipeId;
        [ProtoMember(4)]
        public PotionCauldronRecipeIngredient[] Ingredients;
        [ProtoMember(5)]
        public PotionCauldronOutputStack Output;
        [ProtoMember(6)]
        public string Code;
        [ProtoMember(7)]
        public int PreparationTicks;
        [ProtoMember(8)]
        public int MinTemperature;
        [ProtoMember(9)]
        public int MaxTemperature;
        [ProtoMember(10)]
        public int MinCauldronTier;
    }
}
