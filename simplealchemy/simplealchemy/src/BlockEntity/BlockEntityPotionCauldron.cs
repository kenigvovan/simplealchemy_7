using simplealchemy.src.gui;
using simplealchemy.src.inventories;
using simplealchemy.src.recipies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static simplealchemy.src.recipies.RecipeSystem;

namespace simplealchemy.src
{
    public class BlockEntityPotionCauldron : BlockEntityLiquidContainer, IFirePit, IHeatSource
    {
        public bool canIgniteFuel;
        public override string InventoryClassName => "potioncauldron";
        internal InventoryGeneric ingredientInventory;
        public float fuelBurnTime;
        public float maxFuelBurnTime;
        public PotionCauldronRecipe CurrentRecipe;
        MeshData currentRightMesh;
        public float MeshAngle;
        public bool IsBurning => (double)this.fuelBurnTime > 0.0;
        public bool IsSmoldering => this.canIgniteFuel;
        BlockPotionCauldron ownBlock;
        public bool isSealed;
        public int CapacityLitres { get; set; } = 10;
        public double SealedSinceTotalHours;
        public int firstIngredientSlot = 2;
        private int rotation = 0;
        bool clientSidePrevBurning;
        bool clientRegenMesh = false;


        // Temperature before the half second tick
        public float prevFurnaceTemperature = 20;

        // Current temperature of the furnace
        public float furnaceTemperature = 20;
        // Maximum temperature that can be reached with the currently used fuel
        public int maxTemperature;
        public double extinguishedTotalHours;
        public float potionTemp;
        // How much smoke the current fuel burns?
        public float smokeLevel;
        public int ticksPassedForRecipe = 0;
        public virtual float BurnDurationModifier
        {
            get { return 1f; }
        }
        public virtual float HeatModifier
        {
            get { return 1f; }
        }
        public int cauldronTier = 0;
        public int lastFuelAmount = 0;
        public string CurrentRecipeCode = "";
        Random prng;
        public const int PacketIdOpenCauldron = 1100;
        public const int PacketIdCloseCauldron = 1102;
        public const int PacketIdIgnite = 1103;
        private ImGuiDialogPotionCauldron clientDialog;
        public BlockEntityPotionCauldron()
        {
            //inventory = new InventoryGeneric(5, null, null);
            this.inventory = new InventoryGeneric(6, (string)null, (ICoreAPI)null, (NewSlotDelegate)((id, self) =>
            {
                if (id == 0) return new ItemSlotLiquidOnly((InventoryBase)self, 40f);
                if (id == 1) return new ItemSlotCauldronFuel((InventoryBase)self);
                return new ItemSlotCauldronIngredient((InventoryBase)self);
            }));
            this.inventory.SlotModified += new Action<int>(this.Inventory_SlotModified);
        }
        public override void Initialize(ICoreAPI api)
        {

            base.Initialize(api);
            this.ownBlock = this.Block as BlockPotionCauldron;
            BlockPotionCauldron ownBlock = this.ownBlock;
            int num;
            if (ownBlock == null)
            {
                num = 0;
            }
            else
            {
                bool? exists = ownBlock.Attributes?["capacityLitres"].Exists;
                bool flag = true;
                num = exists.GetValueOrDefault() == flag & exists.HasValue ? 1 : 0;
            }
            if (num != 0)
            {
                this.CapacityLitres = this.ownBlock.Attributes["capacityLitres"].AsInt(50);
                (((InventoryBase)this.inventory)[0] as ItemSlotLiquidOnly).CapacityLitres = (float)this.CapacityLitres;
            }
            if (api.Side == EnumAppSide.Client && this.currentRightMesh == null)
            {
                this.currentRightMesh = this.GenRightMesh();
                this.MarkDirty(true);
            }
            // Tier is derived from block code on both sides so the client dialog
            // can show the right material name without a server sync.
            string codePath = this.ownBlock?.Code?.Path ?? "";
            if (codePath.Contains("copper")) this.cauldronTier = 1;
            else if (codePath.Contains("tinbronze") || codePath.Contains("blackbronze")) this.cauldronTier = 2;
            else if (codePath.Contains("steel")) this.cauldronTier = 4;
            else if (codePath.Contains("iron") || codePath.Contains("meteoriciron")) this.cauldronTier = 3;

            if (api.Side == EnumAppSide.Server)
            {
                this.RegisterGameTickListener(new Action<float>(this.OnEvery3Second), 3000);
                RegisterGameTickListener(OnBurnTick, 100);
                RegisterGameTickListener(On1000msTick, 1000);

                this.FindMatchingRecipe();
            }


            this.prng = new Random((int)(this.Pos.GetHashCode()));
        }
        // Sync to client every 500ms
        private void On1000msTick(float dt)
        {
            if (Api is ICoreServerAPI)
            {
                if (IsBurning)
                {
                    //we have recipe selected and it's temperature is ok for it
                    if (this.CurrentRecipe != null)
                    {
                        if (this.CurrentRecipe.MinTemperature <= this.furnaceTemperature && this.CurrentRecipe.MaxTemperature >= this.furnaceTemperature)
                        {
                            this.ticksPassedForRecipe++;
                        }
                        else
                        {
                            this.ticksPassedForRecipe--;
                            if (this.ticksPassedForRecipe < 0)
                            {
                                this.ticksPassedForRecipe = 0;
                            }
                        }
                    }

                    MarkDirty();
                }
                else if (prevFurnaceTemperature != furnaceTemperature)
                {
                    MarkDirty();
                }

            }
            prevFurnaceTemperature = furnaceTemperature;
        }
        public float changeTemperature(float fromTemp, float toTemp, float dt)
        {
            float diff = Math.Abs(fromTemp - toTemp);

            dt = dt + dt * (diff / 28);


            if (diff < dt)
            {
                return toTemp;
            }

            if (fromTemp > toTemp)
            {
                dt = -dt;
            }

            if (Math.Abs(fromTemp - toTemp) < 1)
            {
                return toTemp;
            }

            return fromTemp + dt;
        }

