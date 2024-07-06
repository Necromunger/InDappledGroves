using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace InDappledGroves.Util.HarmonyPatches
{
    public static class TreeGenPatch
    {
        public static void Execute(Harmony harmony)
        {
            harmony.Patch(typeof(TreeGen).GetMethod("growBranch", BindingFlags.Instance | BindingFlags.NonPublic),
                transpiler: new HarmonyMethod(typeof(TreeGenPatch).GetMethod("growBranchTranspiler", BindingFlags.Static | BindingFlags.Public))
            );

            harmony.Patch(typeof(TreeGen).GetMethod("GrowTree", BindingFlags.Instance | BindingFlags.Public),
                postfix: new HarmonyMethod(typeof(HarmonyModSystem).GetMethod("growTreePostfix", BindingFlags.Static | BindingFlags.Public))
            );

            //harmony.Patch(typeof(BlockEntitySapling).GetMethod("CheckGrow", BindingFlags.Instance | BindingFlags.NonPublic),
            //    postfix: new HarmonyMethod(typeof(HarmonyModSystem).GetMethod("checkGrowPostfix", BindingFlags.Static | BindingFlags.Public))
            //);

        }

        public static IEnumerable<CodeInstruction> growBranchTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            
            CodeMatcher codeMatcher = new CodeMatcher(instructions);

            // Find where the tree generator places blocks, and jump right behind it
            codeMatcher.MatchEndForward(
                new CodeMatch(instruction => instruction.Calls(typeof(TreeGen).
                GetMethod("PlaceBlockEtc", BindingFlags.NonPublic | BindingFlags.Instance, null, 
                new Type[] { typeof(int), typeof(BlockPos), typeof(Random), typeof(float), typeof(float) }, null)))
            )
            .Advance(1);

            // Add arguments for our method, and then call it
            codeMatcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0), // Put "argument 0" (`this`) on stack, so that the next instruction can use it
                CodeInstruction.LoadField(typeof(TreeGen), "blockAccessor"), // Load the BlockAccessor used by the tree generator itself
                new CodeInstruction(OpCodes.Ldarg_2), // Load 2nd argument of the method we are patching (`int depth`)
                new CodeInstruction(OpCodes.Ldloc_S, 10), // Load method local variable #11 (`int iteration`) 
                new CodeInstruction(OpCodes.Ldloc_S, 22), // Load method local variable #23 (`int blockId`)
                new CodeInstruction(OpCodes.Ldloc_S, 17), // Load method local variable #18 (`BlockPos currentPos`) 
                new CodeInstruction(OpCodes.Ldarg, 13), //Load local argument #14 ('bool WideTrunk')
                CodeInstruction.Call(typeof(HarmonyModSystem), "GrowBranchTranspilerCall")
            );

            // Return the newly patched code
            return codeMatcher.InstructionEnumeration();
        }
    }
}
