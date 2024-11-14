using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace InDappledGroves.Blocks
{
    class IDGLogSplitter : Block
    {

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            
            if(blockSel == null || byPlayer.Entity.BlockSelection.Position == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);
            IDGBELogSplitter belogsplitter = world.BlockAccessor.GetBlockEntity(byPlayer.Entity.BlockSelection.Position) as IDGBELogSplitter;

            if (belogsplitter == null) 
                return base.OnBlockInteractStart(world, byPlayer, byPlayer.Entity.BlockSelection);

            return true;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {

            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemStack itemstack = slot.Itemstack;
            CollectibleObject heldCollectible = itemstack?.Collectible;
            BlockPos position = blockSel.Position;
            string curTMode = "";
            IDGBELogSplitter idgbelogsplitter = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBELogSplitter;

            if(idgbelogsplitter != null && (heldCollectible == null || !heldCollectible.HasBehavior<BehaviorIDGTool>()))
            {
                bool oninteractresult = idgbelogsplitter.OnInteract(byPlayer);
                idgbelogsplitter.updateMeshes();
                idgbelogsplitter.MarkDirty(true);
                return oninteractresult;
            }

            if (!idgbelogsplitter.Inventory[1].Empty && heldCollectible != null && heldCollectible.HasBehavior<BehaviorIDGTool>())
            {

                return idgbelogsplitter.handleRecipe(heldCollectible, secondsUsed, world, byPlayer, blockSel);
            }

            return false;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            IDGBELogSplitter belogsplitter = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBELogSplitter;
            if (byPlayer.Entity.Api is ICoreClientAPI api && belogsplitter.recipeHandler.lastSecondsUsed !=0)
            {
                api.TriggerChatMessage("Resistance: "+ belogsplitter.recipeHandler.resistance.ToString());
                api.TriggerChatMessage("SecondsUsed: "+ belogsplitter.recipeHandler.lastSecondsUsed.ToString());
            }
            belogsplitter.recipeHandler.clearRecipe();
            belogsplitter.MarkDirty(true);
            belogsplitter.updateMeshes();
            byPlayer.Entity.StopAnimation("axesplit-fp");
		}

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            
            IDGBELogSplitter belogsplitter = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBELogSplitter;
            if (byPlayer.Entity.Api is ICoreClientAPI api && belogsplitter.recipeHandler.lastSecondsUsed != 0)
            {
                if (api.Side.IsServer()) {  
                api.TriggerChatMessage("Resistance: " + belogsplitter.recipeHandler.resistance.ToString());
                api.TriggerChatMessage("SecondsUsed: " + belogsplitter.recipeHandler.lastSecondsUsed.ToString());
                }
            }
            belogsplitter.recipeHandler.clearRecipe();
            //TODO: Add handler for incomplete recipes.
            belogsplitter.MarkDirty(true);
            belogsplitter.updateMeshes();
            byPlayer.Entity.StopAnimation("axesplit-fp");
            return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
        }

		}
		
}
