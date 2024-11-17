using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace InDappledGroves.Blocks
{
    class IDGWorkstation : Block
    {

        /*TODO: Implement InUseCheck.  If UserUID is not "workstationfree", then 
         * UserUID gets set on BlockEntity when user reaches OnHeldInteractStep method
         * if UserUID == "workstationfree". After that step the UserUID is checked against the
         * interacting characters UID. If the UIDs match, the process is allowed to continue. If not,
         * the interacting users attempt returns false. (Will this affect the original user? Testing will tell)
         * Upon HeldInteractCancel or HeldInteractStop, the UserUID get cleared back to "workstationfree."
         * This should nerf the "Power of Friendship" as Stew calls it.
        */

        

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            IDGBEWorkstation beworkstation = world.BlockAccessor.GetBlockEntity(byPlayer.CurrentBlockSelection.Position) as IDGBEWorkstation;
            if (beworkstation == null)
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
            IDGBEWorkstation beworkstation = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEWorkstation;

            if (beworkstation != null && (heldCollectible == null || !heldCollectible.HasBehavior<BehaviorIDGTool>()))
            {
                bool oninteractresult = beworkstation.OnInteract(byPlayer);
                beworkstation.updateMeshes();
                beworkstation.MarkDirty(true);
                return oninteractresult;
            }

            if (!beworkstation.InputSlot.Empty && heldCollectible != null && heldCollectible.HasBehavior<BehaviorIDGTool>())
            {

                return beworkstation.handleRecipe(heldCollectible, secondsUsed, world, byPlayer, blockSel);
            }

            return false;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            resistance = 0;
            lastSecondsUsed = 0;
            curDmgFromMiningSpeed = 0;
            playNextSound = 0.7f;
            IDGBEWorkstation beworkstation = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEWorkstation;
            beworkstation.MarkDirty(true);
            beworkstation.updateMeshes();
            beworkstation.recipeHandler.recipeProgress = 0;
            if (beworkstation.recipeHandler.recipe != null)
            {
                byPlayer.Entity.StopAnimation(beworkstation.recipeHandler.recipe.Animation);
            }
            
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            resistance = 0;
            lastSecondsUsed = 0;
            curDmgFromMiningSpeed = 0;
            playNextSound = 0.7f;
            IDGBEWorkstation beworkstation = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBEWorkstation;
            beworkstation.MarkDirty(true);
            beworkstation.updateMeshes();
            if(beworkstation.recipeHandler.recipe != null) {
                byPlayer.Entity.StopAnimation(beworkstation.recipeHandler.recipe.Animation);
            }
            
            //byPlayer.Entity.StopAnimation(beworkstation.recipeHandler.recipe.Animation);
            return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
        }

        public override string GetHeldItemName(ItemStack stack)
        {
            base.GetHeldItemName(stack);
            string primary = Variant["primary"];
            string secondary = Variant["secondary"];
            string materials = Lang.Get("material-" + $"{primary}") + (secondary != null ? " and " + Lang.Get("material-" + $"{secondary}") : "");
            string blockid = this.Code.Domain + ":block-" + this.FirstCodePart();
            return string.Format($"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(materials.ToLower())} " + Lang.GetMatching(blockid));
            
        }

        private float playNextSound;
        private float resistance;
        private float lastSecondsUsed;
        private float curDmgFromMiningSpeed;
    }

}
