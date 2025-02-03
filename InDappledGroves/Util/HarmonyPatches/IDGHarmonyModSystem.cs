using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using HarmonyLib;
using System.Text;
using System;
using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using System.Reflection;
using Vintagestory.ServerMods;


namespace InDappledGroves.Util.HarmonyPatches
{
    public class IDGHarmonyModSystem : ModSystem
    {
        private Harmony harmony;
        private readonly string harmonyId = "vinternacht.IDGPatches";

        private static ICoreServerAPI sapi;
        private static IWorldGenBlockAccessor thisBlockAccessor;

        public override void Start(ICoreAPI api)
        {
            //PatchGame();
            base.Start(api);
        }
        //private void PatchGame()
        //{
        //    harmony = new Harmony(harmonyId);
        //    harmony.Patch(typeof(ItemTreeSeed).GetMethod("OnHeldUseStart", BindingFlags.Instance | BindingFlags.Public),
        //        postfix: new HarmonyMethod(typeof(IDGHarmonyModSystem).GetMethod("onHeldUseStartPrefix", BindingFlags.Static | BindingFlags.Public))
        //    );
        //    harmony.PatchAll();
        //}


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ItemTreeSeed), "OnHeldUseStart")]
        //public static void onHeldUseStartPrefix(ItemTreeSeed __instance, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)

        //{
        //    bool foundSapling = false;
        //    byEntity.Api.World.BlockAccessor.WalkBlocks(blockSel.Position.AddCopy(-1, -1, -1), blockSel.Position.AddCopy(1, 1, 1), delegate (Block block, int x, int y, int z)
        //    {
        //        if (block.Code.FirstCodePart() == "sapling")
        //        {
        //            foundSapling = true;

        //        }
        //    });

        //    if (foundSapling)
        //    {
        //        handling = EnumHandHandling.NotHandled;
        //    };
        //}

  
        //public override void Dispose()
        //{
        //    harmony?.UnpatchAll();
        //    harmony = null;
        //    sapi = null;
        //    thisBlockAccessor = null;

        //}
    }
}
