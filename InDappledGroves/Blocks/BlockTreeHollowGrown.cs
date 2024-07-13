namespace InDappledGroves.Blocks
{
    using global::InDappledGroves.BlockEntities;
    using global::InDappledGroves.Util.Config;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;

    public class BlockTreeHollowGrown : Block
        {

        public override string GetHeldItemName(ItemStack stack) => GetName();
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos) => GetName();

        public string GetName()
        {
            var material = Variant["wood"];

            var part = Lang.Get("material-" + $"{material}");
            part = $"{part[0].ToString().ToUpper()}{part.Substring(1)}";
            return string.Format($"{part} {Lang.Get("indappledgroves:block-treehollow")}");
        }

        private WorldInteraction[] interactions;
            public override void OnLoaded(ICoreAPI api)
            {
                if (api.Side != EnumAppSide.Client)
                { return; }
                var capi = api as ICoreClientAPI;
                this.interactions = ObjectCacheUtil.GetOrCreate(api, "treehollowInteractions", () => new WorldInteraction[] {
                new()
                {
                    ActionLangCode = "blockhelp-behavior-rightclickpickup",
                    MouseButton = EnumMouseButton.Right,
                    RequireFreeHand = true
                }
            });
            }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
            {
                var facing = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
                bool placed;
                placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
                if (placed)
                {
                    var block = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
                    var newPath = block.Code.Path;
                    newPath = newPath.Replace("north", facing);
                    block = this.api.World.GetBlock(block.CodeWithPath(newPath));
                    this.api.World.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
                }
                return placed;
            }

            public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
            {
                if (world.BlockAccessor.GetBlockEntity(pos) is BETreeHollowGrown bedc && api.World.Rand.NextDouble() <= IDGTreeConfig.Current.HollowBreakChance)
                    {
                        Block blockToBreak = this;
                        bedc.OnBreak();

                        var newPath = blockToBreak.Code.Domain + ":treehollowplaced-" + blockToBreak.FirstCodePart(2) + "-north";
                        var newBlock = this.api.World.GetBlock(new AssetLocation(newPath)) as BlockTreeHollowPlaced;
                        world.BlockAccessor.SetBlock(newBlock.BlockId, pos);
                        if (world.BlockAccessor.GetBlockEntity(pos) is BETreeHollowPlaced be)
                        {
                            be.Initialize(this.api);
                            be.type = blockToBreak.FirstCodePart(1);
                            be.MarkDirty();
                            world.BlockAccessor.BreakBlock(pos, null, 1);
                        }
                    } else if (api.World.Rand.NextDouble() >= IDGTreeConfig.Current.HollowBreakChance) {
                    if(api.Side == EnumAppSide.Server)
                    api.World.SpawnItemEntity(new ItemStack(api.World.GetItem(new AssetLocation("game:firewood")), 2),pos.ToVec3d(), new Vec3d(0.05f, 0.1f, 0.05f));
                    }
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            }

            public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
            {
                if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BETreeHollowGrown bedc)
                { return bedc.OnInteract(byPlayer); } //, blockSel); }
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
            {
                if (world.BlockAccessor.GetBlockEntity(selection.Position) is BETreeHollowGrown bedc)
                {
                    if (!bedc.Inventory.Empty)
                    {
                        return this.interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                    }
                }
                return null;
            }

        /// <summary>
        /// Allows replacing logs with grown hollows.
        /// </summary>
        /// <param name="blockAccessor"></param>
        /// <param name="pos"></param>
        /// <param name="onBlockFace"></param>
        /// <param name="worldgenRandom"></param>
        /// <returns></returns>
        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldgenRandom)
        {
            Block block = blockAccessor.GetBlock(pos);

            if (block.IsReplacableBy(this) || block.FirstCodePart() == "log")
            {
                if (block.EntityClass != null)
                {
                    blockAccessor.RemoveBlockEntity(pos);
                }

                blockAccessor.SetBlock(BlockId, pos);

                if (EntityClass != null)
                {
                    blockAccessor.SpawnBlockEntity(EntityClass, pos);

                }

                return true;
            }

            return false;
        }
        }
    }