using InDappledGroves.Items.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace InDappledGroves.Util
{
    class InDappledGrovesConfig
    {
        //Multiplier applied to the Mining Speed of a tool used at a Workstation. Should default to 1. Less than 1 slows down, more than 1 speeds up work.
        public float workstationMiningSpdMult;
        //Multiplier applied to the Resistance of a block being worked on a workstation. Should default to 1. Less than 1 speeds up, more than 1 slows down work.
        public float workstationResistanceMult;
        //Multiplier applied to the Mining Speed of a tool used in a ground recipe. Should default to 1. Less than 1 slows down, more than 1 speeds up work.
        public float groundRecipeMiningSpdMult;
        //Multiplier applied to the Resistance of a block being worked on the ground. Should default to 1. Less than 1 speeds up, more than 1 slows down work.
        public float groundRecipeResistaceMult;
        //Multiplier applied when trees are being chopped, higher numbers reduces chopping speed of trees, lower numbers reduce time to chop trees.
        public float treeFellingResistanceMult;

        public InDappledGrovesConfig()
        { }

        public static InDappledGrovesConfig Current { get; set; }

        public static InDappledGrovesConfig GetDefault()
        {
            InDappledGrovesConfig defaultConfig = new();
            defaultConfig.workstationMiningSpdMult = 1;
            defaultConfig.workstationResistanceMult = 1;
            defaultConfig.groundRecipeMiningSpdMult = 1;
            defaultConfig.groundRecipeResistaceMult = 1;

            return defaultConfig;
        }
    }
}
