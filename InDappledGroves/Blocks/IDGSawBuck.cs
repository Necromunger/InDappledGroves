using InDappledGroves.BlockEntities;
using InDappledGroves.Interfaces;
using InDappledGroves.Util;
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
			
			ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject collObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
			string curTMode = "";
			//Check to see if block entity exists
			if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBESawBuck besawbuck) return base.OnBlockInteractStart(world, byPlayer, blockSel);

			//If player is holding something, it has the BehaviorWoodSawer behavior, and the chopping block is not empty.
			if (collObj != null && collObj is IIDGTool tool) curTMode = tool.GetToolModeName(byPlayer.InventoryManager.ActiveHotbarSlot);

			if (!besawbuck.Inventory.Empty && !byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
			{
				recipe = besawbuck.GetMatchingSawbuckRecipe(besawbuck.InputSlot, curTMode);
				if (recipe != null)
				{
					if (stack.Attributes.GetInt("durability") < recipe.ToolDamage && InDappledGrovesConfig.Current.preventToolUseWithLowDurability)
					{
						(api as ICoreClientAPI).TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", recipe.ToolDamage));
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
			IDGBESawBuck besawbuck = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawBuck;
			BlockPos pos = blockSel.Position;

			if (sawTool != null && sawTool is IIDGTool && !besawbuck.Inventory.Empty)
			{
				if (playNextSound < secondsUsed)
				{
					api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, byPlayer, true, 32, 1f);
					playNextSound += .7f;
				}
				if (secondsUsed >= recipe.ToolTime)
				{
					SpawnOutput(recipe, blockSel.Position);

					besawbuck.Inventory.Clear();
					(world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawBuck).updateMeshes();
					besawbuck.MarkDirty(true);
				}
				return !besawbuck.Inventory.Empty;
			}
			return false;
		}

		public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			playNextSound = 0.7f;
			byPlayer.Entity.StopAnimation("axechop");
		}

		public void SpawnOutput(SawbuckRecipe recipe, BlockPos pos)
		{
			int j = recipe.Output.StackSize;
			for (int i = j; i > 0; i--)
			{
				api.World.SpawnItemEntity(new ItemStack(recipe.Output.ResolvedItemstack.Collectible), pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
			}
		}

		private float playNextSound;
	}

}
