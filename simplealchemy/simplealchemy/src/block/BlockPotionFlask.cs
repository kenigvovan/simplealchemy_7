using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using System.Text;
using simplealchemy.src;
using effectshud.src;
using effectshud.src.DefaultEffects;
using Vintagestory.Client.NoObf;

namespace simplesimplealchemy.src
{
    //Add perish time to potions but potion flasks have low perish rates or do not perish
    public class BlockPotionFlask : /*BlockBucket */BlockLiquidContainerTopOpened
    {
        LiquidTopOpenContainerProps Props;
        public override float TransferSizeLitres => Props.TransferSizeLitres;
        public override float CapacityLitres => Props.CapacityLitres;

        protected override string meshRefsCacheKey => Code.ToShortString() + "meshRefs";
        protected override AssetLocation emptyShapeLoc => Props.EmptyShapeLoc;
        protected override AssetLocation contentShapeLoc => Props.LiquidContentShapeLoc;
        protected override AssetLocation liquidContentShapeLoc => Props.LiquidContentShapeLoc;
        protected override float liquidMaxYTranslate => Props.LiquidMaxYTranslate;
        protected override float liquidYTranslatePerLitre => liquidMaxYTranslate / CapacityLitres;

        public string potionId = "";
        public int duration = 0;
        public int tickSec = 0;
        public float helpValue = 0;
        public int tier = 1;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (Attributes?["liquidContainerProps"].Exists == true)
            {
                Props = Attributes["liquidContainerProps"].AsObject<LiquidTopOpenContainerProps>(null, Code.Domain);
            }
        }

        public new MeshData GenMesh(ICoreClientAPI capi, ItemStack contentStack, BlockPos forBlockPos = null)
        {
            if (this == null || Code.Path.Contains("clay")) return null;
            Shape shape = null;
            MeshData flaskmesh = null;


            if (contentStack != null)
            {
                WaterTightContainableProps props = GetContainableProps(contentStack);
                float level = 0;
                FlaskTextureSource contentSource = null;
                if (props == null) return null;
                
                contentSource = new FlaskTextureSource(capi, contentStack, props.Texture, this);

                level = contentStack.StackSize / props.ItemsPerLitre;
                
                
                if (level == 0)
                {
                    shape = capi.Assets.TryGet(emptyShapeLoc).ToObject<Shape>();
                }
                else if (level <= 0.5)
                {
                    shape = capi.Assets.TryGet("simplealchemy:shapes/block/flasks/pointflask_filled.json").ToObject<Shape>();
                }
                else if (level > 0.5)
                {
                    shape = capi.Assets.TryGet("simplealchemy:shapes/block/flasks/pointflask_filled.json").ToObject<Shape>();
                }
                
              

                capi.Tesselator.TesselateShape("potionflask", shape, out flaskmesh, contentSource, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ));
            }

            return flaskmesh;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            if (Code.Path.Contains("clay")) return;
            Dictionary<string, MultiTextureMeshRef> meshrefs = null;

            object obj;
            if (capi.ObjectCache.TryGetValue(meshRefsCacheKey, out obj))
            {
                meshrefs = obj as Dictionary<string, MultiTextureMeshRef>;
            }
            else
            {
                capi.ObjectCache[meshRefsCacheKey] = meshrefs = new Dictionary<string, MultiTextureMeshRef>();
            }

            ItemStack contentStack = GetContent(itemstack);
            if (contentStack == null) return;

            MultiTextureMeshRef meshRef = null;

            if (!meshrefs.TryGetValue(contentStack.Collectible.Code.Path + Code.Path + contentStack.StackSize, out meshRef))
            {
                MeshData meshdata = GenMesh(capi, contentStack);
                if (meshdata == null) return;


                meshrefs[contentStack.Collectible.Code.Path + Code.Path + contentStack.StackSize] = meshRef = capi.Render.UploadMultiTextureMesh(meshdata);

            }

            renderinfo.ModelRef = meshRef;
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
            if (capi == null) return;