        public float emptyFirepitBurnTimeMulBonus = 4f;
        public void heatOutput(float dt)
        {
            float oldTemp = potionTemp;

            // Only Heat ore. Cooling happens already in the itemstack
            if (oldTemp < furnaceTemperature)
            {
                float newTemp = changeTemperature(oldTemp, furnaceTemperature, 2 * dt);
                int maxTemp = this.maxTemperature;
                if (maxTemp > 0)
                {
                    newTemp = Math.Min(maxTemp, newTemp);
                }

                if (oldTemp != newTemp)
                {
                    potionTemp = newTemp;
                }
            }
        }
        private void OnBurnTick(float dt)
        {
            //if (Block.Code.Path.Contains("construct")) return;

            // Only tick on the server and merely sync to client
            if (Api is ICoreClientAPI)
            {
                //renderer?.contentStackRenderer?.OnUpdate(InputStackTemp);
                return;
            }

            // Use up fuel
            if (fuelBurnTime > 0)
            {
                bool lowFuelConsumption = Math.Abs(furnaceTemperature - maxTemperature) < 50;// && inputSlot.Empty;

                fuelBurnTime -= dt / (lowFuelConsumption ? emptyFirepitBurnTimeMulBonus : 1);

                if (fuelBurnTime <= 0)
                {
                    fuelBurnTime = 0;
                    maxFuelBurnTime = 0;
                    if (canIgniteFuel && fuelStack != null)
                    {
                        igniteFuel();
                    }
                    else
                    //if (!canSmelt()) // This check avoids light flicker when a piece of fuel is consumed and more is available
                    {
                        //setBlockState("extinct");
                        extinguishedTotalHours = Api.World.Calendar.TotalHours;
                        // }
                    }
                }
            }
            // Too cold to ignite fuel after 2 hours
            if (!IsBurning && Api.World.Calendar.TotalHours - extinguishedTotalHours > 2)
            {
                canIgniteFuel = false;
                //setBlockState("cold");
            }

            // Furnace is burning: Heat furnace
            if (IsBurning)
            {
                furnaceTemperature = changeTemperature(furnaceTemperature, maxTemperature, dt);
            }

            // Ore follows furnace temperature
            //if (canHeatInput())
            //{
            //    heatInput(dt);
            //}
            //else
            //{
            //    inputStackCookingTime = 0;
            //}

            //if (canHeatOutput())
            //{
            heatOutput(dt);
            // }


            // Finished smelting? Turn to smelted item
            //if (canSmeltInput() && inputStackCookingTime > maxCookingTime())
            //{
            //    smeltItems();
            //}


            // Furnace is not burning and can burn: Ignite the fuel
            if (!IsBurning && canIgniteFuel && fuelStack != null)
            {
                igniteFuel();
            }


            // Furnace is not burning: Cool down furnace and ore also turn of fire
            if (!IsBurning)
            {
                furnaceTemperature = changeTemperature(furnaceTemperature, enviromentTemperature(), dt);
            }


        }
        // Resting temperature
        public virtual int enviromentTemperature()
        {
            return 20;
        }

