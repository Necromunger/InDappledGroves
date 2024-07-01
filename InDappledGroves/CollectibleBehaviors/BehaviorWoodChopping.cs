using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Interfaces;
using InDappledGroves.Util.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames;

namespace InDappledGroves
{
    class BehaviorWoodChopping : CollectibleBehavior, IBehaviorVariant
    { 
        ICoreAPI api;
        ICoreClientAPI capi;
        BlockPos oldBlockPos = null;
        public BehaviorWoodChopping(CollectibleObject collObj) : base(collObj)
        {

            this.collObj = collObj;
        }

        public SkillItem[] GetSkillItems()
        {
            return toolModes ?? new SkillItem[] { null };
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            this.capi = (api as ICoreClientAPI);

            this.toolModes = ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "idgAxeChopModes", delegate
            {

                SkillItem[] array;
                array = new SkillItem[]
                {
                        new SkillItem
                        {
                            Code = new AssetLocation("chopping"),
                            Name = Lang.Get("Chopping", Array.Empty<object>())
                        }
                };

                if (capi != null)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("indappledgroves:textures/icons/" + array[i].Code.FirstCodePart().ToString() + ".svg"), 48, 48, 5, new int?(-1)));
                        array[i].TexturePremultipliedAlpha = false;
                    }
                }
                return array;
            });
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            //if(oldBlockPos != null && blockSel != null && oldBlockPos != blockSel.Position)
            //{
            //    handHandling = EnumHandHandling.NotHandled;
            //    handling = EnumHandling.PassThrough;
            //}
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handHandling, ref handling);

        }
        public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, ref EnumHandling handling)
        {
            return base.OnHeldAttackStep(secondsPassed, slot, byEntity, blockSelection, entitySel, ref handling);
        }

        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, ref EnumHandling handling)
        {
            base.OnHeldAttackStop(secondsPassed, slot, byEntity, blockSelection, entitySel, ref handling);
        }

        public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handling)
        {
            return base.OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSelection, entitySel, cancelReason, ref handling);
        }

        #region TreeFelling
        public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter, ref EnumHandling handled)
        {   
            BlockPos pos = blockSel.Position;
            if (oldBlockPos == null) { oldBlockPos = pos; };
            ITreeAttribute tempAttr = itemslot.Itemstack.TempAttributes;
            int posx = tempAttr.GetInt("lastposX", -1);
            int posy = tempAttr.GetInt("lastposY", -1);
            int posz = tempAttr.GetInt("lastposZ", -1);
            float treeResistance = tempAttr.GetFloat("treeResistance", 1);

            string[] woods = new[] { "log", "ferntree", "fruittree", "bamboo", "lognarrow", "logsection" };
            if (oldBlockPos != pos || api.World.BlockAccessor.GetBlock(pos).Variant["type"] == "placed" || !woods.Contains(api.World.BlockAccessor.GetBlock(pos).FirstCodePart()))
            {
                oldBlockPos = pos;
                api.Logger.Debug(api.Side.ToString() + " registered a blockPos change from " + oldBlockPos.ToString() + " to " + pos.ToString() + " at counter " + counter.ToString());
                api.Logger.Debug(api.Side.ToString() + " remaining resistance is " + remainingResistance);
                handled = EnumHandling.PreventDefault;
                return remainingResistance;
            };

            if (pos.X != posx || pos.Y != posy || pos.Z != posz || counter % 30 == 0)
            {
                float baseResistance;
                int woodTier;

                Stack<BlockPos> foundPositions = FindTree(player.Entity.World, pos, out baseResistance, out woodTier);
                treeResistance = (float)baseResistance/IDGTreeConfig.Current.TreeFellingDivisor;
                if (collObj.ToolTier < woodTier - 3)
                {
                    //handled = EnumHandling.Handled;
                    return treeResistance * 1.25f;
                }
                tempAttr.SetFloat("treeResistance", treeResistance);
            }
            else
            {
                treeResistance = tempAttr.GetFloat("treeResistance", 1f);
            }
            
            tempAttr.SetInt("lastposX", pos.X);
            tempAttr.SetInt("lastposY", pos.Y);
            tempAttr.SetInt("lastposZ", pos.Z);
            float treeDmg = treeResistance - ((collObj.GetMiningSpeed(itemslot.Itemstack, blockSel, api.World.BlockAccessor.GetBlock(pos), player)) * counter / 10) ;
            //api.Logger.Debug("collObj is " + collObj.Code.ToString() + " and its mining speed is " + collObj.GetMiningSpeed(itemslot.Itemstack, blockSel, api.World.BlockAccessor.GetBlock(pos), player));
            remainingResistance = treeDmg;
            api.Logger.Debug(api.Side.ToString() + " remaining resistance is " + remainingResistance);
            handled = EnumHandling.PreventDefault;
            return treeDmg;            
        }
        
        public Stack<BlockPos> FindTree(IWorldAccessor world, BlockPos startPos, out float resistance, out int woodTier)
        {
            Queue<Vec4i> queue = new();
            HashSet<BlockPos> checkedPositions = new();
            Stack<BlockPos> foundPositions = new();
            Block startBlock = api.World.BlockAccessor.GetBlock(startPos);
            BlockPos secondPos = null;
            resistance = 0;
            woodTier = 0;

            api.World.BlockAccessor.WalkBlocks(startPos.AddCopy(1, 1, 1), startPos.AddCopy(-1, 1, -1), (block, x, y, z) =>
            {
                string[] woods = new[] { "log", "ferntree", "fruittree", "bamboo", "lognarrow"};
                if (woods.Contains<string>(block.Code.FirstCodePart())) { secondPos = new BlockPos(x, y, z); }
            }, true);

            if (startBlock.Code.FirstCodePart() == "treestump")
            {
                startPos = secondPos != null ? secondPos : startPos;
            }

            Block block = world.BlockAccessor.GetBlock(startPos, 0);

            if (block.Code == null) return foundPositions;

            JsonObject attributes = block.Attributes;
            string treeFellingGroupCode = attributes?["treeFellingGroupCode"].AsString();
            JsonObject attributes2 = block.Attributes;
            int spreadIndex = attributes2?["treeFellingGroupSpreadIndex"].AsInt(0) ?? 0;
            JsonObject attributes3 = block.Attributes;

            if (attributes3 != null && !attributes3["treeFellingCanChop"].AsBool(true))
            {
                return foundPositions;
            }

            // Must start with a log
            EnumTreeFellingBehavior bh = EnumTreeFellingBehavior.Chop;
            ICustomTreeFellingBehavior ctfbh = block as ICustomTreeFellingBehavior;
            if (ctfbh != null)
            {
                bh = ctfbh.GetTreeFellingBehavior(startPos, null, spreadIndex);
                if (bh == EnumTreeFellingBehavior.NoChop)
                {
                    return foundPositions;
                }
            }

            if (spreadIndex < 2) return foundPositions;

            if (treeFellingGroupCode == null) return foundPositions;

            string treeFellingGroupLeafCode = null;

            queue.Enqueue(new Vec4i(startPos.X, startPos.Y, startPos.Z, spreadIndex));
            foundPositions.Push(startPos);
            checkedPositions.Add(startPos);

            while (queue.Count > 0 && foundPositions.Count <= 2500)
            {

                Vec4i pos = queue.Dequeue();
                block = world.BlockAccessor.GetBlock(pos.X, pos.Y, pos.Z);
                ICustomTreeFellingBehavior ctfbhh = block as ICustomTreeFellingBehavior;
                if (ctfbhh != null)
                {
                    bh = ctfbhh.GetTreeFellingBehavior(startPos, null, spreadIndex);
                }
                if (bh != EnumTreeFellingBehavior.NoChop)
                {
                    for (int i = 0; i < Vec3i.DirectAndIndirectNeighbours.Length; i++)
                    {
                        Vec3i facing = Vec3i.DirectAndIndirectNeighbours[i];
                        BlockPos neibPos = new(pos.X + facing.X, pos.Y + facing.Y, pos.Z + facing.Z);

                        float hordist = GameMath.Sqrt(neibPos.HorDistanceSqTo((double)startPos.X, (double)startPos.Z));
                        float vertdist = (float)(neibPos.Y - startPos.Y);
                        float f = (bh == EnumTreeFellingBehavior.ChopSpreadVertical) ? 0.5f : 2f;

                        // "only breaks blocks inside an upside down square base pyramid"
                        if (hordist - 1f < f * vertdist && !checkedPositions.Contains(neibPos))
                        {
                            block = world.BlockAccessor.GetBlock(neibPos);
                            if (!(block.Code == null) && block.Id != 0)
                            {
                                JsonObject attributes4 = block.Attributes;
                                string ngcode = (attributes4 != null) ? attributes4["treeFellingGroupCode"].AsString(null) : null;
                                if (ngcode != treeFellingGroupCode)
                                {
                                    if (ngcode == null)
                                    {
                                        goto IL_32A;
                                    }
                                    if (ngcode != treeFellingGroupLeafCode)
                                    {
                                        if (treeFellingGroupLeafCode != null || block.BlockMaterial != EnumBlockMaterial.Leaves || ngcode.Length != treeFellingGroupCode.Length + 1 || !ngcode.EndsWith(treeFellingGroupCode))
                                        {
                                            goto IL_32A;
                                        }
                                        treeFellingGroupLeafCode = ngcode;
                                    }
                                }
                                JsonObject attributes5 = block.Attributes;
                                int nspreadIndex = (attributes5 != null) ? attributes5["treeFellingGroupSpreadIndex"].AsInt(0) : 0;
                                if (pos.W >= nspreadIndex)
                                {
                                    checkedPositions.Add(neibPos);
                                    if (bh != EnumTreeFellingBehavior.ChopSpreadVertical || facing.Equals(0, 1, 0) || nspreadIndex <= 0)
                                    {
                                        resistance += block.Resistance;
                                        if (woodTier == 0)
                                        {
                                            woodTier = nspreadIndex;
                                        }
                                        foundPositions.Push(neibPos.Copy());
                                        queue.Enqueue(new Vec4i(neibPos.X, neibPos.Y, neibPos.Z, nspreadIndex));
                                    }
                                }
                            }
                        }
                    IL_32A:;
                    }
                }
            }
            return foundPositions;
        }


        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier, ref EnumHandling bhHandling)
        {
            //IPlayer byPlayer = null;
            //if (byEntity is EntityPlayer)
            //{
            //    byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            //}

            if (oldBlockPos != null && oldBlockPos != blockSel.Position) { bhHandling = EnumHandling.PreventDefault; return false; }

            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier, ref bhHandling);
        }


        //public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1f, ref EnumHandling bhHandling)
        //{
        //    IPlayer byPlayer = null;
        //    if (byEntity is EntityPlayer)
        //    {
        //        byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
        //    }
        //    if (oldBlockPos != null && oldBlockPos != blockSel.Position) { return false; }

        //    return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier, ref bhHandling);
        //}


        //Particle Handlers
        private SimpleParticleProperties InitializeWoodParticles()
        {
            return new SimpleParticleProperties()
            {
                MinPos = new Vec3d(),
                AddPos = new Vec3d(),
                MinQuantity = 0,
                AddQuantity = 3,
                Color = ColorUtil.ToRgba(100, 200, 200, 200),
                GravityEffect = 1f,
                WithTerrainCollision = true,
                ParticleModel = EnumParticleModel.Quad,
                LifeLength = 0.5f,
                MinVelocity = new Vec3f(-1, 2, -1),
                AddVelocity = new Vec3f(2, 0, 2),
                MinSize = 0.07f,
                MaxSize = 0.1f,
                WindAffected = true
            };
        }

        static readonly SimpleParticleProperties dustParticles = new()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(),
            MinQuantity = 0,
            AddQuantity = 3,
            Color = ColorUtil.ToRgba(100, 200, 200, 200),
            GravityEffect = 1f,
            WithTerrainCollision = true,
            ParticleModel = EnumParticleModel.Quad,
            LifeLength = 0.5f,
            MinVelocity = new Vec3f(-1, 2, -1),
            AddVelocity = new Vec3f(2, 0, 2),
            MinSize = 0.07f,
            MaxSize = 0.1f,
            WindAffected = true
        };

        #endregion TreeFelling

        //Create function by which interactions will find recipes using the target block and the current tool mode.
        WorldInteraction[] interactions = null;
        public SkillItem[] toolModes;
    }
}