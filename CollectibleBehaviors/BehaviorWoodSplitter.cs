using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace InDappledGroves
{
    class BehaviorWoodSplitter : CollectibleBehavior
    {
        ICoreAPI api;
        ICoreClientAPI capi;
        
        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            this.groundChopTime = properties["groundChopTime"].AsInt(3);
            this.choppingBlockChopTime = properties["choppingBlockChopTime"].AsInt(2);
        }

        public BehaviorWoodSplitter(CollectibleObject collObj) : base(collObj)
        {
            this.collObj = collObj;
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            this.capi = (api as ICoreClientAPI);
            interactions = ObjectCacheUtil.GetOrCreate(api, "vtaxeInteractions", () =>
            {
                return new WorldInteraction[] {
                    new WorldInteraction()
                        {
                            ActionLangCode = "indappledgroves:itemhelp-axe-chopwood",
                            HotKeyCode = "sprint",
                            MouseButton = EnumMouseButton.Right
                        },
                    };
            });
            woodParticles = InitializeWoodParticles();
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            //-- Do not process the chopping action if the player is not holding ctrl, or no block is selected --//
            if (!byEntity.Controls.Sprint || blockSel == null)
                return;
            
            Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
            if ((interactedBlock.FirstCodePart() == "log" && interactedBlock.Variant["type"] == "placed")
                || interactedBlock.FirstCodePart() == "strippedlog"
                || (interactedBlock.FirstCodePart() == "logsection" && interactedBlock.Variant["type"] == "placed"))
            {
                byEntity.StartAnimation("axechop");
            }
            playNextSound = 0.25f;
            handHandling = EnumHandHandling.PreventDefault;
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {

            BlockPos pos = blockSel.Position;
            if (blockSel != null)
            {

                if (((int)api.Side) == 1 && playNextSound < secondsUsed)
                {
                    api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, null, true, 32, 1f);
                    playNextSound += .27f;
                }
                if (secondsUsed >= groundChopTime)
                {
                    Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
                    if (secondsUsed >= groundChopTime &&
                        ((interactedBlock.FirstCodePart() == "log" && interactedBlock.Variant["type"] == "placed")
                        || interactedBlock.FirstCodePart() == "strippedlog")
                        || (interactedBlock.FirstCodePart() == "logsection" && interactedBlock.Variant["type"] == "placed"))
                    SpawnOutput(blockSel, byEntity);
                    api.World.BlockAccessor.SetBlock(0, blockSel.Position);
                    return false;
                }

            }
            handling = EnumHandling.PreventSubsequent;
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            byEntity.StopAnimation("axechop");
        }

        //-- Spawns firewood when chopping cycle is finished --//
        public void SpawnOutput(BlockSelection blockSel, EntityAgent byEntity)
        {
            if (api.Side == EnumAppSide.Server)
            {
                Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
                //TODO: Check axe for tier and reference config for cut times and yield.
                int firewoodYield = 4;
                
                api.World.BlockAccessor.MarkBlockDirty(blockSel.Position);
                for (int i = firewoodYield; i > 0; i--)
                {
                    api.World.SpawnItemEntity(new ItemStack(api.World.GetItem(new AssetLocation("firewood"))), blockSel.Position.ToVec3d() +
                        new Vec3d(0, .25, 0));
                }

                if (byEntity is EntityPlayer player)
                    player.RightHandItemSlot.Itemstack.Collectible.DamageItem(api.World, byEntity, player.RightHandItemSlot, 1);
            }

        }

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

        static SimpleParticleProperties dustParticles = new SimpleParticleProperties()
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
            return interactions;
        }

        public int groundChopTime;
        public int choppingBlockChopTime;
        WorldInteraction[] interactions = null;
        private double choppingTime;
        private SimpleParticleProperties woodParticles;
        private float playNextSound;
    }
}