        public void igniteFuel()
        {
            igniteWithFuel(fuelStack);

            fuelStack.StackSize -= 1;
            fuelSlot.MarkDirty();

            if (fuelStack.StackSize <= 0)
            {
                fuelStack = null;
            }
        }
        public ItemStack fuelStack
        {
            get { return inventory[1].Itemstack; }
            set { inventory[1].Itemstack = value; inventory[0].MarkDirty(); }
        }
        public ItemSlot fuelSlot
        {
            get { return inventory[1]; }
        }
        public void igniteWithFuel(IItemStack stack)
        {
            CombustibleProperties fuelCopts = stack.Collectible.CombustibleProps;

            maxFuelBurnTime = fuelBurnTime = fuelCopts.BurnDuration * BurnDurationModifier;
            maxTemperature = (int)(fuelCopts.BurnTemperature * HeatModifier);
            smokeLevel = fuelCopts.SmokeLevel;
            //setBlockState("lit");
            MarkDirty(true);
        }


        private void OnEvery3Second(float dt)
        {
            //we check every time for recipy when add something to it's inventory
            //if (!((InventoryBase)this.inventory)[0].Empty && this.CurrentRecipe == null)
            //this.FindMatchingRecipe();
            if (this.CurrentRecipe != null)
            {
                //if (!this.Sealed)
                //  return;
                if (this.CurrentRecipe.PreparationTicks > this.ticksPassedForRecipe)
                {
                    return;
                }
                //check for passed ticks amount for recipy and after that only we trycraft
                if (!this.CurrentRecipe.TryCraftNow(this.Api, this.ticksPassedForRecipe, new ItemSlot[5]
                {
                    ((InventoryBase) this.inventory)[0],
                    ((InventoryBase) this.inventory)[2],
                    ((InventoryBase) this.inventory)[3],
                    ((InventoryBase) this.inventory)[4],
                    ((InventoryBase) this.inventory)[5]
                }))
                    return;
                for (int i = 2; i < inventory.Count; i++)
                {
                    inventory[i].Itemstack = null;
                    inventory[i].MarkDirty();
                }

                this.ticksPassedForRecipe = 0;
                this.inventory[0].MarkDirty();
                //this.GenRightMesh();
                this.MarkDirty(true);
                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(
                    this.Pos,
                    667,
                    null
                );
                //this.Api.World.BlockAccessor.MarkBlockEntityDirty(this.Pos);
                //this.Sealed = false;
            }
            else
            {
                //if (!this.Sealed)
                // return;
                // this.Sealed = false;
                //FindMatchingRecipe();
                this.MarkDirty(true);
            }
        }
        private void FindMatchingRecipe()
        {
            ItemSlot[] itemSlotArray = new ItemSlot[5]
                    {
                       ((InventoryBase) this.inventory)[0],
                ((InventoryBase) this.inventory)[2],
                ((InventoryBase) this.inventory)[3],
                 ((InventoryBase) this.inventory)[4],
                ((InventoryBase) this.inventory)[5]
            };
            this.CurrentRecipe = (PotionCauldronRecipe)null;
            this.CurrentRecipeCode = "";
            foreach (PotionCauldronRecipe barrelRecipe in simplealchemy.potionCauldronRecipes)
            {
                if (this.cauldronTier < barrelRecipe.MinCauldronTier)
                {
                    continue;
                }
                if (barrelRecipe.Code.Equals("spiritpotionbased"))
                {
                    var c = 3;
                }
                int outputStackSize;
                if (barrelRecipe.Matches(itemSlotArray, out outputStackSize))
                {
                    //this.ignoreChange = true;
                    if (barrelRecipe.PreparationTicks > 0.0)
                    {
                        this.CurrentRecipe = barrelRecipe;
                        this.CurrentRecipeCode = barrelRecipe.Code ?? "";
                        //this.CurrentOutSize = outputStackSize;
                    }
                    else
                    {
                        ICoreAPI api = this.Api;
                        if ((api != null ? (api.Side == EnumAppSide.Server ? 1 : 0) : 0) != 0)
                        {
                            barrelRecipe.TryCraftNow(this.Api, 0, itemSlotArray);
                            this.MarkDirty(true);
                            this.Api.World.BlockAccessor.MarkBlockEntityDirty(this.Pos);
                        }
                    }
                    //this.invDialog?.UpdateContents();
                    ICoreAPI api1 = this.Api;
                    if ((api1 != null ? (api1.Side == EnumAppSide.Client ? 1 : 0) : 0) != 0)
                    {
                        // this.currentMesh = this.GenMesh();
                        this.MarkDirty(true);
                    }
                    // this.ignoreChange = false;
                    break;
                }
            }
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
        }
        private void Inventory_SlotModified(int slotId)
        {
            //if (slotId != 0 && slotId != 2 && slotId != 3 && slotId != 4 && slotId != 5)
            //   return;
            // this.invDialog?.UpdateContents();
            ICoreAPI api = this.Api;

            if ((api != null ? (api.Side == EnumAppSide.Client ? 1 : 0) : 0) != 0)
            {
                this.currentRightMesh = this.GenRightMesh();
            }
            this.MarkDirty(true);
            if (api != null && api.Side == EnumAppSide.Server)
            {
                this.FindMatchingRecipe();
                if (CurrentRecipe == null)
                {
                    ticksPassedForRecipe = 0;
                }
                if (fuelStack == null)
                {
                    if (lastFuelAmount != 0)
                    {
                        lastFuelAmount = 0;
                        //this.MarkDirty(true);
                        ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(
                    this.Pos,
                    667,
                    null
                );
                    }
                }
                else if (fuelStack.StackSize != lastFuelAmount && fuelStack.StackSize % 1 == 0)
                {
                    lastFuelAmount = fuelStack.StackSize;
                    //this.MarkDirty(true);
                    ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(
                    this.Pos,
                    667,
                    null
                );
                }
            }
        }
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            //var l = PotionCauldronRecipeRegistry<PotionCauldronRecipe>.Recipes;
            if (byItemStack != null) isSealed = byItemStack.Attributes.GetBool("isSealed");

