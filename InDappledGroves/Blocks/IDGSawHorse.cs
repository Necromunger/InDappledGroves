using InDappledGroves.BlockEntities;
using InDappledGroves.CollectibleBehaviors;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.Blocks
{
    class IDGSawHorse : Block
    {

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            bool posSawhorse = api.World.BlockAccessor.GetBlockEntity(pos) is not IDGBESawHorse besawHorse;
            bool posneibSawHorse = api.World.BlockAccessor.GetBlockEntity(neibpos) is not IDGBESawHorse neibesawHorse2;
            string side = api.World.BlockAccessor.GetBlock(pos).Variant["side"];
            string neiside = api.World.BlockAccessor.GetBlock(pos).Variant["side"];

            if (posSawhorse && posneibSawHorse) base.OnNeighbourBlockChange(world, pos, neibpos);
            
            if (!posSawhorse && !posneibSawHorse)
            {
                besawHorse = api.World.BlockAccessor.GetBlockEntity(pos) as IDGBESawHorse;
                neibesawHorse2 = api.World.BlockAccessor.GetBlockEntity(neibpos) as IDGBESawHorse;
                if (!besawHorse.isPaired && !neibesawHorse2.isPaired && isNotDiagonal(pos, neibpos)) 
                {

                    besawHorse.CreateSawhorseStation(neibpos, neibesawHorse2);
                    neibesawHorse2.conBlockPos = pos.Copy();
                    neibesawHorse2.pairedBlockPos = pos.Copy();
                    neibesawHorse2.isPaired = true;
                    neibesawHorse2.isConBlock = false;
                    Block neibBlock = api.World.BlockAccessor.GetBlock(api.World.BlockAccessor.GetBlock(pos).CodeWithVariants(new string[] { "side", "state" }, new string[] { getFacing(pos, neibpos, "first"), "compound" }));
                    Block posBlock = api.World.BlockAccessor.GetBlock(api.World.BlockAccessor.GetBlock(pos).CodeWithVariants(new string[] { "side", "state" },new string[] { getFacing(pos, neibpos, "second"), "compound" }));
                    api.World.BlockAccessor.ExchangeBlock(neibBlock.BlockId, neibpos);
                    api.World.BlockAccessor.ExchangeBlock(posBlock.BlockId, pos);
                    besawHorse.MarkDirty(true);
                    neibesawHorse2.MarkDirty(true);
                }
            }
            base.OnNeighbourBlockChange(world, pos, neibpos);
        }

        private bool isNotDiagonal(BlockPos pos, BlockPos neibpos)
        {
            return (pos == neibpos.EastCopy() || pos == neibpos.WestCopy() || pos == neibpos.NorthCopy() || pos == neibpos.SouthCopy());
        }

        private string getFacing(BlockPos pos, BlockPos neibpos, string which)
        {
            if (which == "first") {
                if (pos == neibpos.EastCopy()) return "east";
                if (pos == neibpos.NorthCopy()) return "north";
                if (pos == neibpos.WestCopy()) return "west";
                if (pos == neibpos.SouthCopy()) return "south";
            }
           if (which == "second")
            {
                if (pos == neibpos.EastCopy()) return "west";
                if (pos == neibpos.NorthCopy()) return "south";
                if (pos == neibpos.WestCopy()) return "east";
                if (pos == neibpos.SouthCopy()) return "north";
            }
            return "south";
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is IDGBESawHorse besawhorse)
            {
                //bool isPaired = besawhorse.isPaired;
                //bool isConBlock = besawhorse.isConBlock;

                return besawhorse.OnInteract(byPlayer, blockSel);
            } 
            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            CollectibleObject planeTool = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible;
            IDGBESawHorse besawHorse = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawHorse;
            IDGBESawHorse conBlock = besawHorse.isConBlock ? besawHorse : api.World.BlockAccessor.GetBlockEntity(besawHorse.conBlockPos) as IDGBESawHorse;
            BlockPos pos = blockSel.Position;

            if (planeTool != null && planeTool.HasBehavior<BehaviorWoodPlaner>() && !conBlock.Inventory.Empty)
            {
                if (playNextSound < secondsUsed)
                {
                    api.World.PlaySoundAt(new AssetLocation("sounds/block/chop2"), pos.X, pos.Y, pos.Z, byPlayer, true, 32, 1f);
                    playNextSound += .7f;
                }
                if (secondsUsed >= planeTool.GetBehavior<BehaviorWoodPlaner>().sawHorsePlaneTime)
                {
                    planeTool.GetBehavior<BehaviorWoodPlaner>().SpawnOutput(besawHorse.GetRecipe(), byPlayer.Entity, blockSel.Position);
                    conBlock.Inventory.Clear();
                    (world.BlockAccessor.GetBlockEntity(blockSel.Position) as IDGBESawHorse).updateMeshes();
                    conBlock.MarkDirty(true);
                }
                return !conBlock.Inventory.Empty;
            }
            return false;
        }
        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            playNextSound = 0.7f;
            byPlayer.Entity.StopAnimation("axechop");
        }
        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            IDGBESawHorse besawHorse;
            IDGBESawHorse besawHorse2;
            if (api.World.BlockAccessor.GetBlockEntity(pos) is IDGBESawHorse)
            {
                besawHorse = api.World.BlockAccessor.GetBlockEntity(pos) as IDGBESawHorse;
                if (besawHorse.isPaired)
                {
                    if (api.World.BlockAccessor.GetBlockEntity(besawHorse.pairedBlockPos) is IDGBESawHorse)
                    {
                        besawHorse2 = api.World.BlockAccessor.GetBlockEntity(besawHorse.pairedBlockPos) as IDGBESawHorse;


                        api.World.BlockAccessor.ExchangeBlock(api.World.BlockAccessor.GetBlock(api.World.BlockAccessor.GetBlock(besawHorse2.Pos).CodeWithVariant("state","single")).BlockId, besawHorse2.Pos);
                        besawHorse2.isConBlock = false;
                        besawHorse2.conBlockPos = null;
                        besawHorse2.isPaired = false;
                        besawHorse2.pairedBlockPos = null;
                        
                        besawHorse2.MarkDirty(true);
                        if (!besawHorse2.Inventory[0].Empty)
                        {
                            api.World.SpawnItemEntity(besawHorse.Inventory[0].TakeOutWhole(), pos.ToVec3d(), new Vec3d(0f, 0.5f, 0f));
                        }
                        
                    }
                }
            }
            base.OnBlockRemoved(world, pos);
        }

        public PlaningRecipe GetMatchingSawingRecipe(IWorldAccessor world, ItemSlot slots)
        {
            List<PlaningRecipe> recipes = IDGRecipeRegistry.Loaded.PlaningRecipes;
            if (recipes == null) return null;

            PlaningRecipe stationRecipe = null;
            PlaningRecipe nostationRecipe = null;

            for (int j = 0; j < recipes.Count; j++)
            {
                if (recipes[j].Matches(api.World, slots))
                {
                    if (recipes[j].RequiresStation)
                    {
                        stationRecipe = recipes[j];
                    }
                    else
                    {
                        nostationRecipe = recipes[j];
                    }
                }
            }

            return null;
        }

        private float playNextSound;
    }
    
}