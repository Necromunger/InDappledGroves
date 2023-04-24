using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace InDappledGroves.Blocks
{
    class IDGBarkBundle : Block
    {
        public override string GetHeldItemName(ItemStack stack) => GetName();
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos) => GetName();

        public string GetName()
        {
            var material = Variant["bark"];
            var state = Variant["stage"];
            var part1 = Lang.Get($"{material}");
            var part2 = Lang.Get($"{state}");
            part1 = $"{part1[0].ToString().ToUpper()}{part1.Substring(1)}";
            part2 = $"{part2[0].ToString().ToUpper()}{part2.Substring(1)}";
            return string.Format($"{part2} {part1} {Lang.Get("indappledgroves:block-barkbundle")}");
        }
    }
}
