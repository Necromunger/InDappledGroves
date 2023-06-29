using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;

using HarmonyLib;
using Vintagestory.ServerMods.NoObf;

namespace InDappledGroves.Util.HarmonyPatches
{
    public class HarmonyModSystem : ModSystem
    {
        private Harmony harmony;
        private readonly string harmonyId = "teacupangel.vinternacht.transpiler";

        private static ICoreServerAPI sapi;
        private static IWorldGenBlockAccessor blockAccessor;

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
            
            if (depth == 0)
            {
                //if (rootBlockId == -1) GetBlockIds();
                System.Diagnostics.Debug.WriteLine("wideTrunk is " + wideTrunk);
                if (wideTrunk?iteration<=4:iteration == 1)
                {
                    Block block = blockAccessor.GetBlock(blockId);

                    // Have to check if it's wood, because once it gets thin enough the game starts placing leaves instead of wood blocks
                    // and we don't want to replace those
                    if (block.BlockMaterial == EnumBlockMaterial.Wood)
                    {
                        TreeBase.Add(currentPos, block);
                        System.Diagnostics.Debug.WriteLine("TreeBase Length is " + TreeBase.Count + "At Iteration " + iteration);
                        //blockAccessor.SetBlock(rootBlockId, currentPos);
                    }
                }
            }
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll();
            harmony = null;

            sapi = null;
            blockAccessor = null;
            System.Diagnostics.Debug.WriteLine("TreeBase Length is " + TreeBase.Count);
            TreeBase.Clear();

            //rootBlockId = -1;
            //trunkBlockId = -1;
        }
    }
}
