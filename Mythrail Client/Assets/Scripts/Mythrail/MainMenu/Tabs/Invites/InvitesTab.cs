using System;
using System.Collections;
using System.Collections.Generic;
using Riptide;
using UnityEngine;

namespace Mythrail.MainMenu.Tabs.Invites
{
    public class InvitesTab : Tab
    {
        public static InvitesTab instance;

        public event EventHandler<InviteData> OnReceivedInvite;
        public event EventHandler OnInviteExpired;
        public event EventHandler OnOpened;

        public List<Invite> Invites => invites;
        private List<Invite> invites = new List<Invite>();
        
        public float InviteExpireTime => inviteExpireTime;
        [SerializeField] private float inviteExpireTime = 30f;

        private void Awake()
        {
            instance = this;
        }
        
        public void UpdateInvites()
        {
            List<Invite> toBeRemoved = new List<Invite>();

            for (int i = 0; i < invites.Count; i++)
            {
                if (invites[i].expired)
                {
                    toBeRemoved.Add(invites[i]);
                }
            }

            for (int i = 0; i < toBeRemoved.Count; i++)
            {
                invites.Remove(toBeRemoved[i]);
            }
        }

        protected override void Opened()
        {
            OnOpened?.Invoke(this, EventArgs.Empty);
        }

        public void ReceivedInvite(ushort port, string code, string username, string name)
        {
            OnReceivedInvite?.Invoke(this, new InviteData(username, port));
            
            Invite invite = new Invite(port, code, username, name);
            
            invites.Add(invite);
            StartCoroutine(InviteTimer(invite));
        }

        private IEnumerator InviteTimer(Invite invite)
        {
            yield return new WaitForSeconds(InviteExpireTime);
            invite.Expire();
            OnInviteExpired?.Invoke(this, EventArgs.Empty);
        }

        [MessageHandler((ushort)GameServerToClientId.invite)]
        private static void Invited(Message message)
        {
            string username = message.GetString();
            ushort port = message.GetUShort();
            string code = message.GetString();
            string name = message.GetString();
            
            instance.ReceivedInvite(port, code, username, name);
        }
    }

    public class InviteData
    {
        public string username;
        public ushort port;

        public InviteData(string username, ushort port)
        {
            this.username = username;
            this.port = port;
        }
    }

    public class Invite
    {
        public ushort port;
        public string username;
        public string matchName;
        public string code;
        public bool expired;

        public Invite(ushort port, string code, string username, string matchName)
        {
            this.port = port;
            this.code = code;
            this.username = username;
            this.matchName = matchName;
        }

        public void Expire()
        {
            Debug.Log("Expired");
            expired = true;
            InvitesTab.instance.UpdateInvites();
        }
    }

    public class ClientInviteInfo
    {
        public ushort id;
        public string username;
        public bool wantsToInvite;

        public ClientInviteInfo(ushort id, string username)
        {
            this.id = id;
            this.username = username;
        }
    }
}