using InDappledGroves.Items.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace InDappledGroves.config
{
    class InDappledGrovesConfig
    {
        int tier1axespeed = 10;
        int tier1axefirewoodyield = 4;
        int tier1axestickyield = 4;
        int tier2axespeed = 8;
        int tier2axefirewoodyield = 4;
        int tier2axesticlyield = 4;
        int tier3axespeed = 6;
        int tier3axefirewoodyield = 4;
        int tier3axestickyield = 4;
        int tier4axespeed = 4;
        int tier4axefirewoodyield = 4;
        int tier4axestickyield = 4;
        int tier5axespeed = 2;
        int tier5axesfirewoodyield = 4;
        int tier5axestickyield = 4;
        int tier2sawspeed = 8;
        int tier2sawboardyield = 4;
        int tier3sawspeed = 6;
        int tier3sawboardyield = 4;
        int tier4sawspeed = 4;
        int tier4sawboardyield = 4;
        int tier5sawspeed = 2;
        int tier5sawsboardyield = 4;

        public InDappledGrovesConfig()
        { }

        public static InDappledGrovesConfig Current { get; set; }

        public static InDappledGrovesConfig GetDefault()
        {
            //Convert below modifiers to a multiplier rather than a straight number.  This will be beneficial for the multiple points of a saw
            InDappledGrovesConfig defaultConfig = new();
            defaultConfig.tier1axespeed = 10;
            defaultConfig.tier1axefirewoodyield = 4;
            defaultConfig.tier1axestickyield = 4;
            defaultConfig.tier2axespeed = 8;
            defaultConfig.tier2axefirewoodyield = 4;
            defaultConfig.tier2axesticlyield = 4;
            defaultConfig.tier3axespeed = 6;
            defaultConfig.tier3axefirewoodyield = 4;
            defaultConfig.tier3axestickyield = 4;
            defaultConfig.tier4axespeed = 4;
            defaultConfig.tier4axefirewoodyield = 4;
            defaultConfig.tier4axestickyield = 4;
            defaultConfig.tier5axespeed = 2;
            defaultConfig.tier5axesfirewoodyield = 4;
            defaultConfig.tier5axestickyield = 4;
            defaultConfig.tier2sawspeed = 8;
            defaultConfig.tier2sawboardyield = 4;
            defaultConfig.tier3sawspeed = 6;
            defaultConfig.tier3sawboardyield = 4;
            defaultConfig.tier4sawspeed = 4;
            defaultConfig.tier4sawboardyield = 4;
            defaultConfig.tier5sawspeed = 2;
            defaultConfig.tier5sawsboardyield = 4;

            //defaultConfig.HiveSeasons = 
            return defaultConfig;
        }
    }
}
