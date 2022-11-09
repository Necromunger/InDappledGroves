using InDappledGroves.Items.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace InDappledGroves.Util
{
    class InDappledGrovesConfig
    {
        //Multiplier applied to the Mining Speed of a tool used at a Workstation. Should default to 1. Less than 1 slows down, more than 1 speeds up work.
        public float baseWorkstationMiningSpdMult { get; set; } = 1;
        //Multiplier applied to the Resistance of a block being worked on a workstation. Should default to 1. Less than 1 speeds up, more than 1 slows down work.
        public float baseWorkstationResistanceMult { get; set; } = 1;
        //Multiplier applied to the Mining Speed of a tool used in a ground recipe. Should default to 1. Less than 1 slows down, more than 1 speeds up work.
        public float baseGroundRecipeMiningSpdMult { get; set; } = 1;
        //Multiplier applied to the Resistance of a block being worked on the ground. Should default to 1. Less than 1 speeds up, more than 1 slows down work.
        public float baseGroundRecipeResistaceMult { get; set; } = 1;
        //Multiplier applied when trees are being chopped, higher numbers reduces chopping speed of trees, lower numbers reduce time to chop trees.
        public float treeFellingResistanceMult { get; set; } = 1;
        //Rate at which Tree Hollows Update
        public int TreeHollowsMaxItems { get; set; } = 8;
        public int TreeHollowsMaxPerChunk { get; set; } = 1;
        public float TreeHollowsSpawnProbability { get; set; } = 0.2f;
        public double TreeHollowsUpdateMinutes { get; set; } = 360.0;

        public bool RunTreeGenOnChunkReload { get; set; } = false;
        public InDappledGrovesConfig()
        { }

        public static InDappledGrovesConfig Current { get; set; }

        public static InDappledGrovesConfig GetDefault()
        {
            InDappledGrovesConfig defaultConfig = new();
            defaultConfig.baseWorkstationMiningSpdMult = 1;
            defaultConfig.baseWorkstationResistanceMult = 1;
            defaultConfig.baseGroundRecipeMiningSpdMult = 1;
            defaultConfig.baseGroundRecipeResistaceMult = 1;
            defaultConfig.treeFellingResistanceMult = 1;
            defaultConfig.TreeHollowsUpdateMinutes = 15;

            return defaultConfig;
        }
    }
}
