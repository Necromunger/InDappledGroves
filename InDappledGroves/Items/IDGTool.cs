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
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.Items.Tools
{

    class IDGTool : Item, IIDGTool
    {

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = (api as ICoreClientAPI);
            toolModes = BuildSkillList();
            baseWorkstationMiningSpdMult = InDappledGrovesConfig.Current.baseWorkstationMiningSpdMult;
            baseWorkstationResistanceMult = InDappledGrovesConfig.Current.baseWorkstationResistanceMult;
            baseGroundRecipeMiningSpdMult = InDappledGrovesConfig.Current.baseGroundRecipeMiningSpdMult;
            baseGroundRecipeResistaceMult = InDappledGrovesConfig.Current.baseGroundRecipeResistaceMult;
        }

        public IDGTool()
        {
            dustParticles.ParticleModel = EnumParticleModel.Quad;
            dustParticles.AddPos.Set(1, 1, 1);
            dustParticles.MinQuantity = 2;
            dustParticles.AddQuantity = 12;
            dustParticles.LifeLength = 4f;
            dustParticles.MinSize = 0.2f;
            dustParticles.MaxSize = 0.5f;
            dustParticles.MinVelocity.Set(-0.4f, -0.4f, -0.4f);
            dustParticles.AddVelocity.Set(0.8f, 1.2f, 0.8f);
            dustParticles.DieOnRainHeightmap = false;
            dustParticles.WindAffectednes = 0.5f;
            Inventory = new InventoryGeneric(1, "IDGTool-slot", null, null);
        }

        #region ToolMode Stuff
        private SkillItem[] BuildSkillList()
        {
            var skillList = new List<SkillItem>();
            foreach (var behaviour in CollectibleBehaviors)
            {
                if (behaviour is not IBehaviorVariant bwc) continue;
                foreach (var mode in bwc.GetSkillItems())
                {
                    skillList.Add(mode);
                }
            }
            return skillList.ToArray();
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return this.toolModes;
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            return Math.Min(this.toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode", 0));
        }

        public string GetToolModeName(ItemStack stack)
        {
            return toolModes[Math.Min(this.toolModes.Length - 1, stack.Attributes.GetInt("toolMode", 0))].Code.FirstCodePart();
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack stack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {


            if (stack.Attributes.HasAttribute("toolMode"))
            {
                JsonObject transformAttributes = stack.Collectible.Attributes["modeTransforms"][GetToolModeName(stack)];

                if (target is EnumItemRenderTarget.HandFp)
                {
                    renderinfo.Transform = transformAttributes?["fpHandTransform"].AsObject<ModelTransform>() ?? FpHandTransform;
                }
                if (target is EnumItemRenderTarget.HandTp)
                {
                    renderinfo.Transform = transformAttributes?["tpHandTransform"].AsObject<ModelTransform>() ?? TpHandTransform;
                }
            }
            base.OnBeforeRender(capi, stack, target, ref renderinfo);
        }

        #endregion ToolMode Stuff

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (!byEntity.Controls.CtrlKey)
            {
                string curTMode = "";
                if (slot.Itemstack.Collectible is IIDGTool tool) curTMode = tool.GetToolModeName(slot.Itemstack);

                if (blockSel == null)
                    return;

                Inventory[0].Itemstack = new ItemStack(api.World.BlockAccessor.GetBlock(blockSel.Position, 0));

                recipe = GetMatchingGroundRecipe(Inventory[0], curTMode);
                if (recipe == null) return;
                resistance = Inventory[0].Itemstack.Block.Resistance;

                if (slot.Itemstack.Attributes.GetInt("durability") < recipe.BaseToolDmg && slot.Itemstack.Attributes.GetInt("durability") != 0)
                {
                    capi.TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", recipe.BaseToolDmg));
                    return;
                }
                byEntity.StartAnimation("axechop");

                playNextSound = 0.25f;

                handHandling = EnumHandHandling.PreventDefault;
                return;
            }
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (!byEntity.Controls.CtrlKey)
            {
                BlockPos pos = blockSel?.Position;
                if (blockSel != null)
                {

                    if (((int)api.Side) == 1 && playNextSound < secondsUsed)
                    {
                        api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, null, true, 32, 1f);
                        playNextSound += .8f;
                    }

                    //Accumulate damage over time from current tools mining speed.
                    curDmgFromMiningSpeed += slot.Itemstack.Collectible.GetMiningSpeed(slot.Itemstack, blockSel, Inventory[0].Itemstack.Block, byEntity as IPlayer) * (secondsUsed - lastSecondsUsed);

                    //update lastSecondsUsed to this cycle
                    lastSecondsUsed = secondsUsed;

                    //if seconds used + curDmgFromMiningSpeed is greater than resistance, output recipe and break cycle
                    float toolModeMod = getToolModeMod(slot.Itemstack);
                    if (((curDmgFromMiningSpeed / 3) + secondsUsed) * (toolModeMod != 0 ? toolModeMod : 1f) >= resistance)
                    {

                        SpawnOutput(recipe, pos);
                        api.World.BlockAccessor.SetBlock(ReturnStackId(recipe, pos), pos);
                        slot.Itemstack.Collectible.DamageItem(api.World, byEntity, slot, recipe.BaseToolDmg);
                        return false;
                    }

                }
                return true;
            }
            return false;
        }

        private float getToolModeMod(ItemStack stack)
        {
            switch (GetToolModeName(stack))
            {
                case "chopping": return stack.Collectible.Attributes["choppingProps"]["splittingMod"].AsFloat();
                case "sawing": return stack.Collectible.Attributes["sawingProps"]["sawingMod"].AsFloat();
                case "hewing": return stack.Collectible.Attributes["hewingProps"]["hewingMod"].AsFloat();
                case "planing": return stack.Collectible.Attributes["planingProps"]["planingMod"].AsFloat();
                default: return 1f;
            }

        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            byEntity.StopAnimation("axechop");
        }


        //-- Spawns output when chopping cycle is finished --//
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

        public void SpawnOutput(GroundRecipe recipe, BlockPos pos)
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

        public GroundRecipe GetMatchingGroundRecipe(ItemSlot slot, string curTMode)
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

        public bool DoesSlotMatchRecipe(ItemSlot slots)
        {
            List<GroundRecipe> recipes = IDGRecipeRegistry.Loaded.GroundRecipes;
            if (recipes == null) return false;

            for (int j = 0; j < recipes.Count; j++)
            {
                if (recipes[j].Matches(api.World, slots))
                {
                    return true;
                }
            }

            return false;
        }

        public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            bool isLog = (api.World.BlockAccessor.GetBlock(blockSel.Position, 0).FirstCodePart() == "log" || api.World.BlockAccessor.GetBlock(blockSel.Position, 0).FirstCodePart() == "treestump");
            if (this.HasBehavior<BehaviorWoodChopping>() && isLog && api.World.BlockAccessor.GetBlock(blockSel.Position, 0).Variant["type"] == "grown")
            {
                float treeResistance = GetBehavior<BehaviorWoodChopping>().OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
                return base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt / treeResistance, counter);
            }
            else
            {
                return base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
            }

        }

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            bool isLog = (api.World.BlockAccessor.GetBlock(blockSel.Position, 0).FirstCodePart() == "log" || api.World.BlockAccessor.GetBlock(blockSel.Position, 0).FirstCodePart() == "treestump");
            if (this.HasBehavior<BehaviorWoodChopping>() && isLog && api.World.BlockAccessor.GetBlock(blockSel.Position, 0).Variant["type"] == "grown")
            {
                return this.GetBehavior<BehaviorWoodChopping>().OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier = 1);
            }

            return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
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
        #endregion Recipe Processing

        //Particle Handlers
        ICoreClientAPI capi;
        public float baseWorkstationMiningSpdMult;
        public float baseWorkstationResistanceMult;
        public float baseGroundRecipeMiningSpdMult;
        public float baseGroundRecipeResistaceMult;
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
        public string InventoryClassName => "worldinventory";
        public InventoryBase Inventory { get; }
        public SkillItem[] toolModes;
        public GroundRecipe recipe;
        WorldInteraction[] interactions;
        private float resistance;
        private float lastSecondsUsed;
        private float curDmgFromMiningSpeed;
        private SimpleParticleProperties woodParticles;
        private float playNextSound;
    }
}
