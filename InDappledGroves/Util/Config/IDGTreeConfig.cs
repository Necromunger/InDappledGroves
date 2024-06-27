using System;
using System.Collections.Generic;
using System.Text;

namespace InDappledGroves.Util.Config
{
    class IDGTreeConfig
    {
        //Multiplier applied when trees are being chopped, higher numbers reduces chopping speed of trees, lower numbers reduce time to chop trees.
        public float TreeFellingDivisor { get; set; }
        //Rate at which Tree Hollows Update
        public int TreeHollowsMaxItems { get; set; }
        public float TreeHollowsSpawnProbability { get; set; }
        public bool RunTreeGenOnChunkReload { get; set; }
        public string[] stumpTypes { get; set; }
        public string[] woodTypes { get; set; }
        public double HollowBreakChance { get; set; }
        public IDGTreeConfig()
        { }

        public static IDGTreeConfig Current { get; set; }
       

        public static IDGTreeConfig GetDefault()
        {
            IDGTreeConfig defaultConfig = new();

            defaultConfig.TreeFellingDivisor = 2;
            defaultConfig.HollowBreakChance = 0.2f;
            defaultConfig.TreeHollowsMaxItems = 8;
            defaultConfig.TreeHollowsSpawnProbability = 0.2f;
            defaultConfig.RunTreeGenOnChunkReload = false;
            defaultConfig.stumpTypes = new[] { "acacia", "baldcypress", "birch", "ebony", "kapok", "larch", "maple", "oak", "pine", "purpleheart",
                "walnut", "douglasfir", "willow", "honeylocust", "bearnut", "blackpoplar", "poplar", "pyramidalpoplar", "catalpa", 
                "mahogany", "sal", "saxaul", "spruce", "sycamore", "elm", "beech", "eucalyptus", "cedar", "searsialancea", "afrocarpusfalcatus", 
                "lysilomalatisiliquum", "eucalyptuscamaldulensis", "corymbiaaparrerinja", "araucariaheterophylla","dacrydiumcupressinum", 
                "nothofagusmenziesii", "podocarpustotara"};
            defaultConfig.woodTypes = new[] { "acacia", "baldcypress", "birch", "ebony", "kapok", "larch", "maple", "oak", "pine", "purpleheart",
                "walnut", "douglasfir", "willow", "honeylocust", "bearnut", "blackpoplar", "poplar", "pyramidalpoplar", "catalpa",
                "mahogany", "sal", "saxaul", "spruce", "sycamore", "elm", "beech", "eucalyptus", "cedar", "searsialancea", "afrocarpusfalcatus", 
                "lysilomalatisiliquum", "eucalyptuscamaldulensis", "corymbiaaparrerinja", "araucariaheterophylla","dacrydiumcupressinum",
                "nothofagusmenziesii", "podocarpustotara"};

            return defaultConfig;
        }
    }
}
