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
        public List<string> cvalues;  //Chopping Recipe Values
        public List<string> svalues;  //Sawing Recipe Values
        public List<string> pvalues;  //Planing Recipe Values
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
            List<ChoppingBlockRecipe> crecipes = new List<ChoppingBlockRecipe>();
            List<SawingRecipe> srecipes = new List<SawingRecipe>();
            List<PlaningRecipe> precipes = new List<PlaningRecipe>();

            if (networkMessage.cvalues != null)
            {
                foreach (string crec in networkMessage.cvalues)
                {
                    using (MemoryStream ms = new MemoryStream(Ascii85.Decode(crec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        ChoppingBlockRecipe retr = new ChoppingBlockRecipe();
                        retr.FromBytes(reader, clientApi.World);

                        crecipes.Add(retr);
                    }
                }
            }
            IDGRecipeRegistry.Loaded.ChoppingBlockRecipes = crecipes;

            if (networkMessage.svalues != null)
            {
                foreach (string srec in networkMessage.svalues)
                {
                    using (MemoryStream ms = new MemoryStream(Ascii85.Decode(srec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        SawingRecipe retr = new SawingRecipe();
                        retr.FromBytes(reader, clientApi.World);

                        srecipes.Add(retr);
                    }
                }
            }

            IDGRecipeRegistry.Loaded.SawingRecipes = srecipes;

            if (networkMessage.pvalues != null)
            {
                foreach (string prec in networkMessage.pvalues)
                {
                    using (MemoryStream ms = new MemoryStream(Ascii85.Decode(prec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        PlaningRecipe retr = new PlaningRecipe();
                        retr.FromBytes(reader, clientApi.World);

                        precipes.Add(retr);
                    }
                }
            }

            IDGRecipeRegistry.Loaded.PlaningRecipes = precipes;

            System.Diagnostics.Debug.WriteLine(IDGRecipeRegistry.Loaded.ChoppingBlockRecipes.Count + " chopping recipes loaded to client.");

            System.Diagnostics.Debug.WriteLine(IDGRecipeRegistry.Loaded.SawingRecipes.Count + " sawing recipes loaded to client.");

            System.Diagnostics.Debug.WriteLine(IDGRecipeRegistry.Loaded.PlaningRecipes.Count + " planing recipes loaded to client.");
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
            List<string> crecipes = new List<string>();
            List<string> srecipes = new List<string>();
            List<string> precipes = new List<string>();

            foreach (ChoppingBlockRecipe crec in IDGRecipeRegistry.Loaded.ChoppingBlockRecipes)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);

                    crec.ToBytes(writer);

                    string value = Ascii85.Encode(ms.ToArray());
                    crecipes.Add(value);
                }
            }

            foreach (SawingRecipe srec in IDGRecipeRegistry.Loaded.SawingRecipes)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);

                    srec.ToBytes(writer);

                    string value = Ascii85.Encode(ms.ToArray());
                    srecipes.Add(value);
                }
            }

            foreach (PlaningRecipe prec in IDGRecipeRegistry.Loaded.PlaningRecipes)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);

                    prec.ToBytes(writer);

                    string value = Ascii85.Encode(ms.ToArray());
                    precipes.Add(value);
                }
            }

            serverChannel.BroadcastPacket(new RecipeUpload()
            {
                cvalues = crecipes,
                svalues = srecipes,
                pvalues = precipes
            });
        }

        private void OnClientMessage(IPlayer fromPlayer, RecipeResponse networkMessage)
        {
            OnRecipeUploadCmd();
        }


        #endregion
    }
}
