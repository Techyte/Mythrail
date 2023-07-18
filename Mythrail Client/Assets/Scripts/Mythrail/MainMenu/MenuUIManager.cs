using System;
using System.Collections;
using System.Collections.Generic;
using Mythrail.MainMenu.Tabs;
using Mythrail.Multiplayer;
using Mythrail.Notifications;
using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mythrail.MainMenu
{
public class MenuUIManager : MonoBehaviour
{
    public static MenuUIManager instance;
    
    [Header("Persistant")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private TextMeshProUGUI pingText;
    [Space]
    
    [Header("Prefabs")]
    [SerializeField] private Sprite privateMatchNotFoundImage;
    [SerializeField] private Sprite connectedImage;
    [SerializeField] private Sprite disconnectedImage;
    [SerializeField] private Sprite multiplayerImage;
    [Space]
    
    [Header("Profile")]
    [SerializeField] private TMP_InputField usernameField;
    [Space]
    
    [Header("Invites Screen")] 
    [SerializeField] private GameObject inviteDisplay;
    [SerializeField] private Transform invitesHolder;
    [SerializeField] private GameObject invitesScreen;
    public float InviteExpireTime => inviteExpireTime;
    [SerializeField] private float inviteExpireTime = 30f;
    [Space]
    
    [Header("Join Match")]
    [SerializeField] private TMP_InputField privateMatchJoinCodeText;
    [Space]

    [Header("Animators")]
    [SerializeField] private Animator screenShakeAnimator;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Connecting();
        SetPingText();
    }

    private void FixedUpdate()
    {
        SetPingText();
    }

    private void SetPingText()
    {
        pingText.text = "Ping: " + MenuNetworkManager.Singleton.Client.Connection.RTT;
    }

    public void Connecting()
    {
        connectionStatusText.text = "CONNECTING";
    }

    public void Disconnected(object o, DisconnectedEventArgs e)
    {
        connectionStatusText.text = "SERVER SHUT DOWN";
        TabManager.Singleton.OpenMain();
        NotificationManager.QueNotification(disconnectedImage, "Disconnected", "Connection to the Mythrail servers was lost", 2);
    }

    public void Connected()
    {
        connectionStatusText.text = "CONNECTED";
        NotificationManager.QueNotification(connectedImage, "Connected", "Connected to the Mythrail serves.", 2);
    }

    public void LoadUsername(string username)
    {
        usernameField.text = username;
    }

    public void SendUpdatedUsername(string newUsername)
    {
        if(newUsername == MenuNetworkManager.username) return;
        
        Debug.Log("updating username");

        if (!UsernameAcceptable(newUsername))
        {
            return;
        }

        MenuNetworkManager.username = newUsername;
            
        Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.updateUsername);
        message.AddString(MenuNetworkManager.username);
        MenuNetworkManager.Singleton.Client.Send(message);
    }

    public bool UsernameAcceptable(string newUsername)
    {
        if (string.IsNullOrEmpty(newUsername))
        {
            UsernameEmpty();
            return false;
        }

        return true;
    }

    public void ConnectionFailed(object o, EventArgs args)
    {
        connectionStatusText.text = "CONNECTION FAILED";
        NotificationManager.QueNotification(disconnectedImage, "Failed To Connect", "Connection to the Mythrail servers could not be established.", 2);
    }

    public void UpdateInvites()
    {
        MenuNetworkManager.Singleton.UpdateInvites();
        
        List<Invite> invites = MenuNetworkManager.Singleton.Invites;

        for (int i = 0; i < MenuNetworkManager.Singleton.currentInviteObjs.Count; i++)
        {
            Destroy(MenuNetworkManager.Singleton.currentInviteObjs[i]);
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
            
            MenuNetworkManager.Singleton.currentInviteObjs.Add(newInviteObj);
        }
    }

    public void InviteExpired()
    {
        if (invitesScreen.activeSelf)
        {
            UpdateInvites();
        }
    }

    public bool CanMoveMenu()
    {
        if (!UsernameAcceptable(MenuNetworkManager.username))
        {
            Debug.Log("username not acceptable");
            return false;
        }
        
        if (!UsernameAcceptable(usernameField.text))
        {
            Debug.Log("username field not acceptable");
            return false;
        }
        

        if (!MenuNetworkManager.Singleton.Client.IsConnected)
        {
            NotConnected();
            return false;
        }

        return true;
    }

    private void UsernameEmpty()
    {
        NotificationManager.QueNotification(privateMatchNotFoundImage, "Username empty", "The username you enter cannot be empty, please try again.", 2);
        ShakeScreen();
    }

    private void NotConnected()
    {
        NotificationManager.QueNotification(privateMatchNotFoundImage, "No Connection", "You are not connected to the Mythrail servers.", 2);
        ShakeScreen();
    }

    public void ShakeScreen()
    {
        screenShakeAnimator.SetBool("CanShake", true);
        StartCoroutine(ShakeScreenOff());
    }

    private IEnumerator ShakeScreenOff()
    {
        yield return new WaitForSeconds(.1f);
        screenShakeAnimator.SetBool("CanShake", false);
    }

    public void MatchNotFound()
    {
        TabManager.Singleton.OpenMain();
        NotificationManager.QueNotification(privateMatchNotFoundImage, "Incorrect Code", "This is not the game you are looking for...", 2);
        ShakeScreen();
    }

    public void InvalidUsername()
    {
        usernameField.text = "";
        NotificationManager.QueNotification(privateMatchNotFoundImage, "Can't use that name", "A user on this server is already using that name, please try again with a different name", 2);
    }

    public void JoinPrivateMatch()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.joinPrivateMatch);
        message.AddString(privateMatchJoinCodeText.text.ToUpper());
        MenuNetworkManager.Singleton.Client.Send(message);
    }

    public void InvitedBy(string name, ushort port)
    {
        Notification notification = NotificationManager.QueNotification(multiplayerImage,
            $"Invited by {name}", "Click here to join", 5);

        notification.Clicked += (o, e) =>
        {
            MenuNetworkManager.Singleton.JoinMatch(port);
        };
    }

    public void RefreshConnection()
    {
        MenuNetworkManager.Singleton.Connect();
        TabManager.Singleton.OpenMain();
    }
}   
}
