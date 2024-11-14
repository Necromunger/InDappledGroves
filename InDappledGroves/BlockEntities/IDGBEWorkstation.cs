using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Util.Handlers;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames;

namespace InDappledGroves.BlockEntities
{
    abstract class IDGBEWorkstation : BlockEntityDisplay
    {
		public override InventoryBase Inventory { get; }

		public override string InventoryClassName => "workstation";

        public override string AttributeTransformCode => "workstationTransform";

        public bool recipecomplete { get; set; } = false;

        public RecipeHandler recipeHandler { get; set; }

        public float currentMiningDamage { get; set; }

        public ItemSlot InputSlot { get; set; }

        //static List<ChoppingBlockRecipe> choppingBlockrecipes = IDGRecipeRegistry.Loaded.ChoppingBlockRecipes;

        public IDGBEWorkstation()
		{
            //Must initialize inventory in derived classes
            //Inventory = new InventoryDisplayed(this, 2, InventoryClassName + "-slot", null, null);
        }



        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (recipeHandler == null)
            {
                recipeHandler = new RecipeHandler(api);
            }
            this.capi = (api as ICoreClientAPI);
        }

        public virtual bool OnInteract(IPlayer byPlayer)
		{
			ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

			//If The Players Hand Is Empty
			if (activeHotbarSlot.Empty)
			{
				return this.TryTake(byPlayer, InputSlot);
			}

			if (GetMatchingRecipes(Api.World, activeHotbarSlot)){
                this.TryPut(byPlayer, activeHotbarSlot, InputSlot); 
			}
			return true;
		}

        public virtual bool TryPut(IPlayer byPlayer, ItemSlot slot, ItemSlot targetSlot)
        {
            if (targetSlot.Empty)
            {
                Block block = slot.Itemstack.Block;
                int num3 = slot.TryPutInto(this.Api.World, targetSlot, 1);
                if (num3 > 0)
                {

                    AssetLocation assetLocation;
                    if (block == null)
                    {
                        assetLocation = null;
                    }
                    else
                    {
                        BlockSounds sounds = block.Sounds;
                        assetLocation = (sounds?.Place);
                    }
                    AssetLocation assetLocation2 = assetLocation;
                    this.Api.World.PlaySoundAt(assetLocation2 ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
                };
            }
            updateMeshes();
            MarkDirty(true);
            return false;
        }

        public virtual bool TryTake(IPlayer byPlayer,ItemSlot targetSlot)
		{
				if (!targetSlot.Empty)
				{
					ItemStack itemStack = targetSlot.TakeOut(1);
					if (byPlayer.InventoryManager.TryGiveItemstack(itemStack, false))
					{
						Block block = itemStack.Block;
						AssetLocation assetLocation;
						if (block == null)
						{
							assetLocation = null;
						}
						else
						{
							BlockSounds sounds = block.Sounds;
							assetLocation = (sounds?.Place);
						}
						AssetLocation assetLocation2 = assetLocation;
						this.Api.World.PlaySoundAt(assetLocation2 ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
					}
					if (itemStack.StackSize > 0)
					{
						this.Api.World.SpawnItemEntity(itemStack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5), null);
					}
					base.MarkDirty(true, null);
					this.updateMeshes();
					return false;
				}
			return false;
		}

        public virtual bool UpdateInventory(ICoreAPI api, IPlayer byPlayer, ItemStack returnStack, IDGBEWorkstation workstation)
        {
            return recipeHandler.UpdateInventory(api, byPlayer, returnStack, workstation);
        }

        /*TODO: Revise recipe classes to have a parent class holding all basic processing information.
         * Make unique workstation recipes extend the initial class. This should be relatively simple,
         * as the majority of recipes use exactly the same values, excepting their location.
         */ 
        //internal bool handleRecipe(CollectibleObject heldCollectible, float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        //{
        //    ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        //    string curTMode = heldCollectible.GetBehavior<BehaviorIDGTool>().GetToolModeName(slot.Itemstack);
        //    recipe = GetMatchingRecipe(world, InputSlot, BladeSlot.Itemstack.Collectible.Variant["style"].ToString(), curTMode);
        //    if (recipe == null)
        //    {
        //        return false;
        //    }

        //    ItemStack itemstack = slot.Itemstack;
        //    BlockPos position = blockSel.Position;
        //    if (recipeHandler.recipeValues == null)
        //    {
        //        recipeHandler.recipeValues = new RecipeValues(Inventory[1].Itemstack, recipe.IngredientMaterial, recipe.Output.ResolvedItemstack, recipe.ReturnStack.ResolvedItemstack, recipe.BaseToolDmg);
        //    }

        //    bool recipeComplete = recipeHandler.processRecipe(heldCollectible, curTMode, slot, byPlayer.Entity, blockSel.Position, secondsUsed);

        //    if (recipeComplete)
        //    {
        //        recipeComplete = UpdateInventory(Api, byPlayer, recipe.ReturnStack.ResolvedItemstack, this);

        //    }

        //    updateMeshes();
        //    base.MarkDirty(true, null);
        //    return !recipeComplete;
        //}

        public virtual bool GetMatchingRecipes(IWorldAccessor world, ItemSlot slots)
		{

            List<ChoppingBlockRecipe> recipes = IDGRecipeRegistry.Loaded.ChoppingBlockRecipes;
			if (recipes == null) return false;

			for (int j = 0; j < recipes.Count; j++)
			{
				if (recipes[j].Matches(Api.World, slots))
				{
					return true;
				}
			}

			return false;
		}

		public ChoppingBlockRecipe GetMatchingChoppingBlockRecipe(IWorldAccessor world, ItemSlot slots, string toolmode)
		{
			List<ChoppingBlockRecipe> recipes = IDGRecipeRegistry.Loaded.ChoppingBlockRecipes;
			if (recipes == null) return null;

			for (int j = 0; j < recipes.Count; j++)
			{
				if (recipes[j].Matches(Api.World, slots) && (recipes[j].ToolMode == toolmode))
				{
					return recipes[j];
				}
			}

			return null;
		}

		public void SpawnOutput(ChoppingBlockRecipe recipe, BlockPos pos)
		{
			int j = recipe.Output.StackSize;
			for (int i = j; i > 0; i--)
			{
				Api.World.SpawnItemEntity(new ItemStack(recipe.Output.ResolvedItemstack.Collectible, 1), pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
			}
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
            dsc.AppendLine(Lang.GetMatching("indappledgroves:workstationWorkItem") + ": " + (Inventory[1].Empty ? Lang.GetMatching("indappledgroves:Empty") : Inventory[1].Itemstack.GetName()));
            if(Api.Side.IsClient()){
					dsc.AppendLine("Recipe Progress: " + Math.Round((recipeHandler.recipeProgress)*100)+ "%");
				}
        }

	}
}
