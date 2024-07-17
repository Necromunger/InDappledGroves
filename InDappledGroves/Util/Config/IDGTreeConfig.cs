using ProtoBuf;
using System;
using System.Collections.Generic;
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
        [ProtoMember(2)]
        public int TreeHollowsMaxItems { get; set; }
        [ProtoMember(3)]
        public float TreeHollowsSpawnProbability { get; set; }

        [ProtoMember(4)]
        public double HollowBreakChance { get; set; }
        [ProtoMember(5)]
        public bool DisableIDGHollowsWithPrimitiveSurvivalInstalled { get; set; }

        [ProtoMember(6)]
        public bool RunTreeGenOnChunkReload { get; set; }
        [ProtoMember(7)]
        public string[] stumpTypes { get; set; }
        [ProtoMember(8)]
        public string[] woodTypes { get; set; }
        
        public IDGTreeConfig()
        { }

        public static IDGTreeConfig Current { get; set; }

        public static IDGTreeConfig GetDefault()
        {
            IDGTreeConfig defaultConfig = new();

            defaultConfig.TreeFellingMultiplier = 1;
            defaultConfig.HollowBreakChance = 0.2f;
            defaultConfig.TreeHollowsMaxItems = 8;
            defaultConfig.TreeHollowsSpawnProbability = 0.2f;
            defaultConfig.RunTreeGenOnChunkReload = false;
            defaultConfig.DisableIDGHollowsWithPrimitiveSurvivalInstalled = true;
            defaultConfig.stumpTypes = new[] { "acacia", "baldcypress", "birch", "ebony", "kapok", "larch", "maple", "oak", "pine", "purpleheart",
                "walnut", "douglasfir", "willow", "honeylocust", "bearnut", "blackpoplar", "poplar", "pyramidalpoplar", "catalpa",
                "mahogany", "sal", "saxaul", "spruce", "sycamore", "elm", "beech", "eucalyptus", "cedar", "searsialancea", "afrocarpusfalcatus",
                "lysilomalatisiliquum", "eucalyptuscamaldulensis", "corymbiaaparrerinja", "araucariaheterophylla","dacrydiumcupressinum",
                "nothofagusmenziesii", "podocarpustotara", "empresstree", "bluemahoe", "redwood", "yew", "dalbergia", "tuja", "kauri"};
            defaultConfig.woodTypes = new[] { "acacia", "baldcypress", "birch", "ebony", "kapok", "larch", "maple", "oak", "pine", "purpleheart",
                "walnut", "douglasfir", "willow", "honeylocust", "bearnut", "blackpoplar", "poplar", "pyramidalpoplar", "catalpa",
                "mahogany", "sal", "saxaul", "spruce", "sycamore", "elm", "beech", "eucalyptus", "cedar", "searsialancea", "afrocarpusfalcatus",
                "lysilomalatisiliquum", "eucalyptuscamaldulensis", "corymbiaaparrerinja", "araucariaheterophylla","dacrydiumcupressinum",
                "nothofagusmenziesii", "podocarpustotara", "empresstree", "bluemahoe", "redwood", "yew", "dalbergia", "tuja", "kauri"};

            return defaultConfig;
        }

        public static void createConfigFile(ICoreAPI api)
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
