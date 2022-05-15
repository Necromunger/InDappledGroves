using InDappledGroves.BlockEntities;
using InDappledGroves.Blocks;
using InDappledGroves.config;
using InDappledGroves.Items.Tools;
using System;
using Vintagestory.API.Common;

namespace InDappledGroves
{
    public class InDappledGroves : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            //Register Items
            api.RegisterItemClass("idgaxe", typeof(ItemIDGAxe));
            api.RegisterItemClass("idgsaw", typeof(ItemIDGSaw));

            //Register Blocks
            api.RegisterBlockClass("idgchoppingblock", typeof(IDGChoppingBlock));

            //Registser BlockEntities
            api.RegisterBlockEntityClass("idgbechoppingblock", typeof(IDGBEChoppingBlock));

            api.RegisterCollectibleBehaviorClass("WoodSplitter", typeof(BehaviorWoodSplitter));

            try
            {
                var Config = api.LoadModConfig<InDappledGrovesConfig>("indappledgroves.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    InDappledGrovesConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    InDappledGrovesConfig.Current = InDappledGrovesConfig.GetDefault();
                }
            }
            catch
            {
                InDappledGrovesConfig.Current = InDappledGrovesConfig.GetDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                //TODO: Finish Implementing Config
                //if (InDappledGrovesConfig.Current. <= 0)
                //    InDappledGrovesConfig.Current.hiveHoursToHarvest = 1488;
                //api.StoreModConfig(InDappledGrovesConfig.Current, "indappledgroves.json");
            }
        }
    }

}
