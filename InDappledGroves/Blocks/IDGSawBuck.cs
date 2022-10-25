using System;
using InDappledGroves.BlockEntities;
using InDappledGroves.Interfaces;
using InDappledGroves.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace InDappledGroves.Blocks
{
	// Token: 0x02000016 RID: 22
	internal class IDGSawBuck : Block
	{
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			ItemStack itemstack2 = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject collectibleObject = (itemstack2 != null) ? itemstack2.Collectible : null;
			string toolmode = "";
			IDGBESawBuck idgbesawBuck = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawBuck;
			if (idgbesawBuck == null)
			{
				return base.OnBlockInteractStart(world, byPlayer, blockSel);
			}
			if (collectibleObject != null)
			{
				IIDGTool iidgtool = collectibleObject as IIDGTool;
				if (iidgtool != null)
				{
					toolmode = iidgtool.GetToolModeName(byPlayer.InventoryManager.ActiveHotbarSlot);
				}
			}
			if (idgbesawBuck.Inventory.Empty || byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
			{
				return idgbesawBuck.OnInteract(byPlayer);
			}
			this.recipe = idgbesawBuck.GetMatchingSawbuckRecipe(idgbesawBuck.InputSlot, toolmode);
			if (this.recipe == null)
			{
				return false;
			}
			if (itemstack.Attributes.GetInt("durability", 0) < this.recipe.ToolDamage && InDappledGrovesConfig.Current.preventToolUseWithLowDurability)
			{
				(this.api as ICoreClientAPI).TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", new object[]
				{
					this.recipe.ToolDamage
				}));
				return base.OnBlockInteractStart(world, byPlayer, blockSel);
			}
			byPlayer.Entity.StartAnimation("axechop");
			return true;
		}

		public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject collectibleObject = (itemstack != null) ? itemstack.Collectible : null;
			IDGBESawBuck idgbesawBuck = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawBuck;
			BlockPos position = blockSel.Position;
			if (collectibleObject != null && collectibleObject is IIDGTool && !idgbesawBuck.Inventory.Empty)
			{
				if (this.playNextSound < secondsUsed)
				{
					this.api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), (double)position.X, (double)position.Y, (double)position.Z, byPlayer, true, 32f, 1f);
					this.playNextSound += 0.7f;
				}

				curDmgFromMiningSpeed += collectibleObject.GetMiningSpeed(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack, blockSel, idgbesawBuck.Inventory[0].Itemstack.Block, byPlayer) * (secondsUsed - lastSecondsUsed);
				lastSecondsUsed = secondsUsed;

				if (secondsUsed + (curDmgFromMiningSpeed / 2) >= idgbesawBuck.Inventory[0].Itemstack.Block.Resistance)
				{
					this.SpawnOutput(this.recipe, blockSel.Position);
					idgbesawBuck.Inventory.Clear();
					(world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawBuck).updateMeshes();
					idgbesawBuck.MarkDirty(true, null);
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
			byPlayer.Entity.StopAnimation("axechop");
		}

		public void SpawnOutput(IDGRecipeNames.SawbuckRecipe recipe, BlockPos pos)
		{
			for (int i = recipe.Output.StackSize; i > 0; i--)
			{
				this.api.World.SpawnItemEntity(new ItemStack(recipe.Output.ResolvedItemstack.Collectible, 1), pos.ToVec3d(), new Vec3d(0.05000000074505806, 0.10000000149011612, 0.05000000074505806));
			}
		}

		private IDGRecipeNames.SawbuckRecipe recipe;
		private float playNextSound;
		private float resistance;
		private float lastSecondsUsed;
		private float curDmgFromMiningSpeed;
	}
}
