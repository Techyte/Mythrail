using System;
using Mythrail.Notifications;
using Riptide;
using TMPro;
using UnityEngine;

namespace Mythrail.MainMenu.Tabs.Join
{
    public class JoinTabUiManager : TabUiManager
    {
        private JoinTab _joinTab;
    
        [Header("Join Match")]
        [SerializeField] private TMP_InputField privateMatchJoinCodeText;
        [SerializeField] private Sprite privateMatchNotFoundImage;

        private void Awake()
        {
            _joinTab = (JoinTab)tab;

            _joinTab.OnMatchFound += delegate(object sender, ushort e)
            {
                MatchFound(e);
            };
            
            _joinTab.OnMatchNotFound += delegate(object sender, EventArgs args)
            {
                MatchNotFound();
            };
        }

        private void MatchFound(ushort port)
        {
            MenuNetworkManager.Singleton.JoinMatch(port);
        }

        public void MatchNotFound()
        {
            NotificationManager.QueNotification(privateMatchNotFoundImage, "Incorrect Code", "This is not the game you are looking for...", 2);
            MenuUIManager.instance.ShakeScreen();
        }

        public void JoinMatch()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.joinPrivateMatch);
            message.AddString(privateMatchJoinCodeText.text.ToUpper());
            MenuNetworkManager.Singleton.Client.Send(message);
        }
    }   
}