            if (Api.Side == EnumAppSide.Client)
            {
                currentRightMesh = GenRightMesh();
                MarkDirty(true);
            }
        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            /*if (Api.World is IServerWorldAccessor)
            {
                Inventory.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            Inventory.Clear();
            foreach (BlockEntityBehavior behavior in Behaviors)
            {
                behavior.OnBlockBroken(byPlayer);
            }*/
        }

        public virtual void RenderParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking, AdvancedParticleProperties[] particles)
        {

            if (!this.IsBurning)
            {
                return;
            };
            int logsCount = fuelStack == null ? 1 : fuelStack.StackSize;
            bool fireFull = logsCount > 3;  //fireFull means it's a fire with 2 rows of logs (between 4 and 6 logs) - flames will start higher
            double[] x = new double[16];
            float[] z = new float[16];

            for (int i = 0; i < 4; i++)
            {
                AdvancedParticleProperties bps = particles[i];
                bps.WindAffectednesAtPos = 0f;
                bps.basePos.X = pos.X;
                bps.basePos.Y = pos.Y + (fireFull ? 3 / 32f : 1 / 32f);
                bps.basePos.Z = pos.Z;

                //set up flame positions with RNG (this way all three flame evolution particles will be in approx. same position)
                {
                    //x[i] = prng.NextDouble() * 0.4f + 0.33f;   // the multiplier and offset gets the flame position aligned with the top surface of the logs
                    // z[i] = 0.26f + prng.Next(0, 3) * 0.2f + (float)prng.NextDouble() * 0.08f;
                }

                manager.Spawn(bps);
            }
            if (this.ticksPassedForRecipe > 0)
            {
                for (int i = 8; i < 12; i++)
                {
                    AdvancedParticleProperties bps = particles[i];
                    bps.WindAffectednesAtPos = 0f;
                    bps.basePos.X = pos.X;
                    bps.basePos.Y = pos.Y + (fireFull ? 3 / 32f : 1 / 32f);
                    bps.basePos.Z = pos.Z;

                    //set up flame positions with RNG (this way all three flame evolution particles will be in approx. same position)
                    {
                        //x[i] = prng.NextDouble() * 0.4f + 0.33f;   // the multiplier and offset gets the flame position aligned with the top surface of the logs
                        // z[i] = 0.26f + prng.Next(0, 3) * 0.2f + (float)prng.NextDouble() * 0.08f;
                    }

                    manager.Spawn(bps);
                }
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {

            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;

            tree.SetFloat("furnaceTemperature", furnaceTemperature);
            tree.SetInt("maxTemperature", maxTemperature);
            //tree.SetFloat("oreCookingTime", inputStackCookingTime);
            tree.SetFloat("fuelBurnTime", fuelBurnTime);
            tree.SetFloat("maxFuelBurnTime", maxFuelBurnTime);
            tree.SetDouble("extinguishedTotalHours", extinguishedTotalHours);
            tree.SetBool("canIgniteFuel", canIgniteFuel);
            //tree.SetFloat("cachedFuel", cachedFuel);
            tree.SetInt("tickPassedForRecipe", ticksPassedForRecipe);
            tree.SetString("currentRecipeCode", CurrentRecipeCode ?? "");

            base.ToTreeAttributes(tree);

            tree.SetBool("isSealed", isSealed);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

            if (Api != null)
            {
                Inventory.AfterBlocksLoaded(Api.World);
            }


            furnaceTemperature = tree.GetFloat("furnaceTemperature");
            maxTemperature = tree.GetInt("maxTemperature");
            // inputStackCookingTime = tree.GetFloat("oreCookingTime");
            fuelBurnTime = tree.GetFloat("fuelBurnTime");
            maxFuelBurnTime = tree.GetFloat("maxFuelBurnTime");
            extinguishedTotalHours = tree.GetDouble("extinguishedTotalHours");
            canIgniteFuel = tree.GetBool("canIgniteFuel", true);
            ticksPassedForRecipe = tree.GetInt("tickPassedForRecipe");
            CurrentRecipeCode = tree.GetString("currentRecipeCode", "");
            //cachedFuel = tree.GetFloat("cachedFuel", 0);

            /*if (Api?.Side == EnumAppSide.Client)
            {
                UpdateRenderer();

                if (clientDialog != null) SetDialogValues(clientDialog.Attributes);
            }*/


            if (Api?.Side == EnumAppSide.Client && (clientSidePrevBurning != IsBurning))
            {
                GetBehavior<BEBehaviorFirepitAmbient>()?.ToggleAmbientSounds(IsBurning);
                clientSidePrevBurning = IsBurning;
                currentRightMesh = GenRightMesh();
                MarkDirty(true);
                //shouldRedraw = false;
            }
        }

        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
        {
            if (this.IsBurning)
                return 10f;
            return !this.IsSmoldering ? 0.0f : 0.25f;
        }
        internal MeshData GenRightMesh()
        {
            if (ownBlock == null || ownBlock.Code.Path.Contains("clay")) return null;

            MeshData mesh = ownBlock.GenRightMesh(Api as ICoreClientAPI, GetContent(), Pos, false);
            if (fuelStack != null)
            {
                MeshData fuelmesh;
                Shape shape = null;
                float tmpInt = 1;
                if (this.fuelStack.Item != null)
                {
                    tmpInt = this.fuelStack.Item.MaxStackSize;
                }
                else if (this.fuelStack.Block != null)
                {
                    tmpInt = this.fuelStack.Block.MaxStackSize;
                }
                if ((this.fuelStack.StackSize / (float)tmpInt) >= 0.9)
                {
                    shape = Api.Assets.TryGet("simplealchemy:shapes/block/cauldron/8woods.json").ToObject<Shape>();
                }
                else if ((this.fuelStack.StackSize / (float)tmpInt) >= 0.7)
                {
                    shape = Api.Assets.TryGet("simplealchemy:shapes/block/cauldron/6woods.json").ToObject<Shape>();
                }
                else if ((this.fuelStack.StackSize / (float)tmpInt) >= 0.5)
                {
                    shape = Api.Assets.TryGet("simplealchemy:shapes/block/cauldron/4woods.json").ToObject<Shape>();
                }
                else if ((this.fuelStack.StackSize / (float)tmpInt) >= 0.25)
                {
                    shape = Api.Assets.TryGet("simplealchemy:shapes/block/cauldron/2woods.json").ToObject<Shape>();
                }
                else if ((this.fuelStack.StackSize / (float)tmpInt) > 0)
                {
                    shape = Api.Assets.TryGet("simplealchemy:shapes/block/cauldron/2woods.json").ToObject<Shape>();
                }
                if (shape != null)
                {
                    (Api as ICoreClientAPI).Tesselator.TesselateShape(ownBlock, shape, out fuelmesh);
                    mesh.AddMeshData(fuelmesh);
                }

            }
            if (mesh.CustomInts != null)
            {
                for (int i = 0; i < mesh.CustomInts.Count; i++)
                {
                    mesh.CustomInts.Values[i] |= 1 << 27; // Disable water wavy
                    mesh.CustomInts.Values[i] |= 1 << 26; // Enabled weak foam
                }
            }

            return mesh;
        }
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == 667)
            {
                this.clientRegenMesh = true;
            }
        }

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(fromPlayer, packetid, data);
            if (packetid == PacketIdOpenCauldron)
            {
                fromPlayer.InventoryManager.OpenInventory(this.Inventory);
                return;
            }
            if (packetid == PacketIdCloseCauldron)
            {
                fromPlayer.InventoryManager.CloseInventory(this.Inventory);
                return;
            }
            if (packetid == PacketIdIgnite)
            {
                if (!IsBurning && fuelStack != null)
                {
                    canIgniteFuel = true;
                    extinguishedTotalHours = Api.World.Calendar.TotalHours;
                    MarkDirty(true);
                }
                return;
            }

            // BlockEntityLiquidContainer (and its parent BlockEntityContainer) don't route
            // inventory slot-op packets to InvNetworkUtil; only BlockEntityOpenableContainer
            // does. Without this, server discards client slot operations and pushes its
            // unchanged state back, looking like a "rollback" in the dialog.
            if (Api.World.Claims.TryAccess(fromPlayer, Pos, EnumBlockAccessFlags.Use))
            {
                Inventory.InvNetworkUtil.HandleClientPacket(fromPlayer, packetid, data);
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
            }
        }

        public void OpenDialog(IClientPlayer player)
        {
            if (Api?.Side != EnumAppSide.Client) return;
            var capi = (ICoreClientAPI)Api;

            if (clientDialog != null && clientDialog.IsOpen) return;

            clientDialog?.Dispose();
            clientDialog = new ImGuiDialogPotionCauldron(capi, this);
            clientDialog.Open();
            capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, PacketIdOpenCauldron);
        }

        public void OnDialogClosed()
        {
            if (Api?.Side != EnumAppSide.Client) return;
            var capi = (ICoreClientAPI)Api;
            capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, PacketIdCloseCauldron);
        }

        public PotionCauldronRecipe ResolveCurrentRecipe()
        {
            if (string.IsNullOrEmpty(CurrentRecipeCode)) return null;
            foreach (var rec in simplealchemy.potionCauldronRecipes)
            {
                if (rec.Code == CurrentRecipeCode) return rec;
            }
            return null;
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (clientRegenMesh)
            {
                clientRegenMesh = false;
                currentRightMesh = GenRightMesh();
            }
            mesher.AddMeshData(currentRightMesh);
            return true;
        }

        public void RedoMesh()
        {
            if (Api.Side == EnumAppSide.Client)
            {
                currentRightMesh = GenRightMesh();
            }
        }
    }
}
