using InDappledGroves.BlockEntities;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace InDappledGroves.Blocks
{
    class IDGChoppingBlock : Block
    {
		// Token: 0x06000BD6 RID: 3030 RVA: 0x000068EB File Offset: 0x00004AEB
		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			IDGBEChoppingBlock bechoppingblock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEChoppingBlock;
			CollectibleObject colObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
			
			if (bechoppingblock == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);
			if (colObj != null && colObj.HasBehavior<BehaviorWoodSplitter>() && !bechoppingblock.Inventory.Empty)
            {
				byPlayer.Entity.StartAnimation("axechop");
				return true;
            }

			return bechoppingblock.OnInteract(byPlayer, blockSel);
		}

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
			CollectibleObject colObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
			IDGBEChoppingBlock bechoppingblock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEChoppingBlock;
			BlockPos pos = blockSel.Position;

			if (colObj != null && colObj.HasBehavior<BehaviorWoodSplitter>() && !bechoppingblock.Inventory.Empty)
			{
				api.Logger.Debug(secondsUsed.ToString());
				api.Logger.Debug(playNextSound.ToString());
				if (playNextSound < secondsUsed)
				{
					api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, byPlayer, true, 32, 1f);
					playNextSound += .7f;
				}
                if (secondsUsed >= colObj.GetBehavior<BehaviorWoodSplitter>().groundChopTime)
                {
					colObj.GetBehavior<BehaviorWoodSplitter>().SpawnOutput(blockSel, byPlayer.Entity);
					bechoppingblock.Inventory.Clear();
					bechoppingblock.updateMeshes();
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
