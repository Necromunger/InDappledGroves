using InDappledGroves.BlockEntities;
using InDappledGroves.Interfaces;
using InDappledGroves.Util;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.Blocks
{
    class IDGChoppingBlock : Block
    {
		
		ChoppingBlockRecipe recipe;
		// Token: 0x06000BD6 RID: 3030 RVA: 0x000068EB File Offset: 0x00004AEB
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			string curTMode = "";
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject collObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;

			//Check to see if block entity exists
			if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBEChoppingBlock bechoppingblock) return base.OnBlockInteractStart(world, byPlayer, blockSel);

			if (collObj != null && collObj is IIDGTool tool) {curTMode = tool.GetToolModeName(slot);};
			          
			if (!bechoppingblock.Inventory.Empty)
			{
				if (collObj is IIDGTool)
				{
					recipe = bechoppingblock.GetMatchingChoppingBlockRecipe(world, bechoppingblock.InputSlot, curTMode);
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
			}

			return bechoppingblock.OnInteract(byPlayer);
		}

		public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
			CollectibleObject chopTool = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
			IDGBEChoppingBlock bechoppingblock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEChoppingBlock;
			BlockPos pos = blockSel.Position;

			if (chopTool != null && chopTool is IIDGTool && !bechoppingblock.Inventory.Empty)
			{
				if (playNextSound < secondsUsed)
				{
					api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, byPlayer, true, 32, 1f);
					playNextSound += .7f;
                }
				if (secondsUsed >= 2.5)
					//if (secondsUsed >= recipe.ToolTime)
                {	/*Establish a method for determining the miningspeed of the tool 
                 	 * based on the contents of the chopping block,
                 	 * with a default for items without a set blockMaterial, 
                 	 * such as firewood.
                 	 */

					bechoppingblock.SpawnOutput(recipe, byPlayer.Entity, blockSel.Position);

					EntityPlayer playerEntity = byPlayer.Entity;

					chopTool.DamageItem(api.World, playerEntity, playerEntity.RightHandItemSlot, recipe.ToolDamage);

					if (recipe.ReturnStack.ResolvedItemstack.Collectible.FirstCodePart() == "air")
					{
						bechoppingblock.Inventory.Clear();
					} else
                    {
						bechoppingblock.Inventory[0].Itemstack = recipe.ReturnStack.ResolvedItemstack.Clone();
                    }
					(world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEChoppingBlock).updateMeshes();
					bechoppingblock.MarkDirty(true);
					return false;
                }		
				return !bechoppingblock.Inventory.Empty;
			}
			return false;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			playNextSound = 0.7f;
			byPlayer.Entity.StopAnimation("axechop");
		}

		
		private float playNextSound;
	}
		
}
