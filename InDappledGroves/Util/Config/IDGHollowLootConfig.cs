using InDappledGroves.Util.WorldGen;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace InDappledGroves.Util.Config
{
    [ProtoBuf.ProtoContract()]
    class IDGHollowLootConfig
    {
        [ProtoMember(1)]
        public List<JToken> treehollowjson { get; set; }

        public IDGHollowLootConfig()
        {

        }

        public static IDGHollowLootConfig Current { get; set; }

        public static IDGHollowLootConfig GetDefault()
        {

            IDGHollowLootConfig defaultConfig = new();
            List<JToken> treehollowjsonsetup = new();
            for (int i = 0; i < TreeHollows.treehollowloot.Count; i++)
            {
                treehollowjsonsetup.Add(JsonObject.FromJson(TreeHollows.treehollowloot[i].Replace("/", "")).Token);
            }
            
            defaultConfig.treehollowjson = treehollowjsonsetup;
            return defaultConfig;
        }

        

        public static void createConfigFile(ICoreServerAPI api)
        {
            api.Logger.Debug("IDGHollowLootConfig has started");
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
                api.Logger.Error("IDG Hollow Loot Config Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                api.StoreModConfig(IDGHollowLootConfig.Current, "indappledgroves/hollowloot.json");
            }
        }
    }
}
