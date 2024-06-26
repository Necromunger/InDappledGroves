using System;
using InDappledGroves.BlockEntities;
using InDappledGroves.Interfaces;
using InDappledGroves.Util.Config;
using InDappledGroves.Util.RecipeTools;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames;

namespace InDappledGroves.Blocks
{
    // Token: 0x02000016 RID: 22
    internal class IDGSawBuck : Block
	{
		
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
		}

		public override string GetHeldItemName(ItemStack stack) => GetName();
		public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos) => GetName();

		public string GetName()
		{
			var material1 = "material-" + Variant["support"];
			var material2 = "material-" + Variant["crossbrace"];
			var part1 = Lang.Get(material1);
			var part2 = Lang.Get(material2);
			part1 = $"{part1[0].ToString().ToUpper()}{part1.Substring(1)}";
			part2 = $"{part2[0].ToString().ToUpper()}{part2.Substring(1)}";
			return string.Format($"{part1} & {part2} {Lang.Get("indappledgroves:block-sawbuck")}");
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			string curTMode = "";
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject collObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;

			//Check to see if block entity exists
			if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBESawBuck besawbuck) return base.OnBlockInteractStart(world, byPlayer, blockSel);

			if (collObj != null && collObj is IIDGTool tool) { curTMode = tool.GetToolModeName(slot.Itemstack); toolModeMod = tool.GetToolModeMod(slot.Itemstack); };

			if (!besawbuck.Inventory.Empty)
			{
				if (collObj is IIDGTool)
				{
					recipe = besawbuck.GetMatchingSawbuckRecipe(besawbuck.InputSlot, curTMode);
					if (recipe != null)
					{
						resistance = resistance = (besawbuck.Inventory[0].Itemstack.Collectible is Block ? besawbuck.Inventory[0].Itemstack.Block.Resistance : ((float)recipe.IngredientResistance)) * InDappledGroves.baseWorkstationResistanceMult;
						byPlayer.Entity.StartAnimation("sawsaw-fp");
						return true;
					}
					return false;
				}
			}

			return besawbuck.OnInteract(byPlayer);
		}

		public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject sawtool = itemstack?.Collectible;
			IDGBESawBuck idgbesawBuck = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawBuck;
			BlockPos position = blockSel.Position;
			if (sawtool != null && sawtool is IIDGTool && !idgbesawBuck.Inventory.Empty)
			{
				if (this.playNextSound < secondsUsed)
				{
					this.api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), (double)position.X, (double)position.Y, (double)position.Z, byPlayer, true, 32f, 1f);
					this.playNextSound += 0.7f;
				}

				curDmgFromMiningSpeed += (sawtool.GetMiningSpeed(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack, blockSel, idgbesawBuck.Inventory[0].Itemstack.Block, byPlayer) * InDappledGroves.baseWorkstationMiningSpdMult) * (secondsUsed - lastSecondsUsed);
				lastSecondsUsed = secondsUsed;

				EntityPlayer playerEntity = byPlayer.Entity;
				
				float curMiningProgress = (secondsUsed + (curDmgFromMiningSpeed)) * (toolModeMod * IDGToolConfig.Current.baseWorkstationMiningSpdMult);
				float curResistance = resistance * IDGToolConfig.Current.baseWorkstationResistanceMult;

                if (api.Side == EnumAppSide.Server && curMiningProgress >= curResistance)
                {
					idgbesawBuck.SpawnOutput(this.recipe, blockSel.Position);
					sawtool.DamageItem(api.World, playerEntity, playerEntity.RightHandItemSlot, recipe.BaseToolDmg);
					idgbesawBuck.Inventory.Clear();
					(world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawBuck).updateMeshes();
					idgbesawBuck.MarkDirty(true, null);
                    byPlayer.Entity.StopAnimation("sawsaw-fp");
                }
				return !idgbesawBuck.Inventory.Empty;
			}
			return false;
		}

		public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			resistance = 0;
			lastSecondsUsed = 0;
			curDmgFromMiningSpeed = 0;
			playNextSound = 0.7f;
			byPlayer.Entity.StopAnimation("sawsaw-fp");
		}

		public void SpawnOutput(IDGRecipeNames.SawbuckRecipe recipe, BlockPos pos)
		{
			for (int i = recipe.Output.StackSize; i > 0; i--)
			{
				this.api.World.SpawnItemEntity(new ItemStack(recipe.Output.ResolvedItemstack.Collectible, 1), pos.ToVec3d(), new Vec3d(0.05000000074505806, 0.10000000149011612, 0.05000000074505806));
			}
		}

		float toolModeMod;
		private SawbuckRecipe recipe;
		private float playNextSound;
		private float resistance;
		private float lastSecondsUsed;
		private float curDmgFromMiningSpeed;
	}
}
