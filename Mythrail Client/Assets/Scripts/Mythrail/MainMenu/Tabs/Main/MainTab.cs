using System;
using Mythrail.Multiplayer;
using Riptide;

namespace Mythrail.MainMenu.Tabs.Main
{
    public class MainTab : Tab
    {
        private static MainTab instance;
        
        public event EventHandler<MatchInfo[]> OnReceivedMatchInfo;

        private void Awake()
        {
            instance = this;
        }
        
        [MessageHandler((ushort)GameServerToClientId.matches)]
        private static void Matches(Message message)
        {
            instance.OnReceivedMatchInfo?.Invoke(instance, message.GetMatchInfos());
        }
    }
}