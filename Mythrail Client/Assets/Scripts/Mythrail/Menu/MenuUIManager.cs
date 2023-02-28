using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Mythrail.Menu;
using Mythrail.Multiplayer;
using Mythrail.Notifications;
using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuUIManager : MonoBehaviour
{
    [Header("Connection Status Text")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [Space]
    
    [Header("Prefabs")]
    [SerializeField] private GameObject matchObject;
    [SerializeField] private GameObject playerObject;
    [SerializeField] private Sprite privateMatchNotFoundImage;
    [SerializeField] private Sprite connectedImage;
    [SerializeField] private Sprite disconnectedImage;
    [SerializeField] private Sprite multiplayerImage;
    [Space]
    
    [Header("Holders")]
    [SerializeField] private Transform matchHolder;
    [Space]
    
    [Header("Profile")]
    [SerializeField] private TMP_InputField usernameField;
    [Space]
    
    [Header("Tabs")]
    [SerializeField] private GameObject createScreen;
    [SerializeField] private GameObject joinFromCodeScreen;
    [SerializeField] private GameObject mainScreen;
    [Space]
    
    [Header("Tab Navigation")]
    [SerializeField] private Button createBackButton;
    [Space]
    
    [Header("Match Creation")]
    [SerializeField] private Slider maxPlayerCountSlider;
    [SerializeField] private TextMeshProUGUI maxPlayerDisplay;
    [SerializeField] private Slider minPlayerCountSlider;
    [SerializeField] private TextMeshProUGUI minPlayerDisplay;
    [SerializeField] private TMP_InputField matchName;
    [SerializeField] private Button matchMap;
    [SerializeField] private Toggle privateMatch;
    [SerializeField] private Button createConfirmButton;
    [Space]
    
    [Header("Match Creation Popup")]
    [SerializeField] private RectTransform matchPopup;
    [SerializeField] private TextMeshProUGUI matchCreatePopupTitle;
    [SerializeField] private TextMeshProUGUI matchCodeText;
    [SerializeField] private TextMeshProUGUI privateCodeURLText;
    [Space]
    
    [Header("Invite Players Question Popup")]
    [SerializeField] private GameObject invitePlayersQuestionPopup;
    [Space]
    
    [Header("Invite Popup")]
    [SerializeField] private GameObject inviteScreen;
    [SerializeField] private Transform playerHolders;
    [SerializeField] private Button sendInvitesButton;
    [Space]
    
    [Header("Join Match")]
    [SerializeField] private TMP_InputField privateMatchJoinCodeText;
    [Space]

    [Header("Animators")]
    [SerializeField] private Animator screenShakeAnimator;

    private void Start()
    {
        maxPlayerCountSlider.onValueChanged.AddListener(delegate { UpdateMinMax(); });
        minPlayerCountSlider.onValueChanged.AddListener(delegate { UpdateMinMax(); });
        Connecting();
    }

    public void Connecting()
    {
        connectionStatusText.text = "Connecting...";
    }

    public void Disconnected(object o, DisconnectedEventArgs e)
    {
        connectionStatusText.text = "Server Shut Down";
        OpenMainScreen();
        NotificationManager.Singleton.QueNotification(disconnectedImage, "Disconnected", "Connection to the Mythrail servers was lost", 2);
    }

    private void UpdateMinMax()
    {
        maxPlayerDisplay.text = maxPlayerCountSlider.value.ToString();
        minPlayerDisplay.text = minPlayerCountSlider.value.ToString();
    }

    public void Connected()
    {
        connectionStatusText.text = "Connected";
        NotificationManager.Singleton.QueNotification(connectedImage, "Connected", "Connected to the Mythrail serves.", 2);
    }

    public void LoadUsername(string username)
    {
        usernameField.text = username;
    }

    public void SendUpdatedUsername(string newUsername)
    {
        if(newUsername == MenuNetworkManager.username) return;
        
        Debug.Log("updating username");
        
        if (string.IsNullOrEmpty(newUsername))
        {
            NotificationManager.Singleton.QueNotification(privateMatchNotFoundImage, "Username empty", "The username you enter cannot be empty, please try again.", 2);
            ShakeScreen();
            return;
        }

        MenuNetworkManager.username = newUsername;
            
        Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.updateUsername);
        message.AddString(MenuNetworkManager.username);
        MenuNetworkManager.Singleton.Client.Send(message);
    }

    public void ConnectionFailed(object o, EventArgs args)
    {
        connectionStatusText.text = "Connection Failed!";
        NotificationManager.Singleton.QueNotification(disconnectedImage, "Failed To Connect", "Connection to the Mythrail servers could not be established.", 2);
    }

    public void OpenCreateScreen()
    {
        if (string.IsNullOrEmpty(MenuNetworkManager.username))
        {
            UsernameEmpty();
            return;
        }

        if (!MenuNetworkManager.Singleton.Client.IsConnected)
        {
            NotConnected();
            return;
        }
        
        createScreen.SetActive(true);
        mainScreen.SetActive(false);
        joinFromCodeScreen.SetActive(false);
    }

    public void OpenMainScreen()
    {
        createScreen.SetActive(false);
        mainScreen.SetActive(true);
        joinFromCodeScreen.SetActive(false);
    }

    public void OpenJoinPrivateMatchScreen()
    {
        if (string.IsNullOrEmpty(MenuNetworkManager.username))
        {
            UsernameEmpty();
            return;
        }

        if (!MenuNetworkManager.Singleton.Client.IsConnected)
        {
            NotConnected();
            return;
        }
        
        createScreen.SetActive(false);
        mainScreen.SetActive(false);
        joinFromCodeScreen.SetActive(true);
    }

    private void UsernameEmpty()
    {
        NotificationManager.Singleton.QueNotification(privateMatchNotFoundImage, "Username empty", "The username you enter cannot be empty, please try again.", 2);
        ShakeScreen();
    }

    private void NotConnected()
    {
        NotificationManager.Singleton.QueNotification(privateMatchNotFoundImage, "No Connection", "You are not connected to the Mythrail servers.", 2);
        ShakeScreen();
    }

    public void EnableInviteScreen(List<ClientInfo> clientInfos)
    {
        createScreen.SetActive(false);
        invitePlayersQuestionPopup.SetActive(false);
        inviteScreen.SetActive(true);
        OpenInviteScreen(clientInfos);
    }

    private void OpenInviteScreen(List<ClientInfo> clientInfos)
    {
        for (int i = 0; i < clientInfos.Count; i++)
        {
            GameObject PlayerListObject = Instantiate(playerObject, playerHolders);
            PlayerListObject.GetComponentInChildren<TextMeshProUGUI>().text = clientInfos[i].username;
            PlayerListObject.GetComponentInChildren<Toggle>().onValueChanged.AddListener(result =>
            {
                clientInfos[i-1].wantsToInvite = result;
            });
        }
            
        sendInvitesButton.onClick.AddListener(() =>
        {
            List<ClientInfo> invitedClients = new List<ClientInfo>();
            foreach (ClientInfo player in clientInfos)
            {
                if (player.wantsToInvite)
                {
                    invitedClients.Add(player);
                }
            }
            
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.invites);
            message.AddClientInfos(invitedClients.ToArray());
            message.AddUShort(MenuNetworkManager.Singleton.quickPort);
            MenuNetworkManager.Singleton.Client.Send(message);
        });
    }

    public void OpenInviteQuestionScreen()
    {
        inviteScreen.SetActive(false);
        invitePlayersQuestionPopup.SetActive(true);
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

    public void ShowMatchCreationMessage(string code, bool isPrivate)
    {
        matchPopup.gameObject.SetActive(true);
        matchCodeText.text = code;
        matchName.interactable = false;
        matchMap.interactable = false;
        maxPlayerCountSlider.interactable = false;
        minPlayerCountSlider.interactable = false;
        privateMatch.interactable = false;
        createBackButton.interactable = false;
        createConfirmButton.interactable = false;

        matchCreatePopupTitle.text = isPrivate ? "PRIVATE MATCH CREATED" : "PUBLIC MATCH CREATED";

        privateCodeURLText.text = "mythrail://" + code;
    }

    public void MatchNotFound()
    {
        OpenMainScreen();
        NotificationManager.Singleton.QueNotification(privateMatchNotFoundImage, "Incorrect Code", "This is not the game you are looking for...", 2);
        ShakeScreen();
    }

    public void InvalidUsername()
    {
        usernameField.text = "";
        NotificationManager.Singleton.QueNotification(privateMatchNotFoundImage, "Can't use that name", "A user on this server is already using that name, please try again with a different name", 2);
    }

    public void CreateMatchButton(string name, string creator, string code, ushort port)
    {
        GameObject newMatchObj = Instantiate(matchObject, matchHolder);

        newMatchObj.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = name;
        newMatchObj.transform.Find("Creator").GetComponent<TextMeshProUGUI>().text = creator;
        newMatchObj.transform.Find("Port").GetComponent<TextMeshProUGUI>().text = code;
            
        newMatchObj.GetComponent<Button>().onClick.AddListener(delegate
        {
            MenuNetworkManager.Singleton.JoinMatch(port);
        });
            
        MenuNetworkManager._matchButtons.Add(newMatchObj);
    }

    public void CreateMatch()
    {
        if (string.IsNullOrEmpty(matchName.text))
        {
            NotificationManager.Singleton.QueNotification(privateMatchNotFoundImage, "Name empty", "The match name you enter cannot be empty, please try again.", 2);
            ShakeScreen();
            return;
        }
            
        Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.createMatch);
        message.AddUShort((ushort)maxPlayerCountSlider.value);
        message.AddUShort((ushort)minPlayerCountSlider.value);
        message.AddString(matchName.text);
        message.AddBool(privateMatch.isOn);
        MenuNetworkManager.Singleton.Client.Send(message);
    }

    public void JoinPrivateMatch()
    {
        Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.joinPrivateMatch);
        message.AddString(privateMatchJoinCodeText.text.ToUpper());
        MenuNetworkManager.Singleton.Client.Send(message);
    }

    public void CopyPrivateMatchCode()
    {
        GUIUtility.systemCopyBuffer = matchCodeText.text;
    }

    public void CopyPrivateMatchURL()
    {
        GUIUtility.systemCopyBuffer = privateCodeURLText.text;
    }

    public void InvitedBy(string name, ushort port)
    {
        Notification notification = NotificationManager.Singleton.QueNotification(multiplayerImage,
            $"Invited by {name}", "Click here to join", 5);

        notification.Clicked += (o, e) =>
        {
            MenuNetworkManager.Singleton.JoinMatch(port);
        };
    }

    public void RefreshConnection()
    {
        MenuNetworkManager.Singleton.Connect();
        OpenMainScreen();
    }
}
