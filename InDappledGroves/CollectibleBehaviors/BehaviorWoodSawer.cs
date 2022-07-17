using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.CollectibleBehaviors
{
    class BehaviorWoodSawer : CollectibleBehavior
    {
        ICoreAPI api;
        ICoreClientAPI capi;
        public InventoryBase Inventory { get; }
        public string InventoryClassName => "worldinventory";
        public SkillItem[] toolModes;

        public SawingRecipe recipe;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }

        public BehaviorWoodSawer(CollectibleObject collObj) : base(collObj)
        {
            this.collObj = collObj;
            Inventory = new InventoryGeneric(1, "sawingblock-slot", null, null);
        }

        public SkillItem[] GetSkillItems()
        {
            return toolModes;
        }
        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            this.capi = (api as ICoreClientAPI);
            this.groundSawTime = collObj.Attributes["woodworkingProps"]["groundSawTime"].AsInt(4);
            this.sawBuckSawTime = collObj.Attributes["woodworkingProps"]["sawBuckSawTime"].AsInt(2);
            this.groundSawDamage = collObj.Attributes["woodworkingProps"]["groundSawDamage"].AsInt(4);
            this.sawBuckSawDamage = collObj.Attributes["woodworkingProps"]["sawBuckSawDamage"].AsInt(2);
            interactions = ObjectCacheUtil.GetOrCreate(api, "idgsawInteractions", () =>
            {
                return new WorldInteraction[] {
                    new WorldInteraction()
                        {
                            ActionLangCode = "indappledgroves:itemhelp-saw-sawwood",
                            HotKeyCode = "sprint",
                            MouseButton = EnumMouseButton.Right
                        },
                    };
            });
            woodParticles = InitializeWoodParticles();
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {

            if (!byEntity.Controls.Sprint || blockSel == null)
                return;
            Inventory[0].Itemstack = new ItemStack(api.World.BlockAccessor.GetBlock(blockSel.Position));
            recipe = GetMatchingSawingRecipe(byEntity.World, Inventory[0]);
            if (recipe == null || recipe.RequiresStation) return;

            Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
            JsonObject attributes = interactedBlock.Attributes?["woodworkingProps"]["idgSawBuckProps"]["sawable"];

            if (attributes == null || !attributes.Exists || !attributes.AsBool(false)) return;
            if (slot.Itemstack.Attributes.GetInt("durability") < groundSawDamage && slot.Itemstack.Attributes.GetInt("durability") != 0)
            {
                capi.TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", groundSawDamage));
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

                if (((int)api.Side) == 1 && playNextSound < secondsUsed)
                {
                    //api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, null, true, 32, 1f);
                    playNextSound += .7f;
                }
                if (secondsUsed >= groundSawTime)
                {

                    Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
                    if (secondsUsed >= groundSawTime && interactedBlock.Attributes["idgSawBuckProps"]["sawable"].AsBool(false))
                        SpawnOutput(new ItemStack(api.World.BlockAccessor.GetBlock(blockSel.Position)).Collectible, byEntity, pos, groundSawDamage);
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
            //byEntity.StopAnimation("axechop");
        }

        //-- Spawns firewood when chopping cycle is finished --//
        public void SpawnOutput(CollectibleObject chopObj, EntityAgent byEntity, BlockPos pos, int dmg)
        {
            Item itemOutput = api.World.GetItem(new AssetLocation(chopObj.Attributes["idgSawBuckProps"]["output"]["code"].AsString()));
            Block blockOutput = api.World.GetBlock(new AssetLocation(chopObj.Attributes["idgSawBuckProps"]["output"]["code"].AsString()));
            int quantity = chopObj.Attributes["idgSawBuckProps"]["output"]["quantity"].AsInt();

            for (int i = quantity; i > 0; i--)
            {
                api.World.SpawnItemEntity(new ItemStack(itemOutput!=null?itemOutput:blockOutput), pos.ToVec3d() + new Vec3d(0.05f, .1f, 0.05f));
            }

            if (byEntity is EntityPlayer player)
                player.RightHandItemSlot.Itemstack.Collectible.DamageItem(api.World, byEntity, player.RightHandItemSlot, groundSawDamage);

        }

        public void SpawnOutput(SawingRecipe recipe, EntityAgent byEntity, BlockPos pos)
        {
            ItemStack output = recipe.Output.ResolvedItemstack;
            int j = output.StackSize;
            for (int i = j; i > 0; i--)
            {
                api.World.SpawnItemEntity(new ItemStack(output.Collectible), pos.ToVec3d(), new Vec3d(0.05f, .1f, 0.05f));
            }

        }
        public SawingRecipe GetMatchingSawingRecipe(IWorldAccessor world, ItemSlot slots)
        {
            List<SawingRecipe> recipes = IDGRecipeRegistry.Loaded.SawingRecipes;
            if (recipes == null) return null;

            for (int j = 0; j < recipes.Count; j++)
            {
                if (recipes[j].Matches(api.World, slots))
                {
                    return recipes[j];
                }
            }

            return null;
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

        public int groundSawTime;
        public int sawBuckSawTime;
        public int groundSawDamage;
        public int sawBuckSawDamage;
        WorldInteraction[] interactions = null;
        private SimpleParticleProperties woodParticles;
        private float playNextSound;
    }
}

