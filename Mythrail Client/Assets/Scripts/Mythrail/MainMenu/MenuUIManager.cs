using System;
using System.Collections;
using Mythrail.MainMenu.Tabs;
using Mythrail.Notifications;
using Riptide;
using TMPro;
using UnityEngine;

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
    [Space]
    
    [Header("Profile")]
    [SerializeField] private TMP_InputField usernameField;
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
        NotificationManager.Singleton.CreateNotification(disconnectedImage, "Disconnected", "Connection to the Mythrail servers was lost", 2);
    }

    public void Connected()
    {
        connectionStatusText.text = "CONNECTED";
        NotificationManager.Singleton.CreateNotification(connectedImage, "Connected", "Connected to the Mythrail serves.", 2);
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
            usernameField.text = MenuNetworkManager.username;
            return;
        }
        
        SaveUsername(newUsername);

        MenuNetworkManager.username = newUsername;
            
        Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.updateUsername);
        message.AddString(MenuNetworkManager.username);
        MenuNetworkManager.Singleton.Client.Send(message);
    }

    private void SaveUsername(string newUsername)
    {
        PlayerPrefs.SetString("Username", newUsername);
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
        NotificationManager.Singleton.CreateNotification(disconnectedImage, "Failed To Connect", "Connection to the Mythrail servers could not be established.", 2);
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
        NotificationManager.Singleton.CreateNotification(privateMatchNotFoundImage, "Username empty", "The username you enter cannot be empty, please try again.", 2);
        ShakeScreen();
    }

    private void NotConnected()
    {
        NotificationManager.Singleton.CreateNotification(privateMatchNotFoundImage, "No Connection", "You are not connected to the Mythrail servers.", 2);
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

    public void InvalidUsername()
    {
        usernameField.text = "";
        NotificationManager.Singleton.CreateNotification(privateMatchNotFoundImage, "Can't use that name", "A user on this server is already using that name, please try again with a different name", 2);
    }

    public void RefreshConnection()
    {
        MenuNetworkManager.Singleton.Connect();
        TabManager.Singleton.OpenMain();
    }
}   
}
