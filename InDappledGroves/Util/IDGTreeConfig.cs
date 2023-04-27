using InDappledGroves.Items.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace InDappledGroves.Util
{
    class IDGTreeConfig
    {
        //Multiplier applied when trees are being chopped, higher numbers reduces chopping speed of trees, lower numbers reduce time to chop trees.
        public float treeFellingDivisor { get; set; }
        //Rate at which Tree Hollows Update
        public int TreeHollowsMaxItems { get; set; }

        public int TreeHollowsMaxPerChunk { get; set; }
        public float TreeHollowsSpawnProbability { get; set; }
        public double TreeHollowsUpdateMinutes { get; set; }
        public bool RunTreeGenOnChunkReload { get; set; }

        public string[] stumpTypes { get; set; }
        public string[] woodTypes { get; set; }

        public IDGTreeConfig()
        { }

        public static IDGTreeConfig Current { get; set; }

        public static IDGTreeConfig GetDefault()
        {
            IDGTreeConfig defaultConfig = new();

            defaultConfig.treeFellingDivisor = 2;
            defaultConfig.TreeHollowsUpdateMinutes = 15f;
            defaultConfig.TreeHollowsMaxItems = 8;
            defaultConfig.TreeHollowsMaxPerChunk = 1;
            defaultConfig.TreeHollowsSpawnProbability = 0.2f;
            defaultConfig.TreeHollowsUpdateMinutes = 360;
            defaultConfig.RunTreeGenOnChunkReload = false;
            defaultConfig.stumpTypes = new[] { "acacia", "baldcypress", "birch", "ebony", "kapok", "larch", "maple", "oak", "pine", "purpleheart",
                "walnut", "douglasfir", "willow", "honeylocust", "bearnut", "blackpoplar", "pyramidalpoplar", "catalpa",
                "mahogany", "sal", "saxaul", "spruce", "sycamore", "elm", "beech", "eucalyptus", "cedar"};
            defaultConfig.woodTypes = new[] { "acacia", "baldcypress", "birch", "ebony", "kapok", "larch", "maple", "oak", "pine", "purpleheart",
                "walnut", "douglasfir", "willow", "honeylocust", "bearnut", "blackpoplar", "pyramidalpoplar", "catalpa",
                "mahogany", "sal", "saxaul", "spruce", "sycamore", "elm", "beech", "eucalyptus", "cedar"};

            return defaultConfig;
        }
    }
}
