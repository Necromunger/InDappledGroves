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
            if (blockSel == null) return;
            if (blockSel.Block is BlockGroundStorage bgs && byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage bebgs && MatchSlots(bebgs.Inventory, slot.Itemstack.Collectible))
            {
                System.Diagnostics.Debug.WriteLine("Success");
                return;
            }
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        private bool MatchSlots(InventoryBase inv, CollectibleObject slot)
        {
            for(int i=0; i<inv.Count; i++)
            {
                
                if (inv[i].Empty || !(inv[i].Itemstack.Collectible == (inv[0].Itemstack.Collectible))) break;
                if (i == 3) return true;
            }
            return false;
        }
    }
}
