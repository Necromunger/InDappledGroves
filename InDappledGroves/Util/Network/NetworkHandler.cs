using InDappledGroves.Util.Config;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace InDappledGroves.Util.Network
{
    public class NetworkHandler
    {
        internal void RegisterMessages(ICoreAPI api)
        {
            api.Network
                .RegisterChannel("idgnetwork")
                .RegisterMessageType(typeof(NetworkApiTestMessage))
                .RegisterMessageType(typeof(NetworkApiTestResponse))
                .RegisterMessageType(typeof(ToolConfigFromServerMessage))
                .RegisterMessageType(typeof(OnPlayerLoginMessage));
            ; 
        }

        #region Client
        IClientNetworkChannel clientChannel;
        ICoreClientAPI clientApi;
        public void InitializeClientSideNetworkHandler(ICoreClientAPI capi) {
            clientApi = capi;

            clientChannel = capi.Network.GetChannel("idgnetwork")
                .SetMessageHandler<ToolConfigFromServerMessage>(RecieveToolConfigAction);
            ;

        }

        //SetToolConfigValues received from Server
        private void RecieveToolConfigAction(ToolConfigFromServerMessage toolConfig)
        {
            //Fired when the server sends the ToolConfig information to the player's client after login

            //Set Client Tool Config Settings from Server
            InDappledGroves.baseWorkstationMiningSpdMult = toolConfig.baseWorkstationMiningSpdMult;
            InDappledGroves.baseGroundRecipeMiningSpdMult = toolConfig.baseGroundRecipeMiningSpdMult;

            //Set Client TreeConfigSettings from Server
            IDGTreeConfig.Current.TreeFellingMultiplier = toolConfig.TreeFellingMultiplier;

        }

        #endregion

        #region server
        IServerNetworkChannel serverChannel;
        ICoreServerAPI serverApi;
        public void InitializeServerSideNetworkHandler(ICoreServerAPI api)
        {
            serverApi = api;

            //Listen for player join events
            api.Event.PlayerJoin += OnPlayerJoin;

            serverChannel = api.Network.GetChannel("idgnetwork")
                .SetMessageHandler<OnPlayerLoginMessage>(OnPlayerJoin);
        }

        private void OnPlayerJoin(IServerPlayer player)
        {
            OnPlayerJoin(player, new OnPlayerLoginMessage());
        }

        //Send a packet on the client channel containing a new instance of ToolConfigFromServerMessage
        //Which is pre-loaded on creation with all the values for Tool Config.
        private void OnPlayerJoin(IServerPlayer fromPlayer, OnPlayerLoginMessage packet)
        {
            serverChannel.SendPacket(new ToolConfigFromServerMessage(), fromPlayer);
        }

       

        private void OnClientMessage(IPlayer fromPlayer, NetworkApiTestResponse networkMessage)
        {
            serverApi.SendMessageToGroup(
                GlobalConstants.GeneralChatGroup,
                "Received following response from " + fromPlayer.PlayerName + ": " + networkMessage.response,
                EnumChatType.Notification
            );

        }

        #endregion

        [ProtoContract]
        class NetworkApiTestMessage
        {
            [ProtoMember(1)]
            public string message;
        }

        [ProtoContract]
        class NetworkApiTestResponse
        {
            [ProtoMember(1)]
            public string response;
        }

        [ProtoContract]
        class ToolConfigFromServerMessage
        {
            [ProtoMember(1)]
            public float baseWorkstationMiningSpdMult = IDGToolConfig.Current.baseWorkstationMiningSpdMult;
            [ProtoMember(2)]
            public float baseWorkstationResistanceMult = IDGToolConfig.Current.baseWorkstationResistanceMult;
            [ProtoMember(3)]
            public float baseGroundRecipeMiningSpdMult = IDGToolConfig.Current.baseGroundRecipeMiningSpdMult;
            [ProtoMember(4)]
            public float TreeFellingMultiplier = IDGTreeConfig.Current.TreeFellingMultiplier;

        }

        [ProtoContract]
        class OnPlayerLoginMessage
        {
            [ProtoMember(1)]
            readonly IPlayer[] player;
        }
    }
}
