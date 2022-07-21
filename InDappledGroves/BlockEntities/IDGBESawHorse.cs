using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.BlockEntities
{
    class IDGBESawHorse : BlockEntityDisplay
    {
        //public bool isPaired { get; set; }
        public bool isPaired;
        public bool isConBlock { get; set; }
        public BlockPos conBlockPos { get; set; }
        public BlockPos pairedBlockPos { get; set; }

        PlaningRecipe recipe; 

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


            if (Block.Variant["state"] == "compound" && !isConBlock){
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
            else if (!slot.Empty && this.Inventory[1].Empty)
            {
                if (colObj.Attributes != null && colObj.Attributes["woodworkingProps"]["idgSawHorseProps"]["planable"].AsBool(false)) {
                    if (TryPut(slot))
                    {
                        this.Api.World.PlaySoundAt(GetSound(slot) ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
                        MarkDirty(true);
                        return true;
                    }
                }
            }
            else if (colObj != null && colObj.HasBehavior<BehaviorWoodPlaner>() && !this.Inventory.Empty)
            {
                recipe = GetMatchingPlaningRecipe(Api.World, this.Inventory[1]);
                System.Diagnostics.Debug.WriteLine(this.Inventory[1].Itemstack);
                if (recipe != null)
                {
                    if (slot.Itemstack.Attributes.GetInt("durability") < colObj.GetBehavior<BehaviorWoodPlaner>().sawHorsePlaneDamage && InDappledGrovesConfig.Current.preventToolUseWithLowDurability)
                    {
                        (Api.World as ICoreClientAPI).TriggerIngameError(this, "toolittledurability", Lang.Get("indappledgroves:toolittledurability", colObj.GetBehavior<BehaviorWoodPlaner>().sawHorsePlaneDamage));
                        return false;
                    }
                    else
                    {
                        byPlayer.Entity.StartAnimation("axechop");
                        return true;
                    }
                }
                return false;
            }
            return true;
        }

        public PlaningRecipe GetRecipe()
        {
            if (!this.isConBlock)
            {
                if (Api.World.BlockAccessor.GetBlockEntity(conBlockPos) is IDGBESawHorse conHorse) return conHorse.recipe;
            }
            return recipe;
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

        #region rendering
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
            JsonObject North = this.Inventory[1].Itemstack.Collectible?.Attributes["woodworkingProps"]["idgSawHorseProps"]["idgSawHorseTranslate"]["north"];
            JsonObject South = this.Inventory[1].Itemstack.Collectible?.Attributes["woodworkingProps"]["idgSawHorseProps"]["idgSawHorseTranslate"]["south"];
            JsonObject West = this.Inventory[1].Itemstack.Collectible?.Attributes["woodworkingProps"]["idgSawHorseProps"]["idgSawHorseTranslate"]["west"];
            JsonObject East = this.Inventory[1].Itemstack.Collectible?.Attributes["woodworkingProps"]["idgSawHorseProps"]["idgSawHorseTranslate"]["east"];

            float x = 0.5f;
            float y = 0.75f;
            float z = 0f;

            if (Block.Variant["side"] == "north")
            {
                x = North["x"].Exists ? North["x"].AsFloat() : x;
                y = North["y"].Exists ? North["y"].AsFloat() : y;
                z = North["z"].Exists ? North["z"].AsFloat() : z;

                Vec4f offset = mat.TransformVector(new Vec4f(x, y, z, 0));
                mesh.Translate(offset.XYZ);
            }
            else if (Block.Variant["side"] == "south")
            {
                x = South["x"].Exists ? South["x"].AsFloat() : x;
                y = South["y"].Exists ? South["y"].AsFloat() : y;
                z = South["z"].Exists ? South["z"].AsFloat() : z;

                Vec4f offset = mat.TransformVector(new Vec4f(x, y, z, 0));
                mesh.Translate(offset.XYZ);
            }
            else if (Block.Variant["side"] == "west")
            {
                x = West["x"].Exists ? West["x"].AsFloat() : x;
                y = West["y"].Exists ? West["y"].AsFloat() : y;
                z = West["z"].Exists ? West["z"].AsFloat() : z;


                Vec4f offset = mat.TransformVector(new Vec4f(x, y, z, 0));
                mesh.Translate(offset.XYZ);
            }
            else if (Block.Variant["side"] == "east")
            {
                x = East["x"].Exists ? East["x"].AsFloat() : x;
                y = East["y"].Exists ? East["y"].AsFloat() : y;
                z = East["z"].Exists ? East["z"].AsFloat() : z;

                Vec4f offset = mat.TransformVector(new Vec4f(x, y, z, 0));
                mesh.Translate(offset.XYZ);
            }

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

        public float addRotate(string sideAxis)
        {
            JsonObject transforms = this.Inventory[0].Itemstack.Collectible.Attributes["woodworkingProps"]["idgSawHorseTransform"];
            return transforms["rotation"][sideAxis].Exists ? transforms["rotation"][sideAxis].AsFloat() : 0f;
        }

        readonly Matrixf mat = new();
        #endregion

        public PlaningRecipe GetMatchingPlaningRecipe(IWorldAccessor world, ItemSlot slots)
        {
            List<PlaningRecipe> recipes = IDGRecipeRegistry.Loaded.PlaningRecipes;
            if (recipes == null) return null;

            for (int j = 0; j < recipes.Count; j++)
            {
                if (recipes[j].Matches(Api.World, slots))
                {
                    return recipes[j];
                }
            }

            return null;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (Block.Variant["state"] == "compound")
            {
                dsc.AppendLine("My Pos is " + Pos);
                dsc.AppendLine("conBlock is " + conBlockPos);
                dsc.AppendLine("pairedBlock is " + pairedBlockPos);
                dsc.AppendLine("isConBlock is " + isConBlock);
                dsc.AppendLine("isPaired is " + isPaired);
                dsc.AppendLine("Contains " + (conBlockPos != null && Api.World.BlockAccessor.GetBlockEntity(conBlockPos) is IDGBESawHorse besawhorse ? besawhorse.inv[1].Empty ? "nothing" : besawhorse.inv[1].Itemstack.ToString() : "nothing"));
            }
 
        }
    }
}