using ProtoBuf;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using static InDappledGroves.Util.RecipeTools.IDGRecipeNames;

namespace InDappledGroves.Util.RecipeTools
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class RecipeUpload
    {
        public List<string> bwsvalues; //Basic Workstation Recipe Values
        public List<string> cwsvalues;  //Complex Workstation Recipe Values
        public List<string> gvalues;  //Ground Recipe Values
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class RecipeResponse
    {
        public string response;
    }

    public class RecipeUploadSystem : ModSystem
    {
        #region Client
        IClientNetworkChannel clientChannel;
        ICoreClientAPI clientApi;

        public override void StartClientSide(ICoreClientAPI api)
        {
            clientApi = api;

            clientChannel =
                api.Network.RegisterChannel("idgrecipechannel")
                .RegisterMessageType(typeof(RecipeUpload))
                .RegisterMessageType(typeof(RecipeResponse))
                .SetMessageHandler<RecipeUpload>(OnServerMessage)
            ;
        }
        

        private void OnServerMessage(RecipeUpload networkMessage)
        {

            List<BasicWorkstationRecipe> bwsrecipes = new();
            List<ComplexWorkstationRecipe> cwsrecipes = new();
            List<GroundRecipe> grecipes = new();
            #endregion


            #region Register Ground Recipes
            if (networkMessage.gvalues != null)
            {
                foreach (string grec in networkMessage.gvalues)
                {
                    using (MemoryStream ms = new(Ascii85.Decode(grec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        GroundRecipe retr = new GroundRecipe();
                        retr.FromBytes(reader, clientApi.World);

                        grecipes.Add(retr);
                    }
                }
            }

            IDGRecipeRegistry.Loaded.GroundRecipes = grecipes;
            #endregion

            #region Register Basic Workstation Recipes
            if (networkMessage.bwsvalues != null)
            {
                foreach (string bwsrec in networkMessage.bwsvalues)
                {
                    using (MemoryStream ms = new(Ascii85.Decode(bwsrec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        BasicWorkstationRecipe retr = new BasicWorkstationRecipe();
                        retr.FromBytes(reader, clientApi.World);

                        bwsrecipes.Add(retr);
                    }
                }
            }
            IDGRecipeRegistry.Loaded.BasicWorkstationRecipes = bwsrecipes;
            #endregion

            #region Register Complex Workstation Recipes
            if (networkMessage.cwsvalues != null)
            {
                foreach (string cwsrec in networkMessage.cwsvalues)
                {
                    using (MemoryStream ms = new(Ascii85.Decode(cwsrec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        ComplexWorkstationRecipe retr = new ComplexWorkstationRecipe();
                        retr.FromBytes(reader, clientApi.World);

                        cwsrecipes.Add(retr);
                    }
                }
            }
            IDGRecipeRegistry.Loaded.ComplexWorkstationRecipes = cwsrecipes;
            #endregion

        }

        #region Server
        IServerNetworkChannel serverChannel;
        ICoreServerAPI serverApi;

        public override void StartServerSide(ICoreServerAPI api)
        {
            serverApi = api;

            serverChannel =
                api.Network.RegisterChannel("idgrecipechannel")
                .RegisterMessageType(typeof(RecipeUpload))
                .RegisterMessageType(typeof(RecipeResponse))
                .SetMessageHandler<RecipeResponse>(OnClientMessage)
            ;

            api.RegisterCommand("recipeupload", "Resync recipes", "", OnRecipeUploadCmd, Privilege.chat);
            api.Event.PlayerNowPlaying += (hmm) => { OnRecipeUploadCmd(); };
        }

        private void OnRecipeUploadCmd(IServerPlayer player = null, int groupId = 0, CmdArgs args = null)
        {
            List<string> bwsrecipes = new List<string>();
            List<string> cwsrecipes = new List<string>();
            List<string> grecipes = new List<string>();

            foreach (BasicWorkstationRecipe bwsrec in IDGRecipeRegistry.Loaded.BasicWorkstationRecipes)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);

                    bwsrec.ToBytes(writer);

                    string value = Ascii85.Encode(ms.ToArray());
                    bwsrecipes.Add(value);
                }
            }

            foreach (ComplexWorkstationRecipe cwsrec in IDGRecipeRegistry.Loaded.ComplexWorkstationRecipes)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);

                    cwsrec.ToBytes(writer);

                    string value = Ascii85.Encode(ms.ToArray());
                    cwsrecipes.Add(value);
                }
            }

            foreach (GroundRecipe grec in IDGRecipeRegistry.Loaded.GroundRecipes)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);

                    grec.ToBytes(writer);

                    string value = Ascii85.Encode(ms.ToArray());
                    grecipes.Add(value);
                }
            }

            serverChannel.BroadcastPacket(new RecipeUpload()
            {
                bwsvalues = bwsrecipes,
                cwsvalues = cwsrecipes,
                gvalues = grecipes,
            });
        }

        private void OnClientMessage(IPlayer fromPlayer, RecipeResponse networkMessage)
        {
            OnRecipeUploadCmd();
        }


        #endregion
    }
}
