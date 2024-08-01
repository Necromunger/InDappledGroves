using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using static OpenTK.Graphics.OpenGL.GL;

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

            defaultConfig.baseWorkstationMiningSpdMult = 2f;
            defaultConfig.baseWorkstationResistanceMult = 1f;
            defaultConfig.baseGroundRecipeMiningSpdMult = 1f;
            defaultConfig.baseGroundRecipeResistaceMult = 1f;

            return defaultConfig;
        }

        public static void createConfigFile(ICoreAPI api)
        {
            //Tool/Workstation Config
            //Check for Existing Config file, create one if none exists
            try
            {
                var Config = api.LoadModConfig<IDGToolConfig>("indappledgroves/toolconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    IDGToolConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    IDGToolConfig.Current = IDGToolConfig.GetDefault();
                }
            }
            catch
            {
                IDGToolConfig.Current = IDGToolConfig.GetDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                api.StoreModConfig(IDGToolConfig.Current, "indappledgroves/toolconfig.json");
            }
        }
    }
}
