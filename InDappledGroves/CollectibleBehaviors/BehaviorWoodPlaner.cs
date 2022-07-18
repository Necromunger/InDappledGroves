using System;
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
    class BehaviorWoodPlaner : CollectibleBehavior, IBehaviorVariant
    {
        ICoreAPI api;
        private ICoreClientAPI capi;
        public SkillItem[] toolModes;
        public InventoryBase Inventory { get; }
        public string InventoryClassName => "worldinventory";

        public PlaningRecipe recipe;
        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
           
        }

        public SkillItem[] GetSkillItems()
        {
            return toolModes ?? new SkillItem[] { null };
        }

        public BehaviorWoodPlaner(CollectibleObject collObj) : base(collObj)
        {
            this.collObj = collObj;
            Inventory = new InventoryGeneric(1, "planingblock-slot", null, null);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            this.capi = (api as ICoreClientAPI);
            this.groundPlaneTime = collObj.Attributes["woodworkingProps"]["groundPlaneTime"].AsInt(4);
            this.sawHorsePlaneTime = collObj.Attributes["woodworkingProps"]["sawHorsePlaneTime"].AsInt(2);
            this.groundPlaneDamage = collObj.Attributes["woodworkingProps"]["groundPlaneDamage"].AsInt(4);
            this.sawHorsePlaneDamage = collObj.Attributes["woodworkingProps"]["sawHorsePlaneDamage"].AsInt(2);
            interactions = ObjectCacheUtil.GetOrCreate(api, "idgplaneInteractions", () =>
            {
                return new WorldInteraction[] {
                    new WorldInteraction()
                        {
                            ActionLangCode = "indappledgroves:itemhelp-tool-planewood",
                            HotKeyCode = "sprint",
                            MouseButton = EnumMouseButton.Right
                        },
                    };
            });
            woodParticles = InitializeWoodParticles();

            this.toolModes = ObjectCacheUtil.GetOrCreate<SkillItem[]>(api, "idgAxePlaneModes", delegate
            {

                SkillItem[] array;
                array = new SkillItem[]
                {
                        new SkillItem
                        {
                            Code = new AssetLocation("planing"),
                            Name = Lang.Get("Planing", Array.Empty<object>())
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

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            IPlayer player = ((EntityPlayer)byEntity).Player;
            int toolMode = collObj.GetToolMode(slot, player, blockSel);
            SkillItem item = collObj.GetToolModes(slot, (IClientPlayer)player, blockSel)[toolMode];

            //-- Do not process the chopping action if the player is not holding ctrl, block is selected, or the given tools toolMode is not chopping --//
            if (!byEntity.Controls.Sprint || blockSel == null || item.Code.FirstCodePart() == "planing")
                return;

            Inventory[0].Itemstack = new ItemStack(api.World.BlockAccessor.GetBlock(blockSel.Position));
            recipe = GetMatchingPlaningRecipe(byEntity.World, Inventory[0]);
            if (recipe == null || recipe.RequiresStation) return;

            Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
            JsonObject attributes = interactedBlock.Attributes?["woodworkingProps"]["idgSawHorseProps"]["planable"];

            if (attributes == null || !attributes.Exists || !attributes.AsBool(false)) return;
            if (slot.Itemstack.Attributes.GetInt("durability") < groundPlaneDamage && slot.Itemstack.Attributes.GetInt("durability") != 0)
            {
                capi.TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", groundPlaneDamage));
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
                Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
                if (!interactedBlock.Attributes?["idgSawHorseProps"]["planable"].Exists == null) return false;
                if (((int)api.Side) == 1 && playNextSound < secondsUsed)
                {
                    //api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, null, true, 32, 1f);
                    playNextSound += .7f;
                }
                if (secondsUsed >= groundPlaneTime)
                {

                    

                    if (secondsUsed >= groundPlaneTime && interactedBlock.Attributes["idgSawHorseProps"]["planable"].AsBool(false))
                        SpawnOutput(new ItemStack(api.World.BlockAccessor.GetBlock(blockSel.Position)).Collectible, byEntity, pos, groundPlaneDamage);
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

        //-- Spawns output when chopping cycle is finished --//
        public void SpawnOutput(CollectibleObject chopObj, EntityAgent byEntity, BlockPos pos, int dmg)
        {
            Item itemOutput = api.World.GetItem(new AssetLocation(chopObj.Attributes["idgSawHorseProps"]["output"]["code"].AsString()));
            Block blockOutput = api.World.GetBlock(new AssetLocation(chopObj.Attributes["idgSawHorseProps"]["output"]["code"].AsString()));
            int quantity = chopObj.Attributes["idgSawHorseProps"]["output"]["quantity"].AsInt();

            for (int i = quantity; i > 0; i--)
            {
                api.World.SpawnItemEntity(new ItemStack(itemOutput != null ? itemOutput : blockOutput), pos.ToVec3d() + new Vec3d(0.05f, .1f, 0.05f));
            }

            if (byEntity is EntityPlayer player)
                player.RightHandItemSlot.Itemstack.Collectible.DamageItem(api.World, byEntity, player.RightHandItemSlot, groundPlaneDamage);
        }

        public void SpawnOutput(PlaningRecipe recipe, EntityAgent byEntity, BlockPos pos)
        {
            ItemStack output = recipe.Output.ResolvedItemstack;
            int j = output.StackSize;
            for (int i = j; i > 0; i--)
            {
                api.World.SpawnItemEntity(new ItemStack(output.Collectible), pos.ToVec3d(), new Vec3d(0.05f, .1f, 0.05f));
            }

        }

        public PlaningRecipe GetMatchingPlaningRecipe(IWorldAccessor world, ItemSlot slots)
        {
            List<PlaningRecipe> recipes = IDGRecipeRegistry.Loaded.PlaningRecipes;
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

        public int groundPlaneTime;
        public int sawHorsePlaneTime;
        public int groundPlaneDamage;
        public int sawHorsePlaneDamage;
        WorldInteraction[] interactions = null;
        private SimpleParticleProperties woodParticles;
        private float playNextSound;
    }
}

