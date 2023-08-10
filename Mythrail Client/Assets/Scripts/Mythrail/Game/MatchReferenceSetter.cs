using Mythrail.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mythrail.Game
{
    public class MatchReferenceSetter : MonoBehaviour
    {
        private void Awake()
        {
            UIManager uiManager = UIManager.Singleton;
            NetworkManager networkManager = NetworkManager.Singleton;

            uiManager.SetCode();
            
            uiManager.UIHealthBar = GameObject.Find("Health").GetComponentsInChildren<Image>()[1].transform;
            uiManager.loadingScreen = GameObject.Find("Loading");
            uiManager.KillsText = GameObject.Find("KillCounter").GetComponent<TextMeshProUGUI>();
            uiManager.DeathsText = GameObject.Find("DeathCounter").GetComponent<TextMeshProUGUI>();
            uiManager.HUDUsernameDisplay = GameObject.Find("UsernameText").GetComponent<TextMeshProUGUI>();
            uiManager.LoadingStatusDisplay = GameObject.Find("LoadingText").GetComponent<TextMeshProUGUI>();
            uiManager.GunName = GameObject.Find("GunName").GetComponent<TextMeshProUGUI>();
            uiManager.RespawningScreen = GameObject.Find("RespawnScreen");
            uiManager.PlayScreen = GameObject.Find("PlayScreen");
            uiManager.CountdownText = GameObject.Find("CountdownText").GetComponent<TextMeshProUGUI>();
            uiManager.RespawnButton = GameObject.Find("RespawnButton").GetComponent<Button>();
            uiManager.RespawnButton.onClick.AddListener(uiManager.Respawn);
            uiManager.RespawningScreen.SetActive(false);
            networkManager.LoadedBattle();
        }
    }   
}