using InDappledGroves.BlockEntities;
using InDappledGroves.Blocks;
using InDappledGroves.Util;
using InDappledGroves.Items.Tools;
using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Items;
using InDappledGroves.BlockBehaviors;
using Vintagestory.API.Common;
using System.Collections.Generic;
using System;
using HarmonyLib;

namespace InDappledGroves
{
    
    public class InDappledGroves : ModSystem
    {
        public static float baseWorkstationMiningSpdMult;
        public static float baseWorkstationResistanceMult;
        public static float baseGroundRecipeMiningSpdMult;
        public static float baseGroundRecipeResistaceMult;

        public override void Start(ICoreAPI api)
        {

            base.Start(api);
            //Register Items
            api.RegisterItemClass("idgfirewood", typeof(IDGFirewood));
            api.RegisterItemClass("idgplank", typeof(IDGPlank));
            api.RegisterItemClass("idgtool", typeof(IDGTool));
            api.RegisterItemClass("idgbark", typeof(IDGBark));

            //Register Blocks
            api.RegisterBlockClass("idgchoppingblock", typeof(IDGChoppingBlock));
            api.RegisterBlockClass("idgbarkbundle", typeof(IDGBarkBundle));
            api.RegisterBlockClass("idglogslab", typeof(IDGLogSlab));
            api.RegisterBlockClass("idgsawbuck", typeof(IDGSawBuck));
            api.RegisterBlockClass("idgsawhorse", typeof(IDGSawHorse));
            api.RegisterBlockClass("idgbarkbasket", typeof(IDGBarkBasket));
            api.RegisterBlockClass("idgboardblock", typeof(IDGBoardBlock));
            api.RegisterBlockClass("idgblockfirewood", typeof(IDGBlockFirewood));
            api.RegisterBlockClass("blocktreehollowgrown", typeof(BlockTreeHollowGrown));
            api.RegisterBlockClass("blocktreehollowplaced", typeof(BlockTreeHollowPlaced));

            //Register BlockEntities
            api.RegisterBlockEntityClass("idgbechoppingblock", typeof(IDGBEChoppingBlock));
            api.RegisterBlockEntityClass("idgbesawbuck", typeof(IDGBESawBuck));
            api.RegisterBlockEntityClass("idgbesawhorse", typeof(IDGBESawHorse));
            api.RegisterBlockEntityClass("betreehollowgrown", typeof(BETreeHollowGrown));
            api.RegisterBlockEntityClass("betreehollowplaced", typeof(BETreeHollowPlaced));

            //Register CollectibleBehaviors
            api.RegisterCollectibleBehaviorClass("woodsplitter", typeof(BehaviorWoodChopping));
            api.RegisterCollectibleBehaviorClass("woodsawer", typeof(BehaviorWoodSawing));
            api.RegisterCollectibleBehaviorClass("woodplaner", typeof(BehaviorWoodPlaning));
            api.RegisterCollectibleBehaviorClass("woodhewer", typeof(BehaviorWoodHewing));


            //Register BlockBehaviors
            api.RegisterBlockBehaviorClass("Submergible", typeof(BehaviorSubmergible));
            api.RegisterBlockBehaviorClass("IDGPickup", typeof(BehaviorIDGPickup));



            //Tool/Workstation Config
            //Check for Existing Config file, create one if none exists
            try
            {
                var Config = api.LoadModConfig<IDGToolConfig>("indappledgroves/toolconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    IDGToolConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    IDGToolConfig.Current = IDGToolConfig.GetDefault();
                }
            }
            catch
            {
                IDGToolConfig.Current = IDGToolConfig.GetDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                api.StoreModConfig(IDGToolConfig.Current, "indappledgroves/toolconfig.json");
            }

            baseWorkstationMiningSpdMult = IDGToolConfig.Current.baseWorkstationMiningSpdMult;
            baseWorkstationResistanceMult = IDGToolConfig.Current.baseWorkstationResistanceMult;
            baseGroundRecipeMiningSpdMult = IDGToolConfig.Current.baseGroundRecipeMiningSpdMult;
            baseGroundRecipeResistaceMult = IDGToolConfig.Current.baseGroundRecipeResistaceMult;

            //Tree Config
            try
            {
                var Config = api.LoadModConfig<IDGTreeConfig>("indappledgroves/treeconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    IDGTreeConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    IDGTreeConfig.Current = IDGTreeConfig.GetDefault();
                }
            }
            catch
            {
                IDGTreeConfig.Current = IDGTreeConfig.GetDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                api.StoreModConfig(IDGTreeConfig.Current, "indappledgroves/treeconfig.json");
            }

            //TreeHollowLoot Config
            try
            {
                var Config = api.LoadModConfig<IDGHollowLootConfig>("indappledgroves/hollowloot.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    IDGHollowLootConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    IDGHollowLootConfig.Current = IDGHollowLootConfig.GetDefault();
                }
            }
            catch
            {
                IDGHollowLootConfig.Current = IDGHollowLootConfig.GetDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                api.StoreModConfig(IDGHollowLootConfig.Current, "indappledgroves/hollowloot.json");
            }
        }
    }
}