            object obj;
            if (capi.ObjectCache.TryGetValue(meshRefsCacheKey, out obj))
            {
                Dictionary<string, MultiTextureMeshRef> meshrefs = obj as Dictionary<string, MultiTextureMeshRef>;

                foreach (var val in meshrefs)
                {
                    val.Value.Dispose();
                }

                capi.ObjectCache.Remove(meshRefsCacheKey);
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (!hotbarSlot.Empty && hotbarSlot.Itemstack.Collectible.Attributes?.IsTrue("handleLiquidContainerInteract") == true)
            {
                EnumHandHandling handling = EnumHandHandling.NotHandled;
                hotbarSlot.Itemstack.Collectible.OnHeldInteractStart(hotbarSlot, byPlayer.Entity, blockSel, null, true, ref handling);
                if (handling == EnumHandHandling.PreventDefault || handling == EnumHandHandling.PreventDefaultAction) return true;
            }

            if (hotbarSlot.Empty || !(hotbarSlot.Itemstack.Collectible is ILiquidInterface)) return base.OnBlockInteractStart(world, byPlayer, blockSel);


            CollectibleObject obj = hotbarSlot.Itemstack.Collectible;

            bool singleTake = byPlayer.WorldData.EntityControls.Sneak;
            bool singlePut = byPlayer.WorldData.EntityControls.Sprint;

            if (obj is ILiquidSource objLso && !singleTake)
            {
                var contentStackToMove = objLso.GetContent(hotbarSlot.Itemstack);

                float litres = singlePut ? Props.TransferSizeLitres : Props.CapacityLitres;
                int moved = TryPutLiquid(blockSel.Position, contentStackToMove, litres);

                if (moved > 0)
                {
                    objLso.TryTakeContent(hotbarSlot.Itemstack, moved);
                    DoLiquidMovedEffects(byPlayer, contentStackToMove, moved, EnumLiquidDirection.Pour);
                    return true;
                }
            }

            if (obj is ILiquidSink objLsi && !singlePut)
            {
                ItemStack owncontentStack = GetContent(blockSel.Position);

                if (owncontentStack == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);

                var liquidStackForParticles = owncontentStack.Clone();

                float litres = singleTake ? Props.TransferSizeLitres : Props.CapacityLitres;
                int moved;

                if (hotbarSlot.Itemstack.StackSize == 1)
                {
                    moved = objLsi.TryPutLiquid(hotbarSlot.Itemstack, owncontentStack, litres);
                }
                else
                {
                    ItemStack containerStack = hotbarSlot.Itemstack.Clone();
                    containerStack.StackSize = 1;
                    moved = objLsi.TryPutLiquid(containerStack, owncontentStack, litres);

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
                    DoLiquidMovedEffects(byPlayer, liquidStackForParticles, moved, EnumLiquidDirection.Fill);
                    return true;
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if(blockSel == null)
            {
                handling = EnumHandHandling.PreventDefault;
                return;
            }
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            Vec3d pos = byEntity.Pos.AheadCopy(0.4f).XYZ.Add(byEntity.LocalEyePos);
            pos.Y -= 0.4f;

            IPlayer player = (byEntity as EntityPlayer).Player;


            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform tf = new ModelTransform();
                tf.Origin.Set(1.1f, 0.5f, 0.5f);
                tf.EnsureDefaultValues();

                tf.Translation.X -= Math.Min(1.7f, secondsUsed * 4 * 1.8f) / FpHandTransform.ScaleXYZ.X;
                tf.Translation.Y += Math.Min(0.4f, secondsUsed * 1.8f) / FpHandTransform.ScaleXYZ.X;
                tf.Scale = 1 + Math.Min(0.5f, secondsUsed * 4 * 1.8f) / FpHandTransform.ScaleXYZ.X;
                tf.Rotation.X += Math.Min(40f, secondsUsed * 350 * 0.75f) / FpHandTransform.ScaleXYZ.X;

                if (secondsUsed > 0.5f)
                {
                    tf.Translation.Y += GameMath.Sin(30 * secondsUsed) / 10 / FpHandTransform.ScaleXYZ.Y;
                }

                byEntity.Controls.UsingHeldItemTransformBefore = tf;

                return secondsUsed <= 1.5f;
            }
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            ItemStack content = GetContent(slot.Itemstack);
           
            if (secondsUsed > 1.45f && byEntity.World.Side == EnumAppSide.Server && content != null)
            {
                var potionItem = (content.Item as ItemPotion);
                if(potionItem == null)
                {
                    return;
                }
                if (content.MatchesSearchText(byEntity.World, "potion"))
                {
                    string strength = content.Item.Variant["strength"] is string str ? string.Intern(str) : "none";
                  

                    if (potionItem.potionId == "walkspeedtypfffepotion")
                    {
                        effectshud.src.DefaultEffects.WalkSpeedEffect eff = new effectshud.src.DefaultEffects.WalkSpeedEffect(this.tier);
                        eff.SetExpiryInRealSeconds(duration);
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    else if(potionItem.potionId == "walkspeedtypepotion")
                    {
                        float statChangeValue = content.ItemAttributes["statChangeValue"].AsFloat();
  
                        effectshud.src.DefaultEffects.WalkSpeedEffect eff = new effectshud.src.DefaultEffects.WalkSpeedEffect(potionItem.tier);
                        eff.statChangeValue = potionItem.statChange;
                        eff.SetExpiryInRealSeconds(potionItem.duration);
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    else if (potionItem.potionId == "regenerationtypepotion")
                    {

                        effectshud.src.DefaultEffects.RegenerationEffect eff = new effectshud.src.DefaultEffects.RegenerationEffect();
                        eff.Tier = potionItem.tier;
                        eff.hpPerTick= potionItem.statChange;
                        eff.SetExpiryInRealSeconds(potionItem.duration);
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    else if (potionItem.potionId == "meleestrengthtypepotion")
                    {
                        effectshud.src.DefaultEffects.StrengthMeleeEffect eff = new effectshud.src.DefaultEffects.StrengthMeleeEffect();
                        eff.statChangeValue = potionItem.statChange;
                        eff.Tier = potionItem.tier;
                        eff.SetExpiryInRealSeconds(potionItem.duration);
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    else if (potionItem.potionId == "miningspeedtypepotion")
                    {
                        effectshud.src.DefaultEffects.MiningSpeedEffect eff = new effectshud.src.DefaultEffects.MiningSpeedEffect();
                        eff.Tier = potionItem.tier;
                        eff.statChangeValue = potionItem.statChange;
                        eff.SetExpiryInRealSeconds(potionItem.duration);
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    else if (potionItem.potionId == "temporalstabilityrestore")
                    {
                        effectshud.src.DefaultEffects.TemporalStabilityRestoreEffect eff = new effectshud.src.DefaultEffects.TemporalStabilityRestoreEffect();
                        eff.Tier = potionItem.tier;
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    else if (potionItem.potionId == "forgetting")
                    {
                        if (simplealchemy.src.Config.Current.forgetingPotionWorks.Val)
                        {
                            if (simplealchemy.src.simplealchemy.lastPlayerClassChange.TryGetValue((byEntity as EntityPlayer).PlayerUID, out long val))
                            {
                                if (val > byEntity.Api.World.Calendar.ElapsedDays)
                                {
                                    return;
                                }
                            }
                            effectshud.src.DefaultEffects.ForgettingEffect eff = new effectshud.src.DefaultEffects.ForgettingEffect();
                            eff.Tier = potionItem.tier;
                            effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);

                            simplealchemy.src.simplealchemy.lastPlayerClassChange[(byEntity as EntityPlayer).PlayerUID] = (long)byEntity.Api.World.Calendar.ElapsedDays + simplealchemy.src.Config.Current.daysBetweenClassChangeWithPotion.Val;

                        }
                    }
                    else if (potionItem.potionId == "temporalcharge")
                    {
                        if (simplealchemy.src.simplealchemy.lastPlayerClassChange.TryGetValue((byEntity as EntityPlayer).PlayerUID, out long val))
                        {
                            if (val > byEntity.Api.World.Calendar.ElapsedDays)
                            {
                                return;
                            }
                        }
                        effectshud.src.DefaultEffects.TemporalChargeEffect eff = new effectshud.src.DefaultEffects.TemporalChargeEffect();
                        eff.Tier = potionItem.tier;
                        eff.SetExpiryInRealSeconds(potionItem.duration);
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    else if (potionItem.potionId == "canweightbuff")
                    {
                        if (simplealchemy.src.simplealchemy.lastPlayerClassChange.TryGetValue((byEntity as EntityPlayer).PlayerUID, out long val))
                        {
                            if (val > byEntity.Api.World.Calendar.ElapsedDays)
                            {
                                return;
                            }
                        }
                        effectshud.src.DefaultEffects.CANWeightBuffEffect eff = new effectshud.src.DefaultEffects.CANWeightBuffEffect();
                        eff.Tier = potionItem.tier;
                        eff.SetExpiryInRealSeconds(potionItem.duration);
                        eff.statChangeValue = potionItem.statChange;
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    else if (potionItem.potionId == "extendedmaxbreath")
                    {
                        if (simplealchemy.src.simplealchemy.lastPlayerClassChange.TryGetValue((byEntity as EntityPlayer).PlayerUID, out long val))
                        {
                            if (val > byEntity.Api.World.Calendar.ElapsedDays)
                            {
                                return;
                            }
                        }
                        effectshud.src.DefaultEffects.ExtendedMaxBreathEffect eff = new effectshud.src.DefaultEffects.ExtendedMaxBreathEffect();
                        eff.Tier = potionItem.tier;
                        eff.SetExpiryInRealSeconds(potionItem.duration);
                        //eff.statChangeValue = potionItem.statChange;
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    else if (potionItem.potionId == "invisibility")
                    {
                        if (simplealchemy.src.simplealchemy.lastPlayerClassChange.TryGetValue((byEntity as EntityPlayer).PlayerUID, out long val))
                        {
                            if (val > byEntity.Api.World.Calendar.ElapsedDays)
                            {
                                return;
                            }
                        }
                        effectshud.src.DefaultEffects.InvisibilityEffect eff = new effectshud.src.DefaultEffects.InvisibilityEffect();
                        //eff.tier = potionItem.tier;
                        eff.SetExpiryInRealSeconds(potionItem.duration);
                        //eff.statChangeValue = potionItem.statChange;
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    else if (potionItem.potionId == "safefall")
                    {
                        if (simplealchemy.src.simplealchemy.lastPlayerClassChange.TryGetValue((byEntity as EntityPlayer).PlayerUID, out long val))
                        {
                            if (val > byEntity.Api.World.Calendar.ElapsedDays)
                            {
                                return;
                            }
                        }
                        effectshud.src.DefaultEffects.SafeFallEffect eff = new effectshud.src.DefaultEffects.SafeFallEffect();
                        //eff.tier = potionItem.tier;
                        eff.SetExpiryInRealSeconds(potionItem.duration);
                        //eff.statChangeValue = potionItem.statChange;
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    else if (potionItem.potionId == "thorns")
                    {
                        if (simplealchemy.src.simplealchemy.lastPlayerClassChange.TryGetValue((byEntity as EntityPlayer).PlayerUID, out long val))
                        {
                            if (val > byEntity.Api.World.Calendar.ElapsedDays)
                            {
                                return;
                            }
                        }
                        effectshud.src.DefaultEffects.ThornsEffect eff = new effectshud.src.DefaultEffects.ThornsEffect();
                        eff.Tier = potionItem.tier;
                        eff.SetExpiryInRealSeconds(potionItem.duration);
                        //eff.statChangeValue = potionItem.statChange;
                        effectshud.src.effectshud.ApplyEffectOnEntity(byEntity, eff);
                    }
                    //thorns
                    IPlayer player = null;
                    if (byEntity is EntityPlayer) player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

                    splitStackAndPerformAction(byEntity, slot, (stack) => TryTakeLiquid(stack, 1f)?.StackSize ?? 0);
                    slot.MarkDirty();

                    EntityPlayer entityPlayer = byEntity as EntityPlayer;
                    if (entityPlayer == null)
                    {
                        return;
                    }
                    entityPlayer.Player.InventoryManager.BroadcastHotbarSlot();
                }
            }
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        private int splitStackAndPerformAction(Entity byEntity, ItemSlot slot, System.Func<ItemStack, int> action)
        {
            if (slot.Itemstack.StackSize == 1)
            {
                int moved = action(slot.Itemstack);

                if (moved > 0)
                {
                    int maxstacksize = slot.Itemstack.Collectible.MaxStackSize;

                    EntityPlayer entityPlayer = byEntity as EntityPlayer;
                    if (entityPlayer == null)
                    {
                        return moved;
                    }
                    (byEntity as EntityPlayer)?.WalkInventory((pslot) =>
                    {
                        if (pslot.Empty || pslot is ItemSlotCreative || pslot.StackSize == pslot.Itemstack.Collectible.MaxStackSize) return true;
                        int mergableq = slot.Itemstack.Collectible.GetMergableQuantity(slot.Itemstack, pslot.Itemstack, EnumMergePriority.DirectMerge);
                        if (mergableq == 0) return true;

                        BlockLiquidContainerBase selfLiqBlock = slot.Itemstack.Collectible as BlockLiquidContainerBase;
                        BlockLiquidContainerBase invLiqBlock = pslot.Itemstack.Collectible as BlockLiquidContainerBase;

                        int? num3;
                        if (selfLiqBlock == null)
                        {
                            num3 = null;
                        }
                        else
                        {
                            ItemStack content = selfLiqBlock.GetContent(slot.Itemstack);
                            num3 = ((content != null) ? new int?(content.StackSize) : null);
                        }
                        int? num4 = num3;
                        int valueOrDefault = num4.GetValueOrDefault();
                        int? num5;
                        if (invLiqBlock == null)
                        {
                            num5 = null;
                        }
                        else
                        {
                            ItemStack content2 = invLiqBlock.GetContent(pslot.Itemstack);
                            num5 = ((content2 != null) ? new int?(content2.StackSize) : null);
                        }
                        num4 = num5;
                        if (valueOrDefault != num4.GetValueOrDefault())
                        {
                            return true;
                        }

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

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            ItemStack content = GetContent(inSlot.Itemstack);
            if (content != null)
            {
                content.Collectible.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            }
        }

    }

    public class FlaskTextureSource : ITexPositionSource
    {
        public ItemStack forContents;
        private ICoreClientAPI capi;

        TextureAtlasPosition contentTextPos;
        TextureAtlasPosition blockTextPos;
        TextureAtlasPosition corkTextPos;
        TextureAtlasPosition bracingTextPos;
        CompositeTexture contentTexture;

        public FlaskTextureSource(ICoreClientAPI capi, ItemStack forContents, CompositeTexture contentTexture, Block flask)
        {
            this.capi = capi;
            this.forContents = forContents;
            this.contentTexture = contentTexture;
            this.corkTextPos = capi.BlockTextureAtlas.GetPosition(flask, "handle");
            this.blockTextPos = capi.BlockTextureAtlas.GetPosition(flask, "quartz");
            this.bracingTextPos = capi.BlockTextureAtlas.GetPosition(flask, "quartzglass");
        }

        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (textureCode == "handle" && corkTextPos != null) return corkTextPos;
                if (textureCode == "quartz" && blockTextPos != null) return blockTextPos;
                if (textureCode == "quartzglass" && bracingTextPos != null) return bracingTextPos;
                if (contentTextPos == null)
                {
                    int textureSubId;

                    textureSubId = ObjectCacheUtil.GetOrCreate<int>(capi, "contenttexture-" + contentTexture.ToString(), () =>
                    {
                        TextureAtlasPosition texPos;
                        int id = 0;

                        BitmapRef bmp = capi.Assets.TryGet(contentTexture.Base.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"))?.ToBitmap(capi);
                        if (bmp != null)
                        {
                            capi.BlockTextureAtlas.InsertTexture(bmp, out id, out texPos);
                            bmp.Dispose();
                        }

                        return id;
                    });

                    contentTextPos = capi.BlockTextureAtlas.Positions[textureSubId];
                }

                return contentTextPos;
            }
        }

        public Size2i AtlasSize => capi.BlockTextureAtlas.Size;
    }
}