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
            api.RegisterItemClass("itemidgaxe", typeof(ItemIDGAxe));
            api.RegisterItemClass("itemidgsaw", typeof(ItemIDGSaw));

            try
            {
                var Config = api.LoadModConfig<InDappledGrovesConfig>("fieldsofgold.json");
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
                //api.StoreModConfig(InDappledGrovesConfig.Current, "fieldsofgold.json");
            }
        }
    }

    }
}
