using InDappledGroves.BlockEntities;
using InDappledGroves.Blocks;
using InDappledGroves.CollectibleBehaviors;
using InDappledGroves.Items;
using InDappledGroves.BlockBehaviors;
using Vintagestory.API.Common;
using InDappledGroves.Util.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using ProtoBuf;
using Vintagestory.API.Config;
using InDappledGroves.Util.Network;
using InDappledGroves.Util.WorldGen;

namespace InDappledGroves
{

    public class InDappledGroves : ModSystem
    {
        public static float baseWorkstationMiningSpdMult;
        public static float baseWorkstationResistanceMult;
        public static float baseGroundRecipeMiningSpdMult;
        public static float baseGroundRecipeResistaceMult;
        NetworkHandler networkHandler;

        #region Client
        public override void StartClientSide(ICoreClientAPI api)
        {
            networkHandler.InitializeClientSideNetworkHandler(api);
        }
        #endregion

        #region server
        public override void StartServerSide(ICoreServerAPI api)
        {
            IDGToolConfig.createConfigFile(api);
            api.Logger.Debug("IDGToolConfig.createConfigFile Tool Config has run");
            IDGTreeConfig.createConfigFile(api);
            api.Logger.Debug("IDGTreeConfig.createConfigFile Tree Config has run");
            //IDGHollowLootConfig.createConfigFile(api);
            //api.Logger.Debug("IDGTreeConfig.createConfigFile Tree Hollows has run");
            networkHandler.InitializeServerSideNetworkHandler(api);
            
        }
        #endregion

        public override void Start(ICoreAPI api)
        {
            networkHandler = new NetworkHandler();
            base.Start(api);
            //Register Items
            api.RegisterItemClass("idgfirewood", typeof(IDGFirewood));
            api.RegisterItemClass("idgplank", typeof(IDGPlank));
            api.RegisterItemClass("idgbark", typeof(IDGBark));

            //Register Blocks
            api.RegisterBlockClass("idgchoppingblock", typeof(IDGChoppingBlock));
            api.RegisterBlockClass("idgbarkbundle", typeof(IDGBarkBundle));
            api.RegisterBlockClass("idglogslab", typeof(IDGLogSlab));
            api.RegisterBlockClass("idgsawbuck", typeof(IDGSawBuck));
            api.RegisterBlockClass("idgsawhorse", typeof(IDGSawHorse));
            api.RegisterBlockClass("idgbarkbasket", typeof(IDGBarkBasket));
            api.RegisterBlockClass("idgboardblock", typeof(IDGBoardBlock));
            api.RegisterBlockClass("idgblockfirewood", typeof(IDGBlockFirewood));
            api.RegisterBlockClass("blocktreehollowgrown", typeof(BlockTreeHollowGrown));
            api.RegisterBlockClass("blocktreehollowplaced", typeof(BlockTreeHollowPlaced));
            api.RegisterBlockClass("idgblockstump", typeof(BlockStump));
            api.RegisterBlockClass("idgblockburl", typeof(BlockBurl));

            //Register BlockEntities
            api.RegisterBlockEntityClass("idgbechoppingblock", typeof(IDGBEChoppingBlock));
            api.RegisterBlockEntityClass("idgbesawbuck", typeof(IDGBESawBuck));
            api.RegisterBlockEntityClass("idgbesawhorse", typeof(IDGBESawHorse));
            api.RegisterBlockEntityClass("betreehollowgrown", typeof(BETreeHollowGrown));
            api.RegisterBlockEntityClass("betreehollowplaced", typeof(BETreeHollowPlaced));

            //Register CollectibleBehaviors
            api.RegisterCollectibleBehaviorClass("woodsplitter", typeof(BehaviorWoodChopping));
            api.RegisterCollectibleBehaviorClass("woodsawer", typeof(BehaviorWoodSawing));
            api.RegisterCollectibleBehaviorClass("woodplaner", typeof(BehaviorWoodPlaning));
            api.RegisterCollectibleBehaviorClass("woodhewer", typeof(BehaviorWoodHewing));
            api.RegisterCollectibleBehaviorClass("idgtool", typeof(BehaviorIDGTool));


            //Register BlockBehaviors
            api.RegisterBlockBehaviorClass("Submergible", typeof(BehaviorSubmergible));
            api.RegisterBlockBehaviorClass("IDGPickup", typeof(BehaviorIDGPickup));

            if (api.Side.IsServer())
            {
                IDGHollowLootConfig.createConfigFile(api as ICoreServerAPI);
                api.Logger.Debug("IDGTreeConfig.createConfigFile Tree Hollows has run");
            }

            //Registers Channels and Message Types
            networkHandler.RegisterMessages(api);
            api.Logger.Debug("Start Method has finished.");
        }
    }
}
