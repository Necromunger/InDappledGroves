using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Util.Config;
using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames;

namespace InDappledGroves.Blocks
{
    class IDGLogSplitter : Block
    {

		
		LogSplitterRecipe recipe;
		float toolModeMod;
        bool recipecomplete = false;

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
                System.Diagnostics.Debug.WriteLine("Inventory on " + api.Side.ToString() + " contains " + idgbelogsplitter.Inventory[1].ToString());
                return oninteractresult;
            }

            if (!idgbelogsplitter.Inventory.Empty && heldCollectible != null && heldCollectible.HasBehavior<BehaviorIDGTool>()
               )
            {
                   
                    curTMode = heldCollectible.GetBehavior<BehaviorIDGTool>().GetToolModeName(slot.Itemstack);
                    toolModeMod = heldCollectible.GetBehavior<BehaviorIDGTool>().GetToolModeMod(slot.Itemstack);

                //TODO Put Recipe And Resistance Getting To A Separate Method
                    recipe = idgbelogsplitter.GetMatchingLogSplitterRecipe(world, idgbelogsplitter.InputSlot, idgbelogsplitter.BladeSlot.Itemstack.Collectible.Variant["style"].ToString() , curTMode);

                    if (recipe == null)
                    {
                        return false;
                    }

                resistance = (idgbelogsplitter.Inventory[1].Itemstack.Collectible is Block ? idgbelogsplitter.Inventory[1].Itemstack.Block.Resistance
                    : idgbelogsplitter.Inventory[1].Itemstack.Collectible.Attributes["resistance"].AsFloat());
                resistance *= InDappledGroves.baseWorkstationResistanceMult; 
                byPlayer.Entity.StartAnimation("axesplit-fp");

                //TODO Seperate This Process Into It's Own Method
                if (this.playNextSound < secondsUsed)
                {
                    this.api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), (double)position.X, (double)position.Y, (double)position.Z, byPlayer, true, 32f, 1f);
                    this.playNextSound += 0.7f;
                }

                if (idgbelogsplitter.Inventory[1].Itemstack.Collectible is Block)
                {
                    if ((secondsUsed - lastSecondsUsed) > 0.025)
                    {
                        curDmgFromMiningSpeed += (heldCollectible.GetMiningSpeed(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack, blockSel, idgbelogsplitter.Inventory[1].Itemstack.Block, byPlayer) * InDappledGroves.baseWorkstationMiningSpdMult)
                     * (secondsUsed - lastSecondsUsed);
                    }
                }
                else

                {
                    
                    curDmgFromMiningSpeed += (heldCollectible.MiningSpeed[(EnumBlockMaterial)recipe.IngredientMaterial]) * (secondsUsed - lastSecondsUsed);
                }

                lastSecondsUsed = secondsUsed;

                EntityPlayer playerEntity = byPlayer.Entity;

                float curMiningProgress = (secondsUsed + (curDmgFromMiningSpeed)) * (toolModeMod * IDGToolConfig.Current.baseWorkstationMiningSpdMult);
                float curResistance = resistance;

                if (api.Side == EnumAppSide.Server && curMiningProgress >= curResistance)
                {
                    idgbelogsplitter.SpawnOutput(this.recipe, playerEntity, blockSel.Position);
                    heldCollectible.DamageItem(api.World, playerEntity, playerEntity.RightHandItemSlot, recipe.BaseToolDmg);
                    if (recipe.ReturnStack.ResolvedItemstack.Collectible.FirstCodePart() == "air")
                    {
                        if (idgbelogsplitter.Inventory[1].Empty) return false;
                        idgbelogsplitter.Inventory[1].Itemstack = null;
                        idgbelogsplitter.Inventory[0].Itemstack.Collectible.DamageItem(api.World, byPlayer.Entity, idgbelogsplitter.Inventory[0]);
                        return false; //If no stack is returned, clear stack
                    }
                    else
                    {
                        //TODO: Determine if check needed to prevent spawning of excess resources
                        idgbelogsplitter.Inventory[1].Itemstack = null;
                        idgbelogsplitter.ReturnStackPut(recipe.ReturnStack.ResolvedItemstack.Clone());
                        idgbelogsplitter.Inventory[0].Itemstack.Collectible.DamageItem(api.World, byPlayer.Entity, idgbelogsplitter.Inventory[0]);
                        curDmgFromMiningSpeed = 0; //Reset damage accumulation to ensure resistance doesn't carry over.
                        return true; //If a stack is returned from the recipe, allow process to continue after resetting dmg accumulation
                    }
                }
                idgbelogsplitter.updateMeshes();
                idgbelogsplitter.MarkDirty(true);
                return (!idgbelogsplitter.Inventory.Empty);
            }
            return false;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			resistance = 0;
			lastSecondsUsed = 0;
			curDmgFromMiningSpeed = 0;
			playNextSound = 0.7f;
            IDGBELogSplitter belogsplitter = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBELogSplitter;
            belogsplitter.MarkDirty(true);
            belogsplitter.updateMeshes();
            byPlayer.Entity.StopAnimation("axesplit-fp");
			
		}

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            resistance = 0;
            lastSecondsUsed = 0;
            curDmgFromMiningSpeed = 0;
            playNextSound = 0.7f;
            IDGBELogSplitter belogsplitter = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBELogSplitter;
            belogsplitter.MarkDirty(true);
            belogsplitter.updateMeshes();
            byPlayer.Entity.StopAnimation("axesplit-fp");
            return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
        }

        private float playNextSound;
		private float resistance;
		private float lastSecondsUsed;
		private float curDmgFromMiningSpeed;
		}
		
}
