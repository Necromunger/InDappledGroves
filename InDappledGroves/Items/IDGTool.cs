using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Interfaces;
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

namespace InDappledGroves.Items.Tools
{

    class IDGTool : Item, IIDGTool
    {
        // Token: 0x04000CF8 RID: 3320
        private SkillItem[] toolModes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            ICoreClientAPI capi = api as ICoreClientAPI;

            toolModes = BuildSkillList();

        }      

        public IDGTool()
        {
            ICoreClientAPI capi = api as ICoreClientAPI;

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

        // Token: 0x06001848 RID: 6216 RVA: 0x000E49DC File Offset: 0x000E2BDC
        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            return Math.Min(this.toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode", 0));
        }

        public string GetToolMode(ItemSlot slot)
        {
            return toolModes[Math.Min(this.toolModes.Length - 1, slot.Itemstack.Attributes.GetInt("toolMode", 0))].Code.FirstCodePart();
        }

        // Token: 0x06001849 RID: 6217 RVA: 0x000C8EF1 File Offset: 0x000C70F1
        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }
        #endregion ToolMode Stuff

        public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            
            if (this.HasBehavior<BehaviorWoodChopping>() && api.World.BlockAccessor.GetBlock(blockSel.Position).Variant["type"] == "grown")
            {
                float treeResistance = GetBehavior<BehaviorWoodChopping>().OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
                return base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt/treeResistance , counter);
            } else {
                return base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
            }

        }

    

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            if (this.HasBehavior<BehaviorWoodChopping>() && api.World.BlockAccessor.GetBlock(blockSel.Position).Variant["type"] == "grown")
            {
               return this.GetBehavior<BehaviorWoodChopping>().OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier = 1);
            }

            return base.OnBlockBrokenWith(world,byEntity,itemslot,blockSel,dropQuantityMultiplier);
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

        //private void SetParticleColourAndPosition(int colour, Vec3d minpos)
        //{
        //    SetParticleColour(colour);

        //    woodParticles.MinPos = minpos;
        //    woodParticles.AddPos = new Vec3d(1, 1, 1);
        //}
        //private void SetParticleColour(int colour)
        //{
        //    woodParticles.Color = colour;
        //}

        //private readonly SimpleParticleProperties woodParticles;
    }

}
