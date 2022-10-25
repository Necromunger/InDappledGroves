
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace InDappledGroves.BlockBehaviors
{
    public class BehaviorSubmergible : BlockBehavior
    {
        //Behavior Submergible is a method for having a block check
        //if it is being placed in a certain type of liquid block in the world
        //If the liquidcode parameter is met, the output code block is placed.
        //This is intended to be used on a block that uses the transient entityClass,
        //As that will govern what the outputcode block changes into after being soaked.

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            //Get the codes for liquid it must be submerged in. By default, water.
            this.liquidcode = properties["liquidcode"].ToString();
            //Get the codes for the block that is placed if it's in water.
            this.outputcode = properties["outputcode"].ToString();
        }

        public BehaviorSubmergible(Block block) : base(block)
        {
        }

        
        /// <summary>Step 3: Place the block. Return false if it cannot be placed (but you should rather return false in CanPlaceBlock).</summary>
        /// <param name="world">The World being accessed</param>
        /// <param name="byPlayer">The Player Placing The Block</param>
        /// <param name="blockSel">The Selected Block</param>
        /// <param name="byItemStack">The Itemstack Held By The Player</param>
        /// <param name="handling">How to handle the process/if it's been handled</param>
        /// <returns>Whether to allow placing the block</returns>
        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
        {
            //If the block can be placed, check to see if it fits the liquidcode requirements. If it does, place the output
            Block outputblock = byPlayer.Entity.World.BlockAccessor.GetBlock(new AssetLocation(outputcode));
            if ((world.BlockAccessor.GetBlock(blockSel.Position, 0).FirstCodePart() == liquidcode)) {
                world.BlockAccessor.SetBlock(outputblock.BlockId, blockSel.Position);
                handling = EnumHandling.PreventDefault;
                return true;
            }
            return true;
        }
        //Code for block 
        private string liquidcode;
        private string outputcode;
    }
}
