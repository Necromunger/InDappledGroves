using InDappledGroves.Items.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace InDappledGroves.Util
{
    class InDappledGrovesConfig
    {
        //Multiplier applied to the Mining Speed of a tool used at a Workstation. Should default to 1. Less than 1 slows down, more than 1 speeds up work.
        public float baseWorkstationMiningSpdMult { get; set; }
        //Multiplier applied to the Resistance of a block being worked on a workstation. Should default to 1. Less than 1 speeds up, more than 1 slows down work.
        public float baseWorkstationResistanceMult { get; set; }
        //Multiplier applied to the Mining Speed of a tool used in a ground recipe. Should default to 1. Less than 1 slows down, more than 1 speeds up work.
        public float baseGroundRecipeMiningSpdMult { get; set; }
        //Multiplier applied to the Resistance of a block being worked on the ground. Should default to 1. Less than 1 speeds up, more than 1 slows down work.
        public float baseGroundRecipeResistaceMult { get; set; }
        //Multiplier applied when trees are being chopped, higher numbers reduces chopping speed of trees, lower numbers reduce time to chop trees.
        public float treeFellingResistanceMult { get; set; }
        //Rate at which Tree Hollows Update
        public int TreeHollowsMaxItems { get; set; }
        public int TreeHollowsMaxPerChunk { get; set; }
        public float TreeHollowsSpawnProbability { get; set; }
        public double TreeHollowsUpdateMinutes { get; set; }
        public bool RunTreeGenOnChunkReload { get; set; }

        public string[] stumpTypes;
        public string[] woodTypes;
        public InDappledGrovesConfig()
        { }

        public static InDappledGrovesConfig Current { get; set; }

        public static InDappledGrovesConfig GetDefault()
        {
            InDappledGrovesConfig defaultConfig = new();

            defaultConfig.baseWorkstationMiningSpdMult = 1.25f;
            defaultConfig.baseWorkstationResistanceMult = 1f;
            defaultConfig.baseGroundRecipeMiningSpdMult = 1f;
            defaultConfig.baseGroundRecipeResistaceMult = 1f;

            defaultConfig.treeFellingResistanceMult = 1f;
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
