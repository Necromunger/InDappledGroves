using InDappledGroves.Util.WorldGen;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace InDappledGroves.Util.Config
{
    class IDGHollowLootConfig
    {
        //Multiplier applied when trees are being chopped, higher numbers reduces chopping speed of trees, lower numbers reduce time to chop trees.




        public List<JsonObject> treehollowjson { get; set; } = new();

        public IDGHollowLootConfig()
        {

        }

        public static IDGHollowLootConfig Current { get; set; }

        public static IDGHollowLootConfig GetDefault()
        {

            IDGHollowLootConfig defaultConfig = new();
            for (int i = 0; i < TreeHollows.treehollowloot.Count; i++)
            {
                defaultConfig.treehollowjson.Add(JsonObject.FromJson(TreeHollows.treehollowloot[i].Replace("/", "")));
            }

            return defaultConfig;
        }
    }
}
