using InDappledGroves.Items.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace InDappledGroves.Util
{
    class InDappledGrovesConfig
    {
        public bool preventToolUseWithLowDurability;
        public InDappledGrovesConfig()
        { }

        public static InDappledGrovesConfig Current { get; set; }

        public static InDappledGrovesConfig GetDefault()
        {
            //Convert below modifiers to a multiplier rather than a straight number.  This will be beneficial for the multiple points of a saw
            InDappledGrovesConfig defaultConfig = new();
            defaultConfig.preventToolUseWithLowDurability = false;

            //defaultConfig.HiveSeasons = 
            return defaultConfig;
        }
    }
}
