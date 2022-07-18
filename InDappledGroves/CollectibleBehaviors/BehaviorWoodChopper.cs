using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Util;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves
{
    class BehaviorWoodChopper : CollectibleBehavior, IBehaviorVariant
    {
        ICoreAPI api;
        ICoreClientAPI capi;

        public InventoryBase Inventory { get; }
        public string InventoryClassName => "worldinventory";
        public ChoppingRecipe recipe;

        public SkillItem[] toolModes;

        public BehaviorWoodChopper(CollectibleObject collObj) : base(collObj)
        {
            this.collObj = collObj;
            Inventory = new InventoryGeneric(1, "choppingblock-slot", null, null);
        }

        public override void Initialize(JsonObject properties)
        {

            base.Initialize(properties);
        }

        public SkillItem[] GetSkillItems()
        {
            return toolModes ?? new SkillItem[] { null };
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            this.capi = (api as ICoreClientAPI);
            this.groundChopTime = collObj.Attributes["woodworkingProps"]["groundChopTime"].AsInt(4);
            this.choppingBlockChopTime = collObj.Attributes["woodworkingProps"]["choppingBlockChopTime"].AsInt(2);
            this.groundChopDamage = collObj.Attributes["woodworkingProps"]["groundChopDamage"].AsInt(4);
            this.choppingBlockChopDamage = collObj.Attributes["woodworkingProps"]["choppingBlockChopDamage"].AsInt(2);

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
                            HotKeyCode = "crouch",
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
            Inventory[0].Itemstack = new ItemStack(api.World.BlockAccessor.GetBlock(blockSel.Position));
            recipe = GetMatchingChoppingRecipe(byEntity.World, Inventory[0]);
            if (recipe == null || recipe.RequiresStation) return;

            Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
            JsonObject attributes = interactedBlock.Attributes?["woodworkingProps"]["idgChoppingBlockProps"]["choppable"];

            if (attributes == null || !attributes.Exists || !attributes.AsBool(false)) return;
            if (slot.Itemstack.Attributes.GetInt("durability") < groundChopDamage && slot.Itemstack.Attributes.GetInt("durability") != 0)
            {
                capi.TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", groundChopDamage));
                return;
            }
            byEntity.StartAnimation("axechop");

            playNextSound = 0.25f;

            handHandling = EnumHandHandling.PreventDefault;
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            BlockPos pos = blockSel.Position;
            if (blockSel != null && api.World.BlockAccessor.GetBlock(pos).Attributes["woodworkingProps"]["idgChoppingBlockProps"]["choppable"].AsBool(false) && !recipe.RequiresStation)
            {

                if (recipe != null)
                {

                    if (((int)api.Side) == 1 && playNextSound < secondsUsed)
                    {
                        api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, null, true, 32, 1f);
                        playNextSound += .7f;
                    }
                    if (secondsUsed >= groundChopTime)
                    {
                        Block interactedBlock = api.World.BlockAccessor.GetBlock(pos);
                        if (secondsUsed >= groundChopTime && interactedBlock.Attributes["woodworkingProps"]["idgChoppingBlockProps"]["choppable"].AsBool(false))
                            SpawnOutput(recipe, byEntity, pos);
                        api.World.BlockAccessor.SetBlock(0, pos);
                        return false;
                    }
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

        public ChoppingRecipe GetMatchingChoppingRecipe(IWorldAccessor world, ItemSlot slot)
        {
            List<ChoppingRecipe> recipes = IDGRecipeRegistry.Loaded.ChoppingRecipes;
            if (recipes == null) return null;

            for (int j = 0; j < recipes.Count; j++)
            {
                if (recipes[j].Matches(api.World, slot))
                {
                    return recipes[j];
                }
            }

            return null;
        }

        public void SpawnOutput(ChoppingRecipe recipe, EntityAgent byEntity, BlockPos pos)
        {
            int j = recipe.Output.StackSize;
            for (int i = j; i > 0; i--)
            {
                api.World.SpawnItemEntity(new ItemStack(recipe.Output.ResolvedItemstack.Collectible), pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
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

        public int groundChopTime;
        public int choppingBlockChopTime;
        public int groundChopDamage;
        public int choppingBlockChopDamage;
        WorldInteraction[] interactions = null;
        private SimpleParticleProperties woodParticles;
        private float playNextSound;
    }
}