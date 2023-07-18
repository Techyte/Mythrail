using System;
using System.Collections.Generic;
using System.Linq;
using Mythrail.Multiplayer;
using Riptide;

namespace Mythrail.MainMenu.Tabs.Create
{
    public class CreateTab : Tab
    {
        private static CreateTab instance;
        
        public event EventHandler<List<ClientInviteInfo>> OnGetOnlinePlayers;
        public event EventHandler<MatchCreationInfo> OnMatchCreationSuccess;

        public ushort finalPort;

        private void Awake()
        {
            instance = this;
        }

        public void SendInvitedPlayers(List<ClientInviteInfo> clientInfos)
        {
            List<ClientInviteInfo> invitedClients = new List<ClientInviteInfo>();
            foreach (ClientInviteInfo player in clientInfos)
            {
                if (player.wantsToInvite)
                {
                    invitedClients.Add(player);
                }
            }
            
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.invites);
            message.AddClientInfos(invitedClients.ToArray());
            message.AddUShort(finalPort);
            MenuNetworkManager.Singleton.Client.Send(message);
            
            JoinCreatedMatch();
        }

        public void JoinCreatedMatch()
        {
            MenuNetworkManager.Singleton.JoinMatch(finalPort);
        }

        [MessageHandler((ushort)GameServerToClientId.playersResult)]
        private static void PlayersResult(Message message)
        {
            instance.OnGetOnlinePlayers?.Invoke(instance, message.GetClientInfos().ToList());
        }
        
        [MessageHandler((ushort)GameServerToClientId.createMatchSuccess)]
        private static void CreateMatchSuccess(Message message)
        {
            bool isPrivate = message.GetBool();
            string code = message.GetString();
            ushort port = message.GetUShort();
            
            instance.OnMatchCreationSuccess?.Invoke(instance, new MatchCreationInfo(isPrivate, code));
            
            instance.finalPort = port;
        }
    }

    public class MatchCreationInfo
    {
        public bool isPrivate;
        public string code;

        public MatchCreationInfo(bool isPrivate, string code)
        {
            this.isPrivate = isPrivate;
            this.code = code;
        }
    }
}