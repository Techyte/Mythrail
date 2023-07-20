using System;
using System.Collections.Generic;
using Mythrail.Notifications;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mythrail.MainMenu.Tabs.Invites
{
    public class InvitesTabUiManager : TabUiManager
    {
        private InvitesTab _invitesTab;
        
        [SerializeField] private Sprite multiplayerImage;
    
        [Header("Invites Screen")] 
        [SerializeField] private GameObject inviteDisplay;
        [SerializeField] private Transform invitesHolder;

        public List<GameObject> currentInviteObjs = new List<GameObject>();
        
        private void Awake()
        {
            _invitesTab = (InvitesTab)tab;
            
            _invitesTab.OnReceivedInvite += delegate(object sender, InviteData data)
            {
                InvitedBy(data.username, data.port);
            };
            
            _invitesTab.OnInviteExpired += delegate(object sender, EventArgs args)
            {
                InviteExpired();
            };
            
            _invitesTab.OnOpened += delegate(object sender, EventArgs args)
            {
                if (_invitesTab.IsOpen)
                {
                    UpdateInvites();
                }
            };
        }

        public void InvitedBy(string name, ushort port)
        {
            Notification notification = NotificationManager.QueNotification(multiplayerImage,
                $"Invited by {name}", "Click here to join", 5);

            notification.Clicked += (o, e) =>
            {
                MenuNetworkManager.Singleton.JoinMatch(port);
            };
            
            if (_invitesTab.IsOpen)
            {
                UpdateInvites();
            }
        }
        
        public void UpdateInvites()
        {
            _invitesTab.UpdateInvites();
        
            List<Invite> invites = _invitesTab.Invites;

            for (int i = 0; i < currentInviteObjs.Count; i++)
            {
                Destroy(currentInviteObjs[i]);
            }

            for (int i = 0; i < invites.Count; i++)
            {
                GameObject newInviteObj = Instantiate(inviteDisplay, invitesHolder);

                newInviteObj.transform.Find("MatchName").GetComponent<TextMeshProUGUI>().text = invites[i].matchName;
                newInviteObj.transform.Find("Username").GetComponent<TextMeshProUGUI>().text = invites[i].username;
                newInviteObj.transform.Find("Code").GetComponent<TextMeshProUGUI>().text = invites[i].code;
            
                int newI = i;
                newInviteObj.GetComponent<Button>().onClick.AddListener(delegate
                {
                    MenuNetworkManager.Singleton.JoinMatch(invites[newI].port);
                });
            
                currentInviteObjs.Add(newInviteObj);
            }
        }

        public void InviteExpired()
        {
            if (_invitesTab.IsOpen)
            {
                UpdateInvites();
            }
        }
    }   
}