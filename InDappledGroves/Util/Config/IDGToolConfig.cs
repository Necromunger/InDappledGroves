using InDappledGroves.Items.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace InDappledGroves.Util.Config
{
    class IDGToolConfig
    {
        //Multiplier applied to the Mining Speed of a tool used at a Workstation. Should default to 1. Less than 1 slows down, more than 1 speeds up work.
        public float baseWorkstationMiningSpdMult { get; set; }
        //Multiplier applied to the Resistance of a block being worked on a workstation. Should default to 1. Less than 1 speeds up, more than 1 slows down work.
        public float baseWorkstationResistanceMult { get; set; }
        //Multiplier applied to the Mining Speed of a tool used in a ground recipe. Should default to 1. Less than 1 slows down, more than 1 speeds up work.
        public float baseGroundRecipeMiningSpdMult { get; set; }
        //Multiplier applied to the Resistance of a block being worked on the ground. Should default to 1. Less than 1 speeds up, more than 1 slows down work.
        public float baseGroundRecipeResistaceMult { get; set; }


        public IDGToolConfig()
        { }

        public static IDGToolConfig Current { get; set; }

        public static IDGToolConfig GetDefault()
        {
            IDGToolConfig defaultConfig = new();

            defaultConfig.baseWorkstationMiningSpdMult = 1.25f;
            defaultConfig.baseWorkstationResistanceMult = 1f;
            defaultConfig.baseGroundRecipeMiningSpdMult = 1f;
            defaultConfig.baseGroundRecipeResistaceMult = 1f;

            return defaultConfig;
        }
    }
}
