using System;
using Riptide;

namespace Mythrail.MainMenu.Tabs.Join
{
    public class JoinTab : Tab
    {
        private static JoinTab instance;

        public event EventHandler<ushort> OnMatchFound;
        public event EventHandler OnMatchNotFound; 

        private void Awake()
        {
            instance = this;
        }

        [MessageHandler((ushort)GameServerToClientId.joinedPrivateMatch)]
        private static void PrivateMatchJoinSuccess(Message message)
        {
            instance.OnMatchFound?.Invoke(instance, message.GetUShort());
        }

        [MessageHandler((ushort)GameServerToClientId.privateMatchNotFound)]
        private static void PrivateMatchNotFound(Message message)
        {
            instance.OnMatchNotFound.Invoke(instance, EventArgs.Empty);
        }
    }   
}