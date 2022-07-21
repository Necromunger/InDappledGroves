using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Util;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.Blocks
{
    class IDGSawBuck : Block
	{
		SawbuckRecipe recipe;
		// Token: 0x06000BD6 RID: 3030 RVA: 0x000068EB File Offset: 0x00004AEB
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemStack sawToolStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject sawCollObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;

			//Check to see if block entity exists
			if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBESawBuck besawbuck) return base.OnBlockInteractStart(world, byPlayer, blockSel);

			//If player is holding something, it has the BehaviorWoodSawer behavior, and the chopping block is not empty.
			if (sawCollObj != null && sawCollObj.HasBehavior<BehaviorWoodSawer>() && !besawbuck.Inventory.Empty)
			{
				recipe = GetMatchingSawbuckRecipe(world, besawbuck.InputSlot);
				if (recipe != null)
				{
					if (sawToolStack.Attributes.GetInt("durability") < sawCollObj.GetBehavior<BehaviorWoodSawer>().sawBuckSawDamage && InDappledGrovesConfig.Current.preventChoppingWithLowDurability)
					{
						(api as ICoreClientAPI).TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", sawCollObj.GetBehavior<BehaviorWoodSawer>().sawBuckSawDamage));
						return base.OnBlockInteractStart(world, byPlayer, blockSel);
					}
					else
					{
						byPlayer.Entity.StartAnimation("axechop");
						return true;
					}
				}
				return false;
			}
			return besawbuck.OnInteract(byPlayer);
		}

		public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			CollectibleObject sawTool = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
			IDGBESawBuck bebesawbuck = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawBuck;
			BlockPos pos = blockSel.Position;

			if (recipe != null && sawTool != null && sawTool.HasBehavior<BehaviorWoodSawer>() && !bebesawbuck.Inventory.Empty)
			{
				if (playNextSound < secondsUsed)
				{
					api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, byPlayer, true, 32, 1f);
					playNextSound += .7f;
				}
				if (secondsUsed >= sawTool.GetBehavior<BehaviorWoodSawer>().sawBuckSawTime)
				{
					sawTool.GetBehavior<BehaviorWoodSawer>().SpawnOutput(recipe, byPlayer.Entity, blockSel.Position);

					bebesawbuck.Inventory.Clear();
					(world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawBuck).updateMeshes();
					bebesawbuck.MarkDirty(true);
				}
				return !bebesawbuck.Inventory.Empty;
			}
			return false;
		}
		public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			playNextSound = 0.7f;
			byPlayer.Entity.StopAnimation("axechop");
		}

		public SawbuckRecipe GetMatchingSawbuckRecipe(IWorldAccessor world, ItemSlot slots)
		{
			List<SawbuckRecipe> recipes = IDGRecipeRegistry.Loaded.SawbuckRecipes;
			if (recipes == null) return null;

			SawbuckRecipe stationRecipe = null;
			SawbuckRecipe nostationRecipe = null;

			for (int j = 0; j < recipes.Count; j++)
			{
				if (recipes[j].Matches(api.World, slots))
				{
					if (recipes[j].RequiresStation)
					{
						stationRecipe = recipes[j];
					}
					else
					{
						nostationRecipe = recipes[j];
					}
				}
			}
			return null;
		}

		private float playNextSound;
	}

}
