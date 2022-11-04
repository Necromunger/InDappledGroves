using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace InDappledGroves.Items
{
    class IDGBark : Item
    {
        /// <summary>Called when the player right clicks while holding this block/item in his hands</summary>
        /// <param name="slot">Players activehotbar slot</param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="firstEvent">
        /// True when the player pressed the right mouse button on this block. Every subsequent call, while the player holds right mouse down will be false, it gets called every second while right mouse is down
        /// </param>
        /// <param name="handling">Whether or not to do any subsequent actions. If not set or set to NotHandled, the action will not called on the server.</param>
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.World.BlockAccessor?.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage bebgs && MatchSlots(bebgs.Inventory, slot))
            {
                //Deteremine if the block is being placed in water.  This is a temporary patch solution until BehaviorSubmergible gets fully processed.
                string bundlestate = api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Fluid).FirstCodePart() == "water" ? "-soaking" : "-dry";
                //Set the resultant block into the world.
                api.World.BlockAccessor.SetBlock(api.World.BlockAccessor.GetBlock(new AssetLocation("indappledgroves:barkbundle-" + slot.Itemstack.Collectible.Variant["bark"] + bundlestate)).BlockId, blockSel.Position);
                handling = EnumHandHandling.Handled;
            } else
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            }
           
        }

        /// <summary>Matches the slots.</summary>
        /// <param name="inv">The inventory of the target GroundStorage</param>
        /// <param name="slot">The players activehotbarslot</param>
        /// <returns>Returns true if all four groundstorage slots contain the same bark as the player is holding.</returns>
        private bool MatchSlots(InventoryBase inv, ItemSlot slot)
        {
            
            if(!inv[0].Empty)
                for (int i = 0; i < inv.Count; i++)
            {
                    if (inv[0].Empty && !slot.Empty) return false;
                    string barktype = inv[0].Itemstack.Collectible.Variant["bark"];
                    for(int j=0; j < inv.Count; j++)
                    {
                        if (inv[j].Empty || !(inv[j].Itemstack.Collectible.Variant["bark"] == barktype) || !(inv[j].Itemstack.Collectible.Variant["state"] == "dry")) return false;
                    }
                    return true;
            }
            return false;
        }
    }
}
