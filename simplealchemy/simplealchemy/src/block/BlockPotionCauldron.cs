using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace simplealchemy.src
{
    public class BlockPotionCauldron : BlockLiquidContainerBase, ILiquidSource, ILiquidSink, IIgnitable
    {
        public override bool IsTopOpened => true;
        public override bool AllowHeldLiquidTransfer => true;

        static SimmerRecipe[] simmerRecipes;

        public bool isSealed;
        AdvancedParticleProperties[] particles;
        Vec3f[] basePos;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (simmerRecipes == null)
            {
                simmerRecipes = Attributes["simmerRecipes"].AsObject<SimmerRecipe[]>();
                if (simmerRecipes != null)
                {
                    foreach (SimmerRecipe rec in simmerRecipes)
                    {
                        rec.Resolve(api.World, "saucepan");
                    }
                }
            }
            InitializeParticles();

        }
        public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
        {
            BlockEntityPotionCauldron beo = manager.BlockAccess.GetBlockEntity(pos) as BlockEntityPotionCauldron;
            if (beo != null && beo.IsBurning) beo.RenderParticleTick(manager, pos, windAffectednessAtPos, secondsTicking, particles);

            base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
        }

        private void InitializeParticles()
        {
            particles = new AdvancedParticleProperties[16];
            basePos = new Vec3f[particles.Length];

            Cuboidf[] spawnBoxes = new Cuboidf[]
            {
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.125f, x2: 0.3125f, y2: 0.1f, z2: 0.875f),//smoke
                    new Cuboidf(x1: 0.7125f, y1: 0, z1: 0.125f, x2: 0.875f, y2: 0.5f, z2: 0.875f),
                    new Cuboidf(x1: 0.225f, y1: 0.3f, z1: 0.225f, x2: 0.225f, y2: 0.9f, z2: 0.225f),//purple particles
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.7125f, x2: 0.875f, y2: 0.5f, z2: 0.875f)
            };

            // This is smoke particles - similar to the Firepit
            for (int j = 0; j < 4; j++)
            {
                AdvancedParticleProperties props = ParticleProperties[0].Clone();

                Cuboidf box = spawnBoxes[j];
                basePos[j] = new Vec3f(0, 0, 0);

                props.PosOffset[0].avg = box.MidX;
                props.PosOffset[0].var = box.Width / 2;

                props.PosOffset[1].avg = 0f;
                props.PosOffset[1].var = 0f;

                props.PosOffset[2].avg = box.MidZ;
                props.PosOffset[2].var = box.Length / 2;

                props.Quantity.avg = 0.5f;
                props.Quantity.var = 0.2f;
                props.LifeLength.avg = 0.8f;

                particles[j] = props;
            }

            // The rest are flame particles: the spawn pos will be precisely controlled by spawning code in BEClayOven
            // This is the dark orange at the base of a flame
            for (int j = 4; j < 8; j++)
            {
                AdvancedParticleProperties props = ParticleProperties[1].Clone();
                props.PosOffset[1].avg = 0.06f;
                props.PosOffset[1].var = 0.02f;
                props.Quantity.avg = 0.5f;
                props.Quantity.var = 0.2f;
                props.LifeLength.avg = 0.3f;
                props.VertexFlags = 128;

                particles[j] = props;
            }


            // This is the bright orange in the middle of a flame
            for (int j = 8; j < 12; j++)
            {
                AdvancedParticleProperties props = ParticleProperties[2].Clone();
                Cuboidf box = spawnBoxes[j - 8];
                props.PosOffset[0].avg = box.MidX - 0.09f;
                //props.PosOffset[0].var = box.Width / 2;

                props.PosOffset[1].avg = 0.125f;
                props.PosOffset[1].var = 0.2f;

                props.PosOffset[2].avg = box.MidZ - 0.09f;
                // props.PosOffset[2].var = box.Length / 2;

                props.Quantity.avg = 0.5f;
                props.Quantity.var = 0.2f;
                props.LifeLength.avg = 0.8f;

                particles[j] = props;
            }

            // This is the bright yellow at the top of a flame
            for (int j = 12; j < 16; j++)
            {
                AdvancedParticleProperties props = ParticleProperties[3].Clone();
                props.PosOffset[1].avg = 0.12f;
                props.PosOffset[1].var = 0.03f;
                props.Quantity.avg = 0.2f;
                props.Quantity.var = 0.1f;
                props.LifeLength.avg = 0.12f;
                props.VertexFlags = 255;

                particles[j] = props;
            }
        }
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            List<ItemStack> liquidContainerStacks = new List<ItemStack>();

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                if (obj is BlockLiquidContainerTopOpened || obj is ILiquidSource || obj is ILiquidSink || obj is BlockWateringCan)
                {
                    List<ItemStack> stacks = obj.GetHandBookStacks((ICoreClientAPI)api);
                    if (stacks != null) liquidContainerStacks.AddRange(stacks);
                }
            }

            return new WorldInteraction[]
                    {
                new WorldInteraction()
                {
                    ActionLangCode = "game:blockhelp-behavior-rightclickpickup",
                    MouseButton = EnumMouseButton.Right,
                    RequireFreeHand = true
                },
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-bucket-rightclick",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = liquidContainerStacks.ToArray()
                },
                new WorldInteraction()
                {
                    ActionLangCode = "simplealchemy:blockhelp-lid", // json lang file. 
                    HotKeyCodes = new string[] { "sneak", "sprint" },
                    MouseButton = EnumMouseButton.Right
                }
            };
        }

        public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
        {
            if (outputStack != null || GetContent(inputStack) != null) return false;
            List<ItemStack> stacks = new List<ItemStack>();

            foreach (ItemSlot slot in cookingSlotsProvider.Slots)
            {
                if (!slot.Empty) stacks.Add(slot.Itemstack);
            }

            if (stacks.Count <= 0) return false;
            else if (stacks.Count == 1)
            {
                //stacks[0].Collectible.CombustibleProps?.SmeltedStack?.Resolve(world, "saucepan");
                if (stacks[0].Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack == null || !stacks[0].Collectible.CombustibleProps.RequiresContainer) return false;
                return stacks[0].StackSize % stacks[0].Collectible.CombustibleProps.SmeltedRatio == 0;
            }
            else if (simmerRecipes != null)
            {
                foreach (SimmerRecipe rec in simmerRecipes)
                {
                    if (rec.Match(stacks) > 0) return true;
                }
            }
            return false;
        }

        public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
        {
            if (!CanSmelt(world, cookingSlotsProvider, inputSlot.Itemstack, outputSlot.Itemstack)) return;

            List<ItemStack> contents = new List<ItemStack>();
            ItemStack product = null;

            foreach (ItemSlot slot in cookingSlotsProvider.Slots)
            {
                if (!slot.Empty) contents.Add(slot.Itemstack);
            }

            if (contents.Count == 1)
            {
                //contents[0].Collectible.CombustibleProps.SmeltedStack.Resolve(world, "saucepan");

                product = contents[0].Collectible.CombustibleProps.SmeltedStack.ResolvedItemstack.Clone();

                product.StackSize *= (contents[0].StackSize / contents[0].Collectible.CombustibleProps.SmeltedRatio);
            }
            else if (simmerRecipes != null && contents.Count > 1)
            {
                SimmerRecipe match = null;
                int amount = 0;

                foreach (SimmerRecipe rec in simmerRecipes)
                {
                    if ((amount = rec.Match(contents)) > 0)
                    {
                        match = rec;
                        break;
                    }
                }

                if (match == null) return;

                product = match.Simmering.SmeltedStack.ResolvedItemstack.Clone();

                product.StackSize *= amount;

            }

            if (product == null) return;

            if (product.Collectible.Class == "ItemLiquidPortion")
            {
                for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
                {
                    cookingSlotsProvider.Slots[i].Itemstack = null;
                }

                outputSlot.Itemstack = inputSlot.TakeOut(1);

                (outputSlot.Itemstack.Collectible as BlockLiquidContainerBase).TryPutLiquid(outputSlot.Itemstack, product, product.StackSize);

            }
            else
            {
                outputSlot.Itemstack = product;

                for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
                {
                    cookingSlotsProvider.Slots[i].Itemstack = null;
                }

            }
        }

        public override float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
        {
            float dur = 0f;
            List<ItemStack> contents = new List<ItemStack>();
            foreach (ItemSlot slot in cookingSlotsProvider.Slots)
            {
                if (!slot.Empty) contents.Add(slot.Itemstack);
            }
            if (contents.Count == 1 && contents[0].Collectible.CombustibleProps != null) return contents[0].Collectible.CombustibleProps.MeltingDuration * contents[0].StackSize;
            else if (simmerRecipes != null && contents.Count > 1)
            {
                SimmerRecipe match = null;
                int amount = 0;

                foreach (SimmerRecipe rec in simmerRecipes)
                {
                    if ((amount = rec.Match(contents)) > 0)
                    {
                        match = rec;
                        break;
                    }
                }

                if (match == null) return 0;

                return match.Simmering.MeltingDuration * amount;
            }

            return dur;
        }

        public override float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
        {
            float temp = 0f;
            List<ItemStack> contents = new List<ItemStack>();
            foreach (ItemSlot slot in cookingSlotsProvider.Slots)
            {
                if (!slot.Empty) contents.Add(slot.Itemstack);
            }
            if (contents.Count == 1 && contents[0].Collectible.CombustibleProps != null) return contents[0].Collectible.CombustibleProps.MeltingPoint;
            else if (simmerRecipes != null && contents.Count > 1)
            {
                SimmerRecipe match = null;
                int amount = 0;

                foreach (SimmerRecipe rec in simmerRecipes)
                {
                    if ((amount = rec.Match(contents)) > 0)
                    {
                        match = rec;
                        break;
                    }
                }

                if (match == null) return 0;

                return match.Simmering.MeltingPoint;
            }

            return temp;
        }

        public override int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
        {
            if (liquidStack == null) return 0;

            var props = GetContainableProps(liquidStack);
            if (props == null) return 0;

            int desiredItems = (int)(props.ItemsPerLitre * desiredLitres);
            int availItems = liquidStack.StackSize;

            ItemStack stack = GetContent(containerStack);
            ILiquidSink sink = containerStack.Collectible as ILiquidSink;

            if (stack == null)
            {
                if (!props.Containable) return 0;

                int placeableItems = (int)(sink.CapacityLitres * props.ItemsPerLitre);

                ItemStack placedstack = liquidStack.Clone();
                placedstack.StackSize = GameMath.Min(availItems, desiredItems, placeableItems);
                SetContent(containerStack, placedstack);

                return Math.Min(desiredItems, placeableItems);
            }
            else
            {
                if (!stack.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes)) return 0;

                float maxItems = sink.CapacityLitres * props.ItemsPerLitre;
                int placeableItems = (int)(maxItems - (float)stack.StackSize);

                stack.StackSize += Math.Min(placeableItems, desiredItems);

                return Math.Min(placeableItems, desiredItems);
            }
        }

        public static WaterTightContainableProps GetInContainerProps(ItemStack stack)
        {
            try
            {
                JsonObject obj = stack?.ItemAttributes?["waterTightContainerProps"];
                if (obj != null && obj.Exists) return obj.AsObject<WaterTightContainableProps>(null, stack.Collectible.Code.Domain);
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            float currentLitres = GetCurrentLitres(pos);
            BlockEntityPotionCauldron blockEntityContainer = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPotionCauldron;
            if (blockEntityContainer == null)
            {
                return "";
            }

            

            ItemSlot itemSlot = blockEntityContainer.Inventory[GetContainerSlotId(pos)];
            ItemStack itemstack = itemSlot.Itemstack;
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(Lang.Get("Temperature: {0}°C", new object[]
                {
                    (int) blockEntityContainer.furnaceTemperature
                }));

            if (!blockEntityContainer.Inventory[1].Empty)
            {
                sb.AppendLine(Lang.Get("simplealchemy:fuel-cauldron", blockEntityContainer.Inventory[1].Itemstack.StackSize + " " + blockEntityContainer.Inventory[1].GetStackName()));
            }
            string str = "";
            string text = "";
            if (currentLitres <= 0f)
            {
                //return Lang.Get("Empty");
            }
            else
            {

                text = Lang.Get(itemstack.Collectible.Code.Domain + ":incontainer-" + itemstack.Class.ToString().ToLowerInvariant() + "-" + itemstack.Collectible.Code.Path);
                str = Lang.Get("Contents:") + "\n" + Lang.Get("{0} litres of {1}", currentLitres, text);

                if (currentLitres == 1f)
                {
                    str = Lang.Get("Contents:") + "\n" + Lang.Get("{0} litre of {1}", currentLitres, text);
                }
                sb.Append(str);
            }
            

            bool first = true;
            for (int i = 2; i < blockEntityContainer.Inventory.Count; i++)
            {
                if (blockEntityContainer.Inventory[i].Itemstack == null)
                    continue;
                if (first)
                {
                    sb.AppendLine();
                    first = false;
                }
                sb.AppendLine(blockEntityContainer.Inventory[i].Itemstack.StackSize + " " + blockEntityContainer.Inventory[i].GetStackName());
            }
            return sb.ToString();
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {

            BlockEntityPotionCauldron sp = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityPotionCauldron;
            //var c = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.CombustibleProps;
            BlockPos pos = blockSel.Position;

            ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
            if (byPlayer.Entity.ServerControls.Sprint)
            {
                if (byPlayer.Entity.ServerControls.Sneak)
                {

                    if (sp.fuelStack != null)
                    {

                        if (!byPlayer.InventoryManager.TryGiveItemstack(sp.fuelStack))
                        {
                            world.SpawnItemEntity(sp.fuelStack, byPlayer.Entity.ServerPos.XYZ);
                        }
                        sp.fuelStack = null;
                        return true;
                    }

                }
                else if (stack != null && byPlayer.Entity.ServerControls.Sprint && sp != null && stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.BurnTemperature > 0)
                {
                    if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.CombustibleProps.BurnDuration > 0)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(byPlayer.Entity.World, EnumMouseButton.Right, 0, EnumMergePriority.DirectMerge, 1);
                        byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(sp.fuelSlot, ref op);
                        if (op.MovedQuantity > 0)
                        {
                            (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                            return true;
                        }
                    }
                }


            }

            /* if (byPlayer.WorldData.EntityControls.Sneak && byPlayer.WorldData.EntityControls.Sprint)
             {
                 if (sp != null && Attributes.IsTrue("canSeal"))
                 {
                     world.PlaySoundAt(AssetLocation.Create(Attributes["lidSound"].AsString("sounds/block"), Code.Domain), pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f, byPlayer);
                     sp.isSealed = !sp.isSealed;
                     sp.RedoMesh();
                     sp.MarkDirty(true);
                 }

                 return true;
             }*/

            if (sp?.isSealed == true) return false;
            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (!hotbarSlot.Empty && hotbarSlot.Itemstack.Collectible.Attributes?.IsTrue("handleLiquidContainerInteract") == true)
            {
                EnumHandHandling handling = EnumHandHandling.NotHandled;
                hotbarSlot.Itemstack.Collectible.OnHeldInteractStart(hotbarSlot, byPlayer.Entity, blockSel, null, true, ref handling);
                if (handling == EnumHandHandling.PreventDefault || handling == EnumHandHandling.PreventDefaultAction) return true;
            }

            //if (hotbarSlot.Empty) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            if (!hotbarSlot.Empty)
            {
                CollectibleObject obj = hotbarSlot.Itemstack.Collectible;

                bool singleTake = byPlayer.WorldData.EntityControls.Sneak;
                bool singlePut = byPlayer.WorldData.EntityControls.Sprint;

                if (obj is ILiquidSource && !singleTake)
                {
                    int moved = TryPutLiquid(blockSel.Position, (obj as ILiquidSource).GetContent(hotbarSlot.Itemstack), singlePut ? 1 : 9999);

                    if (moved > 0)
                    {
                        (obj as ILiquidSource).TryTakeContent(hotbarSlot.Itemstack, moved);
                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                        return true;
                    }
                }

                if (obj is ILiquidSink && !singlePut)
                {
                    ItemStack owncontentStack = GetContent(blockSel.Position);
                    int moved = 0;

                    if (hotbarSlot.Itemstack.StackSize == 1)
                    {
                        moved = (obj as ILiquidSink).TryPutLiquid(hotbarSlot.Itemstack, owncontentStack, singleTake ? 1 : 9999);
                    }
                    else
                    {
                        ItemStack containerStack = hotbarSlot.Itemstack.Clone();
                        containerStack.StackSize = 1;
                        moved = (obj as ILiquidSink).TryPutLiquid(containerStack, owncontentStack, singleTake ? 1 : 9999);

                        if (moved > 0)
                        {
                            hotbarSlot.TakeOut(1);
                            if (!byPlayer.InventoryManager.TryGiveItemstack(containerStack, true))
                            {
                                api.World.SpawnItemEntity(containerStack, byPlayer.Entity.SidedPos.XYZ);
                            }
                        }
                    }

                    if (moved > 0)
                    {
                        TryTakeContent(blockSel.Position, moved);
                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                        return true;
                    }
                }
            }
            if (byPlayer.Entity.ServerControls.Sneak)
            {
                for (int i = sp.Inventory.Count - 1; i >= sp.firstIngredientSlot; i--)
                {
                    //ItemStackMoveOperation op = new ItemStackMoveOperation(byPlayer.Entity.World, EnumMouseButton.Button1, 0, EnumMergePriority.DirectMerge, 1);
                    if (sp.Inventory[i].Itemstack == null)
                    {
                        continue;
                    }

                    if (!byPlayer.InventoryManager.TryGiveItemstack(sp.Inventory[i].Itemstack))
                    {
                        world.SpawnItemEntity(sp.Inventory[i].Itemstack, byPlayer.Entity.ServerPos.XYZ);
                    }
                    sp.Inventory[i].Itemstack = null;
                    sp.Inventory[i].MarkDirty();
                    return true;

                }
            }
            else
            {
                if (!byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
                {
                    AssetLocation tmpObjectCode;
                    if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Block != null)
                    {
                        tmpObjectCode = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Block.Code;
                    }
                    else if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item != null)
                    {
                        tmpObjectCode = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item.Code;
                    }
                    else
                    {
                        return false;
                    }
                    if (Config.Current.allowedIngredientsGroupsItems.Val.ContainsKey(tmpObjectCode.Domain))
                    {
                        foreach (var pref in (Config.Current.allowedIngredientsGroupsItems.Val[tmpObjectCode.Domain]))
                        {
                            if (tmpObjectCode.Path.StartsWith(pref))
                            {
                                for (int i = sp.firstIngredientSlot; i < sp.Inventory.Count; i++)
                                {
                                    //var p = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Block.Code.ToString();
                                    ItemStackMoveOperation op = new ItemStackMoveOperation(byPlayer.Entity.World, EnumMouseButton.Right, 0, EnumMergePriority.DirectMerge, 1);
                                    byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(sp.Inventory[i], ref op);
                                    if (op.MovedQuantity > 0)
                                    {
                                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                                        return true;
                                    }
                                }
                                return false;
                            }
                        }
                    }
                    if (Config.Current.allowedIngredientsItems.Val.ContainsKey(tmpObjectCode.Domain))
                    {
                        if (Config.Current.allowedIngredientsItems.Val[tmpObjectCode.Domain].Contains(tmpObjectCode.Path))
                        {
                            for (int i = sp.firstIngredientSlot; i < sp.Inventory.Count; i++)
                            {
                                //var p = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Block.Code.ToString();
                                ItemStackMoveOperation op = new ItemStackMoveOperation(byPlayer.Entity.World, EnumMouseButton.Right, 0, EnumMergePriority.DirectMerge, 1);
                                byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(sp.Inventory[i], ref op);
                                if (op.MovedQuantity > 0)
                                {
                                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                }
            }
            return false;
            //return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (itemslot.Itemstack?.Attributes.GetBool("isSealed") == true) return;

            if (blockSel == null || byEntity.Controls.Sneak)
            {
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }




            if (AllowHeldLiquidTransfer)
            {
                IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
                byPlayer = (byEntity as EntityPlayer)?.Player;

                ItemStack contentStack = GetContent(itemslot.Itemstack);
                WaterTightContainableProps props = contentStack == null ? null : GetContentProps(contentStack);

                Block targetedBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);

                if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
                {
                    byEntity.World.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
                    byPlayer?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
                    return;
                }

                if (!TryFillFromBlock(itemslot, byEntity, blockSel.Position))
                {
                    BlockLiquidContainerTopOpened targetCntBlock = targetedBlock as BlockLiquidContainerTopOpened;
                    if (targetCntBlock != null)
                    {
                        if (targetCntBlock.TryPutLiquid(blockSel.Position, contentStack, targetCntBlock.CapacityLitres) > 0)
                        {
                            TryTakeContent(itemslot.Itemstack, 1);
                            byEntity.World.PlaySoundAt(props.FillSpillSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
                        }

                    }
                    else
                    {
                        if (byEntity.Controls.Sprint)
                        {
                            SpillContents(itemslot, byEntity, blockSel);
                        }
                    }
                }
            }

            if (AllowHeldLiquidTransfer)
            {
                // Prevent placing on normal use
                handHandling = EnumHandHandling.PreventDefaultAction;
            }

            /* IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
                if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
                {
                    byEntity.World.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
                    byPlayer?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
                    return;
                }
                // Prevent placing on normal use
                handHandling = EnumHandHandling.PreventDefaultAction;
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);*/
        }

        private bool SpillContents(ItemSlot containerSlot, EntityAgent byEntity, BlockSelection blockSel)
        {
            BlockPos pos = blockSel.Position;
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            IBlockAccessor blockAcc = byEntity.World.BlockAccessor;
            BlockPos secondPos = blockSel.Position.AddCopy(blockSel.Face);
            var contentStack = GetContent(containerSlot.Itemstack);

            WaterTightContainableProps props = GetContentProps(containerSlot.Itemstack);

            if (props == null || !props.AllowSpill || props.WhenSpilled == null) return false;

            if (!byEntity.World.Claims.TryAccess(byPlayer, secondPos, EnumBlockAccessFlags.BuildOrBreak))
            {
                return false;
            }

            var action = props.WhenSpilled.Action;
            float currentlitres = GetCurrentLitres(containerSlot.Itemstack);

            if (currentlitres > 0 && currentlitres < 10)
            {
                action = WaterTightContainableProps.EnumSpilledAction.DropContents;
            }

            if (action == WaterTightContainableProps.EnumSpilledAction.PlaceBlock)
            {
                Block waterBlock = byEntity.World.GetBlock(props.WhenSpilled.Stack.Code);

                if (props.WhenSpilled.StackByFillLevel != null)
                {
                    JsonItemStack fillLevelStack;
                    props.WhenSpilled.StackByFillLevel.TryGetValue((int)currentlitres, out fillLevelStack);
                    if (fillLevelStack != null) waterBlock = byEntity.World.GetBlock(fillLevelStack.Code);
                }

                Block currentblock = blockAcc.GetBlock(pos);
                if (currentblock.Replaceable >= 6000)
                {
                    blockAcc.SetBlock(waterBlock.BlockId, pos);
                    blockAcc.TriggerNeighbourBlockUpdate(pos);
                    blockAcc.MarkBlockDirty(pos);
                }
                else
                {
                    if (blockAcc.GetBlock(secondPos).Replaceable >= 6000)
                    {
                        blockAcc.SetBlock(waterBlock.BlockId, secondPos);
                        blockAcc.TriggerNeighbourBlockUpdate(pos);
                        blockAcc.MarkBlockDirty(secondPos);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (action == WaterTightContainableProps.EnumSpilledAction.DropContents)
            {
                props.WhenSpilled.Stack.Resolve(byEntity.World, "liquidcontainerbasespill");

                ItemStack stack = props.WhenSpilled.Stack.ResolvedItemstack.Clone();
                stack.StackSize = contentStack.StackSize;

                byEntity.World.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(blockSel.HitPosition));
            }


            int moved = splitStackAndPerformAction(byEntity, containerSlot, (stack) => { SetContent(stack, null); return contentStack.StackSize; });

            DoLiquidMovedEffects(byPlayer, contentStack, moved, EnumLiquidDirection.Pour);
            return true;
        }

        private int splitStackAndPerformAction(Entity byEntity, ItemSlot slot, System.Func<ItemStack, int> action)
        {
            if (slot.Itemstack.StackSize == 1)
            {
                int moved = action(slot.Itemstack);

                if (moved > 0)
                {
                    int maxstacksize = slot.Itemstack.Collectible.MaxStackSize;

                    (byEntity as EntityPlayer)?.WalkInventory((pslot) =>
                    {
                        if (pslot.Empty || pslot is ItemSlotCreative || pslot.StackSize == pslot.Itemstack.Collectible.MaxStackSize) return true;
                        int mergableq = slot.Itemstack.Collectible.GetMergableQuantity(slot.Itemstack, pslot.Itemstack, EnumMergePriority.DirectMerge);
                        if (mergableq == 0) return true;

                        var selfLiqBlock = slot.Itemstack.Collectible as BlockLiquidContainerBase;
                        var invLiqBlock = pslot.Itemstack.Collectible as BlockLiquidContainerBase;

                        if ((selfLiqBlock?.GetContent(slot.Itemstack)?.StackSize ?? 0) != (invLiqBlock?.GetContent(pslot.Itemstack)?.StackSize ?? 0)) return true;

                        slot.Itemstack.StackSize += mergableq;
                        pslot.TakeOut(mergableq);

                        slot.MarkDirty();
                        pslot.MarkDirty();
                        return true;
                    });
                }

                return moved;
            }
            else
            {
                ItemStack containerStack = slot.Itemstack.Clone();
                containerStack.StackSize = 1;

                int moved = action(containerStack);

                if (moved > 0)
                {
                    slot.TakeOut(1);
                    if ((byEntity as EntityPlayer)?.Player.InventoryManager.TryGiveItemstack(containerStack, true) != true)
                    {
                        api.World.SpawnItemEntity(containerStack, byEntity.SidedPos.XYZ);
                    }

                    slot.MarkDirty();
                }

                return moved;
            }
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            Dictionary<int, MultiTextureMeshRef> meshrefs = null;
            bool isSealed = itemstack.Attributes.GetBool("isSealed");

            object obj;
            if (capi.ObjectCache.TryGetValue((Variant["metal"]) + "MeshRefs", out obj))
            {
                meshrefs = obj as Dictionary<int, MultiTextureMeshRef>;
            }
            else
            {
                capi.ObjectCache[(Variant["metal"]) + "MeshRefs"] = meshrefs = new Dictionary<int, MultiTextureMeshRef>();
            }

            ItemStack contentStack = GetContent(itemstack);
            if (contentStack == null) return;

            int hashcode = GetSaucepanHashCode(capi.World, contentStack, isSealed);

            MultiTextureMeshRef meshRef = null;

            if (!meshrefs.TryGetValue(hashcode, out meshRef))
            {
                MeshData meshdata = GenRightMesh(capi, contentStack, null, isSealed);
                //meshdata.Rgba2 = null;


                meshrefs[hashcode] = meshRef = capi.Render.UploadMultiTextureMesh(meshdata);

            }

            renderinfo.ModelRef = meshRef;
        }

        public string GetOutputText(IWorldAccessor world, InventorySmelting inv)
        {
            List<ItemStack> contents = new List<ItemStack>();
            ItemStack product = null;

            foreach (ItemSlot slot in new ItemSlot[] { inv[3], inv[4], inv[5], inv[6] })
            {
                if (!slot.Empty) contents.Add(slot.Itemstack);
            }

            if (contents.Count == 1)
            {
                product = contents[0].Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;

                if (product == null) return null;

                return Lang.Get("firepit-gui-willcreate", contents[0].StackSize / contents[0].Collectible.CombustibleProps.SmeltedRatio, product.GetName());
            }
            else if (simmerRecipes != null && contents.Count > 1)
            {
                SimmerRecipe match = null;
                int amount = 0;

                foreach (SimmerRecipe rec in simmerRecipes)
                {
                    if ((amount = rec.Match(contents)) > 0)
                    {
                        match = rec;
                        break;
                    }
                }

                if (match == null) return null;

                product = match.Simmering.SmeltedStack.ResolvedItemstack;

                if (product == null) return null;

                return Lang.Get("firepit-gui-willcreate", amount, product.GetName());
            }

            return null;
        }

        public MeshData GenRightMesh(ICoreClientAPI capi, ItemStack contentStack, BlockPos forBlockPos = null, bool isSealed = false)
        {
            Shape shape = capi.Assets.TryGet("simplealchemy:shapes/block/" + FirstCodePart() + "/" + (isSealed && Attributes.IsTrue("canSeal") ? "lid" : "empty") + ".json").ToObject<Shape>();
            MeshData bucketmesh;
            capi.Tesselator.TesselateShape(this, shape, out bucketmesh);



            if (contentStack != null)
            {
                WaterTightContainableProps props = GetInContainerProps(contentStack);
                ContainerTextureSource contentSource = new ContainerTextureSource(capi, contentStack, props.Texture);

                MeshData contentMesh;

                if (props.Texture == null) return null;


                float maxLevel = Attributes["maxFillLevel"].AsFloat();
                float fullness = contentStack.StackSize / (props.ItemsPerLitre * CapacityLitres);

                #region Normal Cauldron

                if (maxLevel is 10f)
                {
                    if (fullness <= 0.2f % maxLevel)
                    {
                        shape = capi.Assets.TryGet("simplealchemy:shapes/block/cauldron/contents" + "-0.2.json").ToObject<Shape>();
                    }
                    else if (fullness <= 0.5f % maxLevel)
                    {
                        shape = capi.Assets.TryGet("simplealchemy:shapes/block/" + FirstCodePart() + "/contents" + "-0.4.json").ToObject<Shape>();
                    }
                    else if (fullness <= 0.7f)
                    {
                        shape = capi.Assets.TryGet("simplealchemy:shapes/block/" + FirstCodePart() + "/contents" + "-0.6.json").ToObject<Shape>();
                    }
                    else if (fullness <= 0.9f)
                    {
                        shape = capi.Assets.TryGet("simplealchemy:shapes/block/" + FirstCodePart() + "/contents" + "-0.8.json").ToObject<Shape>();
                    }
                    else if (fullness <= 1f)
                    {
                        shape = capi.Assets.TryGet("simplealchemy:shapes/block/" + FirstCodePart() + "/contents" + "-1.json").ToObject<Shape>();
                    }
                }

                #endregion  Normal Cauldron

                #region Small Cauldron

                if (maxLevel is 8f)
                {
                    if (fullness <= 0.2f % maxLevel)
                    {
                        shape = capi.Assets.TryGet("simplealchemy:shapes/block/" + FirstCodePart() + "/contents" + "-" + 0.1f.ToString().Replace(',', '.') + ".json").ToObject<Shape>();
                    }
                    else if (fullness <= 0.4f % maxLevel)
                    {
                        shape = capi.Assets.TryGet("simplealchemy:shapes/block/" + FirstCodePart() + "/contents" + "-" + 0.2f.ToString().Replace(',', '.') + ".json").ToObject<Shape>();
                    }
                    else if (fullness <= 0.6f % maxLevel)
                    {
                        shape = capi.Assets.TryGet("simplealchemy:shapes/block/" + FirstCodePart() + "/contents" + "-" + 0.3f.ToString().Replace(',', '.') + ".json").ToObject<Shape>();
                    }
                    else if (fullness <= 0.8f % maxLevel)
                    {
                        shape = capi.Assets.TryGet("simplealchemy:shapes/block/" + FirstCodePart() + "/contents" + "-" + 0.4f.ToString().Replace(',', '.') + ".json").ToObject<Shape>();
                    }
                    else if (fullness <= 1f % maxLevel)
                    {
                        shape = capi.Assets.TryGet("simplealchemy:shapes/block/" + FirstCodePart() + "/contents" + "-" + 0.5f.ToString().Replace(',', '.') + ".json").ToObject<Shape>();
                    }
                }

                #endregion Small Cauldron



                capi.Tesselator.TesselateShape("saucepan", shape, out contentMesh, contentSource, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ));


                if (props.ClimateColorMap != null)
                {
                    int col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, 196, 128, false);
                    if (forBlockPos != null)
                    {
                        col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, forBlockPos.X, forBlockPos.Y, forBlockPos.Z, false);
                    }

                    byte[] rgba = ColorUtil.ToBGRABytes(col);

                    for (int i = 0; i < contentMesh.Rgba.Length; i++)
                    {
                        contentMesh.Rgba[i] = (byte)((contentMesh.Rgba[i] * rgba[i % 4]) / 255);
                    }
                }

                for (int i = 0; i < contentMesh.Flags.Length; i++)
                {
                    contentMesh.Flags[i] = contentMesh.Flags[i] & ~(1 << 12); // Remove water waving flag
                }

                bucketmesh.AddMeshData(contentMesh);





                // Water flags
                if (forBlockPos != null)
                {
                    bucketmesh.CustomInts = new CustomMeshDataPartInt(bucketmesh.FlagsCount);
                    bucketmesh.CustomInts.Count = bucketmesh.FlagsCount;
                    //bucketmesh.CustomInts.Values.Fill(0x4000000); // light foam only

                    bucketmesh.CustomFloats = new CustomMeshDataPartFloat(bucketmesh.FlagsCount * 2);
                    bucketmesh.CustomFloats.Count = bucketmesh.FlagsCount * 2;
                }
            }


            return bucketmesh;
        }

        public int GetSaucepanHashCode(IClientWorldAccessor world, ItemStack contentStack, bool isSealed)
        {
            string s = contentStack.StackSize + "x" + contentStack.Collectible.Code.ToShortString();
            if (isSealed) s += "sealed";
            return s.GetHashCode();
        }


        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack drop = base.OnPickBlock(world, pos);

            BlockEntityPotionCauldron sp = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPotionCauldron;

            if (sp != null)
            {
                drop.Attributes.SetBool("isSealed", sp.isSealed);
            }

            return drop;
        }
        public EnumIgniteState OnTryIgniteBlock(
          EntityAgent byEntity,
          BlockPos pos,
          float secondsIgniting)
        {
            BlockEntityPotionCauldron bef = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPotionCauldron;
            if (bef != null && bef.fuelSlot.Empty) return EnumIgniteState.NotIgnitablePreventDefault;
            if (bef != null && bef.IsBurning) return EnumIgniteState.NotIgnitablePreventDefault;

            return secondsIgniting > 3 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }
        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            BlockEntityPotionCauldron bef = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPotionCauldron;
            if (bef != null && !bef.canIgniteFuel)
            {
                bef.canIgniteFuel = true;
                bef.extinguishedTotalHours = api.World.Calendar.TotalHours;
            }

            handling = EnumHandling.PreventDefault;
        }
        // Override to drop the barrel empty and drop its contents instead
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                ItemStack[] drops = new ItemStack[] { new ItemStack(this) };

                for (int i = 0; i < drops.Length; i++)
                {
                    world.SpawnItemEntity(drops[i], new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                }

                world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer);
            }

            if (EntityClass != null)
            {
                BlockEntity entity = world.BlockAccessor.GetBlockEntity(pos);
                if (entity != null)
                {
                    entity.OnBlockBroken();
                }
            }

            world.BlockAccessor.SetBlock(0, pos);
        }

        public EnumIgniteState OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
           if (!(this.api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirepit).IsBurning)
			{
				return EnumIgniteState.NotIgnitable;
			}
			if (secondsIgniting <= 2f)
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
        }
    }

    public class SimmerRecipe
    {
        public CraftingRecipeIngredient[] Ingredients;

        public CombustibleProperties Simmering;

        public bool Resolve(IWorldAccessor world, string debug)
        {
            bool result = true;

            foreach (CraftingRecipeIngredient ing in Ingredients)
            {
                result &= ing.Resolve(world, debug);
            }

            result &= Simmering.SmeltedStack.Resolve(world, debug);

            return result;
        }

        public int Match(List<ItemStack> Inputs)
        {
            if (Inputs.Count != Ingredients.Length) return 0;
            List<CraftingRecipeIngredient> matched = new List<CraftingRecipeIngredient>();
            int amount = -1;

            foreach (ItemStack input in Inputs)
            {
                CraftingRecipeIngredient match = null;

                foreach (CraftingRecipeIngredient ing in Ingredients)
                {
                    if ((ing.ResolvedItemstack == null && !ing.IsWildCard) || matched.Contains(ing) || !ing.SatisfiesAsIngredient(input)) continue;
                    match = ing;
                    break;
                }

                if (match == null || input.StackSize % match.Quantity != 0 || (input.StackSize / match.Quantity) % Simmering.SmeltedRatio != 0) return 0;

                int maxAmount = (input.StackSize / match.Quantity) / Simmering.SmeltedRatio;

                if (amount == -1) amount = maxAmount;
                else if (maxAmount != amount) return 0;

                if (amount == 0) return amount;

                matched.Add(match);


            }

            return amount;
        }
    }
}

