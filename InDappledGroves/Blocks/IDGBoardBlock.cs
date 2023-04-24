using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace InDappledGroves.Blocks
{
    class IDGBoardBlock : Block
    {
        public override string GetHeldItemName(ItemStack stack) => GetName();
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos) => GetName();

        public string GetName()
        {
            var material = Variant["wood"];

            var part = Lang.Get($"{material}");
            part = $"{part[0].ToString().ToUpper()}{part.Substring(1)}";
            return string.Format($"{part} {Lang.Get("indappledgroves:block-board")}");
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (this.Variant["type"] == "smooth")
            {
                api.World.SpawnItemEntity(new ItemStack(api.World.GetItem(new AssetLocation("game:plank-" + this.Variant["wood"]))), pos.ToVec3d(), new Vec3d(0.1f, 0.1f, 0.1f));

            }
            else
            {
                api.World.SpawnItemEntity(new ItemStack(api.World.GetItem(new AssetLocation("indappledgroves:plank-" + this.Variant["wood"] + "-" + this.Variant["type"] + "-" + this.Variant["state"]))), pos.ToVec3d(), new Vec3d(0.1f, 0.1f, 0.1f));
            }
            api.World.BlockAccessor.SetBlock(0, pos);
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            
            base.OnBlockRemoved(world, pos);
        }
    }
}
