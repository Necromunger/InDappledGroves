using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;

using HarmonyLib;
using Vintagestory.ServerMods.NoObf;
using System.Linq;
using Vintagestory.API.Config;

namespace InDappledGroves.Util.HarmonyPatches
{
    public class HarmonyModSystem : ModSystem
    {
        private Harmony harmony;
        private readonly string harmonyId = "teacupangel.vinternacht.transpiler";

        private static ICoreServerAPI sapi;
        private static IBlockAccessor thisBlockAccessor;

        private static Dictionary<BlockPos, Block> TreeBase = new();

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;

            harmony = new Harmony(harmonyId);

            sapi.Logger.Notification("VinterTranspiler: Executing");
            TreeGenPatch.Execute(harmony);
        }

        public static void GrowBranchTranspilerCall(IBlockAccessor blockAccessor, int depth, int iteration, int blockId, BlockPos currentPos, bool wideTrunk)
        {
            if (thisBlockAccessor == null)
            {
                thisBlockAccessor = blockAccessor;
            }
            if (depth == 0 && iteration == 1)
            {
                if (((wideTrunk && TreeBase.Count < 5 || !wideTrunk && TreeBase.Count < 1)))
                {
                    Block block = blockAccessor.GetBlock(blockId);
                    if (block.FirstCodePart() == "log")
                    {
                        TreeBase.Add(currentPos, block);
                    }
                }
            }
        }

        public static void growTreePostfix()
        {
            if (TreeBase.Count != 0)
            {
                foreach (KeyValuePair<BlockPos,Block> entry in TreeBase)
                {
                    string stumpType = entry.Value.FirstCodePart(2);
                    AssetLocation withPath = new AssetLocation("indappledgroves:treestump-grown-" + stumpType + "-" + "east");
                    Block withBlock = thisBlockAccessor.GetBlock(withPath);
                    thisBlockAccessor.SetBlock(0, entry.Key);
                    withBlock.TryPlaceBlockForWorldGen(thisBlockAccessor, entry.Key, BlockFacing.UP, null);
                }
                TreeBase.Clear();
            }
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll();
            harmony = null;

            sapi = null;
            thisBlockAccessor = null;
            System.Diagnostics.Debug.WriteLine("TreeBase Length is " + TreeBase.Count);
            TreeBase.Clear();

        }
    }
}
