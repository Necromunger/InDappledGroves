using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
namespace InDappledGroves.BlockBehaviors
{
    class BehaviorIDGPickup : BlockBehavior
    {
        AssetLocation pickupdropcode;
        int quantity;
        String type;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            pickupdropcode = new AssetLocation(properties["pickupdrop"].AsString());
            quantity = properties["quantity"].Exists ? properties["quantity"].AsInt() : 1;
            type = properties["type"].AsString();
        }
        //This is a hacky as fuck solution to the firewood block not responding to the onBlockInteractStart method for... reasons? Will correct later.
        public BehaviorIDGPickup(Block block) : base(block)
        {
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {

            if (byPlayer.InventoryManager.ActiveHotbarSlot.Empty && type != null)
            {

                if (type == "block")
                {
                    world.SpawnItemEntity(new ItemStack(world.GetBlock(pickupdropcode), quantity), blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                    world.BlockAccessor.SetBlock(0, blockSel.Position);

                } else if (type == "item")
                {

                    world.SpawnItemEntity(new ItemStack(new Item(world.GetItem(pickupdropcode).ItemId), quantity), blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                    world.BlockAccessor.SetBlock(0, blockSel.Position);

                } 
       
            }
            handling = EnumHandling.Handled;
            return true;
        }
    }
}
