using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Interfaces;
using InDappledGroves.Util;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves
{
    class BehaviorWoodChopping : CollectibleBehavior, IBehaviorVariant
    {
        ICoreAPI api;
        ICoreClientAPI capi;

        public InventoryBase Inventory { get; }
        public string InventoryClassName => "worldinventory";
        public GroundRecipe recipe;
        public float workstationMiningSpdMult;
        public float workstationResistanceMult;
        public float groundRecipeMiningSpdMult;
        public float groundRecipeResistaceMult;

        public SkillItem[] toolModes;

        public BehaviorWoodChopping(CollectibleObject collObj) : base(collObj)
        {
            this.collObj = collObj;
            Inventory = new InventoryGeneric(1, "choppingtool-slot", null, null);
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            workstationMiningSpdMult = InDappledGrovesConfig.Current.workstationMiningSpdMult;
            workstationResistanceMult = InDappledGrovesConfig.Current.workstationResistanceMult;
            groundRecipeMiningSpdMult = InDappledGrovesConfig.Current.groundRecipeMiningSpdMult;
            groundRecipeResistaceMult = InDappledGrovesConfig.Current.groundRecipeResistaceMult;
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

            interactions = ObjectCacheUtil.GetOrCreate(api, "idgaxeInteractions", () =>
            {
                return new WorldInteraction[] {
                    new WorldInteraction()
                        {
                            ActionLangCode = "indappledgroves:itemhelp-axe-chopwood",
                            MouseButton = EnumMouseButton.Right
                        },
                    };
            });
            woodParticles = InitializeWoodParticles();
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            string curTMode = "";
            if (slot.Itemstack.Collectible is IIDGTool tool) curTMode = tool.GetToolModeName(slot.Itemstack);
            
            if (blockSel == null) return;

            Inventory[0].Itemstack = new ItemStack(api.World.BlockAccessor.GetBlock(blockSel.Position, 0));

            recipe = GetMatchingGroundRecipe(byEntity.World, Inventory[0], curTMode);
            
            if (recipe == null) return;

            resistance = (Inventory[0].Itemstack.Block.Resistance) * groundRecipeResistaceMult;

            if (slot.Itemstack.Attributes.GetInt("durability") < recipe.BaseToolDmg && slot.Itemstack.Attributes.GetInt("durability") != 0)
            {
                capi.TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", recipe.BaseToolDmg));
                return;
            }

            byEntity.StartAnimation("axechop");

            playNextSound = 0.25f;

            handHandling = EnumHandHandling.PreventDefault;
        }
 
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            BlockPos pos = blockSel.Position;
            if (blockSel != null)
            {

                if (recipe != null)
                {

                    if (((int)api.Side) == 1 && playNextSound < secondsUsed)
                    {
                        api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, null, true, 32, 1f);
                        playNextSound += .7f;
                    }
                    
                    curDmgFromMiningSpeed += collObj.GetMiningSpeed(slot.Itemstack, blockSel, Inventory[0].Itemstack.Block, byEntity as IPlayer) * (secondsUsed - lastSecondsUsed);
                    lastSecondsUsed = secondsUsed;

                    float toolModeMod = 1;

                    if(slot.Itemstack.Collectible is IIDGTool tool && slot.Itemstack.Collectible.Attributes["woodWorkingProps"].Exists)
                    {
                        toolModeMod = getToolModeMod(slot.Itemstack, tool) == 0?1f: getToolModeMod(slot.Itemstack, tool);
                        
                    }
                    if ((((curDmgFromMiningSpeed * groundRecipeMiningSpdMult) * toolModeMod) + secondsUsed) >= resistance)
                    {
                        SpawnOutput(recipe, byEntity, pos);
                        api.World.BlockAccessor.SetBlock(ReturnStackId(recipe, pos), pos);
                        slot.Itemstack.Collectible.DamageItem(api.World, byEntity, slot, recipe.BaseToolDmg);
                        handling = EnumHandling.PreventSubsequent;
                        return false;
                    }
                }
            }
            handling = EnumHandling.PreventSubsequent;
            return true;
        }

        private float getToolModeMod(ItemStack stack, IIDGTool tool)
        {
            switch (tool.GetToolModeName(stack))
            {
                case "chopping": return stack.Collectible.Attributes["woodWorkingProps"]["splittingMod"].AsFloat();
                case "sawing": return stack.Collectible.Attributes["woodWorkingProps"]["sawingMod"].AsFloat();
                case "hewing": return stack.Collectible.Attributes["woodWorkingProps"]["hewingMod"].AsFloat();
                case "planing": return stack.Collectible.Attributes["woodWorkingProps"]["planingMod"].AsFloat();
                default: return 1f;
            }
            
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            resistance = 0.0f;
            curDmgFromMiningSpeed = 0.0f;
            lastSecondsUsed = 0.0f;
            handling = EnumHandling.PreventDefault;
            byEntity.StopAnimation("axechop");
        }

        #region Recipe Processing
        public GroundRecipe GetMatchingGroundRecipe(IWorldAccessor world, ItemSlot slot, string curTMode)
        {
            List<GroundRecipe> recipes = IDGRecipeRegistry.Loaded.GroundRecipes;
            if (recipes == null) return null;

            for (int j = 0; j < recipes.Count; j++)
            {
                if (recipes[j].Matches(api.World, slot) && recipes[j].ToolMode == curTMode)
                {
                    return recipes[j];
                }
            }

            return null;
        }

        private int ReturnStackId(GroundRecipe recipe, BlockPos pos)
        {
            if (recipe.ReturnStack.ResolvedItemstack.Collectible is Block)
            {
                return recipe.ReturnStack.ResolvedItemstack.Id;
            }
            else if (recipe.ReturnStack.ResolvedItemstack.Collectible is Item)
            {
                SpawnReturnstackItem(recipe.ReturnStack.ResolvedItemstack, pos);
                return 0;
            }
                return 0;
        }

        public void SpawnOutput(GroundRecipe recipe, EntityAgent byEntity, BlockPos pos)
        {
            int j = recipe.Output.StackSize;
            for (int i = j; i > 0; i--)
            {
                api.World.SpawnItemEntity(new ItemStack(recipe.Output.ResolvedItemstack.Collectible), pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
            }
        }

        public void SpawnReturnstackItem(ItemStack stack, BlockPos pos)
        {
            int j = stack.StackSize;
            for (int i = j; i > 0; i--)
            {
                api.World.SpawnItemEntity(new ItemStack(recipe.ReturnStack.ResolvedItemstack.Collectible), pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
            }
        }
        #endregion Recipe Processing

        #region TreeFelling
        public float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {

            System.Diagnostics.Debug.WriteLine("Remaining Tree Resistance is " + remainingResistance);
            ITreeAttribute tempAttr = itemslot.Itemstack.TempAttributes;
            int posx = tempAttr.GetInt("lastposX", -1);
            int posy = tempAttr.GetInt("lastposY", -1);
            int posz = tempAttr.GetInt("lastposZ", -1);
            float treeResistance = tempAttr.GetFloat("treeResistance", 1) * (itemslot.Itemstack.Collectible.Attributes["woodWorkingProps"]["fellingmultiplier"].AsFloat(1f));

            BlockPos pos = blockSel.Position;

            if (pos.X != posx || pos.Y != posy || pos.Z != posz || counter % 30 == 0)
            {
                Stack<BlockPos> foundPositions = FindTree(player.Entity.World, pos);
                treeResistance = (float)Math.Max(1, Math.Sqrt(foundPositions.Count));

                tempAttr.SetFloat("treeResistance", treeResistance);
            }

            tempAttr.SetInt("lastposX", pos.X);
            tempAttr.SetInt("lastposY", pos.Y);
            tempAttr.SetInt("lastposZ", pos.Z);
            return treeResistance * 1.25f;

        }



        public bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer player) byPlayer = byEntity.World.PlayerByUid(player.PlayerUID);

            double windspeed = api.ModLoader.GetModSystem<WeatherSystemBase>()?.WeatherDataSlowAccess.GetWindSpeed(byEntity.SidedPos.XYZ) ?? 0;

            Stack<BlockPos> foundPositions = FindTree(world, blockSel.Position);

            if (foundPositions.Count == 0)
            {
                return collObj.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
            }

            bool damageable = collObj.DamagedBy != null && collObj.DamagedBy.Contains(EnumItemDamageSource.BlockBreaking);

            float leavesMul = 1;
            float leavesBranchyMul = 0.8f;
            int blocksbroken = 0;

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

                if (itemslot.Itemstack == null) return true;

                if (isLeaves && leavesMul > 0.03f) leavesMul *= 0.85f;
                if (isBranchy && leavesBranchyMul > 0.015f) leavesBranchyMul *= 0.6f;
            }

            if (blocksbroken > 35)
            {
                Vec3d pos = blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5);
                api.World.PlaySoundAt(new AssetLocation("sounds/effect/treefell"), pos.X, pos.Y, pos.Z, byPlayer, false, 32, GameMath.Clamp(blocksbroken / 100f, 0.25f, 1));
            }

            return true;
        }

        public Stack<BlockPos> FindTree(IWorldAccessor world, BlockPos startPos)
        {
            Queue<Vec4i> queue = new();
            HashSet<BlockPos> checkedPositions = new();
            Stack<BlockPos> foundPositions = new();

            Block block = world.BlockAccessor.GetBlock(startPos, 0);
            if (block.Code == null) return foundPositions;

            string treeFellingGroupCode = block.Attributes?["treeFellingGroupCode"].AsString();
            int spreadIndex = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt(0) ?? 0;

            // Must start with a log
            if (spreadIndex < 2) return foundPositions;
            if (treeFellingGroupCode == null) return foundPositions;

            queue.Enqueue(new Vec4i(startPos.X, startPos.Y, startPos.Z, spreadIndex));
            foundPositions.Push(startPos);
            checkedPositions.Add(startPos);

            while (queue.Count > 0)
            {
                if (foundPositions.Count > 2500)
                {
                    break;
                }

                Vec4i pos = queue.Dequeue();

                for (int i = 0; i < Vec3i.DirectAndIndirectNeighbours.Length; i++)
                {
                    Vec3i facing = Vec3i.DirectAndIndirectNeighbours[i];
                    BlockPos neibPos = new(pos.X + facing.X, pos.Y + facing.Y, pos.Z + facing.Z);

                    float hordist = GameMath.Sqrt(neibPos.HorDistanceSqTo(startPos.X, startPos.Z));
                    float vertdist = (neibPos.Y - startPos.Y);

                    // "only breaks blocks inside an upside down square base pyramid"
                    if (hordist - 1 >= 2 * vertdist) continue;
                    if (checkedPositions.Contains(neibPos)) continue;

                    block = world.BlockAccessor.GetBlock(neibPos, 0);
                    if (block.Code == null || block.Id == 0) continue;

                    string ngcode = block.Attributes?["treeFellingGroupCode"].AsString();

                    // Only break the same type tree blocks
                    if (ngcode != treeFellingGroupCode) continue;

                    // Only spread from "high to low". i.e. spread from log to leaves, but not from leaves to logs
                    int nspreadIndex = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt(0) ?? 0;
                    if (pos.W < nspreadIndex) continue;

                    foundPositions.Push(neibPos.Copy());
                    queue.Enqueue(new Vec4i(neibPos.X, neibPos.Y, neibPos.Z, nspreadIndex));


                    checkedPositions.Add(neibPos);
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

        private void SetParticleColourAndPosition(int colour, Vec3d minpos)
        {
            SetParticleColour(colour);

            woodParticles.MinPos = minpos;
            woodParticles.AddPos = new Vec3d(1, 1, 1);
        }

        private void SetParticleColour(int colour)
        {
            woodParticles.Color = colour;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            if (inSlot.Itemstack.Collectible is IIDGTool tool && tool.GetToolModeName(inSlot.Itemstack) == "chopping") {
                return interactions;
                }
            return null;
        }

        //Create function by which interactions will find recipes using the target block and the current tool mode.
        WorldInteraction[] interactions = null;
        private float resistance;
        private float lastSecondsUsed;
        private float curDmgFromMiningSpeed;
        private SimpleParticleProperties woodParticles;
        private float playNextSound;
    }
}