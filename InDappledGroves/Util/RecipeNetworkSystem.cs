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
        public List<string> cbvalues;  //ChoppingBlock Recipe Values
        public List<string> sbvalues;  //SawBuck Recipe Values
        public List<string> shvalues;  //SawHorse Recipe Values
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
                api.Network.RegisterChannel("networkapitest")
                .RegisterMessageType(typeof(RecipeUpload))
                .RegisterMessageType(typeof(RecipeResponse))
                .SetMessageHandler<RecipeUpload>(OnServerMessage)
            ;
        }

        private void OnServerMessage(RecipeUpload networkMessage)
        {
            List<ChoppingBlockRecipe> crecipes = new();
            List<SawbuckRecipe> srecipes = new();
            List<SawHorseRecipe> precipes = new();
            List<GroundRecipe> grecipes = new();

            if (networkMessage.cbvalues != null)
            {
                foreach (string crec in networkMessage.cbvalues)
                {
                    using (MemoryStream ms = new(Ascii85.Decode(crec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        ChoppingBlockRecipe retr = new ChoppingBlockRecipe();
                        retr.FromBytes(reader, clientApi.World);

                        crecipes.Add(retr);
                    }
                }
            }
            IDGRecipeRegistry.Loaded.ChoppingBlockRecipes = crecipes;

            if (networkMessage.sbvalues != null)
            {
                foreach (string srec in networkMessage.sbvalues)
                {
                    using (MemoryStream ms = new MemoryStream(Ascii85.Decode(srec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        SawbuckRecipe retr = new SawbuckRecipe();
                        retr.FromBytes(reader, clientApi.World);

                        srecipes.Add(retr);
                    }
                }
            }

            IDGRecipeRegistry.Loaded.SawbuckRecipes = srecipes;

            if (networkMessage.shvalues != null)
            {
                foreach (string prec in networkMessage.shvalues)
                {
                    using (MemoryStream ms = new MemoryStream(Ascii85.Decode(prec)))
                    {
                        BinaryReader reader = new BinaryReader(ms);

                        SawHorseRecipe retr = new SawHorseRecipe();
                        retr.FromBytes(reader, clientApi.World);

                        precipes.Add(retr);
                    }
                }
            }

            IDGRecipeRegistry.Loaded.SawHorseRecipes = precipes;

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
            List<string> grecipes = new List<string>();

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

            foreach (SawbuckRecipe srec in IDGRecipeRegistry.Loaded.SawbuckRecipes)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);

                    srec.ToBytes(writer);

                    string value = Ascii85.Encode(ms.ToArray());
                    srecipes.Add(value);
                }
            }

            foreach (SawHorseRecipe prec in IDGRecipeRegistry.Loaded.SawHorseRecipes)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);

                    prec.ToBytes(writer);

                    string value = Ascii85.Encode(ms.ToArray());
                    precipes.Add(value);
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
                cbvalues = crecipes,
                sbvalues = srecipes,
                shvalues = precipes,
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
