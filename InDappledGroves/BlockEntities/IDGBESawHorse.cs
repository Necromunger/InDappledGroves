using InDappledGroves.CollectibleBehaviors;
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
        public BlockPos conBlockPos { get; set; }
        public BlockPos pairedBlockPos { get; set; }

        public

        readonly InventoryGeneric inv;
        public override InventoryBase Inventory => inv;

        public override string InventoryClassName => "sawhorse";

        public IDGBESawHorse()
        {
            inv = new InventoryGeneric(2, "sawhorse-slot", null, null);
            meshes = new MeshData[2];
        }

        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            CollectibleObject colObj = slot.Itemstack?.Collectible;
            bool isPlaningBox = blockSel.SelectionBoxIndex == 1;


            if (!isConBlock){
                return (Api.World.BlockAccessor.GetBlockEntity(conBlockPos) as IDGBESawHorse).OnInteract(byPlayer, blockSel);
            }

            //If players hand is empty, try to take item from sawhorse station
            if (slot.Empty)
            {
                if (TryTake(byPlayer))
                {
                    MarkDirty(true);
                    return true;
                }
            }
            //If players hand is not empty, and the item they're holding can be planed, attempt to put
            else if (!slot.Empty)
            {
                if(colObj.Attributes != null && colObj.HasBehavior<BehaviorWoodPlaner>())
                {
                    return true;
                }
                else if (colObj.Attributes != null && colObj.Attributes["idgSawHorseProps"]["planable"].AsBool(false)) {
                    if (TryPut(slot))
                    {
                        this.Api.World.PlaySoundAt(GetSound(slot) ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
                        MarkDirty(true);
                        return true;
                    }
                }
            }
            return true;
        }

        private AssetLocation GetSound(ItemSlot slot) {
            if (slot.Itemstack == null)
            {
                return null;
            }
            else
            {
                Block block = slot.Itemstack.Block;
                if (block == null)
                {
                    return null;
                }
                else
                {
                    BlockSounds sounds = block.Sounds;
                    return (sounds?.Place);
                }
            }
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
                return moved > 0;
            }
            updateMeshes();
            return false;
        }
          public void CreateSawhorseStation(BlockPos placedSawHorse, IDGBESawHorse neiSawHorseBE)
        {
            isPaired = true;
            isConBlock = true;
            conBlockPos = Pos;
            pairedBlockPos = placedSawHorse;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("ispaired", isPaired);
            tree.SetBool("isconblock", isConBlock);
            if (conBlockPos != null)
            {
                tree.SetBlockPos("conblock", conBlockPos);
            } else
            {
                conBlockPos = null;
            }
            if (pairedBlockPos != null)
            {
                tree.SetBlockPos("pairedblock", pairedBlockPos);
            }
            else
            {
                pairedBlockPos = null;
            }
            
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            isPaired = tree.GetBool("ispaired");
            isConBlock = tree.GetBool("isconblock");
            conBlockPos = tree.GetBlockPos("conblock", null);
            pairedBlockPos = tree.GetBlockPos("pairedblock", null);
            
            MarkDirty(true);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            dsc.AppendLine("My Pos is " + Pos);
            dsc.AppendLine("conBlock is " + conBlockPos);
            dsc.AppendLine("pairedBlock is " + pairedBlockPos);
            dsc.AppendLine("isConBlock is " + isConBlock);
            dsc.AppendLine("isPaired is " + isPaired);
            dsc.AppendLine("Contains " + (conBlockPos != null && Api.World.BlockAccessor.GetBlockEntity(conBlockPos) is IDGBESawHorse besawhorse ? besawhorse.inv[1].Empty ? "nothing" : besawhorse.inv[1].Itemstack.ToString() : "nothing"));
            base.GetBlockInfo(forPlayer, dsc);
        }

        public override void updateMeshes()
        {
                this.updateMesh(1);

            base.updateMeshes();
        }

        protected override void updateMesh(int index)
        {
            if (this.Api == null || this.Api.Side == EnumAppSide.Server)
            {
                return;
            }
            if (this.Inventory[index].Empty)
            {
                this.meshes[index] = null;
                return;
            }
            MeshData meshData = this.genMesh(this.Inventory[index].Itemstack);
            this.TranslateMesh(meshData, index);
            this.meshes[index] = meshData;
        }

        public override void TranslateMesh(MeshData mesh, int index)
        {
            float x = 0.5f;
            float y = 0.75f;
            float z = 0f;
            
            Vec4f offset = mat.TransformVector(new Vec4f(x, y, z, 0));
            mesh.Translate(offset.XYZ);
        }
        protected override MeshData genMesh(ItemStack stack)
        {

            IContainedMeshSource containedMeshSource = stack.Collectible as IContainedMeshSource;
            MeshData meshData;
            if (containedMeshSource != null)
            {
                meshData = containedMeshSource.GenMesh(stack, this.capi.BlockTextureAtlas, this.Pos);
                meshData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, base.Block.Shape.rotateY * 0.017453292f, 0f);
            }
            else
            {
                this.nowTesselatingObj = stack.Collectible;
                this.nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                capi.Tesselator.TesselateItem(stack.Item, out meshData, this);
            }
            ModelTransform transform = stack.Collectible.Attributes.AsObject<ModelTransform>();
            transform.EnsureDefaultValues();
            transform.Rotation.X = 0;
            transform.Rotation.Y = Block.Shape.rotateY+45;
            transform.Rotation.Z = 0;
            meshData.ModelTransform(transform);

            return meshData;
        }

        readonly Matrixf mat = new();
    }
}