using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Util.Config;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames;

namespace InDappledGroves.Blocks
{
    class IDGChoppingBlock : Block
    {

		
		ChoppingBlockRecipe recipe;
		float toolModeMod;
        bool recipecomplete = false;
        /*TODO: Implement InUseCheck.  If UserUID is not "workstationfree", then 
         * UserUID gets set on BlockEntity when user reaches OnHeldInteractStep method
         * if UserUID == "workstationfree". After that step the UserUID is checked against the
         * interacting characters UID. If the UIDs match, the process is allowed to continue. If not,
         * the interacting users attempt returns false. (Will this affect the original user? Testing will tell)
         * Upon HeldInteractCancel or HeldInteractStop, the UserUID get cleared back to "workstationfree."
         * This should nerf the "Power of Friendship" as Stew calls it.
        */
        
        public override string GetHeldItemName(ItemStack stack) => GetName();
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos) => GetName();

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            IDGBEChoppingBlock bechoppingblock = world.BlockAccessor.GetBlockEntity(byPlayer.Entity.BlockSelection.Position) as IDGBEChoppingBlock;
            //Check to see if block entity exists
            if (bechoppingblock == null) return base.OnBlockInteractStart(world, byPlayer, byPlayer.Entity.BlockSelection);
            return bechoppingblock.OnInteract(byPlayer);
        }

        //public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        //{
        //    string curTMode = "";
        //    ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        //    ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
        //    CollectibleObject collObj = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;

        //    //Check to see if block entity exists
        //    if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not IDGBEChoppingBlock bechoppingblock) return base.OnBlockInteractStart(world, byPlayer, blockSel);

        //    if (collObj == null) return bechoppingblock.OnInteract(byPlayer);

        //    if (collObj.HasBehavior<BehaviorIDGTool>())
        //    {
        //        curTMode = collObj.GetBehavior<BehaviorIDGTool>().GetToolModeName(slot.Itemstack);
        //        toolModeMod = collObj.GetBehavior<BehaviorIDGTool>().GetToolModeMod(slot.Itemstack);
        //    };

        //    if (!bechoppingblock.Inventory.Empty)
        //    {
        //        if (collObj.HasBehavior<BehaviorIDGTool>())
        //        {
        //            recipe = bechoppingblock.GetMatchingChoppingBlockRecipe(world, bechoppingblock.InputSlot, curTMode);
        //            if (recipe != null)
        //            {
        //                resistance = (bechoppingblock.Inventory[0].Itemstack.Collectible is Block ? bechoppingblock.Inventory[0].Itemstack.Block.Resistance : ((float)recipe.IngredientResistance)) * InDappledGroves.baseWorkstationResistanceMult;
        //                byPlayer.Entity.StartAnimation("sawsaw-fp");
        //                return true;
        //            }
        //            return false;
        //        }
        //    }

        //    return bechoppingblock.OnInteract(byPlayer);
        //}
        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {

            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            CollectibleObject chopTool = itemstack?.Collectible;
            BlockPos position = blockSel.Position;
            string curTMode = "";
            IDGBEChoppingBlock idgbechoppingblock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEChoppingBlock;
            
            idgbechoppingblock?.updateMeshes();
            idgbechoppingblock?.MarkDirty(true, null);

            if (idgbechoppingblock != null
                && !idgbechoppingblock.Inventory.Empty
                && chopTool != null
                && chopTool.HasBehavior<BehaviorIDGTool>()
               )
            {

                //{
                //    if (api is ICoreClientAPI capi) capi.TriggerIngameError(this, "workstationinuse", "This workstation already in use");
                //    return false;
                //}
                curTMode = chopTool.GetBehavior<BehaviorIDGTool>().GetToolModeName(slot.Itemstack);
                toolModeMod = chopTool.GetBehavior<BehaviorIDGTool>().GetToolModeMod(slot.Itemstack);
                recipe = idgbechoppingblock.GetMatchingChoppingBlockRecipe(world, idgbechoppingblock.InputSlot, curTMode);
                if (recipe == null)
                {
                    return false;
                }

                resistance = (idgbechoppingblock.Inventory[0].Itemstack.Collectible is Block ? idgbechoppingblock.Inventory[0].Itemstack.Block.Resistance
                    : idgbechoppingblock.Inventory[0].Itemstack.Collectible.Attributes["resistance"].AsFloat() * InDappledGroves.baseWorkstationResistanceMult);

                byPlayer.Entity.StartAnimation("axesplit-fp");


                if (this.playNextSound < secondsUsed)
                {
                    this.api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), (double)position.X, (double)position.Y, (double)position.Z, byPlayer, true, 32f, 1f);
                    this.playNextSound += 0.7f;
                }

                if (idgbechoppingblock.Inventory[0].Itemstack.Collectible is Block)
                {
                    if ((secondsUsed - lastSecondsUsed) > 0.025)
                    {
                        curDmgFromMiningSpeed += (chopTool.GetMiningSpeed(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack, blockSel, idgbechoppingblock.Inventory[0].Itemstack.Block, byPlayer) * InDappledGroves.baseWorkstationMiningSpdMult)
                     * (secondsUsed - lastSecondsUsed);
                    }
                }
                else

                {

                    curDmgFromMiningSpeed += (chopTool.MiningSpeed[(EnumBlockMaterial)recipe.IngredientMaterial] * InDappledGroves.baseWorkstationMiningSpdMult) * (secondsUsed - lastSecondsUsed);
                }

                lastSecondsUsed = secondsUsed;

                EntityPlayer playerEntity = byPlayer.Entity;

                float curMiningProgress = (secondsUsed + (curDmgFromMiningSpeed)) * (toolModeMod * IDGToolConfig.Current.baseWorkstationMiningSpdMult);
                float curResistance = resistance * IDGToolConfig.Current.baseWorkstationResistanceMult;

                if (api.Side == EnumAppSide.Server && curMiningProgress >= curResistance)
                {
                    idgbechoppingblock.SpawnOutput(this.recipe, playerEntity, blockSel.Position);
                    chopTool.DamageItem(api.World, playerEntity, playerEntity.RightHandItemSlot, recipe.BaseToolDmg);
                    if (recipe.ReturnStack.ResolvedItemstack.Collectible.FirstCodePart() == "air")
                    {
                        if (idgbechoppingblock.Inventory[0].Empty) return false;
                        idgbechoppingblock.Inventory.Clear();
                        idgbechoppingblock.updateMeshes();
                        idgbechoppingblock.MarkDirty(true);
                        return false; //If no stack is returned, clear stack
                    }
                    else
                    {
                        //TODO: Determine if check needed to prevent spawning of excess resources
                        idgbechoppingblock.Inventory.Clear();
                        idgbechoppingblock.ReturnStackPut(recipe.ReturnStack.ResolvedItemstack.Clone());
                        idgbechoppingblock.updateMeshes();
                        idgbechoppingblock.MarkDirty(true);
                        curDmgFromMiningSpeed = 0; //Reset damage accumulation to ensure resistance doesn't carry over.
                        return true; //If a stack is returned from the recipe, allow process to continue after resetting dmg accumulation
                    }


                }
                idgbechoppingblock.updateMeshes();
                idgbechoppingblock.MarkDirty(true);
                return (!idgbechoppingblock.Inventory.Empty);
            }
            idgbechoppingblock.updateMeshes();
            idgbechoppingblock.MarkDirty(true);
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

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            resistance = 0;
            lastSecondsUsed = 0;
            curDmgFromMiningSpeed = 0;
            playNextSound = 0.7f;
            IDGBEChoppingBlock bechoppingblock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEChoppingBlock;
            bechoppingblock.MarkDirty(true);
            bechoppingblock.updateMeshes();
            byPlayer.Entity.StopAnimation("axesplit-fp");
            return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
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
