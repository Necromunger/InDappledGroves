
using Vintagestory.API.Common;

namespace InDappledGroves.BlockBehaviors
{
    public class BehaviorSubmergible : BlockBehavior
    {

        public BehaviorSubmergible(Block block) : base(block)
        {
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode) {
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref handling, ref failureCode);
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
        {
            System.Diagnostics.Debug.WriteLine(world.BlockAccessor.GetBlock(blockSel.Position, 0).FirstCodePart());
            if (world.BlockAccessor.GetBlock(blockSel.Position, 0).FirstCodePart() == "water") {
                world.BlockAccessor.SetBlock(world.GetBlock(block.CodeWithVariant("stage", "submerged")).BlockId, blockSel.Position);
                System.Diagnostics.Debug.WriteLine(block.CodeWithVariant("stage", "submerged"));
                handling = EnumHandling.PreventDefault;
                return true;
            }
            return true;
        }
    }
}
