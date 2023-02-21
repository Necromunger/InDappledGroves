using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace InDappledGroves.Blocks
{
    class IDGBlockFirewood : Block
    {
    //    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    //    {
    //        //TODO: Find out why this block isn't actually responding to onBlockInteractStart.
    //        if (byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
    //        {
    //            api.World.BlockAccessor.SetBlock(0, blockSel.Position);
    //            api.World.SpawnItemEntity(new ItemStack(new Item(api.World.GetItem(new AssetLocation("firewood")).ItemId)), blockSel.Position.ToVec3d());
    //        }
    //        return true;
    //    }
    }
}
