using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace InDappledGroves.Items
{
    class IDGBark : Item
    {
        public override string GetHeldItemName(ItemStack stack) => GetName();

        public string GetName()
        {
            var material = Variant["bark"];
            var state = Variant["state"];
            var part = Lang.Get($"material-{material}");
            var part2 = Lang.Get($"{state}");
            part = $"{part[0].ToString().ToUpper()}{part.Substring(1)}";
            part2 = $"{part2[0].ToString().ToUpper()}{part2.Substring(1)}";
            return $"{part2} {part} {Lang.Get("indappledgroves:item-bark")}";
        }


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
            BlockPos pos = blockSel?.Position;

            if (pos != null && api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityGroundStorage bebgs)
            {
                if (MatchSlots(bebgs.Inventory, slot))
                {
                    // Only modify the world serverside to avoid desyncs.
                    if (api.World is Vintagestory.API.Server.IServerWorldAccessor)
                    {
                        //Deteremine if the block is being placed in water.  This is a temporary patch solution until BehaviorSubmergible gets fully processed.
                        string bundlestate = api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Fluid).FirstCodePart() == "water" ? "-soaking" : "-dry";
                        //Set the resultant block into the world.
                        api.World.BlockAccessor.SetBlock(api.World.BlockAccessor.GetBlock(new AssetLocation("indappledgroves:barkbundle-" + slot.Itemstack.Collectible.Variant["bark"] + bundlestate)).BlockId, blockSel.Position);
                    }
                    //Consume the last piece of bark on both client and server.
                    slot.TakeOut(1);
                    slot.MarkDirty();
                    handling = EnumHandHandling.Handled;
                }
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
            if (slot.Empty || slot.Itemstack.Collectible.Variant["state"] != "dry")
            {
                return false;
            }
            if (inv[0].Empty || inv[0].Itemstack.Collectible.Variant["state"] != "dry")
            {
                return false;
            }
            string firstbarktype = inv[0].Itemstack.Collectible.Variant["bark"];
            if (slot.Itemstack.Collectible.Variant["bark"] != firstbarktype)
            {
                return false;
            }
            for (int i = 1; i < inv.Count; i++)
            {
                if (inv[i].Empty || inv[i].Itemstack.Collectible.Variant["bark"] != firstbarktype || inv[i].Itemstack.Collectible.Variant["state"] != "dry")
                {
                    return false;
                }
            }
            return true;
        }
    }
}
