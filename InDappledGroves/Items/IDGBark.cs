using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace InDappledGroves.Items
{
    class IDGBark : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.World.BlockAccessor?.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage bebgs && MatchSlots(bebgs.Inventory, slot))
            {
                string bundlestate = api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Fluid).FirstCodePart() == "water" ? "-soaking" : "-dry";
                api.World.BlockAccessor.SetBlock(api.World.BlockAccessor.GetBlock(new AssetLocation("indappledgroves:barkbundle-" + slot.Itemstack.Collectible.Variant["bark"] + bundlestate)).BlockId, blockSel.Position);
                handling = EnumHandHandling.Handled;
            } else
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            }
           
        }

        private bool MatchSlots(InventoryBase inv, ItemSlot slot)
        {
            
            if(!inv[0].Empty)
            for(int i=0; i<inv.Count; i++)
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
