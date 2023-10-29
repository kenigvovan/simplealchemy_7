using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace simplealchemy.src
{

    public class ItemPotion : Item
    {
        public Dictionary<string, float> dic = new Dictionary<string, float>();
        public string potionId;
        public int duration;
        public int tickSec = 0;
        public float statChange = 0;
        public int tier = 1;
        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
        {
            return "eat";
        }

        public override void OnGroundIdle(EntityItem entityItem)
        {
            entityItem.Die(EnumDespawnReason.Removed);

            if (entityItem.World.Side == EnumAppSide.Server)
            {
                WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(entityItem.Itemstack);
                float litres = (float)entityItem.Itemstack.StackSize / props.ItemsPerLitre;

                entityItem.World.SpawnCubeParticles(entityItem.SidedPos.XYZ, entityItem.Itemstack, 0.75f, (int)(litres * 2), 0.45f);
                entityItem.World.PlaySoundAt(new AssetLocation("sounds/environment/smallsplash"), (float)entityItem.SidedPos.X, (float)entityItem.SidedPos.Y, (float)entityItem.SidedPos.Z, null);
            }


            base.OnGroundIdle(entityItem);

        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            string strength = Variant["strength"] is string str ? string.Intern(str) : "none";
            JsonObject potion = Attributes?["potioninfo"];
            if (potion?.Exists == true)
            {
                potionId = potion["potionId"].AsString();
                duration = potion["duration"][strength].AsInt();
                statChange = Attributes["statChangeValue"].AsFloat();
                      

            }
            switch (strength)
            {
                case "strong":
                    tier = 3;
                    break;
                case "medium":
                    tier = 2;
                    break;
                default:
                    break;
            }
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            //api.Logger.Debug("potion {0}, {1}", dic.Count, potionId);
            if (potionId != "" && potionId != null)
            {
                string poop = potionId;
                //api.Logger.Debug("[Potion] check if drinkable {0}", byEntity.WatchedAttributes.GetLong(potionId));
                /* This checks if the potion effect callback is on */
                if (byEntity.WatchedAttributes.GetLong(potionId) == 0)
                {
                    byEntity.World.RegisterCallback((dt) =>
                    {
                        if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
                        {
                            if (Code.Path.Contains("portion"))
                            {
                                byEntity.World.PlaySoundAt(new AssetLocation("simplealchemy:sounds/player/drink"), byEntity);
                            }
                            else
                            {
                                byEntity.PlayEntitySound("eat", (byEntity as EntityPlayer)?.Player);
                            }
                        }
                    }, 200);
                    handling = EnumHandHandling.PreventDefault;
                    return;
                }
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
            if (secondsUsed > 1.45f && byEntity.World.Side == EnumAppSide.Server)
            {
                if (potionId == "recallpotionid")
                {

                }
                else if (tickSec == 0)
                {
                    //TempEffect potionEffect = new TempEffect();
                    //potionEffect.tempEntityStats((byEntity as EntityPlayer), dic, "potionmod", duration, potionId);
                }
                else
                {
                   // TempEffect potionEffect = new TempEffect();
                   // potionEffect.tempTickEntityStats((byEntity as EntityPlayer), dic, "potionmod", duration, potionId, tickSec, health);
                }
                if (byEntity is EntityPlayer)
                {
                    IServerPlayer player = (byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID) as IServerPlayer);
                    if (potionId == "recallpotionid")
                    {
                        FuzzyEntityPos spawn = player.GetSpawnPosition(false);
                        byEntity.TeleportTo(spawn);
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.InfoLogChatGroup, "You feel the effects of the " + slot.Itemstack.GetName(), EnumChatType.Notification);
                    }
                }

                slot.TakeOut(1);
                slot.MarkDirty();
            }
        }


        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            dsc.AppendLine(Lang.Get("\n"));
            if(potionId.Equals("walkspeedtypepotion"))
            {
                dsc.AppendLine(Lang.Get("simplealchemy:stat_change_by_percents", statChange > 0? "+" + statChange * tier * 100: "-" + statChange, "walkspeed"));
            }
            else if (potionId.Equals("miningspeedtypepotion"))
            {
                dsc.AppendLine(Lang.Get("simplealchemy:stat_change_by_percents", statChange > 0 ? "+" + statChange * tier * 100 : "-" + statChange, "mining_speed"));
            }
            else if (potionId.Equals("regenerationtypepotion"))
            {
                dsc.AppendLine(Lang.Get("simplealchemy:regen_hp_per_tick", statChange));
            }
            else if (potionId.Equals("meleetypepotion"))
            {
                dsc.AppendLine(Lang.Get("simplealchemy:stat_change_by_percents", statChange > 0 ? "+" + statChange * tier * 100 : "-" + statChange, "melee_attack"));
            }
            else if (potionId.Equals("temporalstabilityrestore"))
            {
                dsc.AppendLine(Lang.Get("simplealchemy:restore_stability",  tier * 30));
            }
            else if (potionId.Equals("forgetting"))
            {
                dsc.AppendLine(Lang.Get("simplealchemy:forgetting_potion_desc"));
            }

            dsc.AppendLine(Lang.Get("simplealchemy:duration_sec", duration));

        }
    }
}