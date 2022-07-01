using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace InDappledGroves.BlockEntities
{
    class IDGBESawHorse : BlockEntityDisplay
    {
        //public bool isPaired { get; set; }
        public bool isPaired;
        public bool isConBlock { get; set; }
        public BlockPos conBlock { get; set; }
        public BlockPos pairedBlock { get; set; }

        readonly InventoryGeneric inv;
        public override InventoryBase Inventory => inv;

        public override string InventoryClassName => "sawhorse";

        public IDGBESawHorse()
        {
            inv = new InventoryGeneric(2, "sawhorse-slot", null, null);
            meshes = new MeshData[1];
        }

        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            CollectibleObject colObj = slot.Itemstack?.Collectible;
            bool isPlaningBox = blockSel.SelectionBoxIndex == 1;
            //bool isPlanable = (colObj == null || colObj.Attributes == null) ? false : colObj.Attributes["planable"].AsBool();

            if (!isPlaningBox) return false;
            if (slot.Empty)
            {
                if (TryTake(byPlayer))
                {
                    MarkDirty(true);
                    return true;
                }
            }
            else if (!slot.Empty && slot.Itemstack.Collectible.FirstCodePart() == "plank")
            {
                if (TryPut(slot))
                {
                    MarkDirty(true);
                    return true;
                }
            }
            else if (slot.Itemstack.Collectible.FirstCodePart() == "axe")
            {
                if (!inv[1].Empty)
                {
                    ItemStack stack = inv[1].TakeOutWhole();
                    stack.StackSize = 2;
                    
                }
            }
            return false;
        }

        private bool TryTake(IPlayer byPlayer)
        {
            if (!inv[1].Empty)
            {
                ItemStack stack = inv[1].TakeOutWhole();
                if (byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    AssetLocation sound = stack.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                }
                if (stack.StackSize > 0)
                {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
                updateMeshes();
                return true;
            }

            return false;
        }

        private bool TryPut(ItemSlot slot)
        {
            if (inv[1].Empty)
            {
                int moved = slot.TryPutInto(Api.World, inv[1]);
                updateMeshes();
                return moved > 0;
            }
            return false;
        }

        public void CreateSawhorseStation(BlockPos placedSawHorse)
        {
            isPaired = true;
            isConBlock = true;
            conBlock = Pos;
            pairedBlock = placedSawHorse;           
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("ispaired", isPaired);
            tree.SetBool("isconblock", isConBlock);
            if (conBlock != null)
            {
                tree.SetBlockPos("conblock", conBlock);
            } else
            {
                conBlock = null;
            }
            if (pairedBlock != null)
            {
                tree.SetBlockPos("pairedblock", pairedBlock);
            }
            else
            {
                pairedBlock = null;
            }
            
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            isPaired = tree.GetBool("ispaired");
            isConBlock = tree.GetBool("isconblock");
            conBlock = tree.GetBlockPos("conblock", null);
            pairedBlock = tree.GetBlockPos("pairedblock", null);
            MarkDirty(true);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            dsc.AppendLine("My Pos is " + Pos);
            dsc.AppendLine("conBlock is " + conBlock);
            dsc.AppendLine("pairedBlock is " + pairedBlock);
            dsc.AppendLine("isConBlock is " + isConBlock);
            dsc.AppendLine("isPaired is " + isPaired);
            dsc.AppendLine("Contains " + (conBlock != null && Api.World.BlockAccessor.GetBlockEntity(conBlock) is IDGBESawHorse fish ? fish.inv[1].Empty ? "nothing" : fish.inv[1].Itemstack.ToString() : "nothing"));
            base.GetBlockInfo(forPlayer, dsc);
        }
    }
}