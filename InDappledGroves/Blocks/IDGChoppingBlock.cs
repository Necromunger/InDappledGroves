using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Interfaces;
using InDappledGroves.Util.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames;

namespace InDappledGroves.Blocks
{
    class IDGChoppingBlock : Block
    {

		
		ChoppingBlockRecipe recipe;
		float toolModeMod;

        public override string GetHeldItemName(ItemStack stack) => GetName();
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos) => GetName();

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
            string curTMode = "";
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
			CollectibleObject collObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;

			//Check to see if block entity exists
			if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBEChoppingBlock bechoppingblock) return base.OnBlockInteractStart(world, byPlayer, blockSel);

			if (collObj != null && collObj.HasBehavior<BehaviorIDGTool>()) { curTMode = collObj.GetBehavior<BehaviorIDGTool>().GetToolModeName(slot.Itemstack); 
				toolModeMod = collObj.GetBehavior<BehaviorIDGTool>().GetToolModeMod(slot.Itemstack); 
			};
			          
			if (!bechoppingblock.Inventory.Empty)
			{
				if (collObj != null && collObj.HasBehavior<BehaviorIDGTool>())
				{
					recipe = bechoppingblock.GetMatchingChoppingBlockRecipe(world, bechoppingblock.InputSlot, curTMode);
					if (recipe != null)
					{
						resistance = (bechoppingblock.Inventory[0].Itemstack.Collectible is Block ? bechoppingblock.Inventory[0].Itemstack.Block.Resistance : bechoppingblock.Inventory[0].Itemstack.Collectible.Attributes["resistance"].AsFloat() * InDappledGroves.baseWorkstationResistanceMult);
							byPlayer.Entity.StartAnimation("axesplit-fp");
							return true;
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

			if (chopTool != null && chopTool.HasBehavior<BehaviorIDGTool>() && !bechoppingblock.Inventory.Empty)
			{
				if (playNextSound < secondsUsed)
				{
					api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, byPlayer, true, 32, 1f);
					playNextSound += .7f;
                }

				if (bechoppingblock.Inventory[0].Itemstack.Collectible is Block)
				{
					curDmgFromMiningSpeed += (chopTool.GetMiningSpeed(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack, blockSel, bechoppingblock.Inventory[0].Itemstack.Block, byPlayer) * InDappledGroves.baseWorkstationMiningSpdMult) * (secondsUsed - lastSecondsUsed);
				}
				else
				{
					curDmgFromMiningSpeed += (chopTool.MiningSpeed[(EnumBlockMaterial)recipe.IngredientMaterial]*InDappledGroves.baseWorkstationMiningSpdMult) * (secondsUsed - lastSecondsUsed);
				}

				lastSecondsUsed = secondsUsed;
				float curMiningProgress = (secondsUsed + (curDmgFromMiningSpeed)) * (toolModeMod * IDGToolConfig.Current.baseWorkstationMiningSpdMult);
				float curResistance = resistance * IDGToolConfig.Current.baseWorkstationResistanceMult;
				api.Logger.Debug("Resistance of item on block is: " + resistance + ". Resistance after multiplier is " + curResistance + ".");
				if (curMiningProgress >= curResistance) 
				{
					if (api.Side == EnumAppSide.Server)
					{
						bechoppingblock.SpawnOutput(recipe, byPlayer.Entity, blockSel.Position);

						EntityPlayer playerEntity = byPlayer.Entity;

						chopTool.DamageItem(api.World, playerEntity, playerEntity.RightHandItemSlot, recipe.BaseToolDmg);

						if (recipe.ReturnStack.ResolvedItemstack.Collectible.FirstCodePart() == "air")
						{
							bechoppingblock.Inventory.Clear();
						}
						else
						{
							bechoppingblock.Inventory.Clear();
							bechoppingblock.ReturnStackPut(recipe.ReturnStack.ResolvedItemstack.Clone());
						}
						byPlayer.Entity.StopAnimation("axesplit-fp");
					}
                    return false;
                }
				return !bechoppingblock.Inventory.Empty;
            }
            return false;
        }

		

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			resistance = 0;
			lastSecondsUsed = 0;
			curDmgFromMiningSpeed = 0;
			playNextSound = 0.7f;
            IDGBEChoppingBlock bechoppingblock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEChoppingBlock;
            bechoppingblock.MarkDirty(true);
            bechoppingblock.updateMeshes();
            byPlayer.Entity.StopAnimation("axesplit-fp");
			
		}

        public string GetName()
        {
            var material = Variant["wood"];

            var part = Lang.Get("material-" + $"{material}");
            part = $"{part[0].ToString().ToUpper()}{part.Substring(1)}";
            return string.Format($"{part} {Lang.Get("indappledgroves:block-choppingblock")}");
        }

        private float playNextSound;
		private float resistance;
		private float lastSecondsUsed;
		private float curDmgFromMiningSpeed;
		}
		
}
