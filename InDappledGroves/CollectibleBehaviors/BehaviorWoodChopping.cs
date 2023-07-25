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

            //interactions = ObjectCacheUtil.GetOrCreate(api, "idgaxeInteractions", () =>
            //{
            //    return new WorldInteraction[] {
            //        new WorldInteraction()
            //            {
            //                ActionLangCode = "indappledgroves:itemhelp-axe-chopwood",
            //                MouseButton = EnumMouseButton.Right
            //            },
            //        };
            //});
        }

        #region TreeFelling
        public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter, ref EnumHandling handled)
        {
            BlockPos pos = blockSel.Position;
            string[] woods = new[] {"log", "ferntree", "fruittree", "bamboo", "lognarrow", "logsection"};
            if (api.World.BlockAccessor.GetBlock(pos).Variant["type"] == "placed" || !woods.Contains(api.World.BlockAccessor.GetBlock(pos).FirstCodePart()))
            {
                handled = EnumHandling.PreventSubsequent;
                return base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter, ref handled);
            };

            ITreeAttribute tempAttr = itemslot.Itemstack.TempAttributes;
            int posx = tempAttr.GetInt("lastposX", -1);
            int posy = tempAttr.GetInt("lastposY", -1);
            int posz = tempAttr.GetInt("lastposZ", -1);
            
            float treeResistance = tempAttr.GetFloat("treeResistance", 1);

            if (pos.X != posx || pos.Y != posy || pos.Z != posz || counter % 30 == 0)
            {
                float baseResistance;
                int woodTier;

                Stack<BlockPos> foundPositions = FindTree(player.Entity.World, pos, out baseResistance, out woodTier);
                treeResistance = (float)baseResistance/IDGTreeConfig.Current.TreeFellingDivisor;
                if (collObj.ToolTier < woodTier - 3)
                {
                    handled = EnumHandling.Handled;
                    return treeResistance * 1.25f;
                }
                tempAttr.SetFloat("treeResistance", treeResistance);
            }
            else
            {
                treeResistance = tempAttr.GetFloat("treeResistance", 1f);
            }
            handled = EnumHandling.Handled;
            tempAttr.SetInt("lastposX", pos.X);
            tempAttr.SetInt("lastposY", pos.Y);
            tempAttr.SetInt("lastposZ", pos.Z);
            float treeDmg = treeResistance - ((collObj.GetMiningSpeed(itemslot.Itemstack, blockSel, api.World.BlockAccessor.GetBlock(pos), player)) * counter / 10);
            remainingResistance = treeDmg;
            return treeDmg;
        }

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier, ref EnumHandling bhHandling)
        {
            

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer player) byPlayer = byEntity.World.PlayerByUid(player.PlayerUID);

            WeatherSystemBase modSystem = this.api.ModLoader.GetModSystem<WeatherSystemBase>(true);
            double windspeed = (modSystem != null) ? modSystem.WeatherDataSlowAccess.GetWindSpeed(byEntity.SidedPos.XYZ) : 0.0;
            float num;
            int woodTier;

            Stack<BlockPos> foundPositions = this.FindTree(world, blockSel.Position, out num, out woodTier);

            if (foundPositions.Count == 0)
            {
                bhHandling = EnumHandling.Handled;
                return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier, ref bhHandling);
            }

            bool damageable = collObj.DamagedBy != null && collObj.DamagedBy.Contains(EnumItemDamageSource.BlockBreaking);
            float leavesMul = 1f;
            float leavesBranchyMul = 0.8f;
            int blocksbroken = 0;

            bool isStump = api.World.BlockAccessor.GetBlock(blockSel.Position).FirstCodePart() == "treestump";
            while (foundPositions.Count > 0)
            {
                BlockPos pos = foundPositions.Pop();
                blocksbroken++;

                Block block = world.BlockAccessor.GetBlock(pos, 0);
                bool isLog = block.BlockMaterial == EnumBlockMaterial.Wood;
                bool isBranchy = block.Code.Path.Contains("branchy");
                bool isLeaves = block.BlockMaterial == EnumBlockMaterial.Leaves;

                world.BlockAccessor.BreakBlock(pos, byPlayer, isLeaves ? leavesMul : (isBranchy ? leavesBranchyMul : 1));

                if (world.Side == EnumAppSide.Client)
                {
                    dustParticles.Color = block.GetRandomColor(world.Api as ICoreClientAPI, pos, BlockFacing.UP);
                    dustParticles.Color |= 255 << 24;
                    dustParticles.MinPos.Set(pos.X, pos.Y, pos.Z);

                    if (block.BlockMaterial == EnumBlockMaterial.Leaves)
                    {
                        dustParticles.GravityEffect = (float)world.Rand.NextDouble() * 0.1f + 0.01f;
                        dustParticles.ParticleModel = EnumParticleModel.Quad;
                        dustParticles.MinVelocity.Set(-0.4f + 4 * (float)windspeed, -0.4f, -0.4f);
                        dustParticles.AddVelocity.Set(0.8f + 4 * (float)windspeed, 1.2f, 0.8f);

                    }
                    else
                    {
                        dustParticles.GravityEffect = 0.8f;
                        dustParticles.ParticleModel = EnumParticleModel.Cube;
                        dustParticles.MinVelocity.Set(-0.4f + (float)windspeed, -0.4f, -0.4f);
                        dustParticles.AddVelocity.Set(0.8f + (float)windspeed, 1.2f, 0.8f);
                    }


                    world.SpawnParticles(dustParticles);
                }


                if (damageable && isLog)
                {
                    collObj.DamageItem(world, byEntity, itemslot);
                }


                if (itemslot.Itemstack == null)
                {
                    bhHandling = EnumHandling.PreventDefault;
                    return true;
                }

                if (isLeaves && leavesMul > 0.03f) leavesMul *= 0.85f;
                if (isBranchy && leavesBranchyMul > 0.015f) leavesBranchyMul *= 0.6f;
            }

            if (blocksbroken > 35)
            {
                Vec3d pos = blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5);
                api.World.PlaySoundAt(new AssetLocation("sounds/effect/treefell"), pos.X, pos.Y, pos.Z, byPlayer, false, 32, GameMath.Clamp(blocksbroken / 100f, 0.25f, 1));
            }

            bhHandling = EnumHandling.Handled;
            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier, ref bhHandling);
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