using System;
using System.Collections.Generic;
using Mythrail.MainMenu.Tabs.Invites;
using Mythrail.Multiplayer;
using Mythrail.Notifications;
using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mythrail.MainMenu.Tabs.Create
{
    public class CreateTabUiManager : TabUiManager
    {
        private static CreateTabUiManager instance;

        private CreateTab _createTab;
        
        [Header("Creation Settings Screen")]
        [SerializeField] private Slider maxPlayerCountSlider;
        [SerializeField] private TextMeshProUGUI maxPlayerDisplay;
        [Space]
        [SerializeField] private Slider minPlayerCountSlider;
        [SerializeField] private TextMeshProUGUI minPlayerDisplay;
        [Space]
        [SerializeField] private TMP_InputField matchName;
        [Space]
        [SerializeField] private Button matchMap;
        [Space]
        [SerializeField] private Toggle privateMatch;
        [Space]
        [SerializeField] private Button createConfirmButton;
    
        [Header("Invite Players Question Popup")]
        [SerializeField] private GameObject invitePlayersQuestionPopup;
        [Space]
    
        [Header("Invite Popup")]
        [SerializeField] private GameObject inviteScreen;
        [SerializeField] private Transform playerHolders;
        [SerializeField] private Button sendInvitesButton;
        [Space]
        
        [Header("Prefabs")]
        [SerializeField] private GameObject playerObject;
        [SerializeField] private Sprite privateMatchNotFoundImage;
        [Space]
    
        [Header("Back Button")]
        [SerializeField] private Button createBackButton;
        [Space]
    
        [Header("Match Creation Popup")]
        [SerializeField] private RectTransform matchPopup;
        [SerializeField] private TextMeshProUGUI matchCreatePopupTitle;
        [SerializeField] private TextMeshProUGUI matchCodeText;
        [SerializeField] private TextMeshProUGUI privateCodeURLText;

        private void Awake()
        {
            instance = this;

            _createTab = (CreateTab)tab;
            
            _createTab.OnGetOnlinePlayers += delegate(object sender, List<ClientInviteInfo> list)
            {
                EnableInviteScreen(list);
            };
            
            _createTab.OnMatchCreationSuccess += delegate(object sender, MatchCreationInfo info)
            {
                ShowMatchCreationMessage(info.code, info.isPrivate);
            };
        }

        private void Start()
        {
            maxPlayerCountSlider.onValueChanged.AddListener(delegate { UpdateMinMax(); });
            minPlayerCountSlider.onValueChanged.AddListener(delegate { UpdateMinMax(); });
        }

        private void UpdateMinMax()
        {
            maxPlayerDisplay.text = maxPlayerCountSlider.value.ToString();
            minPlayerDisplay.text = minPlayerCountSlider.value.ToString();
        }

        private void OpenInviteScreen(List<ClientInviteInfo> clientInfos)
        {
            for (int i = 0; i < clientInfos.Count; i++)
            {
                GameObject PlayerListObject = Instantiate(playerObject, playerHolders);
                PlayerListObject.GetComponentInChildren<TextMeshProUGUI>().text = clientInfos[i].username;

                int index = i;
                
                PlayerListObject.GetComponentInChildren<Toggle>().onValueChanged.AddListener(result =>
                {
                    clientInfos[index].wantsToInvite = result;
                });
            }
            
            sendInvitesButton.onClick.AddListener(() =>
            {
                _createTab.SendInvitedPlayers(clientInfos);
            });
        }

        // Confirm creation button
        public void CreateMatch()
        {
            if (string.IsNullOrEmpty(matchName.text))
            {
                NotificationManager.Singleton.CreateNotification(privateMatchNotFoundImage, "Name empty", "The match name you enter cannot be empty, please try again.", 2);
                MenuUIManager.instance.ShakeScreen();
                return;
            }
            
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.createMatch);
            message.AddUShort((ushort)maxPlayerCountSlider.value);
            message.AddUShort((ushort)minPlayerCountSlider.value);
            message.AddString(matchName.text);
            message.AddBool(privateMatch.isOn);
            MenuNetworkManager.Singleton.Client.Send(message);
        }
        
        // Match creation success
        private void ShowMatchCreationMessage(string code, bool isPrivate)
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
        
        public void CopyMatchCode()
        {
            GUIUtility.systemCopyBuffer = matchCodeText.text;
        }

        public void CopyMatchURL()
        {
            GUIUtility.systemCopyBuffer = privateCodeURLText.text;
        }

        // Continue from match info
        public void OpenInviteQuestionScreen()
        {
            inviteScreen.SetActive(false);
            invitePlayersQuestionPopup.SetActive(true);
        }
        
        // Confirm invite players question
        public void GetCurrentPlayers()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToGameServerId.getPlayers);
            MenuNetworkManager.Singleton.Client.Send(message);
        }

        // Invite players question confirm
        public void EnableInviteScreen(List<ClientInviteInfo> clientInfos)
        {
            invitePlayersQuestionPopup.SetActive(false);
            inviteScreen.SetActive(true);
            OpenInviteScreen(clientInfos);
        }
    }
}