using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ProfileData
{
    public string username;
    public GameObject currentGun;

    public ProfileData()
    {
        this.username = "";
    }

    public ProfileData(string username)
    {
        this.username = username;
    }
}

public class Launcher : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI text;
    public TMP_InputField usernameFeild;
    public TMP_InputField roomnameFeild;
    public Slider maxPlayerSlider;
    public TextMeshProUGUI maxPlayersValue;
    public static ProfileData myProfile = new ProfileData();

    public GameObject tabMain;
    public GameObject tabRooms;
    public GameObject tabCreate;

    public GameObject buttonRoom;

    private List<RoomInfo> roomList;

    public void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        usernameFeild.text = PlayerPrefs.GetString("PlayerUsername");

        Connect();
    }

    public void Connect()
    {
        PhotonNetwork.GameVersion = "0.0.5";
        PhotonNetwork.ConnectUsingSettings();
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(usernameFeild.text))
        {
            myProfile.username = "RANDOM_USER_" + Random.Range(100, 1000);
        }
        else
        {
            myProfile.username = usernameFeild.text;
        }
    }

    public override void OnJoinedRoom()
    {
        StartGame();

        base.OnJoinedRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);

        base.OnLeftRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Create();

        base.OnJoinRandomFailed(returnCode, message);
    }

    public override void OnConnectedToMaster()
    {
        text.text = "Connected!";
        PhotonNetwork.JoinLobby();
        base.OnConnectedToMaster();
    }

    public void Join()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public void Create()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = (byte)maxPlayerSlider.value;

        ExitGames.Client.Photon.Hashtable properties = new ExitGames.Client.Photon.Hashtable();
        properties.Add("map", 0);
        options.CustomRoomProperties = properties;

        PhotonNetwork.CreateRoom(roomnameFeild.text, options);
    }

    public void ChangeMap()
    {

    }

    public void ChangeMaxPlayerSlider(float value)
    {
        maxPlayersValue.text = Mathf.RoundToInt(value).ToString();
    }

    public void StartGame()
    {
        if (string.IsNullOrEmpty(usernameFeild.text))
        {
            myProfile.username = "RANDOM_USER_" + Random.Range(100, 1000);
        }
        else
        {
            myProfile.username = usernameFeild.text;
        }

        if(PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            PlayerPrefs.SetString("PlayerUsername", usernameFeild.text);
            PhotonNetwork.LoadLevel(1);
        }
    }

    public void TabCloseAll()
    {
        tabMain.SetActive(false);
        tabRooms.SetActive(false);
        tabCreate.SetActive(false);
    }

    public void TabOpenMain()
    {
        TabCloseAll();
        tabMain.SetActive(true);
    }

    public void TabOpenRooms()
    {
        TabCloseAll();
        tabRooms.SetActive(true);
    }

    public void TabOpenCreate()
    {
        TabCloseAll();
        tabCreate.SetActive(true);
    }

    private void ClearRoomList()
    {
        Transform content = tabRooms.transform.Find("ServerList/Viewport/Content");
        foreach (Transform a in content) Destroy(a.gameObject);
    }

    public override void OnRoomListUpdate(List<RoomInfo> list)
    {
        roomList = list;
        ClearRoomList();

        Transform content = tabRooms.transform.Find("ServerList/Viewport/Content");

        foreach(RoomInfo a in roomList)
        {
            GameObject newRoomButton = Instantiate(buttonRoom, content);

            newRoomButton.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = a.Name;
            newRoomButton.transform.Find("Players").GetComponent<TextMeshProUGUI>().text = a.PlayerCount + " / " + a.MaxPlayers;

            newRoomButton.GetComponent<Button>().onClick.AddListener(delegate { JoinRoom(newRoomButton.transform); });
        }

        base.OnRoomListUpdate(roomList);
    }

    private void JoinRoom(Transform button)
    {
        string roomName = button.transform.Find("Name").GetComponent<TextMeshProUGUI>().text;
        PhotonNetwork.JoinRoom(roomName);
    }
}
