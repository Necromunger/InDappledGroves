
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace InDappledGroves.BlockBehaviors
{
    public class BehaviorSubmergible : BlockBehavior
    {

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            this.liquidcode = properties["liquidcode"].ToString();
            this.outputcode = properties["outputcode"].ToString();
        }

        public BehaviorSubmergible(Block block) : base(block)
        {
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode) {
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref handling, ref failureCode);
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
        {
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
