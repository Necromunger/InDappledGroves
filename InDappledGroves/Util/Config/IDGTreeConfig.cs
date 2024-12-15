using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace InDappledGroves.Util.Config
{
    [ProtoBuf.ProtoContract()]
    class IDGTreeConfig
    {

        //Multiplier applied when trees are being chopped, higher numbers reduces chopping speed of trees, lower numbers reduce time to chop trees.
        //Default is 1
        [ProtoMember(1)]
        public float TreeFellingMultiplier { get; set; }
        //Rate at which Tree Hollows Update
      
        public IDGTreeConfig()
        { }

        public static IDGTreeConfig Current { get; set; }

        public static IDGTreeConfig GetDefault()
        {
            IDGTreeConfig defaultConfig = new()
            {
                TreeFellingMultiplier = 1
            };
            return defaultConfig;
        }

        public static void CreateConfigFile(ICoreAPI api)
        {
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
                api.Logger.Error("IDG Tree Config Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                api.StoreModConfig(IDGTreeConfig.Current, "indappledgroves/treeconfig.json");
            }
        }
    }
}
