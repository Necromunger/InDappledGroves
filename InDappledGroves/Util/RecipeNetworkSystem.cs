using ProtoBuf;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using static InDappledGroves.Util.IDGRecipeNames;

namespace InDappledGroves.Util
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class RecipeUpload
    {
        public List<string> svalues;
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
                api.Network.RegisterChannel("networkapitest")
                .RegisterMessageType(typeof(RecipeUpload))
                .RegisterMessageType(typeof(RecipeResponse))
                .SetMessageHandler<RecipeUpload>(OnServerMessage)
            ;
        }

        private void OnServerMessage(RecipeUpload networkMessage)
        {
            List<ChoppingRecipe> srecipes = new List<ChoppingRecipe>();

            if (networkMessage.svalues != null)
            {
                foreach (string drec in networkMessage.svalues)
                {
                    using (MemoryStream ms = new MemoryStream(Ascii85.Decode(drec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        ChoppingRecipe retr = new ChoppingRecipe();
                        retr.FromBytes(reader, clientApi.World);

                        srecipes.Add(retr);
                    }
                }
            }

            IDGRecipeRegistry.Loaded.ChoppingRecipes = srecipes;
            if (networkMessage.svalues != null)
            {
                foreach (string crec in networkMessage.svalues)
                {
                    using (MemoryStream ms = new MemoryStream(Ascii85.Decode(crec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        ChoppingRecipe retr = new ChoppingRecipe();
                        retr.FromBytes(reader, clientApi.World);

                        srecipes.Add(retr);
                    }
                }
            }
            IDGRecipeRegistry.Loaded.ChoppingRecipes = srecipes;

            System.Diagnostics.Debug.WriteLine(IDGRecipeRegistry.Loaded.ChoppingRecipes.Count + " chopping recipes loaded to client.");
        }

        #endregion

        #region Server
        IServerNetworkChannel serverChannel;
        ICoreServerAPI serverApi;

        public override void StartServerSide(ICoreServerAPI api)
        {
            serverApi = api;

            serverChannel =
                api.Network.RegisterChannel("networkapitest")
                .RegisterMessageType(typeof(RecipeUpload))
                .RegisterMessageType(typeof(RecipeResponse))
                .SetMessageHandler<RecipeResponse>(OnClientMessage)
            ;

            api.RegisterCommand("recipeupload", "Resync recipes", "", OnRecipeUploadCmd, Privilege.chat);
            api.Event.PlayerNowPlaying += (hmm) => { OnRecipeUploadCmd(); };
        }

        private void OnRecipeUploadCmd(IServerPlayer player = null, int groupId = 0, CmdArgs args = null)
        {
            List<string> srecipes = new List<string>();

            foreach (ChoppingRecipe drec in IDGRecipeRegistry.Loaded.ChoppingRecipes)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);

                    drec.ToBytes(writer);

                    string value = Ascii85.Encode(ms.ToArray());
                    srecipes.Add(value);
                }
            }

            foreach (ChoppingRecipe crec in IDGRecipeRegistry.Loaded.ChoppingRecipes)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);

                    crec.ToBytes(writer);

                    string value = Ascii85.Encode(ms.ToArray());
                    srecipes.Add(value);
                }
            }

            serverChannel.BroadcastPacket(new RecipeUpload()
            {
                svalues = srecipes
            });
        }

        private void OnClientMessage(IPlayer fromPlayer, RecipeResponse networkMessage)
        {
            OnRecipeUploadCmd();
        }


        #endregion
    }
}
